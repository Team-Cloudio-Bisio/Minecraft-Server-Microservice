using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using MinecraftServerMicroservice.Model;

namespace MinecraftServerMicroservice.Controllers;

[ApiController]
[Route("[controller]")]
public class ServerManagerController : ControllerBase
{
    private ServerManager _serverManager;

    public ServerManagerController()
    {
        _serverManager = new ServerManager();
    }

    // Get async request (the creation requires time)
    [HttpGet("createServer", Name = "CreateServer")]
    public async Task<string> CreateServer(
        string serverName = "",
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
        var res = await _serverManager.CreateServer(
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

    [HttpGet("deleteServer", Name = "DeleteServer")]
    public async Task<string> DeleteServer(string serverName)
    {
        return await _serverManager.DeleteServer(serverName); ;
    }

    [HttpGet("startServer", Name = "StartServer")]
    public async Task<string> StartServer(string serverName)
    {
        return await _serverManager.StartServer(serverName);
    }

    [HttpGet("stopServer", Name = "StopServer")]
    public async Task<string> StopServer(string serverName)
    {
        return await _serverManager.StopServer(serverName);
    }

    /*[HttpGet("pingServer", Name = "PingServer")]
    public bool PingServer()
    {
        _serverManager.PingServer("");

        return Random.Shared.NextDouble() > 0.5; // TEST: random boolean
    }*/

    [HttpGet("sendCommand", Name = "SendCommand")]
    public string SendCommand(string serverName, string command)
    {
        return _serverManager.SendCommand(serverName, command);
    }
}
