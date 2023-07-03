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

        var output = await _serverManager.SendCommand(serverName, $"/difficulty {difficulty}");
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

        var output = await _serverManager.SendCommand(serverName, command);
        if (!output.Contains("not found")) 
            result = StatusCode(200, $"Command output: '{output}'");
        else
            result = StatusCode(401, $"Command failed. Error: '{output}'");

        return result;
    }





    // Old version

    // Get async request (the creation requires time)
    /*[HttpGet("", Name = "CreateServer")]
    public async Task<string> CreateServer(
        string serverName,
        string minecraftVersion = "",
        string serverOperators = "",
        string serverWorldURL = "",
        bool serverEnableStatus = true,
        string serverMOTD = "Server Powered by Azure & Kubernetes",
        string serverDifficulty = "easy",
        string serverGameMode = "survival",
        int serverMaxPlayers = 20,
        bool serverOnlineMode = true,
        int serverPlayerIdleTimeout = 0,
        bool serverEnableWhitelist = false,
        string serverWhitelist = "")
    {
        var res = await _serverManager.FullCreateServer(
            serverName: serverName,
            minecraftVersion: minecraftVersion,
            serverOperators: serverOperators,
            serverWorldURL: serverWorldURL,
            serverEnableStatus: serverEnableStatus,
            serverMOTD: serverMOTD,
            serverDifficulty: serverDifficulty,
            serverGameMode: serverGameMode,
            serverMaxPlayers: serverMaxPlayers,
            serverOnlineMode: serverOnlineMode,
            serverPlayerIdleTimeout: serverPlayerIdleTimeout,
            serverEnableWhitelist: serverEnableWhitelist,
            serverWhitelist: serverWhitelist
        );

        return "OK: " + res;
    }

    [HttpDelete("{serverName}/deleteServer", Name = "DeleteServer")]
    public async Task<string> DeleteServer(string serverName)
    {
        return await _serverManager.DeleteServer(serverName); ;
    }

    [HttpGet("{serverName}/startServer", Name = "StartServer")]
    public async Task<string> StartServer(string serverName)
    {
        return await _serverManager.StartServer(serverName);
    }

    [HttpGet("{serverName}/stopServer", Name = "StopServer")]
    public async Task<string> StopServer(string serverName)
    {
        return await _serverManager.StopServer(serverName);
    }

    [HttpPost("{serverName}/setGamemode", Name = "SetGamemode")]
    public string SetGamemode(string serverName, string command)
    {
        // TO-DO

        return _serverManager.SendCommand(serverName, command);
    }

    [HttpPost("{serverName}/setDifficulty", Name = "SetDifficulty")]
    public string SetDifficulty(string serverName, string command)
    {
        // TO-DO

        return _serverManager.SendCommand(serverName, command);
    }

    [HttpPost("{serverName}/sendCommand", Name = "SendCommand")]
    public string SendCommand(string serverName, string command)
    {
        return _serverManager.SendCommand(serverName, command);
    }*/
}
