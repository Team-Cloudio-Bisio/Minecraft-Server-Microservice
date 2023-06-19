using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using MinecraftServerMicroservice.Model;

namespace MinecraftServerMicroservice.Controllers;

[ApiController]
[Route("[controller]")]
public class ServerManagerController : ControllerBase
{
    private ServerManager _serverManager = new ServerManager();

    // Get async request (the creation requires time)
    [HttpGet("createServer", Name = "CreateServer")]
    public async Task<string> CreateServer()
    {
        _serverManager.CreateServer("serverResourceGroup");

        // e.g. (return JSON of the created resources)
        return "{" +
                    "\"properties\": {" +
                        "\"sku\": \"Standard\"," +
                        "\"provisioningState\": \"Succeeded\"," +
                        "\"provisioningTimeoutInSeconds\": 1800," +
                        "\"isCustomProvisioningTimeout\": false," +
                        "\"containers\": [" +
                            "{" +
                                "\"name\": \"minecraft-test-prova-aci\"," +
                                "\"properties\": {" +
                                    "\"image\": \"itzg/minecraft-server:latest\"," +
                                    "\"ports\": [" +
                                        "{" +
                                            "\"protocol\": \"TCP\"," +
                                            "\"port\": 25565" +
                                        "}" +
                                    "]," +
                                    "\"environmentVariables\": [" +
                                        "{" +
                                            "\"name\": \"VERSION\"," +
                                            "\"value\": \"1.20.1\"" +
                                        "}," +
                                    "]," +
                                    "\"instanceView\": {" +
                                    // [...]
                                    "}," +
                                    "\"resources\": {" +
                                        "\"requests\": {" +
                                            "\"memoryInGB\": 2,\"cpu\": 1" +
                                        "}" +
                                    "}," +
                                    "\"volumeMounts\": [" +
                                        "{" +
                                            "\"name\": \"azurefile\"," +
                                            "\"mountPath\": \"/data\"" +
                                        "}" +
                                    "]" +
                                "}" +
                            "}" +
                        "]" +
                    // [...]
                    "}" +
                "}" +
            "}";
    }

    [HttpGet("startServer", Name = "StartServer")]
    public string StartServer()
    {
        _serverManager.StartServer("");

        return "Server started";
    }

    [HttpGet("stopServer", Name = "StopServer")]
    public string StopServer()
    {
        _serverManager.StopServer("");

        return "Server stopped";
    }

    [HttpGet("deleteServer", Name = "DeleteServer")]
    public string DeleteServer()
    {
        _serverManager.DeleteServer("");

        return "Server deleted";
    }

    [HttpGet("pingServer", Name = "PingServer")]
    public bool PingServer()
    {
        _serverManager.PingServer("");

        return Random.Shared.NextDouble() > 0.5; // TEST: random boolean
    }

    [HttpGet("sendCommand", Name = "SendCommand")]
    public string SendCommand(string command)
    {
        _serverManager.SendCommand(command);

        return "Sent command: " + command;
    }
}
