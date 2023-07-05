using IdentityModel.OidcClient;
using Microsoft.AspNetCore.Mvc;
using MinecraftServerMicroservice.Model;
using MinecraftServerMicroservice.Utils;

namespace MinecraftServerMicroservice.Controllers;

[ApiController]
[Route("[controller]")]
public class ServerManagerController : ControllerBase
{
    private readonly ServerManager _serverManager;
    private readonly DBUtils _dbUtils;

    public ServerManagerController()
    {
        _serverManager = new ServerManager();
        _dbUtils = new DBUtils();
    }

    [HttpPost("MinecraftServer", Name = "CreateServer")]
    public async Task<Server> CreateServer(Server server)
    {
        Server createdServer = await _serverManager.CreateServer(server);
        
        // TEST: Push to DB
        var res = _dbUtils.AddServer(createdServer);
        System.Diagnostics.Debug.WriteLine($"Server add to DB: {res}");

        return createdServer;
    }

    [HttpDelete("MinecraftServer", Name = "DeleteServer")]
    public async Task<IActionResult> DeleteServer(string serverName)
    {
        IActionResult result;

        OperationInfo deleteInfo = await _serverManager.DeleteServer(serverName);

        if (deleteInfo.deployment == "deleted" && 
            deleteInfo.service == "deleted" && 
            deleteInfo.storage == "deleted")
            result = StatusCode(200, new Message { message = "OK", description = deleteInfo });
        else
            result = StatusCode(401, new Message { message = "NO", description = deleteInfo });

        // TEST: Delete from DB
        var res = _dbUtils.RemoveServer(serverName);
        System.Diagnostics.Debug.WriteLine($"Server delete from DB: {res}");

        return result;
    }

    [HttpGet("{serverName}/startServer", Name = "StartServer")]
    public async Task<IActionResult> StartServer(string serverName)
    {
        IActionResult result;

        OperationInfo startInfo = await _serverManager.StartServer(serverName);

        if (startInfo.deployment == "started")
            result = StatusCode(200, new Message { message = "OK", description = startInfo });
        else
            result = StatusCode(401, new Message { message = "NO", description = startInfo });

        return result;
    }

    [HttpGet("{serverName}/stopServer", Name = "StopServer")]
    public async Task<IActionResult> StopServer(string serverName)
    {
        IActionResult result;

        OperationInfo stopInfo = await _serverManager.StopServer(serverName);

        if (stopInfo.deployment == "stopped")
            result = StatusCode(200, new Message { message = "OK", description = stopInfo });
        else
            result = StatusCode(401, new Message { message = "NO", description = stopInfo });

        return result;
    }

    [HttpGet("{serverName}/setGamemode", Name = "SetGamemode")]
    public async Task<IActionResult> SetGamemode(string serverName, Gamemode gamemode)
    {
        IActionResult result;

        // Temp: that changes the gamemode of connected players
        string output = await _serverManager.SendCommandInteractive(serverName, $"/gamemode {gamemode} @a");

        if (output.Contains("not found", StringComparison.OrdinalIgnoreCase))
            result = StatusCode(404, new Message { message = "NO", description = $"Server named '{serverName}' not found." });
        else if (output.StartsWith("Unknown or incomplete command", StringComparison.OrdinalIgnoreCase))
            result = StatusCode(401, new Message { message = "NO", description = $"Gamemode of server '{serverName}' couldn't be changed to '{Enum.GetName(typeof(Gamemode), gamemode)}'. Error: {output}." });
        else
            result = StatusCode(200, new Message { message = "OK", description = $"Gamemode of server '{serverName}' changed to '{Enum.GetName(typeof(Gamemode), gamemode)}'." });

        /*
        string output = await _serverManager.UpdateServerProperty(serverName, "gamemode", Enum.GetName(typeof(Gamemode), gamemode));
        
        if (output.Contains("not found", StringComparison.OrdinalIgnoreCase))
            result = StatusCode(404, new Message { message = "NO", description = $"Server named '{serverName}' not found." } );
        else if (output.Contains("invalid property", StringComparison.OrdinalIgnoreCase))
            result = StatusCode(401, new Message { message = "NO", description = $"Gamemode of server '{serverName}' couldn't be changed to '{Enum.GetName(typeof(Gamemode), gamemode)}'. Error: {output}"});
        else
            result = StatusCode(200, new Message { message = "OK", description = $"Gamemode of server '{serverName}' changed to '{Enum.GetName(typeof(Gamemode), gamemode)}'." });
        */

        return result;
    }

    [HttpGet("{serverName}/setDifficulty", Name = "SetDifficulty")]
    public async Task<IActionResult> SetDifficulty(string serverName, Difficulty difficulty)
    {
        IActionResult result;

        var output = await _serverManager.SendCommandInteractive(serverName, $"/difficulty {difficulty}");
        
        if (output.Contains("not found", StringComparison.OrdinalIgnoreCase))
            result = StatusCode(404, new Message { message = "NO", description = $"Server named '{serverName}' not found." });
        else if (output.StartsWith("Unknown or incomplete command", StringComparison.OrdinalIgnoreCase))
            result = StatusCode(401, new Message { message = "NO", description = $"Difficulty of server '{serverName}' couldn't be changed to '{Enum.GetName(typeof(Difficulty), difficulty)}'. Error: {output}." });
        else
            result = StatusCode(200, new Message { message = "OK", description = $"Difficulty of server '{serverName}' changed to '{Enum.GetName(typeof(Difficulty), difficulty)}'." });

        return result;
    }

    // EXTRA ====================================================

    // test
    [HttpGet("getServer", Name = "GetServer")]
    public async Task<List<ServerInfo>> GetServer()
    {
        return await _serverManager.GetAllServerInformation();
    }

    [HttpGet("createServer", Name = "FullCreateServer")]
    public async Task<string> FullCreateServer(
        string serverName,
        string serverDifficulty = "easy",
        string serverGameMode = "survival",
        bool serverEnableCommandBlocks = false,
        bool serverEnableStatus = true,
        bool serverEnableWhitelist = false,
        string serverMOTD = "MC Server Powered by Azure & Kubernetes",
        bool serverOnlineMode = true,
        string serverOperators = "",
        int serverMaxPlayers = 20,
        int serverPlayerIdleTimeout = 0,
        string minecraftVersion = "",
        string serverWhitelist = "",
        string serverWorldURL = "")
    {
        // check the server doesn't already exist(?)

        return await _serverManager.FullCreateServer(
            serverName: serverName,
            serverDifficulty: serverDifficulty,
            serverGameMode: serverGameMode,
            serverEnableCommandBlocks: serverEnableCommandBlocks,
            serverEnableStatus: serverEnableStatus,
            serverEnableWhitelist: serverEnableWhitelist,
            serverMOTD: serverMOTD,
            serverOnlineMode: serverOnlineMode,
            serverOperators: serverOperators,
            serverMaxPlayers: serverMaxPlayers,
            serverPlayerIdleTimeout: serverPlayerIdleTimeout,
            minecraftVersion: minecraftVersion,
            serverWhitelist: serverWhitelist,
            serverWorldURL: serverWorldURL);
    }

    [HttpGet("{serverName}/sendCommand", Name = "SendCommand")]
    public async Task<IActionResult> SendCommand(string serverName, string command)
    {
        IActionResult result;

        var output = await _serverManager.SendCommandInteractive(serverName, command);
        if (output.Contains("not found", StringComparison.OrdinalIgnoreCase))
            result = StatusCode(200, new Message { message = "NO", description = $"Server named '{serverName}' not found." });
        else
            result = StatusCode(401, new Message { message = "OK", description = $"Command '{command}' to '{serverName}' failed. Output: {output}." });

        return result;
    }

    [HttpGet("{serverName}/updateServerProperty", Name = "UpdateServerProperty")]
    public async Task<IActionResult> UpdateServerProperty(string serverName, string property, string newValue)
    {
        IActionResult result;

        var output = await _serverManager.UpdateServerProperty(serverName, property, newValue);
        if (output.Contains("not found", StringComparison.OrdinalIgnoreCase))
            result = StatusCode(404, new Message { message = "NO", description = $"Server named '{serverName}' not found." });
        else if (output.Contains("invalid property", StringComparison.OrdinalIgnoreCase))
            result = StatusCode(401, new Message { message = "NO", description = $"Property '{property}' is invalid." });
        else
            result = StatusCode(200, new Message { message = "OK", description = $"Property '{property}' of server '{serverName}' changed to '{newValue}'." });


        return result;
    }

    [HttpGet("{serverName}/getWhitelist", Name = "GetWhitelist")]
    public async Task<IActionResult> GetWhitelist(string serverName)
    {
        IActionResult result;

        var output = await _serverManager.GetWhitelist(serverName);
        if (!output.Contains("not found", StringComparison.OrdinalIgnoreCase))
            result = StatusCode(200, $"{output}");
        else
            result = StatusCode(401, $"Command failed. Error: '{output}'");

        return result;
    }
}
