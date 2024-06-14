using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
using GoLive.Generator.ApiClientGenerator.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.ApiClientGenerator;

internal static class Scanner
{
    private static readonly SymbolDisplayFormat displayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
                              | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                              | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat nonGlobalDisplayFormat =
        new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public static bool CanBeController(SyntaxNode node)
        => node is ClassDeclarationSyntax c
           // Don't generate routes for abstract controllers
           && !c.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword))
           && c.BaseList?.Types.Count > 0;

    public static IEnumerable<ControllerRoute> ScanForControllers(SemanticModel semantic)
    {
        var allNodes = semantic.SyntaxTree.GetRoot().DescendantNodes();

        foreach (var node in allNodes) {
            if (CanBeController(node)
                && semantic.GetDeclaredSymbol(node) is INamedTypeSymbol classSymbol
                && IsController(classSymbol))
                yield return ConvertToRoute(classSymbol);
        }
    }

    public static ControllerRoute ConvertToRoute(INamedTypeSymbol classSymbol)
    {
        const string suffix = "Controller";
        var name = classSymbol.Name.EndsWith(suffix)
            ? classSymbol.Name.Substring(0, classSymbol.Name.Length - suffix.Length)
            : classSymbol.Name;

        var actionMethods = ScanForActionMethods(classSymbol).ToList();

        var parentClass = classSymbol.BaseType;

        if (parentClass != null && !parentClass.ToDisplayString(nonGlobalDisplayFormat).StartsWith("Microsoft.AspNetCore.Mvc", StringComparison.InvariantCultureIgnoreCase))
        {
            var addRoutes = ConvertToRoute(parentClass);

            if (addRoutes != null && addRoutes.Actions.Any())
            {
                actionMethods.AddRange(addRoutes.Actions);
            }
        }
            
        // Extract the route from the HttpActionAttribute
        var attribute = FindAttribute(classSymbol, a => a.ToString() == "Microsoft.AspNetCore.Mvc.RouteAttribute");
        var route = attribute?.ConstructorArguments.FirstOrDefault().Value?.ToString();

        var areaAttribute = FindAttribute(classSymbol, a => a.ToString() == "Microsoft.AspNetCore.Mvc.AreaAttribute");
        var area = areaAttribute?.ConstructorArguments.FirstOrDefault().Value?.ToString();
        return new ControllerRoute(name, area, route, new EquatableArray<ActionRoute>(actionMethods.ToArray()));
    }

    private static IEnumerable<ActionRoute> ScanForActionMethods(INamedTypeSymbol classSymbol)
    {
        foreach (var member in classSymbol.GetMembers())
        {
            if (member
                is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, IsAbstract: false } methodSymbol
                and not { MethodKind: MethodKind.Constructor }
               )
            {
                if (methodSymbol.Name.StartsWith("get_") || methodSymbol.Name.StartsWith("set_"))
                {
                    continue;
                }

                var allAttributes = methodSymbol.GetAttributes();

                if (allAttributes.FindAttribute(a => a.ToString() == ApiClientGenerator.ApiClientGeneratorIgnoreAttributeNamespace) is not null)
                    continue;

                var name = methodSymbol.Name;
                var returnType = methodSymbol.ReturnType;

                // Unwrap Task<T>
                returnType = UnwrapTaskTypes(returnType);

                // Take unwrapped T and check whether we need to 
                // unwrap further to V when T = ActionResult<V>
                if (returnType is INamedTypeSymbol actionResultType && actionResultType.OriginalDefinition.ToString() == "Microsoft.AspNetCore.Mvc.ActionResult<TValue>")
                {
                    returnType = actionResultType.TypeArguments.First();
                }

                if (returnType?.SpecialType == SpecialType.System_Void 
                    // If the return type is simple IActionResult -- assume that the return type is essentially void
                    || returnType?.OriginalDefinition.ToString() is "Microsoft.AspNetCore.Mvc.IActionResult"
                        or "Microsoft.AspNetCore.Mvc.ActionResult") {
                    returnType = null;
                }

                // Extract the route from the HttpActionAttribute
                var attribute = allAttributes.FindAttribute(a => a.BaseType?.ToString() == "Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute");
                var route = attribute?.ConstructorArguments.FirstOrDefault().Value?.ToString();
                var method = attribute?.AttributeClass?.Name switch
                {
                    "HttpGetAttribute" => HttpMethod.Get,
                    "HttpPutAttribute" => HttpMethod.Put,
                    "HttpPostAttribute" => HttpMethod.Post,
                    "HttpDeleteAttribute" => HttpMethod.Delete,
                    _ => HttpMethod.Get
                    //   _ => throw new InvalidOperationException($"Unknown attribute {attribute?.AttributeClass?.Name}")
                };

                var routeAttr = FindAttribute(methodSymbol, a => a.OriginalDefinition.ToString() == "Microsoft.AspNetCore.Mvc.RouteAttribute");

                if (routeAttr != null)
                {
                    route = routeAttr.ConstructorArguments.FirstOrDefault().Value?.ToString();
                }

                var customFormatterAttribute = FindAttribute(methodSymbol, a => a.Name == "FormFormatterAttribute");

                bool useCustomFormatter = customFormatterAttribute != null;

                var parameterAttributes = methodSymbol.Parameters
                    .Select(p => (p,
                        attrs: p.GetAttributes().Select(a => a.AttributeClass?.Name).Where(n => n is not null)))
                    .Where(t => 
                        t.p.Type.ToString() != "System.Threading.CancellationToken"
                        && t.attrs.All(a => a != "FromServicesAttribute"))
                    .ToImmutableArray();
                var parameters = parameterAttributes
                    .Select(t => t.p)
                    .Select(p => new ParameterMapping(p.Name,
                        new Parameter(p.Type.ToString(), p.HasExplicitDefaultValue,
                            p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null, p.Type.TypeKind == TypeKind.Enum)))
                    .ToArray();

                var bodyParameter = parameterAttributes
                    .Where(t
                        => t.attrs.All(a => a != "FromQueryAttribute" && a != "FromRouteAttribute")
                           && (!IsPrimitive(t.p.Type) || t.attrs.Any(a => a == "FromBodyAttribute")))
                    .Select(t => t.p)
                    .Select(p => new ParameterMapping(p.Name,
                        new Parameter(p.Type.ToString(), p.HasExplicitDefaultValue,
                            p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null, p.Type.TypeKind == TypeKind.Enum)))
                    .FirstOrDefault();
                    
                yield return new ActionRoute(name, method, route,
                    returnType?.ToDisplayString(displayFormat), returnType?.IsReferenceType != true,
                    useCustomFormatter, new EquatableArray<ParameterMapping>(parameters), bodyParameter);
            }
        }
    }

    public static bool IsController(INamedTypeSymbol classDeclaration)
        => InheritsFrom(classDeclaration, "Microsoft.AspNetCore.Mvc.ControllerBase")
           && FindAttribute(classDeclaration, attr => attr.ToString() == ApiClientGenerator.ApiClientGeneratorIgnoreAttributeNamespace) is null;

    private static bool InheritsFrom(INamedTypeSymbol classDeclaration, string qualifiedBaseTypeName) {
        var currentDeclared = classDeclaration;

        while (currentDeclared.BaseType != null)
        {
            var currentBaseType = currentDeclared.BaseType;
            if (string.Equals(currentBaseType.ToDisplayString(nonGlobalDisplayFormat), qualifiedBaseTypeName, StringComparison.Ordinal))
            {
                return true;
            }

            currentDeclared = currentBaseType;
        }

        return false;
    }

    private static bool IsPrimitive(ITypeSymbol typeSymbol) {
        while (true) {
            switch (typeSymbol.SpecialType) {
                case SpecialType.System_Boolean:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Char:
                case SpecialType.System_String:
                    return true;
                case SpecialType.System_Nullable_T when typeSymbol is INamedTypeSymbol { TypeArguments: [var underlyingType] }:
                    typeSymbol = underlyingType;
                    continue;
            }

            switch (typeSymbol.TypeKind) {
                case TypeKind.Enum:
                    return true;
            }

            if (typeSymbol is INamedTypeSymbol {
                    ConstructedFrom: var constructedFrom, TypeArguments: [var nullableUnderlyingType]
                } && constructedFrom.ToDisplayString(nonGlobalDisplayFormat) == "System.Nullable") {
                typeSymbol = nullableUnderlyingType;
                continue;
            }
                
            return false;
        }
    }

    private static ITypeSymbol UnwrapTaskTypes(ITypeSymbol type) {
        if (type is not INamedTypeSymbol namedType)
            return type;

        if (namedType.ToString() is "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask")
            return null;

        return namedType.OriginalDefinition.ToString() is 
            "System.Threading.Tasks.Task<TResult>"
            or "System.Threading.Tasks.ValueTask<TResult>" 
            ? namedType.TypeArguments.First() : type;
    }

    private static AttributeData? FindAttribute(INamedTypeSymbol symbol, Func<INamedTypeSymbol, bool> selectAttribute)
        => symbol.GetAttributesWithInherited().FindAttribute(selectAttribute);
        
    private static AttributeData? FindAttribute(ISymbol symbol, Func<INamedTypeSymbol, bool> selectAttribute)
        => symbol.GetAttributes().FindAttribute(selectAttribute);

    private static AttributeData? FindAttribute(this IEnumerable<AttributeData> attributes, Func<INamedTypeSymbol, bool> selectAttribute)
        => attributes.FirstOrDefault(a => a?.AttributeClass != null && selectAttribute(a.AttributeClass));

    private static IEnumerable<AttributeData> GetAllBaseTypeAttributes(this INamedTypeSymbol typeSymbol) {
        IEnumerable<AttributeData> attributes = typeSymbol.GetAttributes();
        while ((typeSymbol = typeSymbol.BaseType) is not null) {
            attributes = attributes.Concat(typeSymbol.GetAttributes());
        }

        return attributes;
    }
        
    public static IEnumerable<AttributeData> GetAttributesWithInherited(this INamedTypeSymbol typeSymbol) {
        ImmutableArray<AttributeData> attributes = typeSymbol.GetAttributes();
        return typeSymbol.BaseType is not null
            ? attributes.Concat(typeSymbol.BaseType.GetAllBaseTypeAttributes().Where(a => a.IsInherited()))
            : attributes;
    }

    private static bool IsInherited(this AttributeData attribute) {
        if (attribute.AttributeClass == null) {
            return false;
        }

        foreach (var attributeAttribute in attribute.AttributeClass.GetAttributes()) {
            var @class = attributeAttribute.AttributeClass;
            if (@class is { Name: nameof(AttributeUsageAttribute), ContainingNamespace.Name: "System" }) {
                foreach (KeyValuePair<string, TypedConstant> kvp in attributeAttribute.NamedArguments) {
                    if (kvp.Key == nameof(AttributeUsageAttribute.Inherited))
                        return (bool)kvp.Value.Value!;
                }

                // Default value of Inherited is true
                return true;
            }
        }

        return false;
    }
}