using GoLive.Generator.ApiClientGenerator.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace GoLive.Generator.ApiClientGenerator.Generation;

class HttpActionGenerator(RouteGeneratorSettings config, SourceStringBuilder source) : ActionGenerator(config)
{
    public override void Generate(ControllerRoute controllerRoute, ActionRoute action) {
        source.AppendLine();

        source.AppendLine($"public async {GetActionReturnType(action)} {action.Name}({GetActionParameters(action)})");

        source.AppendOpenCurlyBracketLine();

        bool byteReturnType = action.ReturnTypeName == "byte[]";

        bool containsFileUpload =
            action.Mapping.Any(f => f.Parameter.FullTypeName == FormFile);

        string routeValue = action.Route switch {
            { Length: > 0 } route when route[0] is '/' or '~' => route,
            { } route                                         => $"{controllerRoute.BaseRoute?.TrimEnd('/')}/{route}",
            null when controllerRoute.BaseRoute is not null   => controllerRoute.BaseRoute,
            null when controllerRoute.Area is null            => "/[area]/[controller]/[action]",
            _                                                 => "/[controller]/[action]"
        };
        routeValue = ReplaceRouteParams(routeValue, action, controllerRoute);

        routeValue = routeValue.TrimStart('~');
        routeValue = routeValue.Replace("*", ""); // TODO - to remove greedy url params

        if (!string.IsNullOrWhiteSpace(config.PrefixUrl)) {
            routeValue = $"{config.PrefixUrl}{routeValue}";
        }

        var routeString = $"$\"{routeValue}\"";

        if (config.HideUrlsRegex is { Count: > 0 }) {
            if (config.HideUrlsRegex.Any(e => Regex.IsMatch(routeValue, e))) {
                return;
            }
        }

        

        var queryStringParams = action.Mapping
                                      .Where(m => !routeValue.Contains($"{{{m.Key}}}") && action.Body?.Key != m.Key)
                                      .ToList();
        if (queryStringParams.Count > 0) {
            source.AppendLine($"{config.QueryStringDictionary ?? "Dictionary<string, string>"} queryString=new();");
            var format = config.QueryStringFormat ?? (config.QueryStringDictionary is null
                ? ".ToString()"
                : null);

            foreach (ParameterMapping parameterMapping in queryStringParams) {
                if (parameterMapping.Parameter.HasDefaultValue) {
                    source.AppendLine(
                        $"if ({parameterMapping.Key} != {GetDefaultValue(parameterMapping.Parameter)})");

                    source.AppendOpenCurlyBracketLine();
                }

                var parameterFormatter = parameterMapping.Parameter.IsEnum ? ".ToString()" : format;
                source.AppendLine(
                    $"queryString.Add(\"{parameterMapping.Key}\", {parameterMapping.Key}{parameterFormatter});");

                if (parameterMapping.Parameter.HasDefaultValue)
                    source.AppendCloseCurlyBracketLine();
            }

            string queryStringBuilder = config.QueryStringBuilder
                                     ?? "Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString";
            routeString = $"{queryStringBuilder}({routeString}, queryString)";
        }


        var methodString = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(action.Method.Method.ToLower());

        //if (methodString == "Post" && action.Body == null)
        //{
        //    action.Body = new ParameterMapping()
        //}

        var formatterArg = string.IsNullOrEmpty(config.CustomDiscriminator)
            ? null
            : $"{config.CustomDiscriminator}, ";

        string callStatement;

        if (containsFileUpload) {
            callStatement =
                $"_client.{methodString}Async({routeString}, multiPartContent, {formatterArg}cancellationToken: {CancellationTokenParameterName})";
        }
        else if (action.Body is { Key: var key }) {
            callStatement =
                $"_client.{methodString}AsJsonAsync({routeString}, {key}, {formatterArg}cancellationToken: {CancellationTokenParameterName})";
        }
        else if (methodString == "Post") {
            callStatement = $"_client.{methodString}AsJsonAsync({routeString}, new {{}}, cancellationToken: {CancellationTokenParameterName})";
        }
        else {
            callStatement = $"_client.{methodString}Async({routeString}, cancellationToken: {CancellationTokenParameterName})";
        }

        if (action.ReturnTypeName == null) {
            source.AppendLine(config.UseResponseWrapper
                ? $"return await {config.ResponseWrapperType}.FromResponseTask({callStatement});"
                : $"await {callStatement}");
        }
        else {
            if (byteReturnType || !config.UseResponseWrapper)
                source.AppendLine($"using var result = await {callStatement};");

            string readValue;
            if (byteReturnType) {
                readValue = "result.Content?.ReadAsByteArrayAsync()";
                if (config.UseResponseWrapper)
                    source.AppendMultipleLines($"""
                                                return new {config.ResponseWrapperType}<{action.ReturnTypeName}>(
                                                    result.StatusCode,
                                                    await ({readValue} 
                                                            ?? Task.FromResult<{GetNullableReturnType(action)}>(default)));
                                                """);
            }
            else if (config.UseResponseWrapper) {
                readValue =
                    $"{config.ResponseWrapperType}<{action.ReturnTypeName}>.FromResponseTask({callStatement}, {formatterArg}cancellationToken: {CancellationTokenParameterName})";
            }
            else {
                readValue =
                    $"result.Content?.ReadFromJsonAsync<{action.ReturnTypeName}>({formatterArg}cancellationToken: {CancellationTokenParameterName})";
            }

            if (!byteReturnType || !config.UseResponseWrapper)
                source.AppendLine($"return await {readValue};");
        }

        source.AppendCloseCurlyBracketLine();
    }
    
    private static string ReplaceRouteParams(string routeValue, ActionRoute action, ControllerRoute controller)
        => Regex.Replace(
            routeValue.Replace("[area]",       controller.Area)
                      .Replace("[controller]", controller.Name)
                      .Replace("[action]",     action.Name),
            @"{(?<parameter>[^}:]+)(:[^}]+)}", "{${parameter}}");
}