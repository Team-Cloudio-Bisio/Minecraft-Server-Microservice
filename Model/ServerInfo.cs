namespace MinecraftServerMicroservice.Model
{
    public enum ServerStatus
    {
        running,
        waiting,
        terminated,
        notFound
    }

    public class ServerInfo
    {
        public string serverName { get; set; }

        public ServerStatus deployment { get; set; }
        public int connectedPlayers { get; set; }
        public int maxPlayers { get; set; }
    }
}
