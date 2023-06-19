namespace MinecraftServerMicroservice.Model
{
    public class ServerManager
    {
        public string CreateServer(string resourceGroup)
        {
            // Creates Azure Resources with ARM Template:
            // ACI Container
            // Storage Account for File Share (data)

            return "JSON of created resources (info & IDs)";
        }

        public void StartServer(string id)
        {
            // Start container
        }

        public void StopServer(string id)
        {
            // Stop container
        }

        public void DeleteServer(string id)
        {
            // Delete server resources (ACI container + storage)
        }

        public bool PingServer(string id)
        {
            // Check if the server is online
            return false;
        }

        public void SendCommand(string command)
        {
            // Send exec command (manage, change whitelist, send chat message, etc.)
        }
    }
}
