using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HamsterCheese.AmongUsMemory;

namespace YourCheese
{
    class PlayerComparer : Comparer<PlayerData>
    {
        public override int Compare(PlayerData x, PlayerData y) //Greater means lower on the list
        {
            if(x.PlayerInfo.Value.Disconnected != y.PlayerInfo.Value.Disconnected)
            {
                if (x.PlayerInfo.Value.Disconnected == 1) return 1; //Disconnected players are greater than connected ones
                else return -1;
            }
            else
            {
                if(x.PlayerInfo.Value.IsDead != y.PlayerInfo.Value.IsDead)
                {
                    if (x.PlayerInfo.Value.IsDead == 1) return 1; //Dead players are greater than alive ones
                    else return -1;
                }
                else
                {
                    if (x.PlayerInfo.Value.IsImpostor != y.PlayerInfo.Value.IsImpostor)
                    {
                        if (x.PlayerInfo.Value.IsImpostor == 1) return -1; //Non-Imposters are greater than Imposters
                        else return 1;
                    }
                    else return 0;
                }
            }
        }
    }
}

/*
 * Comparison Hierarchy:
 * Disconnected players go on the bottom
 * Dead players can be above disconnected players
 * Imposters go on the top
 * 
 * Example:
 * Alive    Connected       Imposter
 * Alive    Connected       Non-Imposter
 * Dead     Connected       *
 * *        Disconnected    *
 */