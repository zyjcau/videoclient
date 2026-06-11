using System;
using System.Collections.Generic;
using System.Web.Security;
using EngineIOSharp.Common.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using SocketIOSharp.Client;
using SocketIOSharp.Common;
using VideoClient.Manager;
using VideoClient.Util;
using VidyoClient.Ext;

namespace VidyoClient
{
    public class TangoService
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private VideoManager _form;

        private VideoSocketService _socketService;

        public TangoService(VideoManager form)
        {
            _form = form;
            _socketService = form.socketService;

            if (Config.GetStringConfig("tango_use_https") == "true")
            {
                hostScheme = EngineIOScheme.https;
            }
            else
            {
                hostScheme = EngineIOScheme.http;
            }
        }

        private SocketIOClient imClient;
        public bool connected;
        private EngineIOScheme imHostScheme;
        private String imHost;
        private ushort imPort;

        private EngineIOScheme hostScheme = EngineIOScheme.https;
        private string host;
        private ushort port;


        public uint uid;
        private string platType = "windows";
        public string sessionJson;
        public bool loggedIn = false;

        

        /// <summary>
        /// 0 disconnect
        /// 1 connected
        /// -1 error
        /// </summary>
        private const string Sync_Connection_State = "sync_connection_state";

        private const string Sync_User_Fav_hd_List = "sync_user_fav_hd_list";

        public JObject Login(string serverUrl, string username, string password, string autoLogin)
        {
            string requestUrl = (hostScheme == EngineIOScheme.https ? "https" : "http") + "://" + serverUrl + "/login";
            Dictionary<string, string> headers = new Dictionary<string, string>();
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("un", username);
            parameters.Add("pwd", FormsAuthentication.HashPasswordForStoringInConfigFile(password, "MD5"));
            parameters.Add("platType", "windows");
            string json = Http.SendPost(requestUrl, headers, parameters);

            log("TangoLogin result -> " + json);

            JObject result = (JObject) JsonConvert.DeserializeObject(json);

            if (!String.IsNullOrEmpty(json))
            {
                if (result.Value<bool>("success"))
                {
                    Config.SetStringConfig("tango_server_url", serverUrl);
                    Config.SetStringConfig("tango_username", username);
                    Config.SetStringConfig("tango_password", password);
                    Config.SetStringConfig("tango_auto_login", autoLogin);

                    // QueryUserHardDeviceList(username);

                    InitSession(serverUrl, result);
                }
            }

            return result;
        }

        public void InitSession(string serverUrl, JObject result)
        {
            JToken data = result["data"];

            if (serverUrl.Contains(":"))
            {
                this.host = serverUrl.Substring(0, serverUrl.IndexOf(":"));
            }
            else
            {
                this.host = serverUrl;
            }

            this.sessionJson = JsonConvert.SerializeObject(data);
            this.loggedIn = true;

            JToken serverConfig = result["serverConfig"];
            if (null != serverConfig)
            {
                String ih = serverConfig.Value<String>("im_host");
                if (ih == "default")
                {
                    this.imHost = host;
                }
                else
                {
                    this.imHost = ih;
                }

                this.imPort = serverConfig.Value<ushort>("im_port");
                imHostScheme = (serverConfig.Value<bool>("im_enable_ssl") ? EngineIOScheme.https : EngineIOScheme.http);
            }
            else
            {
                log("not found serverConfig, use default parameters.");
                this.imHost = this.host;
                this.imPort = (ushort) Config.GetIntegerConfig("tango_im_port");
            }

            this.uid = data.Value<uint>("id");
            Connect(imHost, imPort, uid);
        }

        public void AutoTangoLogin()
        {
            string autoLogin = Config.GetStringConfig("tango_auto_login");
            string serverUrl = Config.GetStringConfig("tango_server_url");
            string username = Config.GetStringConfig("tango_username");
            string password = Config.GetStringConfig("tango_password");
            if ("true".Equals(autoLogin) &&
                !String.IsNullOrEmpty(serverUrl) &&
                !String.IsNullOrEmpty(username) &&
                !String.IsNullOrEmpty(password))
            {
                Login(serverUrl, username, password, autoLogin);
            }
        }

        public void Connect(string host, ushort port, uint uid)
        {
            if (null != imClient) return;

            this.host = host;
            this.port = port;
            this.uid = uid;

            log("start connect to " + imHostScheme + "://" + host + ":" + port + "?uid=" + uid + "&platType=" +
                platType);

            Dictionary<string, string> query = new Dictionary<string, string>();
            query.Add("uid", uid.ToString());
            query.Add("platType", platType);

            SocketIOClientOption option = new SocketIOClientOption(imHostScheme, host, port, Query: query);
            option.Reconnection = true;
            // SocketIOClientOption option = new SocketIOClientOption(scheme, host, port, Path: "?uid=" + uid + "&platType=" + platType);
            // SocketIOClientOption option = new SocketIOClientOption(scheme, host + "?uid=" + uid + "&platType=" + platType, port);

            imClient = new SocketIOClient(option);
            imClient.Connect();
            imClient.On(SocketIOEvent.CONNECTION, () =>
            {
                // _form.formTransparentLayer?.SetIMStatus("在线");
                connected = true;
                _socketService.SendToAll(Sync_Connection_State, 1);
                // _socketService.SendToAll(Sync_User_Fav_hd_List, userHardDeviceList);
                log("connect success to " + host + ":" + port + "?uid=" + uid);
                
                // _form.RefreshTextStatusBar();
            });
            imClient.On(SocketIOEvent.DISCONNECT, () =>
            {
                // _form.formTransparentLayer?.SetIMStatus("离线");
                connected = false;
                _socketService.SendToAll(Sync_Connection_State, 0);
                log("disconnect");
                
                // _form.RefreshTextStatusBar();
            });
            imClient.On(SocketIOEvent.ERROR, () =>
            {
                // _form.formTransparentLayer?.SetIMStatus("连接异常");
                connected = false;
                _socketService.SendToAll(Sync_Connection_State, -1);
                log("connect error");
            });
            imClient.On("reconnect", () =>
            {
                // _form.formTransparentLayer?.SetIMStatus("重连中...");
                log("reconnecting...");
            });
            imClient.On("ping", () =>
            {
                // imClient.Emit("pong");
                // imClient.Emit("ping");
                log("onPing");
            });
            imClient.On("pong", () => { log("onPong"); });
            imClient.On("heartbeat", () =>
            {
                imClient.Emit("heartbeat", "ack");
                // log("heartbeat");
            });

            imClient.On("sync_state_all", (Data) =>
            {
                if (Data != null && Data.Length > 0 && Data[0] != null)
                {
                    _socketService.SendToAll("sync_state_all", Data[0]);
                    // log("sync_state_all data -> " + Data[0].ToString());
                }
            });

            imClient.On("sync_state_single", (Data) =>
            {
                if (Data != null && Data.Length > 0 && Data[0] != null)
                {
                    _socketService.SendToAll("sync_state_single", Data[0]);
                    // log("sync_state_single data -> " + Data[0].ToString());
                }
            });

            imClient.On("sync_constraint", (Data) =>
            {
                if (Data != null && Data.Length > 0 && Data[0] != null)
                {
                    _socketService.SendToAll("sync_constraint", Data[0]);
                    // log("sync_constraint data -> " + Data[0].ToString());
                }
            });

            imClient.On("message", (Data) =>
            {
                if (Data != null && Data.Length > 0 && Data[0] != null)
                {
                    _socketService.SendToAll("message", Data[0]);
                    // log("message data -> " + Data[0].ToString());
                }
            });

            imClient.On("sync_client_version", (Data) =>
            {
                if (Data != null && Data.Length > 0 && Data[0] != null)
                {
                    _socketService.SendToAll("sync_client_version", Data[0]);
                    //TODO 下载客户端
                }
            });
        }

        public void SendMessage(string json)
        {
            if (IsConnected())
            {
                imClient.Emit("message", json);
                // imClient.Emit("message", JsonConvert.DeserializeObject(json));
            }
        }

        public void SendSync(string json)
        {
            if (IsConnected())
            {
                imClient.Emit("sync", json);
                // imClient.Emit("sync", JsonConvert.DeserializeObject(json));
            }
        }

        public void Disconnect()
        {
            if (null != imClient)
            {
                imClient.Dispose();
                imClient = null;
            }

            this.sessionJson = null;
            this.loggedIn = false;

            // ClearUserHardDeviceList();
        }

        public bool IsConnected()
        {
            return connected;
        }

        public String GetStatusString()
        {
            return (loggedIn ? "已登录" : "未登录") + "（" + (connected ? "已连接" : "已断开") + "）";
        }
        
        public void log(string content)
        {
            // _logger.Info("[SipContactsManager] " + content);
        }
        
    }
}