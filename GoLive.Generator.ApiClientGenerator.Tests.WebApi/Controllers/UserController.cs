using Microsoft.AspNetCore.Mvc;

namespace GoLive.Generator.ApiClientGenerator.Tests.WebApi.Controllers;

[Route("/[controller]")]
public abstract class ApiController : ControllerBase
{
    
}

[ApiController]
public class UserController : ApiController
{
    private readonly List<string> users = new() {
        "Tom", "Frank", "Nelly", "Tobias"
    };

    [HttpGet(Name = "GetUsers")]
    public IEnumerable<string> Get() => users;

    [HttpGet("{userId:int}")]
    public string? GetUser(int userId) => userId >= 0 && userId < users.Count ? users[userId] : null;

    [HttpGet("{userId:int}")]
    public Task Log(int userId, [FromServices] ILogger<UserController> logger) =>
        Task.CompletedTask; 

    [HttpPost]
    public int GetUser([FromBody] string user) {
        int id = users.Count;
        users.Add(user);
        return id;
    }
}