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

namespace SETestMod
{
    // Класс общих данных мода.
    public class ModData
    {
        public const ushort NET_ID = 14038; // Идентификатор сообщений этого мода.
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

    // Класс сессии для мода.
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ModSession : MySessionComponentBase
    {
        // Строки команд.
        private const string strCmdTest = "test"; // Команда тестирования.

        // Подключение инициализации сессии для мода до старта (на клиенте и сервере).
        public override void BeforeStart()
        {
            try
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(ModData.NET_ID, ReceivedMessage); // Регистрация обработчика сообщений.
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
                MyAPIGateway.Multiplayer?.UnregisterSecureMessageHandler(ModData.NET_ID, ReceivedMessage); // Удаление регистрации сообщений.
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
                    MyAPIGateway.Multiplayer.SendMessageToServer(ModData.NET_ID, btMsg, true);
                }
                else if(strMsg == "/tm") // Показ помощи.
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
                if (ModData.IsServer) // На сервере - ответ о полученном сообщении обратно клиенту.
                {
                    string strResponse = $"User ID={senderPlayerId} sent [{strMsg}] to the server.";
                    MyLog.Default.WriteLineAndConsole(ModData.strLogPref + strResponse);
                    var strCmd = strMsg.Split(' ').FirstOrDefault(); // :( Странно ругается компилятор в игре.
                    switch (strCmd)
                    {
                        case strCmdTest:
                            byte[] btTestAccept = ModData.encode.GetBytes("Start testing.");
                            MyAPIGateway.Multiplayer.SendMessageTo(ModData.NET_ID, btTestAccept, senderPlayerId, true);
                            break;
                        default:
                            byte[] btError = ModData.encode.GetBytes(ModData.strError + "unknown command.");
                            MyAPIGateway.Multiplayer.SendMessageTo(ModData.NET_ID, btError, senderPlayerId, true);
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