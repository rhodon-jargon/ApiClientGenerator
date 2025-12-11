using System.Linq;
using GoLive.Generator.ApiClientGenerator.Data;

namespace GoLive.Generator.ApiClientGenerator.Generation;

class ServerImplementationActionGenerator(RouteGeneratorSettings config, SourceStringBuilder source) : ActionGenerator(config) {
    public override void Generate(ControllerRoute controllerRoute, ActionRoute action) {
        source.AppendLine();

        source.AppendLine($"public async {GetActionReturnType(action)} {action.Name}({GetActionParameters(action)})");
        source.AppendOpenCurlyBracketLine();

        source.AppendLine("await using var _scope = _implementation.GetScope();");
        source.AppendLine("var _services = _scope.ServiceProvider;");
        source.AppendLine($"var controller = _implementation.CreateController<{controllerRoute.QualifiedName}>(_services);");

        if (config.ServerImplementationTransformParameterTypes is { } types) {
            foreach (ParameterMapping parameterMapping in action.Mapping) {
                if (types.Contains(parameterMapping.Parameter.FullTypeName))
                    source.AppendLine($"{parameterMapping.Key} = await _implementation.TransformAsync({parameterMapping.Key});");
            }
        }

        source.AppendLine();
        source.AppendLine("try");
        source.AppendOpenCurlyBracketLine();

        var arguments = string.Join(", ", action.ServerParameters
                                                .Select(p => p.FullTypeName switch {
                                                     Scanner.CancellationTokenType => CancellationTokenParameterName,
                                                     _ when p.IsService => $"_services.GetRequiredService<{p.FullTypeName}>()",
                                                     _ => p.Name
                                                 }));

        var callAction = $"{(action.IsAsync ? "await " : null)}controller.{action.Name}({arguments})";

        source.AppendLine(
            (action.ReturnTypeName, config.UseResponseWrapper) switch {
                ({ } typeName, true) => $"return _implementation.WrapResult<{typeName}>({callAction});",
                (not null, false)    => $"return {callAction};",
                _                    => $"{callAction};"
            });
        if (action.ReturnTypeName is null && config.UseResponseWrapper)
            source.AppendLine("return _implementation.CreateWrapper();");

        source.AppendCloseCurlyBracketLine();
        source.AppendLine("catch (Exception e) when (_implementation.CanHandleException(e))");
        source.AppendOpenCurlyBracketLine();
        
        bool hasReturn = config.UseResponseWrapper || action.ReturnTypeName is not null;
        source.AppendLine(
            $"{(hasReturn ? "return " : null)}_implementation.HandleException{(action.ReturnTypeName is { } n ? $"<{n}>" : null)}(e);");

        source.AppendCloseCurlyBracketLine();

        source.AppendCloseCurlyBracketLine();
    }
}