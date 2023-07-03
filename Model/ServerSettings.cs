using System.Security.Cryptography;

namespace MinecraftServerMicroservice.Model
{
    public enum Difficulty
    {
        peaceful,
        easy,
        normal,
        hard
    }

    public enum Gamemode
    {
        survival,
        creative,
        adventure,
        spectator
    }

    public class ServerSettings
    {

        // create with preset settings
        public ServerSettings()
        {
            seed = RandomNumberGenerator.GetInt32(Int32.MaxValue);
            maxPlayers = 4;
            difficulty = Difficulty.easy;
            gamemode = Gamemode.survival;
        }

        public Guid settingsID { get; set; }
        public int seed { get; set; }
        public int maxPlayers { get; set; }
        public Difficulty difficulty { get; set; }
        public Gamemode gamemode { get; set; }


        public string ToString()
        {
            return "ServerSettings (seed, maxPlayers, difficulty, gamemode): " + seed + " - " +
                                    maxPlayers + " - " + difficulty + " - " + gamemode;
        }
    }
}
