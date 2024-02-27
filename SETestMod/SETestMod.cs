using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Common.Utils;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using VRage;
using VRage.ModAPI;
using VRage.Utils;

namespace SETest.Mod
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Mod : MySessionComponentBase
    {
        private static readonly ushort PACKET_REQUEST = 14038;
        private static bool bInit = false;
        private static bool bIsServer;
        private static readonly Encoding encode = Encoding.Unicode;
        public static void Init()
        {
            try
            {
                MyLog.Default.WriteLineAndConsole("SETestMod: Initialized.");
                bInit = true;
                bIsServer = MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;

                MyAPIGateway.Utilities.MessageEntered += EnteredMessage;
                
                if (bIsServer)
                {
                    MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(PACKET_REQUEST, ReceivedRequest);
                }
                else
                {
                    RequestMessages();
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"SETestMod: Failed to init! {ex}");
            }
        }
        protected override void UnloadData()
        {
            try
            {
                MyAPIGateway.Utilities.MessageEntered -= EnteredMessage;
                MyAPIGateway.Multiplayer?.UnregisterSecureMessageHandler(PACKET_REQUEST, ReceivedRequest);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"SETestMod: Failed to unregister message handler! {ex}");
            }
            MyLog.Default.WriteLineAndConsole($"SETestMod: Unloaded.");
        }
        public static void EnteredMessage(string message, ref bool visible)
        {
            try
            {
                if (!bInit)
                    return;

                if (message.Equals("/ch", StringComparison.InvariantCultureIgnoreCase))
                {
                    visible = false;
                    MyAPIGateway.Utilities.ShowMissionScreen("SETestMod", "TEST", "", "", null, "CLOSE");
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"SETestMod: Error in message receiving! {ex}");
            }
        }
        public static void RequestMessages()
        {
            try
            {
                if (bIsServer) return;

                byte[] bytes = encode.GetBytes(MyAPIGateway.Multiplayer.MyId.ToString());
                MyAPIGateway.Multiplayer.SendMessageToServer(PACKET_REQUEST, bytes, true);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"SETestMod: Error in message sending! {ex}");
            }
        }
        public static void ReceivedRequest(ushort handlerId, byte[] messageSentBytes, ulong senderPlayerId, bool isArrivedFromServer)
        {
            try
            {
                if (!bIsServer) return;
                string data = encode.GetString(messageSentBytes);
                MyLog.Default.WriteLineAndConsole($"SETestMod: Got message: {data}");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"SETestMod: Error in receiving request! {ex}");
            }
        }
        public override void UpdateAfterSimulation()
        {
            try
            {
                if (!bInit)
                {
                    if (MyAPIGateway.Session == null)
                        return;
                    Init();
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"SETestMod: Failed to update after simulation! {ex}");
            }
        }
    }
}