using k8s.KubeConfigModels;
using Microsoft.AspNetCore.Hosting.Server;
using MinecraftServerMicroservice.Model;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using User = MinecraftServerMicroservice.Model.User;

namespace MinecraftServerMicroservice.Utils
{
    public class DBUtils
    {
        private const string ENDPOINT_BASE = "http://localhost:4000/";
        private HttpClient _client = new HttpClient();

        public async Task<bool> AddServer(Server server)
        {
            var response = await _client.PostAsync($"{ENDPOINT_BASE}Server/insertServer", new StringContent(JsonSerializer.Serialize(server), Encoding.UTF8, MediaTypeNames.Application.Json));

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> RemoveServer(string serverName)
        {
            var response = await _client.DeleteAsync($"{ENDPOINT_BASE}Server/deleteServer?serverName={serverName}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateServer(Server server)
        {
            var response = await _client.PostAsync($"{ENDPOINT_BASE}Server/insertServer", new StringContent(JsonSerializer.Serialize(server), Encoding.UTF8, MediaTypeNames.Application.Json));

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }

        public async Task<List<Server>> GetServerList(User user)
        {
            List<Server>? userServers = new List<Server>();

            var response = await _client.GetAsync($"{ENDPOINT_BASE}Server/getServers");
            string jsonString = await response.Content.ReadAsStringAsync();

            List<Server> servers = JsonSerializer.Deserialize<List<Server>>(jsonString);

            if (response.IsSuccessStatusCode && servers.Count > 0)
            {
                foreach (Server server in servers)
                {
                    if (server.admin.Equals(user))
                    {
                        userServers.Add(server);
                    }
                }
            }
            return userServers;
        }

        public async Task<Server> GetServer(string serverName)
        {
            Server resultServer;

            var response = await _client.GetAsync($"{ENDPOINT_BASE}Server/getServers");
            string jsonString = await response.Content.ReadAsStringAsync();

            List<Server> servers = JsonSerializer.Deserialize<List<Server>>(jsonString);

            if (response.IsSuccessStatusCode && servers.Count > 0)
            {
                foreach (Server server in servers)
                {
                    if (server.serverName.Equals(serverName))
                    {
                        resultServer = server;
                        return resultServer;
                    }
                }
            }
            return null;
        }
    }
}