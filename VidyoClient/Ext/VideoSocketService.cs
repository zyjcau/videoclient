using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using SocketIOSharp.Common;
using SocketIOSharp.Server;
using SocketIOSharp.Server.Client;
using VideoClient.Entity;
using VideoClient.Manager;
using VideoClient.UI;
using VidyoClient.Ext;

namespace VidyoClient.Ext
{
    public class VideoSocketService
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        VideoManager _videoManager;
        private SocketIOServer server;

        private ConcurrentDictionary<int, SocketIOSocket> _sockets = new ConcurrentDictionary<int, SocketIOSocket>();

        public VideoSocketService(VideoManager videoManager, ushort port)
        {
            this._videoManager = videoManager;
            server = new SocketIOServer(new SocketIOServerOption(port));
            Init();
        }

        private const string EventVidyoConnectionStatusUpdated = "vidyo_connection_status_updated";
        private const string EventSystemStatusUpdated = "system_status_updated";
        private const string EventModerationCommandReceived = "moderation_command_received";
        private const string EventVideoSourcesUpdated = "video_sources_updated";
        private const string EventLayoutStatusUpdated = "layout_status_updated";
        private const string EventFunctionAvailableUpdated = "function_available_updated"; //功能可用性，比如发送辅流
        private const string EventFunctionStateUpdated = "function_state_updated";
        private const string EventChatMessageReceived = "chat_message_received";

        private const string EventParticipantUpdated = "participant_updated";
        private const string EventParticipantOnAdded = "participant_onadded";
        private const string EventParticipantOnRemoved = "participant_onremoved";

        private void Init()
        {
            server.OnConnection((socket) =>
            {
                Log("Socket Client Connected.(" + socket.GetHashCode() + ")");

                _sockets.TryAdd(socket.GetHashCode(), socket);

                Send(socket, EventSystemStatusUpdated, _videoManager.GetSystemStatusJson());
                if (_videoManager.isConnected)
                {
                    Send(socket, EventVideoSourcesUpdated, _videoManager.GetVideoSourcesJson());
                    Send(socket, EventParticipantUpdated, _videoManager.participantManager.GetParticipantListJson());
                }

                socket.On("getSystemStatus", (Data) =>
                {
                    foreach (JToken Token in Data)
                    {
                        // Console.Write(Token + " ");
                    }

                    // Console.WriteLine();
                    socket.Emit("systemStatus", _videoManager.GetSystemStatusJson());
                });

                socket.On("addUserHardDeviceInfo", (Data) =>
                {
                    foreach (JToken Token in Data)
                    {
                        // Console.Write(Token + " ");
                    }

                    // Console.WriteLine();
                    Log("received addUserHardDeviceInfo,->" + Data[0]);
                });

                socket.On(SocketIOEvent.DISCONNECT, () =>
                {
                    Log("Socket Client Disconnected.(" + socket.GetHashCode() + ")");
                    if (_sockets.ContainsKey(socket.GetHashCode()))
                    {
                        _sockets.TryRemove(socket.GetHashCode(), out socket);
                    }
                });
            });
        }

        public void Send(SocketIOSocket socket, string ev, string data)
        {
            if (null != socket)
            {
                socket.Emit(ev, data);
            }
        }

        public void Send(SocketIOSocket socket, string ev, Object data)
        {
            if (null != socket)
            {
                socket.Emit(ev, data);
            }
        }

        public void SendToAll(string ev, string data)
        {
            foreach (KeyValuePair<int, SocketIOSocket> kv in _sockets)
            {
                Send(kv.Value, ev, data);
            }
        }

        public void SendToAll(string ev, Object data)
        {
            foreach (KeyValuePair<int, SocketIOSocket> kv in _sockets)
            {
                Send(kv.Value, ev, data);
            }
        }

        public void SendVidyoConnectionStatusEventToAll(int statusCode)
        {
            SendToAll(EventVidyoConnectionStatusUpdated, statusCode);
        }

        public void SendSystemStatusUpdatedEventToAll()
        {
            SendToAll(EventSystemStatusUpdated, _videoManager.GetSystemStatusJson());
        }

        public void SendModerationCommandEventToAll(
            Device.DeviceType deviceType,
            Room.RoomModerationType moderationType,
            bool state)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"code\":0,\"data\":");

            sb.Append("{");
            sb.Append("\"deviceType\":\"").Append(deviceType).Append("\",");
            sb.Append("\"moderationType\":\"").Append(moderationType).Append("\",");
            sb.Append("\"state\":").Append(state ? "true" : "false");
            sb.Append("}");

            sb.Append("}");
            SendToAll(EventModerationCommandReceived, sb.ToString());
        }

        public void SendVideoSourcesUpdatedEventToAll()
        {
            SendToAll(EventVideoSourcesUpdated, _videoManager.GetVideoSourcesJson());
        }

        public void SendLayoutStatusEventToAll(String json)
        {
            SendToAll(EventLayoutStatusUpdated, json);
        }

        public void SendFunctionAvailableEventToAll(bool sendMinorStream)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"sendMinorStream\":").Append(sendMinorStream ? "true" : "false");
            sb.Append("}");
            String json = "{\"code\":" + 0 + ",\"data\":" + sb + "}";
            SendToAll(EventFunctionAvailableUpdated, json);
        }

        public void SendFunctionStateEventToAll(bool recordingStateUpdated)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"recordingStateUpdated\":").Append(recordingStateUpdated ? "true" : "false");
            sb.Append("}");
            String json = "{\"code\":" + 0 + ",\"data\":" + sb + "}";
            SendToAll(EventFunctionStateUpdated, json);
        }

        public void SendChatMessageEventToAll(ChatMessageObj chatMessage)
        {
            string json = JsonConvert.SerializeObject(chatMessage);
            SendToAll(EventChatMessageReceived, json);
        }

        public void SendParticipantUpdatedEventToAll()
        {
            string json = _videoManager.participantManager.GetParticipantListJson();
            Log("GetParticipantListJson -> " + json);
            SendToAll(EventParticipantUpdated, json);
        }

        public void SendParticipantOnAddedEventToAll(VideoParticipant participant)
        {
            SendToAll(EventParticipantOnAdded, participant);
        }

        public void SendParticipantOnRemovedEventToAll(VideoParticipant participant)
        {
            SendToAll(EventParticipantOnRemoved, participant);
        }

        public void Start()
        {
            server.Start();
        }

        public void Dispose()
        {
            server.Dispose();
        }

        private void Log(string content)
        {
            _logger.Info("[VideoSocketService] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[VideoSocketService] " + content, args);
        }
    }
}