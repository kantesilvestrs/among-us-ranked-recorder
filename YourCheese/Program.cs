
using HamsterCheese.AmongUsMemory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YourCheese
{
    enum GameWinnerType
    {
        Impostors,
        Crewmates
    }

    class Program
    {
        static List<PlayerData> playerDatas = new List<PlayerData>();

        static readonly string[] Colors = new string[]
        {
            "Red",
            "Blue",
            "Green",
            "Pink",
            "Orange",
            "Yellow",
            "Black",
            "White",
            "Purple",
            "Brown",
            "Cyan",
            "Lime"
        };

        static string GetPlayerName(IntPtr PlayerName)
        {
            return HamsterCheese.AmongUsMemory.Utils.ReadString(PlayerName);
        }

        static GameWinnerType GetGameWinnerType(List<PlayerData> playerDataList)
        {
            var impostors = playerDataList.FindAll((playerData) => playerData.PlayerInfo.Value.IsImpostor == 1);
            var crewmates = playerDataList.FindAll((playerData) => playerData.PlayerInfo.Value.IsImpostor == 0);

            int aliveImpostors = impostors.FindAll((impostor) => impostor.PlayerInfo.Value.Disconnected != 1 && impostor.PlayerInfo.Value.IsDead == 0)
               .Count();

            int aliveCrewmates = crewmates.FindAll((crewmate) => crewmate.PlayerInfo.Value.Disconnected != 1 && crewmate.PlayerInfo.Value.IsDead == 0)
                .Count();

            if (aliveCrewmates > aliveImpostors)
            {
                return GameWinnerType.Crewmates;

            }
            else
            {
                return GameWinnerType.Impostors;
            }
        }

        static string GetGameWinners(List<PlayerData> playerDataList)
        {

            var impostors = playerDataList.FindAll((playerData) => playerData.PlayerInfo.Value.IsImpostor == 1);
            var crewmates = playerDataList.FindAll((playerData) => playerData.PlayerInfo.Value.IsImpostor == 0);



            int aliveImpostors = impostors.FindAll((impostor) => impostor.PlayerInfo.Value.Disconnected != 1 && impostor.PlayerInfo.Value.IsDead == 0)
                .Count();

            int aliveCrewmates = crewmates.FindAll((crewmate) => crewmate.PlayerInfo.Value.Disconnected != 1 && crewmate.PlayerInfo.Value.IsDead == 0)
                .Count();


            var winners = playerDataList
                    .FindAll((playerData) => playerData.PlayerInfo.Value.IsImpostor == (aliveCrewmates > aliveImpostors ? 0 : 1))
                    .Select((playerData) => GetPlayerName(playerData.PlayerInfo.Value.PlayerName))
                    .ToList();

            return string.Join(", ", winners);
        }

        static void Main()
        {
            StreamWriter resultsFile = null;
            string newGameFileName = null;
            bool validGameSession = false;
            List<string> deadPlayers = new List<string>();
            List<string> disconnectedPlayers = new List<string>();

            Console.SetWindowSize(80, 25);

            bool amongUsProcessAvailable = HamsterCheese.AmongUsMemory.Cheese.Init();

            while (!amongUsProcessAvailable)
            {
                Console.Clear();
                Console.WriteLine("Waiting for game process...");
                amongUsProcessAvailable = HamsterCheese.AmongUsMemory.Cheese.Init();
                System.Threading.Thread.Sleep(1000);
            }

            // Initialize result recorder
            if (amongUsProcessAvailable)
            {
                Console.WriteLine("Starting up Among Us MMR Result Recorder...");
                // Start a new recording on each session
                HamsterCheese.AmongUsMemory.Cheese.ObserveShipStatus((x) =>
                {
                    Console.WriteLine("Cleaning out any previous sessions...");
                    // Stop player observing
                    foreach (var player in playerDatas)
                        player.StopObserveState();

                    // Close off previous session
                    if (validGameSession)
                    {
                        if (resultsFile != null)
                        {
                            Console.WriteLine("Game has been finished...");
                            Console.WriteLine($"Results saved in: {newGameFileName}");
                            // TODO: Calculate game winner
                            resultsFile.WriteLine($"GAME_WINNER_TYPE, {Enum.GetName(typeof(GameWinnerType), GetGameWinnerType(playerDatas))}");
                            resultsFile.WriteLine($"GAME_WINNERS, {GetGameWinners(playerDatas)}");
                            resultsFile.WriteLine($"GAME_END, {DateTime.Now:yyyy-MM-dd-HH-mm-ss}");
                            resultsFile.Close();
                            resultsFile.Dispose();
                            resultsFile = null;
                        }

                        validGameSession = false;
                    }

                    Console.WriteLine("Getting information about players...");
                    playerDatas = HamsterCheese.AmongUsMemory.Cheese.GetAllPlayers();

                    if (playerDatas.Count() < 1)
                    {
                        // Dispose of streamwriter
                        if (resultsFile != null)
                        {
                            resultsFile.Close();
                            resultsFile.Dispose();
                            resultsFile = null;
                        }
                    }
                    else
                    {
                        validGameSession = true;
                    }

                    if (validGameSession)
                    {
                        Console.Clear();
                        newGameFileName = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                            $@"\AmongUsMMR-Results-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt"
                            );
                        resultsFile = File.CreateText(newGameFileName);

                        Console.WriteLine("Recording game results...");
                        resultsFile.WriteLine($"GAME_START, {Guid.NewGuid()}, {DateTime.Now:yyyy-MM-dd-HH-mm-ss}");

                        Console.WriteLine("Registering players...");
                        foreach (var data in playerDatas)
                        {
                            if (data.PlayerInfo != null)
                            {
                                var Name = HamsterCheese.AmongUsMemory.Utils.ReadString(data.PlayerInfo.Value.PlayerName);
                                resultsFile.WriteLine($"ADD_PLAYER, {Name}, {Colors[data.PlayerInfo.Value.ColorId]}, {data.PlayerInfo.Value.IsImpostor.ToString()}");
                            }
                        }


                        foreach (var player in playerDatas)
                        {
                            player.onDie = null;
                            player.onDie += (name) =>
                            {
                                if (!deadPlayers.Any((deadPlayer) => deadPlayer == name))
                                {
                                    deadPlayers.Add(name);
                                    //Console.WriteLine("Player Died:" + Colors[colorId]);
                                    resultsFile.WriteLine($"PLAYER_DEAD_OR_VOTED_OFF, {name}");
                                }
                            };

                            player.onDisconnect = null;
                            player.onDisconnect += (name) =>
                            {
                                if(!disconnectedPlayers.Any((dcPlayer) => dcPlayer == name))
                                {
                                    disconnectedPlayers.Add(name);
                                    Console.WriteLine($"{name} has disconnected from the game.");
                                    resultsFile.WriteLine($"PLAYER_DISCONNECTED, {name}");
                                }
                            };

                            // player state check
                            player.StartObserveState();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Waiting for the next game to begin...");
                    }
                });

            }

            System.Threading.Thread.Sleep(86400000);
        }
    }
}


