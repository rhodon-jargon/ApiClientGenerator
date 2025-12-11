using System.Net;
using GoLive.Generator.ApiClientGenerator.Tests.WebApi.Generated;
using Microsoft.AspNetCore.Mvc;

namespace GoLive.Generator.ApiClientGenerator.Tests.WebApi;

public partial class ServerApiClientImplementation
{
    internal partial bool CanHandleException(Exception exception)
        => true;

    internal partial Result HandleException(Exception exception)
        => new Result(HttpStatusCode.InternalServerError);

    internal partial Result<T> HandleException<T>(Exception exception)
        => new(HttpStatusCode.InternalServerError, default);

    internal partial Result CreateWrapper()
        => new Result(HttpStatusCode.OK);

    internal partial Result<T> WrapResult<T>(T result)
        => new Result<T>(HttpStatusCode.OK, result);
    
    internal Result<T> WrapResult<T>(ActionResult<T> result)
        => new Result<T>(result.Value switch {
            ProblemDetails {Status: { } status} => (HttpStatusCode) status,
            not null => HttpStatusCode.OK,
            _ => HttpStatusCode.InternalServerError
        }, result.Value);

    internal partial ValueTask<int> TransformAsync(int value)
        => ValueTask.FromResult(value);

}