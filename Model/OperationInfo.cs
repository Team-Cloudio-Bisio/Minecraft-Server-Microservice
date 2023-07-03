namespace MinecraftServerMicroservice.Model
{
    public class OperationInfo
    {
        public string serverName { get; set; }

        public string deployment { get; set; } = "";
        public string service { get; set; } = "";
        public string storage { get; set; } = "";
    }
}
