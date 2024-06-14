namespace GoLive.Generator.ApiClientGenerator.Data
{
    public record Parameter(string FullTypeName, bool HasDefaultValue, object? DefaultValue, bool IsEnum);
}