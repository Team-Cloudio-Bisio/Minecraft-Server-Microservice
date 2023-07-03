using k8s.KubeConfigModels;
using k8s.Models;

namespace MinecraftServerMicroservice.Model
{
    public class Server
    {
        public string serverName { get; set; }

        public string ip { get; set; }

        public string containerID { get; set; }

        public Guid? settingsID { get; set; }

        public ServerSettingsExtended settings { get; set; }

        public List<User> admin { get; set; }

        public List<User> whitelist { get; set; }
    }
}
