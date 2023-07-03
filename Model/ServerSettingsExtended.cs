using k8s.Models;
using System.Text.RegularExpressions;

namespace MinecraftServerMicroservice.Model
{
    public enum Type
    {
        vanilla,
        paper,
    }

    /*public enum Difficulty
    {
        peaceful = 0,
        easy = 1,
        normal = 2,
        hard = 3
    }

    public enum Gamemode
    {
        survival = 0,
        creative = 1,
        adventure = 2,
        spectator = 3
    }*/

    public class ServerSettingsExtended
    {
        public const string USERNAME_PATTERN = @"^(?!.*_.*_)[A-Za-z0-9_]{3,16}$";

        public ServerSettingsExtended()
        {

        }

        public Guid settingsID { get; set; }

        
        public bool allowNether { get; set; } = true;
        
        public Difficulty difficulty { get; set; } = Difficulty.easy;
        
        public bool enableCommandBlocks { get; set; } = true;

        public bool enableStatus { get; set; } = true;

        public bool enableWhitelist { get; set; } = false;


        // If set to false players will join in the gamemode they left in.
        public bool forceGamemode { get; set; } = false;

        public Gamemode gamemode { get; set; } = Gamemode.survival;
        
        // TO-DO: set
        public int maxPlayers { get; } = 20;
        
        // TO-DO: set motd
        public string motd { get; set; } = "Server Hosted by Cloudio Bisio";
        
        // Determines wheter the server will authenticate players with the Minecraft account servers or allow any player to connect without authentication. Cannot be changed after the server creation.
        public bool onlineMode { get; } = true;

        public string seed { get; }

        public Type type { get; set; } = Type.vanilla;
        
        public string version { get; } = "latest";

        public List<string> whitelist { get; set; }

        // Updates the whitelist (format with comma separated usernames: "username1,username2,username3"). Returns the number of users that were added.
        public int SetWhitelist(string newWhitelist)
        {
            int userAdded = 0;

            whitelist = new List<string>();

            List<string> entries = newWhitelist.Split(',').ToList<string>();
            foreach (string entry in entries)
            {
                if(ValidateUsername(entry))
                {
                    whitelist.Add(entry);
                    userAdded++;
                }
            }

            return userAdded;
        }

        // Adds an user to the whitelist. Returns true if the user was added correctly.
        public bool AddUserToWhitelist(string username)
        {
            bool userAdded = false;

            if (ValidateUsername(username))
            {
                whitelist.Add(username);
                userAdded = true;
            }

            return userAdded;
        }

        // Removes an user from the whitelist. Returns true if the user was found and removed correctly.
        public bool RemoveUserFromWhitelist(string username)
        {
            bool userRemoved = false;

            if (ValidateUsername(username))
            {
                userRemoved = whitelist.Remove(username);
            }

            return userRemoved;
        }

        public List<V1EnvVar> GetEnvironmentVariables()
        {
            List<V1EnvVar> envVariables = new List<V1EnvVar>();

            envVariables.Add(new V1EnvVar("EULA", "true")); // NB: always present and needed for the server to work

            return envVariables;
        }

        public bool ValidateUsername(string username)
        {
            return Regex.IsMatch(username, USERNAME_PATTERN);
        }

        public string ToJSON()
        {
            string result = "";

            return result;
        }
    }
}