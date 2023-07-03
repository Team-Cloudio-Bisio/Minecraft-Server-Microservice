using IdentityModel.OidcClient;
using Microsoft.AspNetCore.Mvc;
using MinecraftServerMicroservice.Model;

namespace MinecraftServerMicroservice.Controllers;

[ApiController]
[Route("[controller]")]
public class ServerManagerController : ControllerBase
{
    private readonly ServerManager _serverManager;

    public ServerManagerController()
    {
        _serverManager = new ServerManager();
    }

    [HttpPost("MinecraftServer", Name = "CreateServer")]
    public async Task<Server> CreateServer(Server server)
    {
        // Non funziona
        return await _serverManager.CreateServer(server);
    }

    [HttpGet("createServer", Name = "FullCreateServer")]
    public async Task<string> FullCreateServer(string server)
    {
        // check the server doesn't already exist(?)

        return await _serverManager.FullCreateServer(server);
    }

    [HttpDelete("MinecraftServer", Name = "DeleteServer")]
    public async Task<IActionResult> DeleteServer(string serverName)
    {
        IActionResult result;

        OperationInfo deleteInfo = await _serverManager.DeleteServer(serverName);

        if (deleteInfo.deployment == "deleted" && 
            deleteInfo.service == "deleted" && 
            deleteInfo.storage == "deleted")
            result = StatusCode(200, deleteInfo.serverName);
        else
            result = StatusCode(401, deleteInfo);

        return result;
    }

    [HttpGet("{serverName}/startServer", Name = "StartServer")]
    public async Task<IActionResult> StartServer(string serverName)
    {
        IActionResult result;

        OperationInfo startInfo = await _serverManager.StartServer(serverName);

        if (startInfo.deployment == "started")
            result = StatusCode(200, startInfo.serverName);
        else
            result = StatusCode(401, startInfo);

        return result;
    }

    [HttpGet("{serverName}/stopServer", Name = "StopServer")]
    public async Task<IActionResult> StopServer(string serverName)
    {
        IActionResult result;

        OperationInfo stopInfo = await _serverManager.StopServer(serverName);

        if (stopInfo.deployment == "stopped")
            result = StatusCode(200, stopInfo.serverName);
        else
            result = StatusCode(401, stopInfo);

        return result;
    }

    [HttpGet("{serverName}/setGamemode", Name = "SetGamemode")]
    public async Task<IActionResult> SetGamemode(string serverName, Gamemode gamemode)
    {
        IActionResult result;

        string output = await _serverManager.UpdateProperty(serverName, "gamemode", "creative");
        
        if (output.ToLower().Contains("not found"))
            result = StatusCode(404, $"Server named '{serverName}' not found.");
        else if (output.ToLower().Contains("invalid property"))
            result = StatusCode(401, "Couldn't change gamemode");
        else
            result = StatusCode(200, output);

        return result;
    }

    [HttpGet("{serverName}/setDifficulty", Name = "SetDifficulty")]
    public async Task<IActionResult> SetDifficulty(string serverName, Difficulty difficulty)
    {
        IActionResult result;

        var output = await _serverManager.SendCommandInteractive(serverName, $"/difficulty {difficulty}");
        if (!output.Contains("not found"))
            result = StatusCode(200, $"Difficulty set to {difficulty}");
        else
            result = StatusCode(401, "Couldn't change difficulty");

        return result;
    }

    [HttpGet("{serverName}/sendCommand", Name = "SendCommand")]
    public async Task<IActionResult> SendCommand(string serverName, string command)
    {
        IActionResult result;

        var output = await _serverManager.SendCommandInteractive(serverName, command);
        if (!output.Contains("not found")) 
            result = StatusCode(200, $"Command output: '{output}'");
        else
            result = StatusCode(401, $"Command failed. Error: '{output}'");

        return result;
    }

    [HttpGet("{serverName}/updateProperty", Name = "UpdateProperty")]
    public async Task<IActionResult> UpdateProperty(string serverName, string property, string command)
    {
        IActionResult result;

        var output = await _serverManager.UpdateProperty(serverName, property, command);
        if (!output.Contains("not found"))
            result = StatusCode(200, $"Command output: '{output}'");
        else
            result = StatusCode(401, $"Command failed. Error: '{output}'");

        return result;
    }
}
