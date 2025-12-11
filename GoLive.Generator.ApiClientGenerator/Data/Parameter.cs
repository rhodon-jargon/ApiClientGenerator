namespace GoLive.Generator.ApiClientGenerator.Data;

public record Parameter(string FullTypeName, bool HasDefaultValue, object? DefaultValue, bool IsEnum);

public record ServerParameter(string FullTypeName, string Name, bool IsService);