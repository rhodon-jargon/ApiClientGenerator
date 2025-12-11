using GoLive.Generator.ApiClientGenerator.Data;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace GoLive.Generator.ApiClientGenerator.Generation;

interface IActionGenerator
{
    public void Generate(ControllerRoute controllerRoute, ActionRoute action);
}

abstract class ActionGenerator(RouteGeneratorSettings config) : IActionGenerator
{
    protected const string FormFile = "Microsoft.AspNetCore.Http.IFormFile";
    protected const string CancellationTokenParameterName = "_token";
    
    protected readonly RouteGeneratorSettings config = config;

    protected string GetActionParameters(ActionRoute action)
        => string.Join(", ", action.Mapping.Select(m =>
                                        m.Parameter.FullTypeName == FormFile
                                            ? "System.Net.Http.MultipartFormDataContent multiPartContent"
                                            : $"{m.Parameter.FullTypeName} {m.Key} {GetDefaultValueSetter(m.Parameter)}")
                                   .Append($"CancellationToken {CancellationTokenParameterName} = default"));

    protected string GetActionReturnType(ActionRoute action)
        => config.UseResponseWrapper switch {
            true when action.ReturnTypeName is null  => $"Task<{config.ResponseWrapperType}>",
            true                                     => $"Task<{config.ResponseWrapperType}<{action.ReturnTypeName}>>",
            false when action.ReturnTypeName is null => "Task",
            false                                    => $"Task<{GetNullableReturnType(action)}>"
        };

    protected string? GetNullableReturnType(ActionRoute action)
        => action.ReturnTypeStruct || action.ReturnTypeName?.EndsWith("?") == true
            ? action.ReturnTypeName
            : $"{action.ReturnTypeName}?";

    private static string GetDefaultValueSetter(Parameter argParameter)
        => argParameter.HasDefaultValue ? " = " + GetDefaultValue(argParameter) : string.Empty;

    protected static string GetDefaultValue(Parameter argParameter)
        => argParameter.DefaultValue switch
        {
            null     => "default",
            bool b   => b.ToString().ToLower(),
            string e => SymbolDisplay.FormatLiteral(e, true),
            _        => argParameter.DefaultValue.ToString()
        };

    public abstract void Generate(ControllerRoute controllerRoute, ActionRoute action);
}