using GoLive.Generator.ApiClientGenerator.Data;

namespace GoLive.Generator.ApiClientGenerator.Generation;

class ClientGenerator(RouteGeneratorSettings config, SourceStringBuilder source, IActionGenerator actionGenerator) : IControllerGenerator
{
    protected readonly SourceStringBuilder source = source;
    protected readonly RouteGeneratorSettings config = config;

    public void Generate(ControllerRoute route) {
        source.AppendLine();

        ClassDefinition(route);
        source.AppendOpenCurlyBracketLine();

        GeneratePrefix(route);

        foreach (ActionRoute action in route.Actions) {
            actionGenerator.Generate(route, action);
        }

        source.AppendCloseCurlyBracketLine();
    }

    protected virtual void GeneratePrefix(ControllerRoute route) {
        source.AppendLine("private readonly HttpClient _client;");

        source.AppendLine();
        source.AppendLine($"public {ClientClassName(route)}(HttpClient client)");

        source.AppendOpenCurlyBracketLine();
        source.AppendLine("_client = client;");
        source.AppendCloseCurlyBracketLine();
    }

    protected virtual void ClassDefinition(ControllerRoute route)
        => source.AppendLine($"public class {ClientClassName(route)}{(config.GenerateInterface ? $" : I{ClientClassName(route)}" : null)}");

    public static string ClientClassName(ControllerRoute route)
        => $"{ClientPropertyName(route)}Client";

    public static string ClientPropertyName(ControllerRoute route)
        => !string.IsNullOrWhiteSpace(route.Area)
            ? $"{route.Area}_{route.Name}"
            : route.Name;
}