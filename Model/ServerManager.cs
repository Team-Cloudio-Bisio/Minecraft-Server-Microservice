using k8s;
using k8s.Models;
using MinecraftServerMicroservice.Utils;
using System;
using System.Diagnostics;
using System.Drawing;

namespace MinecraftServerMicroservice.Model
{
    public class ServerManager
    {
        public const string MC_SERVER_NAMESPACE = "minecraft-servers";
        public const string MC_CONFIG = "config-scripts";

        public const string MC_SERVICE_SUFFIX = "-service";
        public const string MC_STORAGE_SUFFIX = "-volume";

        private readonly KubernetesClientConfiguration? _kubeConfig;
        private readonly Kubernetes? _client;

        public ServerManager()
        {
            try
            {
                // Load Kubernetes Config file (containing the connection string)
                _kubeConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile();
                _client = new Kubernetes(_kubeConfig);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error while initializing the ServerManager: {ex.Message}");
            }
            System.Diagnostics.Debug.WriteLine("ServerManager Setup");

        }

        public async Task<Server> CreateServer(Server server)
        {
            string ipAddress = await FullCreateServer(server.serverName);

            server.ip = ipAddress;

            return server;
        }

        /// <summary>
        /// Method <c>FullCreateServer</c> creates a Minecraft Server and pushes it
        /// to the Azure cluster. The parameters are used to configure the server.
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="minecraftVersion"></param>
        /// <param name="serverOperators"></param>
        /// <param name="serverWorldURL"></param>
        /// <param name="serverEnableStatus"></param>
        /// <param name="serverMOTD"></param>
        /// <param name="serverDifficulty"></param>
        /// <param name="serverGameMode"></param>
        /// <param name="serverMaxPlayers"></param>
        /// <param name="serverOnlineMode"></param>
        /// <param name="serverPlayerIdleTimeout"></param>
        /// <param name="serverEnableWhitelist"></param>
        /// <param name="serverWhitelist"></param>
        /// <returns>The server's IP address.</returns>
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
            if (_client == null)
                return "Client not yet initialized";

            // Check if the server exists
            if (await CheckIfServerExist(serverName))
                return $"Server named '{serverName}' already exists.";

            // Validate parameters
            List<V1EnvVar> envVariables = new List<V1EnvVar>();
            envVariables.Add(new V1EnvVar("EULA", "true"));

            if (minecraftVersion.Length > 0)
                envVariables.Add(new V1EnvVar("VERSION", minecraftVersion));

            if (serverOperators.Length > 0)
                envVariables.Add(new V1EnvVar("OPS", serverOperators));

            if (serverWorldURL.Length > 0)
                envVariables.Add(new V1EnvVar("WORLD", serverWorldURL));

            envVariables.Add(new V1EnvVar("ENABLE_COMMAND_BLOCK", serverEnableCommandBlocks.ToString().ToLower()));

            envVariables.Add(new V1EnvVar("ENABLE_STATUS", serverEnableStatus.ToString().ToLower()));

            if (serverMOTD.Length < 59)
                envVariables.Add(new V1EnvVar("MOTD", serverMOTD));

            serverDifficulty = serverDifficulty.ToLower();
            if (serverDifficulty == "peaceful" || serverDifficulty == "easy" || serverDifficulty == "medium" || serverDifficulty == "hard")
                envVariables.Add(new V1EnvVar("DIFFICULTY", serverDifficulty));

            serverGameMode = serverGameMode.ToLower();
            if (serverGameMode == "creative" || serverGameMode == "survival" || serverGameMode == "adventure" || serverGameMode == "spectator")
                envVariables.Add(new V1EnvVar("MODE", serverGameMode));

            if (serverMaxPlayers >= 1)
                envVariables.Add(new V1EnvVar("MAX_PLAYERS", serverMaxPlayers.ToString()));

            envVariables.Add(new V1EnvVar("ONLINE_MODE", serverOnlineMode.ToString().ToLower()));

            if (serverPlayerIdleTimeout > 1)
                envVariables.Add(new V1EnvVar("PLAYER_IDLE_TIMEOUT", serverPlayerIdleTimeout.ToString()));

            envVariables.Add(new V1EnvVar("ENABLE_WHITELIST", serverEnableWhitelist.ToString().ToLower()));

            if (serverWhitelist.Length > 0)
                envVariables.Add(new V1EnvVar("WHITELIST", serverWhitelist));
            

            // Configure Storage (Persistent Volume, needed to store the server data, even after the server is stopped)
            V1PersistentVolumeClaim persistentVolumeClaim = new V1PersistentVolumeClaim()
            {
                ApiVersion = $"{V1PersistentVolumeClaim.KubeGroup}/{V1PersistentVolumeClaim.KubeApiVersion}",
                Kind = V1PersistentVolumeClaim.KubeKind,
                Metadata = new V1ObjectMeta()
                {
                    Name = serverName + MC_STORAGE_SUFFIX
                },
                Spec = new V1PersistentVolumeClaimSpec()
                {
                    AccessModes = new List<string>() { "ReadWriteOnce" },
                    Resources = new V1ResourceRequirements()
                    {
                        Requests = new Dictionary<string, ResourceQuantity>()
                        {
                            { "storage", new ResourceQuantity("1Gi") }
                        }
                    },
                }
            };

            // Configure Deployment (Workload resource containing the container that runs the Minecraft server)
            V1Deployment deployment = new V1Deployment()
            {
                ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                Kind = V1Deployment.KubeKind,
                Metadata = new V1ObjectMeta()
                {
                    Name = serverName,
                    Labels = new Dictionary<string, string>()
                    {
                        ["app"] = serverName,
                    }
                },
                Spec = new V1DeploymentSpec()
                {
                    Selector = new V1LabelSelector()
                    {
                        MatchLabels = new Dictionary<string, string>()
                        {
                            ["app"] = serverName,
                        }
                    },
                    Template = new V1PodTemplateSpec()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            Labels = new Dictionary<string, string>()
                            {
                                ["app"] = serverName,
                            }
                        },
                        Spec = new V1PodSpec()
                        {
                            Containers = new List<V1Container>()
                            {
                                // Minecraft server container
                                new V1Container()
                                {
                                    Name = $"{serverName}-container",
                                    Image = "itzg/minecraft-server:latest", // Minecraft server Docker image
                                    Env = envVariables, // Environment variables used to configure the server creation
                                    ImagePullPolicy = "Always",
                                    Ports = new List<V1ContainerPort>()
                                    {
                                        new V1ContainerPort(25565),
                                    },
                                    ReadinessProbe = new V1Probe()
                                    {
                                        Exec = new V1ExecAction()
                                        {
                                            Command = new List<string>()
                                            {
                                                "/usr/local/bin/mc-monitor",
                                                "status",
                                                "--host",
                                                "localhost",
                                            }
                                        },
                                        InitialDelaySeconds = 20,
                                        PeriodSeconds = 5,
                                        FailureThreshold = 20,
                                    },
                                    LivenessProbe = new V1Probe()
                                    {
                                        Exec = new V1ExecAction()
                                        {
                                            Command = new List<string>()
                                            {
                                                "/usr/local/bin/mc-monitor",
                                                "status",
                                                "--host",
                                                "localhost",
                                            }
                                        },
                                        InitialDelaySeconds = 20,
                                        PeriodSeconds = 5,
                                    },
                                    VolumeMounts = new List<V1VolumeMount>()
                                    {
                                        new V1VolumeMount
                                        {
                                            Name = $"{serverName}-data",
                                            MountPath = "/data",
                                        },
                                        new V1VolumeMount
                                        {
                                            Name = $"{serverName}-scripts",
                                            MountPath = "/data/scripts",
                                        },
                                    },
                                },
                            },
                            Volumes = new List<V1Volume>()
                            {
                                // Volume to make Minecraft server data persistent (world data, settings, etc.) when the container is stopped and restarted.
                                new V1Volume()
                                {
                                    Name = $"{serverName}-data",
                                    PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource()
                                    {
                                        ClaimName = serverName + MC_STORAGE_SUFFIX
                                    },
                                },
                                // Volume used to load utility scripts (ConfigMap)
                                new V1Volume()
                                {
                                    Name = $"{serverName}-scripts",
                                    ConfigMap = new V1ConfigMapVolumeSource()
                                    {
                                        Name = MC_CONFIG
                                    }
                                },
                            },
                        }
                    },
                }
            };

            // Configure Service to make the Server reachable from outside the Kubernetes cluster
            V1Service service = new V1Service()
            {
                ApiVersion = $"{V1Service.KubeGroup}/{V1Service.KubeApiVersion}",
                Kind = V1Service.KubeKind,
                Metadata = new V1ObjectMeta()
                {
                    Name = serverName + MC_SERVICE_SUFFIX,
                },
                Spec = new V1ServiceSpec()
                {
                    Type = "LoadBalancer",
                    Selector = new Dictionary<string, string>()
                    {
                        ["app"] = serverName,
                    },
                    Ports = new List<V1ServicePort>()
                    {
                        new V1ServicePort()
                        {
                            Port = 25565,
                            TargetPort = 25565,
                        }
                    }
                }
            };

            // Create the Persistent Volume Claim
            V1PersistentVolumeClaim pvcResponse = await _client.CreateNamespacedPersistentVolumeClaimAsync(persistentVolumeClaim, MC_SERVER_NAMESPACE);
            System.Diagnostics.Debug.WriteLine($"PVC created with name: {pvcResponse.Metadata.Name}");

            V1Deployment createdDeployment = await _client.CreateNamespacedDeploymentAsync(deployment, MC_SERVER_NAMESPACE);
            System.Diagnostics.Debug.WriteLine($"Deployment created with name: {createdDeployment.Metadata.Name}");
            
            V1Service createdService = await _client.CreateNamespacedServiceAsync(service, MC_SERVER_NAMESPACE);
            System.Diagnostics.Debug.WriteLine($"Service created with name: {createdService.Metadata.Name}");

            await WaitForServiceReady(service.Metadata.Name, MC_SERVER_NAMESPACE);
            V1Service runningService = await _client.ReadNamespacedServiceAsync(service.Metadata.Name, MC_SERVER_NAMESPACE);
            return runningService.Status.LoadBalancer.Ingress[0].Ip;
        }

        public async Task<OperationInfo> DeleteServer(string serverName)
        {
            OperationInfo deleteInfo = new OperationInfo();
            deleteInfo.serverName = serverName;
            
            // Delete Deployment
            try
            {
                await _client.DeleteNamespacedDeploymentAsync(serverName, MC_SERVER_NAMESPACE);
                deleteInfo.deployment = "deleted";
            }
            catch (k8s.Autorest.HttpOperationException e)
            {
                if (e.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                {
                    deleteInfo.deployment = "notFound";
                    System.Diagnostics.Debug.WriteLine($"Deployment '{serverName}' not found.");
                }
            }
            // Delete Service
            try
            {
                await _client.DeleteNamespacedServiceAsync(serverName + MC_SERVICE_SUFFIX, MC_SERVER_NAMESPACE);
                deleteInfo.service = "deleted";
            }
            catch (k8s.Autorest.HttpOperationException e)
            {
                if (e.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                {
                    deleteInfo.service = "notFound";
                    System.Diagnostics.Debug.WriteLine($"Service '{serverName}' not found.");
                }
            }
            // Delete Storage
            try
            {
                await _client.DeleteNamespacedPersistentVolumeClaimAsync(serverName + MC_STORAGE_SUFFIX, MC_SERVER_NAMESPACE);
                deleteInfo.storage = "deleted";
            }
            catch (k8s.Autorest.HttpOperationException e)
            {
                if (e.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                {
                    deleteInfo.storage = "notFound";
                    System.Diagnostics.Debug.WriteLine($"PVC '{serverName}' not found.");
                }
            }

            return deleteInfo;
        }

        

        public async Task<OperationInfo> StartServer(string serverName)
        {
            OperationInfo startInfo = new OperationInfo();
            startInfo.serverName = serverName;

            try
            {
                V1Deployment deployment = await _client.ReadNamespacedDeploymentAsync(serverName, MC_SERVER_NAMESPACE);

                deployment.Spec.Replicas = 1;

                await _client.ReplaceNamespacedDeploymentAsync(deployment, serverName, MC_SERVER_NAMESPACE);

                startInfo.deployment = "started";
            }
            catch (k8s.Autorest.HttpOperationException e)
            {
                if (e.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.deployment = "notFound";
                    System.Diagnostics.Debug.WriteLine($"PVC '{serverName}' not found.");
                }
            }

            return startInfo;
        }

        public async Task<OperationInfo> StopServer(string serverName)
        {
            OperationInfo stopInfo = new OperationInfo();
            stopInfo.serverName = serverName;

            try
            {
                V1Deployment deployment = await _client.ReadNamespacedDeploymentAsync(serverName, MC_SERVER_NAMESPACE);

                deployment.Spec.Replicas = 0;

                await _client.ReplaceNamespacedDeploymentAsync(deployment, serverName, MC_SERVER_NAMESPACE);

                stopInfo.deployment = "stopped";
            }
            catch (k8s.Autorest.HttpOperationException e)
            {
                if (e.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                {
                    stopInfo.deployment = "notFound";
                    System.Diagnostics.Debug.WriteLine($"PVC '{serverName}' not found.");
                }
            }
            return stopInfo;
        }

        

        /// <summary>
        /// Method <c>SendCommandFireAndForget</c> sends a command to the minecraft server.
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="command"></param>
        /// <returns>A string indicating if the command was correctly sent.</returns>
        public async Task<string> SendCommandFireAndForget(string serverName, string command)
        {
            // Check if the server exists
            if (!(await CheckIfServerExist(serverName)))
                return "Not found";

            // Fire and forget command (mc-send-to-console doesn't provide output)
            System.Diagnostics.Debug.WriteLine($"Command: kubectl exec --namespace={MC_SERVER_NAMESPACE} deployment/{serverName} -- mc-send-to-console {command}");
            ExecCommand(serverName, $"rcon-cli {command}");

            return $"Sent command '{command}'.";
        }

        /// <summary>
        /// Method <c>SendCommandInteractive</c> sends a command to the minecraft server and
        /// returns the output.
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="command"></param>
        /// <returns>A string containing the command output.</returns>
        public async Task<string> SendCommandInteractive(string serverName, string command)
        {
            // Check if the server exists
            if (!(await CheckIfServerExist(serverName)))
                return "Not found";

            // Send command and retrieve the output (rcon-cli provides output)
            System.Diagnostics.Debug.WriteLine($"Command: kubectl exec --namespace={MC_SERVER_NAMESPACE} deployment/{serverName} -- rcon-cli {command}");
            var commandOutput = ExecCommand(serverName, $"rcon-cli {command}");
            System.Diagnostics.Debug.WriteLine($"Output: {commandOutput}");

            return commandOutput;
        }

        /// <summary>
        /// Method <c>UpdateProperty</c> changes the value of a property in setting.properties file.
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="property"></param>
        /// <param name="newValue"></param>
        /// <returns>The command output or an error.</returns>
        public async Task<string> UpdateServerProperty(string serverName, string property, string newValue)
        {
            // Check if the server exists
            if (!(await CheckIfServerExist(serverName)))
                return "Not found";

            // Allow only valid properties
            if (property != "gamemode")
                return "Invalid property";

            string command = $"/bin/bash update-server-property.sh {property} {newValue}";
            string commandOutput = ExecCommand(serverName, command);
            System.Diagnostics.Debug.WriteLine($"Output: {commandOutput}");

            return commandOutput;
        }

        /// <summary>
        /// Method <c>GetServerList</c> retrieve the list of servers
        /// created by the user associated with a given e-mail.
        /// Useful to keep track of the current state of servers belonging
        /// to a certain user.
        /// </summary>
        /// <param name="email">The e-mail of the user</param>
        /// <returns>A list of <c>ServerStatus</c> each one containing the
        /// server name and its current status.</returns>
        public List<ServerInfo> GetServerList(string email)
        {
            List<ServerInfo> serverList = new List<ServerInfo>();

            // Retrieve the server names from the DB (for the given email)

            // Test
            serverList.Add(new ServerInfo() { serverName = "server1", deploymentStatus = ServerStatus.Running, connectedPlayers = 5, maxPlayers = 20 });
            serverList.Add(new ServerInfo() { serverName = "server2", deploymentStatus = ServerStatus.Terminated, connectedPlayers = 0, maxPlayers = 20 });
            serverList.Add(new ServerInfo() { serverName = "server3", deploymentStatus = ServerStatus.Waiting, connectedPlayers = 0, maxPlayers = 20 });
            
            return serverList;
        }

        private async Task WaitForServiceReady(string serviceName, string namespaceName)
        {
            while (true)
            {
                var service = await _client.ReadNamespacedServiceAsync(serviceName, namespaceName);
                if (service?.Status?.LoadBalancer?.Ingress != null &&
                    service.Status.LoadBalancer.Ingress.Count > 0) // Ugly ass condition
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(5)); // Adjust the polling interval as needed
            }
        }
        private async Task<bool> CheckIfServerExist(string serverName)
        {
            bool result = false;

            try
            {
                V1Deployment deployment = await _client.ReadNamespacedDeploymentAsync(serverName, MC_SERVER_NAMESPACE);
                result = true;
            }
            catch (k8s.Autorest.HttpOperationException e)
            {
                if (e.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"Deployment '{serverName}' not found.");
                }
            }

            return result;
        }
        private string ExecCommand(string serverName, string command)
        {
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = "kubectl",
                Arguments = $"exec --namespace={MC_SERVER_NAMESPACE} deployment/{serverName} -- {command}",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var process = new Process()
            {
                StartInfo = processStartInfo
            };

            process.Start();

            var commandOutput = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return commandOutput;
        }

        public async Task<List<ServerInfo>> GetAllServerInformation()
        {
            List<ServerInfo> infoList = new List<ServerInfo>();
            V1DeploymentList deployments = _client.ListNamespacedDeployment(MC_SERVER_NAMESPACE);

            foreach (var deployment in deployments.Items)
            {
                ServerStatus status = await GetServerContainerState(deployment.Name());

                ServerInfo serverInfo = new ServerInfo()
                {
                    serverName = deployment.Name(),
                    ready = deployment.Status.Replicas == 1,
                    deploymentStatus = status,
                    deploymentStatusString = Enum.GetName(typeof(ServerStatus), status),
                    // list players
                };
                infoList.Add(serverInfo);
                System.Diagnostics.Debug.WriteLine(serverInfo.ToJSONstring());
            }

            return infoList;
        }

        private async Task<ServerStatus> GetServerContainerState(string serverName)
        {
            // Check if the server exists
            if (!(await CheckIfServerExist(serverName)))
                return ServerStatus.NotFound;

            V1PodList pods = _client.ListNamespacedPod(MC_SERVER_NAMESPACE, labelSelector: $"app={serverName}");
            
            try
            {
                V1ContainerState state = pods.Items[0].Status.ContainerStatuses[0].State;

                if (state?.Running != null)
                    return ServerStatus.Running;
                else if (state?.Waiting != null)
                    return ServerStatus.Waiting;
                else if (state?.Terminated != null)
                    return ServerStatus.Terminated;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Error while retrieving the state of server '{serverName}': {e.Message}");
            }

            return ServerStatus.NotFound;
        }

        public async Task<string> GetWhitelist(string serverName)
        {
            // Check if the server exists
            if (!(await CheckIfServerExist(serverName)))
                return "not found";

            // Test
            var command = "cat whitelist.json";
            System.Diagnostics.Debug.WriteLine($"Command: {command}");
            var commandOutput = ExecCommand(serverName, command);
            System.Diagnostics.Debug.WriteLine($"Output: {commandOutput}");

            return commandOutput;
        }

        // ================================================
        // EXTRAS
        public async Task<string> PingServer(string serverName)
        {
            string result = "";
            V1Deployment deployment = await _client.ReadNamespacedDeploymentAsync(serverName, MC_SERVER_NAMESPACE);
            // Check status?

            // Obtain the IP/DNS and ping it?

            // Check if the server is online

            if (deployment?.Status != null)
            {
                result = deployment.Status.ToString();
            }
            else
                result = $"Server '{serverName}' not found.";

            return result;
        }
        public async Task<string> GetServerInfo(string serverName)
        {
            V1Service service = await _client.ReadNamespacedServiceAsync(serverName, MC_SERVER_NAMESPACE);
            Console.WriteLine($"Service created with name: {service.Metadata.Name}");

            string hostname = "-", extIP = "-";
            if (service.Status.LoadBalancer.Ingress != null && service.Status.LoadBalancer.Ingress.Count > 0)
            {
                extIP = service.Status.LoadBalancer.Ingress[0].Ip;
                hostname = service.Status.LoadBalancer.Ingress[0].Hostname;
            }

            var serverInfo = $"External IP: {extIP}\nHostname: {hostname}";
            return serverInfo;
        }


        // Open shell to pod:
        // kubectl exec --stdin --tty --namespace=minecraft-servers deployment/test-mc -- /bin/bash

        // Continuously print logs to output (add -n +1 to print from start):
        // kubectl exec --namespace=minecraft-servers deployment/test-mc -- tail -f logs/latest.log
    }
}
