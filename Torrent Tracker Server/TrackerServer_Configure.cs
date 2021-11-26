using System;
using System.IO;
using Utf8Json;

namespace Tracker_Server
{
    public class TrackerServer_Configure
    {
        //Default Value

        //Tracker Server Worker Thread Count. if 0, Default => [ CPU Cores * 2 ]
        static ushort Tracker_Server_WorkerThreadCount = 0;
        public static ushort WorkerThreadCount
        {
            get { return Tracker_Server_WorkerThreadCount;}
        }

        //WebServer & HTTP TrackerServer Listening Port
        static ushort Web_And_Http_Tracker_Listen_PORT_NUM = 5000;
        public static ushort Web_And_Http_Listen_PORT
        {
            get { return Web_And_Http_Tracker_Listen_PORT_NUM; }
        }

        //UDP TrackerServer Listening Port
        static ushort UDP_Tracker_Listen_PORT_NUM = 8081;
        public static ushort UDP_Listen_PORT
        {
            get { return UDP_Tracker_Listen_PORT_NUM; }
        }

        //Websocket TrackerServer Listening Port
        static ushort Websocket_Tracker_Listen_PORT_NUM = 8081;
        public static ushort Websocket_Listen_PORT
        {
            get { return Websocket_Tracker_Listen_PORT_NUM; }
        }

        //TrackerServer Maximum Connect Peer Count
        static int Tracker_MaxNumWant = 100;
        public static int MaxNumWant
        {
            get { return Tracker_MaxNumWant; }
        }

        //TrackerServer Announce Interval Time (Second)
        static int Tracker_Interval = 180;
        public static int Interval
        {
            get { return Tracker_Interval; }
        }

        //TrackerServer Announce Interval Time (Second)
        static int WebTracker_Interval = 60;
        public static int Interval_Web
        {
            get { return WebTracker_Interval; }
        }

        //TrackerServer Announce Interval Time (Second)
        static int WebTracker_Min_Interval = 30;
        public static int Min_Interval_Web
        {
            get { return WebTracker_Min_Interval; }
        }

        //string TelegramRequestURL_Example = 
        // $"https://api.telegram.org/{TelegramBot_API_Key}/sendmessage" +
        // $"?chat_id={TelegramBot_TargetChatID}" +
        // $"&parse_mode={TelegramBot_parse_mode}" +
        // $"&disable_web_page_preview={TelegramBot_disable_web_page_preview}" +
        // $"&text=";

        //Telegram Bot API Parameters.
        static bool TelegramBot_Enable = false;
        public static bool Telegram_Enable
        {
            get { return TelegramBot_Enable; }
        }

        static string TelegramBot_API_Key ="";
        public static string Telegram_API_Key
        {
            get { return TelegramBot_API_Key; }
        }

        static string TelegramBot_TargetChatID ="";
        public static string Telegram_TargetChatID
        {
            get { return TelegramBot_TargetChatID; }
        }

        static string TelegramBot_parse_mode ="markdown";
        public static string Telegram_parse_mode
        {
            get { return TelegramBot_parse_mode; }
        }

        static bool TelegramBot_disable_web_page_preview = true;
        public static bool Telegram_disable_web_page_preview
        {
            get { return TelegramBot_disable_web_page_preview; }
        }
        
        public static void LoadServerConfigFromFile()
        {
            try
            {
                var currentDir = Directory.GetCurrentDirectory();

                var configFileName = "serverConfig.json";

                var configFilePath = Path.Combine(currentDir, configFileName);

                Console.WriteLine($"\n======================================\nStart Load ConfigFile From: \n{configFilePath}\n");

                if (!Directory.Exists(currentDir))
                    Directory.CreateDirectory(currentDir);

                if (!File.Exists(configFilePath))
                {
                    Console.WriteLine($"ConfigFile Not Found.");
                    saveConfigFile(configFilePath);
                    Console.WriteLine($"Create Default ConfigFile.\n");
                }

                byte[] data = File.ReadAllBytes(configFilePath);

                dynamic config = JsonSerializer.Deserialize<dynamic>(data);

                ushort var1 = 0;
                if (config.ContainsKey("Tracker_Server_WorkerThreadCount")
                    && ushort.TryParse(config["Tracker_Server_WorkerThreadCount"].ToString(), out var1))
                {
                    Tracker_Server_WorkerThreadCount = var1;
                    Console.WriteLine($"Tracker_Server_WorkerThreadCount, \nSet: {Tracker_Server_WorkerThreadCount}\n");
                }
                else
                {
                    Console.WriteLine($"invalid Tracker_Server_WorkerThreadCount! \nSet Default: {Tracker_Server_WorkerThreadCount}\n");
                }

                ushort var2 = 0;
                if (config.ContainsKey("Web_And_Http_Tracker_Listen_PORT_NUM")
                    && ushort.TryParse(config["Web_And_Http_Tracker_Listen_PORT_NUM"].ToString(), out var2))
                {
                    Web_And_Http_Tracker_Listen_PORT_NUM = var2;
                    Console.WriteLine($"Web_And_Http_Tracker_Listen_PORT_NUM, \nSet: {Web_And_Http_Tracker_Listen_PORT_NUM}\n");
                }
                else
                {
                    Console.WriteLine($"invalid Web_And_Http_Tracker_Listen_PORT_NUM! \nSet Default: {Web_And_Http_Tracker_Listen_PORT_NUM}\n");
                }

                ushort var3 = 0;
                if (config.ContainsKey("UDP_Tracker_Listen_PORT_NUM")
                    && ushort.TryParse(config["UDP_Tracker_Listen_PORT_NUM"].ToString(), out var3))
                {
                    UDP_Tracker_Listen_PORT_NUM = var3;
                    Console.WriteLine($"UDP_Tracker_Listen_PORT_NUM, \nSet: {UDP_Tracker_Listen_PORT_NUM}\n");
                }
                else
                {
                    Console.WriteLine($"invalid UDP_Tracker_Listen_PORT_NUM! \nSet Default: {UDP_Tracker_Listen_PORT_NUM}\n");
                }

                ushort var4 = 0;
                if (config.ContainsKey("Websocket_Tracker_Listen_PORT_NUM")
                    && ushort.TryParse(config["Websocket_Tracker_Listen_PORT_NUM"].ToString(), out var4))
                {
                    Websocket_Tracker_Listen_PORT_NUM = var4;
                    Console.WriteLine($"Websocket_Tracker_Listen_PORT_NUM, \nSet: {Websocket_Tracker_Listen_PORT_NUM}\n");
                }
                else
                {
                    Console.WriteLine($"invalid Websocket_Tracker_Listen_PORT_NUM! \nSet Default: {Websocket_Tracker_Listen_PORT_NUM}\n");
                }

                int var5 = 0;
                if (config.ContainsKey("Tracker_MaxNumWant")
                    && int.TryParse(config["Tracker_MaxNumWant"].ToString(), out var5))
                {
                    Tracker_MaxNumWant = var5;
                    Console.WriteLine($"Tracker_MaxNumWant, \nSet: {Tracker_MaxNumWant}\n");
                }
                else
                {
                    Console.WriteLine($"invalid Tracker_MaxNumWant! \nSet Default: {Tracker_MaxNumWant}\n");
                }

                int var6 = 0;
                if (config.ContainsKey("Tracker_Interval")
                    && int.TryParse(config["Tracker_Interval"].ToString(), out var6))
                {
                    Tracker_Interval = var6;
                    Console.WriteLine($"Tracker_Interval, \nSet: {Tracker_Interval}\n");
                }
                else
                {
                    Console.WriteLine($"invalid Tracker_Interval! \nSet Default: {Tracker_Interval}\n");
                }

                int var7 = 0;
                if (config.ContainsKey("WebTracker_Interval")
                    && int.TryParse(config["WebTracker_Interval"].ToString(), out var7))
                {
                    WebTracker_Interval = var7;
                    Console.WriteLine($"WebTracker_Interval, \nSet: {WebTracker_Interval}\n");
                }
                else
                {
                    Console.WriteLine($"invalid WebTracker_Interval! \nSet Default: {WebTracker_Interval}\n");
                }

                int var8 = 0;
                if (config.ContainsKey("WebTracker_Min_Interval")
                    && int.TryParse(config["WebTracker_Min_Interval"].ToString(), out var8))
                {
                    WebTracker_Min_Interval = var8;
                    Console.WriteLine($"WebTracker_Min_Interval, \nSet: {WebTracker_Min_Interval}\n");
                }
                else
                {
                    Console.WriteLine($"invalid WebTracker_Min_Interval! \nSet Default: {WebTracker_Min_Interval}\n");
                }

                bool var9 = false;
                if (config.ContainsKey("TelegramBot_Enable")
                    && bool.TryParse(config["TelegramBot_Enable"].ToString(), out var9))
                {
                    TelegramBot_Enable = var9;
                    Console.WriteLine($"TelegramBot_Enable, \nSet: {TelegramBot_Enable}\n");
                }
                else
                {
                    Console.WriteLine($"invalid TelegramBot_Enable! \nSet Default: {TelegramBot_Enable}\n");
                }

                if (config.ContainsKey("TelegramBot_API_Key"))
                {
                    TelegramBot_API_Key = config["TelegramBot_API_Key"];
                    Console.WriteLine($"TelegramBot_API_Key, \nSet: {TelegramBot_API_Key}\n");
                }
                else
                {
                    Console.WriteLine($"No Parameter TelegramBot_API_Key in config File! \nSet Default: {TelegramBot_API_Key}\n");
                }

                if (config.ContainsKey("TelegramBot_TargetChatID"))
                {
                    TelegramBot_TargetChatID = config["TelegramBot_TargetChatID"];
                    Console.WriteLine($"TelegramBot_TargetChatID, \nSet: {TelegramBot_TargetChatID}\n");
                }
                else
                {
                    Console.WriteLine($"No Parameter TelegramBot_TargetChatID in config File! \nSet Default: {TelegramBot_TargetChatID}\n");
                }

                // if (config.ContainsKey("TelegramBot_parse_mode"))
                // {
                //     TelegramBot_parse_mode = config["TelegramBot_parse_mode"];
                //     Console.WriteLine($"TelegramBot_parse_mode, \nSet: {TelegramBot_parse_mode}\n");
                // }
                // else
                // {
                //     Console.WriteLine($"No Parameter TelegramBot_parse_mode in config File! \nSet Default: {TelegramBot_parse_mode}\n");
                // }

                bool var0 = false;
                if (config.ContainsKey("TelegramBot_disable_web_page_preview")
                    && bool.TryParse(config["TelegramBot_disable_web_page_preview"].ToString(), out var0))
                {
                    TelegramBot_disable_web_page_preview = var0;
                    Console.WriteLine($"TelegramBot_disable_web_page_preview, \nSet: {TelegramBot_disable_web_page_preview}\n");
                }
                else
                {
                    Console.WriteLine($"invalid TelegramBot_disable_web_page_preview! \nSet Default: {TelegramBot_disable_web_page_preview}\n");
                }

                //overwrite config file.
                saveConfigFile(configFilePath);

                Console.WriteLine($"Apply & Update ConfigFile. \n======================================\n");
            }
            catch (Exception e)
            {
                Console.WriteLine($"LoadServerConfigFromFile() Exception:{e}");
            }
        }


        static void saveConfigFile(string configFilePath)
        {
            //SetDefault Value
            JsonObject configObj = new JsonObject();
            configObj.Add("Tracker_Server_WorkerThreadCount", Tracker_Server_WorkerThreadCount);
            configObj.Add("Web_And_Http_Tracker_Listen_PORT_NUM", Web_And_Http_Tracker_Listen_PORT_NUM);
            configObj.Add("UDP_Tracker_Listen_PORT_NUM", UDP_Tracker_Listen_PORT_NUM);
            configObj.Add("Websocket_Tracker_Listen_PORT_NUM", Websocket_Tracker_Listen_PORT_NUM);
            configObj.Add("Tracker_MaxNumWant", Tracker_MaxNumWant);
            configObj.Add("Tracker_Interval", Tracker_Interval);
            configObj.Add("WebTracker_Interval", WebTracker_Interval);
            configObj.Add("WebTracker_Min_Interval", WebTracker_Min_Interval);

            configObj.Add("TelegramBot_Enable", TelegramBot_Enable);
            configObj.Add("TelegramBot_API_Key", TelegramBot_API_Key);
            configObj.Add("TelegramBot_TargetChatID", TelegramBot_TargetChatID);
            //configObj.Add("TelegramBot_parse_mode", TelegramBot_parse_mode);
            configObj.Add("TelegramBot_disable_web_page_preview", TelegramBot_disable_web_page_preview);

            //Create Default Config File.
            var configData = JsonSerializer.Serialize(configObj);
            File.WriteAllBytes(configFilePath, JsonSerializer.PrettyPrintByteArray(configData));
        }
    }
}



