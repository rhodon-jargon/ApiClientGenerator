// ReSharper disable All
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using GoLive.Generator.ApiClientGenerator;

namespace GoLive.Generator.ApiClientGenerator.Tests.WebApi.Generated
{

    public class ApiClient
    {
        public ApiClient(HttpClient client)
        {
            InheritingUser2 = new InheritingUser2Client(client);
            User = new UserClient(client);
            WeatherForecast = new WeatherForecastClient(client);
        }

        public InheritingUser2Client InheritingUser2 { get; }

        public UserClient User { get; }

        public WeatherForecastClient WeatherForecast { get; }
    }
    public class Result
    {
        public Result() {}
        public Result(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }
        public HttpStatusCode StatusCode { get; }
        public bool Success => ((int)StatusCode >= 200) && ((int)StatusCode <= 299);
        public static async Task<Result> FromResponseTask(Task<HttpResponseMessage> responseTask) {
            using HttpResponseMessage message = await responseTask;
            return new(message.StatusCode);
        }
    }
    public class Result<T> : Result
    {
        public Result() {}
        public Result(HttpStatusCode statusCode, T? data) : base(statusCode)
        {
            Data = data;
        }
        public T? Data { get; }
        
        public T SuccessData => Success ? Data ?? throw new NullReferenceException("Response had an empty body!")
                                        : throw new InvalidOperationException("Request was not successful!");
        public bool TryGetSuccessData([NotNullWhen(true)] out T? data)
        {
            data = Data;
            return Success && data is not null;
        }
        public static async Task<Result<T>> FromResponseTask(
            Task<HttpResponseMessage> responseTask, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default) {
            using HttpResponseMessage message = await responseTask;
            return new(message.StatusCode, 
                await (message.Content?.ReadFromJsonAsync<T>(options, cancellationToken: cancellationToken)
                ?? Task.FromResult<T?>(default)));
        }
    }

    public class InheritingUser2Client
    {
        private readonly HttpClient _client;

        public InheritingUser2Client (HttpClient client)
        {
            _client = client;
        }

        public async Task<Result<global::System.Collections.Generic.IEnumerable<string>>> Get(CancellationToken _token = default)
        {
            return await Result<global::System.Collections.Generic.IEnumerable<string>>.FromResponseTask(_client.GetAsync($"/apiInheritingUser2", cancellationToken: _token), cancellationToken: _token);
        }

        public async Task<Result<string?>> GetUser(int userId , CancellationToken _token = default)
        {
            return await Result<string?>.FromResponseTask(_client.GetAsync($"/apiInheritingUser2/{userId}", cancellationToken: _token), cancellationToken: _token);
        }

        public async Task<Result> Log(int? userId , CancellationToken _token = default)
        {
            return await Result.FromResponseTask(_client.GetAsync($"/apiInheritingUser2/{userId}", cancellationToken: _token));
        }

        public async Task<Result<int>> GetUser(string user , CancellationToken _token = default)
        {
            return await Result<int>.FromResponseTask(_client.PostAsJsonAsync($"/apiInheritingUser2", user, cancellationToken: _token), cancellationToken: _token);
        }
    }

    public class UserClient
    {
        private readonly HttpClient _client;

        public UserClient (HttpClient client)
        {
            _client = client;
        }

        public async Task<Result<global::System.Collections.Generic.IEnumerable<string>>> Get(CancellationToken _token = default)
        {
            return await Result<global::System.Collections.Generic.IEnumerable<string>>.FromResponseTask(_client.GetAsync($"/api/User", cancellationToken: _token), cancellationToken: _token);
        }

        public async Task<Result<string?>> GetUser(int userId , CancellationToken _token = default)
        {
            return await Result<string?>.FromResponseTask(_client.GetAsync($"/api/User/{userId}", cancellationToken: _token), cancellationToken: _token);
        }

        public async Task<Result> Log(int? userId , CancellationToken _token = default)
        {
            return await Result.FromResponseTask(_client.GetAsync($"/api/User/{userId}", cancellationToken: _token));
        }

        public async Task<Result<int>> GetUser(string user , CancellationToken _token = default)
        {
            return await Result<int>.FromResponseTask(_client.PostAsJsonAsync($"/api/User", user, cancellationToken: _token), cancellationToken: _token);
        }
    }

    public class WeatherForecastClient
    {
        private readonly HttpClient _client;

        public WeatherForecastClient (HttpClient client)
        {
            _client = client;
        }

        public async Task<Result<global::System.Collections.Generic.IEnumerable<global::GoLive.Generator.ApiClientGenerator.Tests.WebApi.WeatherForecast>>> Get(CancellationToken _token = default)
        {
            return await Result<global::System.Collections.Generic.IEnumerable<global::GoLive.Generator.ApiClientGenerator.Tests.WebApi.WeatherForecast>>.FromResponseTask(_client.GetAsync($"/apiWeatherForecast", cancellationToken: _token), cancellationToken: _token);
        }

        public async Task<Result> SecretUrl(CancellationToken _token = default)
        {
            return await Result.FromResponseTask(_client.GetAsync($"/apiWeatherForecast/_secretUrl", cancellationToken: _token));
        }

        public async Task<Result<byte[]>> GetBytes(CancellationToken _token = default)
        {
            using var result = await _client.GetAsync($"/apiWeatherForecast", cancellationToken: _token);
            return new Result<byte[]>(
                result.StatusCode,
                await (result.Content?.ReadAsByteArrayAsync() 
                        ?? Task.FromResult<byte[]?>(default)));
        }

        public async Task<Result<global::GoLive.Generator.ApiClientGenerator.Tests.WebApi.WeatherForecast>> GetSingle(int Id , CancellationToken _token = default)
        {
            Dictionary<string, string> queryString=new();
            queryString.Add("Id", Id.ToString());
            return await Result<global::GoLive.Generator.ApiClientGenerator.Tests.WebApi.WeatherForecast>.FromResponseTask(_client.GetAsync(Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString($"/apiWeatherForecast", queryString), cancellationToken: _token), cancellationToken: _token);
        }
    }
}
// ReSharper disable All
