using k8s;
using k8s.Models;
using System.Diagnostics;

namespace MinecraftServerMicroservice.Model
{
    public class ServerManager
    {
        public const string MC_SERVER_NAMESPACE = "default";

        public const string MC_SERVICE_SUFFIX = "-service";
        public const string MC_STORAGE_SUFFIX = "-volume";

        private KubernetesClientConfiguration? _kubeConfig;
        private Kubernetes? _client;

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
                //Console.WriteLine($"Error while initializing the ServerManager: {ex.Message}");
            }
            System.Diagnostics.Debug.WriteLine("ServerManager Setup");

            // get a list of the current pods the user has access to?
        }

        /// <summary>
        /// Method <c>CreateServer</c> creates a minecraft server.
        /// </summary>
        /// <param name='serverName'>
        /// The univoque name that will be associate to the server.
        /// </param>
        public async Task<string> CreateServer(
            string serverName = "",
            string minecraftVersion = "",
            string serverOperators = "",
            string serverWorldURL = "",
            bool serverEnableStatus = true,
            string serverMOTD = "MC Server Powered by Azure & Kubernetes",
            string serverDifficulty = "easy",
            string serverGameMode = "survival",
            int serverMaxPlayers = 20,
            bool serverOnlineMode = true,
            int serverPlayerIdleTimeout = 0,
            bool serverEnableWhitelist = false,
            string serverWhitelist = "")
        {
            if (_client == null)
                return "Client not yet initialized";

            // Validate parameters
            List<V1EnvVar> envVariables = new List<V1EnvVar>();
            envVariables.Add(new V1EnvVar("EULA", "true"));

            if (minecraftVersion.Length > 0)
                envVariables.Add(new V1EnvVar("VERSION", minecraftVersion));

            if (serverOperators.Length > 0)
                envVariables.Add(new V1EnvVar("OPS", serverOperators));

            if (serverWorldURL.Length > 0)
                envVariables.Add(new V1EnvVar("WORLD", serverWorldURL));

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

            // Configure Deployment (Workload resource containing the server docker image)
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
                                new V1Container()
                                {
                                    Name = $"{serverName}-container",
                                    Image = "itzg/minecraft-server:latest",
                                    Env = envVariables,
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
                                    },
                                },
                            },
                            Volumes = new List<V1Volume>()
                            {
                                new V1Volume()
                                {
                                    Name = $"{serverName}-data",
                                    PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource()
                                    {
                                        ClaimName = serverName + MC_STORAGE_SUFFIX
                                    },
                                },
                            },
                        }
                    },
                }
            };

            // Configure Service to make the Server reachable
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

        public async Task<string> DeleteServer(string serverName)
        {
            string deployment = "", service = "", pvc = "", res = "";

            // Delete Deployment
            try
            {
                await _client.DeleteNamespacedDeploymentAsync(serverName, MC_SERVER_NAMESPACE);
                deployment = "Deleted";
            }
            catch (k8s.Autorest.HttpOperationException e)
            {
                if (e.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                {
                    deployment = $"Not found";
                    System.Diagnostics.Debug.WriteLine($"Deployment '{serverName}' not found.");
                }
            }
            // Delete Service
            try
            {
                await _client.DeleteNamespacedServiceAsync(serverName + MC_SERVICE_SUFFIX, MC_SERVER_NAMESPACE);
                service = "Deleted";
            }
            catch (k8s.Autorest.HttpOperationException e)
            {
                if (e.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                {
                    service = $"Not found";
                    System.Diagnostics.Debug.WriteLine($"Service '{serverName}' not found.");
                }
            }
            // Delete Storage
            try
            {
                await _client.DeleteNamespacedPersistentVolumeClaimAsync(serverName + MC_STORAGE_SUFFIX, MC_SERVER_NAMESPACE);
                pvc = "Deleted";
            }
            catch (k8s.Autorest.HttpOperationException e)
            {
                if (e.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                {
                    pvc = $"Not found";
                    System.Diagnostics.Debug.WriteLine($"PVC '{serverName}' not found.");
                }
            }

            res = $"{{" +
                    $"\"ServerName\" = \"{serverName}\"," +
                    $"\"Deployment\" = \"{deployment}\"," +
                    $"\"Service\" = \"{service}\"," +
                    $"\"Storage\" = \"{pvc}\"," +
                  $"}}";

            return res;
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

        public async Task<string> StartServer(string serverName)
        {
            V1Deployment deployment = await _client.ReadNamespacedDeploymentAsync(serverName, MC_SERVER_NAMESPACE);

            deployment.Spec.Replicas = 1;

            await _client.ReplaceNamespacedDeploymentAsync(deployment, serverName, MC_SERVER_NAMESPACE);

            return $"Server '{serverName}' Started. Status: ...";
        }

        public async Task<string> StopServer(string serverName)
        {
            V1Deployment deployment = await _client.ReadNamespacedDeploymentAsync(serverName, MC_SERVER_NAMESPACE);

            deployment.Spec.Replicas = 0;

            await _client.ReplaceNamespacedDeploymentAsync(deployment, serverName, MC_SERVER_NAMESPACE);

            return $"Server '{serverName}' Stopped. Status: ...";
        }

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

        // Example: /op mikyll98
        public string SendCommand(string serverName, string command)
        {
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = "kubectl",
                Arguments = $"exec deployment/{serverName} -- mc-send-to-console {command}",
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
    }
}
