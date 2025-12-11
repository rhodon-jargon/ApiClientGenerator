using System.Net.Http;

namespace GoLive.Generator.ApiClientGenerator.Data;

internal record ActionRoute(string Name, HttpMethod Method, string? Route, string? ReturnTypeName, bool ReturnTypeStruct, bool IsAsync,
    bool HasCustomFormatter, EquatableArray<ParameterMapping> Mapping, ParameterMapping? Body, EquatableArray<ServerParameter> ServerParameters);