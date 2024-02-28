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
using VRage.Game.ModAPI;
using System.Security.Cryptography.X509Certificates;
using VRage.ObjectBuilders;
using VRage.ObjectBuilders.Private;

namespace SETestMod
{
    // Класс общих данных мода.
    public static class ModData
    {
        public const ushort NET_MSG_ID = 14038; // Идентификатор сообщений этого мода.
        // public const ushort NET_CMD_ID = 14039; // Идентификатор команд этого мода.
        public static readonly Encoding encode = Encoding.Unicode; // Кодировка.
        public const string strModName = "SETestMod"; // Имя мода.
        public const string strLogPref = strModName + " => "; // Префикс для логирования.
        public const string strError = "Error => "; // Префикс сообщения об ошибке.
        //
        private static bool bInit = false; // Признак инициализированного мода.
        private static bool bIsServer = true; // Признак выполнения кода на сервере.

        // Свойство инициализированности.
        public static bool Initialized { get { return bInit; } set { bInit = value; } }

        // Свойство выполнения на сервере.
        public static bool IsServer { get { return bIsServer; } set { bIsServer = value; } }
    }

    // Класс утилит.
    public static class ModUtilites
    {
        // Создание и синхронизация сущностей.
        public static void CreateAndSyncEntities(this List<MyObjectBuilder_EntityBase> entities)
        {
            MyAPIGateway.Entities.RemapObjectBuilderCollection(entities);
            entities.ForEach(item => MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(item));
            MyAPIGateway.Multiplayer.SendEntitiesCreated(entities);
        }
    }

    // Класс сессии для мода.
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ModSession : MySessionComponentBase
    {
        // Строки команд.
        private const string strCmdMeteor = "meteor"; // Команда запуска метеора.

        // Подключение инициализации сессии для мода до старта (на клиенте и сервере).
        public override void BeforeStart()
        {
            try
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(ModData.NET_MSG_ID, ReceivedMessage); // Регистрация обработчика сообщений.
                ModData.IsServer = MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;
                if (!ModData.IsServer)
                {
                    MyAPIGateway.Utilities.MessageEntered += EnteredMessage; // Добавление делегата обработки введённых сообщений.
                }
                ModData.Initialized = true; // Так как до сих пор всё в порядке - признак инициализированности включён.
                MyLog.Default.WriteLineAndConsole(ModData.strLogPref + "Initialized.");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole(ModData.strLogPref + $"Failed to init! {ex}");
            }
        }

        // Подключение выгрузки данных сессии для мода (на клиенте и сервере).
        protected override void UnloadData()
        {
            try
            {
                MyAPIGateway.Multiplayer?.UnregisterSecureMessageHandler(ModData.NET_MSG_ID, ReceivedMessage); // Удаление регистрации сообщений.
                if (!ModData.IsServer)
                {
                    MyAPIGateway.Utilities.MessageEntered -= EnteredMessage; // Удаление делегата обработки введённых сообщений.
                }
                ModData.Initialized = false; // Так как до сих пор всё в порядке - признак инициализированности выключен.
                MyLog.Default.WriteLineAndConsole(ModData.strLogPref + "Unloaded.");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole(ModData.strLogPref + $"Failed to unload data! {ex}");
            }
        }

        // Делегат обработки введённых сообщений (на клиенте).
        public static void EnteredMessage(string strMsg, ref bool visible)
        {
            try
            {
                if (!ModData.Initialized) return; // Если ещё не инициализировано - выход из обработчика.
                // Тестовая проверка ввода пользователя.
                if (strMsg.StartsWith("/tm "))
                {
                    visible = false; // Не показывать в чат запрос пользователя.
                    byte[] btMsg = ModData.encode.GetBytes(strMsg.Substring(4));
                    MyAPIGateway.Multiplayer.SendMessageToServer(ModData.NET_MSG_ID, btMsg, true);
                }
                else if (strMsg == "/tm") // Показ помощи.
                {
                    visible = false; // Не показывать в чат запрос пользователя.
                    /* Вывод помощи на экран. */
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole(ModData.strLogPref + $"Error in message processing! {ex}");
            }
        }

        // Обработчик сетевых сообщений (на клиенте и сервере).
        public static void ReceivedMessage(ushort handlerId, byte[] messageSentBytes, ulong senderPlayerId, bool isArrivedFromServer)
        {
            try
            {
                string strMsg = ModData.encode.GetString(messageSentBytes);
                if (ModData.IsServer) // На сервере - ответ о полученном сообщении обратно клиенту и выполнение конманд.
                {
                    string strResponse = $"User ID={senderPlayerId} sent [{strMsg}] to the server.";
                    MyLog.Default.WriteLineAndConsole(ModData.strLogPref + strResponse);
                    var strCmd = strMsg.Split(' ').FirstOrDefault(); // :(
                    switch (strCmd)
                    {
                        case strCmdMeteor:
                            byte[] btTestAccept = ModData.encode.GetBytes("Spawning meteor!");
                            MyAPIGateway.Multiplayer.SendMessageTo(ModData.NET_MSG_ID, btTestAccept, senderPlayerId, true);
                            List<IMyPlayer> players = new List<IMyPlayer>(); // :(
                            MyAPIGateway.Multiplayer.Players.GetPlayers(players, l => (l.SteamUserId == senderPlayerId));
                            if (players.Count == 1)
                            {
                                IMyPlayer player = players.First();
                                MatrixD worldMatrix;
                                if (player.Controller.ControlledEntity.Entity.Parent == null)
                                {
                                    worldMatrix = player.Controller.ControlledEntity.GetHeadMatrix(true, true, false);
                                    worldMatrix.Translation += worldMatrix.Forward * 2.5f;
                                }
                                else
                                {
                                    worldMatrix = player.Controller.ControlledEntity.Entity.WorldMatrix;
                                    worldMatrix.Translation = worldMatrix.Translation + worldMatrix.Forward * 2.5f + worldMatrix.Up * 0.5f;
                                }
                                var meteorBuilder = new MyObjectBuilder_Meteor
                                {
                                    Item = new MyObjectBuilder_InventoryItem
                                    {
                                        Amount = 1000,
                                        PhysicalContent = new MyObjectBuilder_Ore { SubtypeName = "Stone" }
                                    },
                                    PersistentFlags = MyPersistentEntityFlags2.InScene,
                                    PositionAndOrientation = new MyPositionAndOrientation
                                    {
                                        Position = worldMatrix.Translation,
                                        Forward = (Vector3)worldMatrix.Forward,
                                        Up = (Vector3)worldMatrix.Up,
                                    },
                                    LinearVelocity = worldMatrix.Forward * 15000,
                                    Integrity = 100,
                                };
                                List<MyObjectBuilder_EntityBase> entities = new List<MyObjectBuilder_EntityBase>(); // :(
                                entities.Add(meteorBuilder);
                                ModUtilites.CreateAndSyncEntities(entities);
                            }
                            else
                            {
                                byte[] btTestError = ModData.encode.GetBytes(ModData.strError + "Player SteamID error.");
                                MyAPIGateway.Multiplayer.SendMessageTo(ModData.NET_MSG_ID, btTestError, senderPlayerId, true);
                            }
                            break;
                        default:
                            byte[] btError = ModData.encode.GetBytes(ModData.strError + "unknown command.");
                            MyAPIGateway.Multiplayer.SendMessageTo(ModData.NET_MSG_ID, btError, senderPlayerId, true);
                            break;
                    }
                }
                else // На клиенте - вывод сообщения от сервера.
                {
                    MyAPIGateway.Utilities.ShowMessage(ModData.strModName, strMsg);
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole(ModData.strLogPref + $"Error in receiving request! {ex}");
            }
        }
    }
}