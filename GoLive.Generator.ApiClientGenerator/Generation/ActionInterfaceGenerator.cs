using GoLive.Generator.ApiClientGenerator.Data;

namespace GoLive.Generator.ApiClientGenerator.Generation;

class ActionInterfaceGenerator(RouteGeneratorSettings config, SourceStringBuilder source) : ActionGenerator(config)
{
    public override void Generate(ControllerRoute controllerRoute, ActionRoute action) {
        source.AppendLine();

        source.AppendLine($"{GetActionReturnType(action)} {action.Name}({GetActionParameters(action)});");
    }
}