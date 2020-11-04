

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HamsterCheese.AmongUsMemory
{
    public class PlayerData
    {
        #region ObserveStates
        private bool observe_dieFlag = false;
        private bool observe_disconnectFlag = false;
        #endregion


        /// <summary>
        /// Player Control Instance
        /// </summary>
        public PlayerControl Instance;

        /// <summary>
        /// Player Die Event
        /// </summary>
        public System.Action<string> onDie;

        /// <summary>
        /// Player Disconnect Event
        /// </summary>
        public System.Action<string> onDisconnect;

        /// <summary>
        /// Player Info Pointer&Offset
        /// </summary>
        public string PlayerInfoPTR = null;
        public IntPtr PlayerInfoPTROffset;

        /// <summary>
        /// Player Controll Pointer&Offset
        /// </summary>
        public IntPtr PlayerControllPTR;
        public string PlayerControllPTROffset;


        Dictionary<string, CancellationTokenSource> Tokens = new Dictionary<string, CancellationTokenSource>();


        /// <summary>
        /// PlayerInfo
        /// </summary>
        public PlayerInfo? PlayerInfo
        {
            get
            {
                if (PlayerInfoPTROffset == IntPtr.Zero)
                {
                    var ptr =  Methods.Call_PlayerControl_GetData(this.PlayerControllPTR);
                    PlayerInfoPTR = ptr.GetAddress();
                    PlayerInfo pInfo = Utils.FromBytes<PlayerInfo>(Cheese.mem.ReadBytes(PlayerInfoPTR, Utils.SizeOf<PlayerInfo>()));
                    PlayerInfoPTROffset = new IntPtr(ptr);
                    playerInfo = pInfo;
                    return playerInfo;

                }
                else
                {
                    PlayerInfo pInfo = Utils.FromBytes<PlayerInfo>(Cheese.mem.ReadBytes(PlayerInfoPTR, Utils.SizeOf<PlayerInfo>()));
                    playerInfo = pInfo;
                    return playerInfo;
                }

            }
        }
        private PlayerInfo? playerInfo = null;

        public void StopObserveState()
        {
            var key = Tokens.ContainsKey("ObserveState");
            if(key)
            {
                if (Tokens["ObserveState"].IsCancellationRequested == false)
                {
                    Tokens["ObserveState"].Cancel();
                    Tokens.Remove("ObserveState");
                }
            } 
        }
        
        public void StartObserveState()
        {
            if(Tokens.ContainsKey("ObserveState"))
            {
                //Console.WriteLine("Already Observed!");
                return;
            }
            else
            {
                CancellationTokenSource cts = new CancellationTokenSource(); 
                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        if (PlayerInfo.HasValue)
                        {
                            if (observe_dieFlag == false && PlayerInfo.Value.IsDead == 1)
                            {
                                observe_dieFlag = true;
                                var name = HamsterCheese.AmongUsMemory.Utils.ReadString(PlayerInfo.Value.PlayerName);
                                onDie?.Invoke(name);
                            }

                            if(observe_disconnectFlag == false && PlayerInfo.Value.Disconnected == 1)
                            {
                                observe_disconnectFlag = true;
                                var name = HamsterCheese.AmongUsMemory.Utils.ReadString(PlayerInfo.Value.PlayerName);
                                onDisconnect?.Invoke(name);
                            }
                        }
                        System.Threading.Thread.Sleep(25); 
                    }
                }, cts.Token);

                Tokens.Add("ObserveState", cts);
            }
          
        }

        public void ReadMemory()
        {
            Instance = Utils.FromBytes<PlayerControl>(Cheese.mem.ReadBytes(PlayerControllPTROffset, Utils.SizeOf<PlayerControl>()));
        }

        public bool IsLocalPlayer
        {
            get
            {
                if (Instance.myLight == IntPtr.Zero) return false;
                else
                {
                    return true;
                }
            }
        }

    }
}