namespace GoLive.Generator.ApiClientGenerator.Data;

internal record ControllerRoute(string Name, string? Area, string BaseRoute, EquatableArray<ActionRoute> Actions);