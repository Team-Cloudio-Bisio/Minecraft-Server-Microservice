using System.Text.Json;

namespace MinecraftServerMicroservice.Model
{
    public enum ServerStatus
    {
        NotFound = -1,
        Running = 0,
        Waiting = 1,
        Terminated = 2
    }

    public class ServerInfo
    {
        public string serverName { get; set; }

        public bool ready { get; set; } = false;
        public ServerStatus deploymentStatus { get; set; }
        public string deploymentStatusString { get; set; }
        public int connectedPlayers { get; set; } = -1;
        public int maxPlayers { get; set; } = -1;

        public string ToJSONstring()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
