using GoLive.Generator.ApiClientGenerator.Data;

namespace GoLive.Generator.ApiClientGenerator.Generation;

class ClientInterfaceGenerator(
    RouteGeneratorSettings config,
    SourceStringBuilder source,
    IActionGenerator actionGenerator) : ClientGenerator(config, source, actionGenerator)
{
    protected override void GeneratePrefix(ControllerRoute route) {
        // Do not generate a constructor
    }

    protected override void ClassDefinition(ControllerRoute route)
        => source.AppendLine($"public interface I{ClientClassName(route)}");
}