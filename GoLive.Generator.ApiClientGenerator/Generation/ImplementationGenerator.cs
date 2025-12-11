using GoLive.Generator.ApiClientGenerator.Data;

namespace GoLive.Generator.ApiClientGenerator.Generation;

class ImplementationGenerator(
    RouteGeneratorSettings config,
    SourceStringBuilder source,
    IActionGenerator actionGenerator) : ClientGenerator(config, source, actionGenerator)
{
    protected override void ClassDefinition(ControllerRoute route) {
        source.AppendLine($"public class {ImplementationClassName(route)}{(config.GenerateInterface ? $" : I{ClientClassName(route)}" : null)}");
    }

    protected override void GeneratePrefix(ControllerRoute route) {
        source.AppendLine("private readonly ServerApiClientImplementation _implementation;");

        source.AppendLine();
        source.AppendLine($"public {ImplementationClassName(route)}(ServerApiClientImplementation implementation)");

        source.AppendOpenCurlyBracketLine();
        source.AppendLine("_implementation = implementation;");
        source.AppendCloseCurlyBracketLine();
    }

    public static string ImplementationClassName(ControllerRoute route)
        => $"{ClientClassName(route)}Implementation";
}