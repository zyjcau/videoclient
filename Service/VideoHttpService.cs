using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using EventBus;
using HTTPServerLib;
using NLog;
using VideoClient.Manager;
using VideoClient.UI;
using VideoClient.Util;
using VideoClient.VidyoClient.Ext;

namespace VidyoClient
{
    public class VideoHttpService : HttpServer
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly VideoManager _videoManager;

        private readonly Dictionary<string, Delegate> _interfaces = new Dictionary<string, Delegate>();

        public VideoHttpService(VideoManager videoManager, string ip, int port) : base(ip, port)
        {
            _videoManager = videoManager;

            //初始化http接口
            _interfaces.Add("openFolder", new Action<HttpRequest, HttpResponse>(OpenFolder));
            _interfaces.Add("startProcess", new Action<HttpRequest, HttpResponse>(StartProcess));
            _interfaces.Add("getVersion", new Action<HttpRequest, HttpResponse>(GetVersion));
            _interfaces.Add("saveConfig", new Action<HttpRequest, HttpResponse>(SaveConfig));

            _interfaces.Add("join", new Action<HttpRequest, HttpResponse>(Join));
            _interfaces.Add("leave", new Action<HttpRequest, HttpResponse>(Leave));

            _interfaces.Add("openCefWindow", new Action<HttpRequest, HttpResponse>(OpenCefWindow));
            _interfaces.Add("openTangoDrawWindow", new Action<HttpRequest, HttpResponse>(OpenTangoDrawWindow));
            _interfaces.Add("openStartShareWindow", new Action<HttpRequest, HttpResponse>(OpenStartShareWindow));
            _interfaces.Add("openSettingsWindow", new Action<HttpRequest, HttpResponse>(OpenSettingsWindow));
            _interfaces.Add("openAVCheckWindow", new Action<HttpRequest, HttpResponse>(OpenAVCheckWindow));

            _interfaces.Add("setVideoLocationSize", new Action<HttpRequest, HttpResponse>(SetVideoLocationSize));
            _interfaces.Add("setVideoVisible", new Action<HttpRequest, HttpResponse>(SetVideoVisible));

            _interfaces.Add("setMicPrivacy", new Action<HttpRequest, HttpResponse>(SetMicPrivacy));
            _interfaces.Add("setSpeakerPrivacy", new Action<HttpRequest, HttpResponse>(SetSpeakerPrivacy));
            _interfaces.Add("setCameraPrivacy", new Action<HttpRequest, HttpResponse>(SetCameraPrivacy));
            _interfaces.Add("listCamera", new Action<HttpRequest, HttpResponse>(ListCamera));
            _interfaces.Add("configCamera", new Action<HttpRequest, HttpResponse>(ConfigCamera));
            _interfaces.Add("assignCamera", new Action<HttpRequest, HttpResponse>(AssignCamera));

            _interfaces.Add("assignMicrophone", new Action<HttpRequest, HttpResponse>(AssignMicrophone));
            _interfaces.Add("assignSpeaker", new Action<HttpRequest, HttpResponse>(AssignSpeaker));

            _interfaces.Add("getIODeviceState", new Action<HttpRequest, HttpResponse>(GetIODeviceState));
            _interfaces.Add("getSystemStatus", new Action<HttpRequest, HttpResponse>(GetSystemStatus));
            _interfaces.Add("sendPost", new Action<HttpRequest, HttpResponse>(SendPost));
            _interfaces.Add("sendPostXml", new Action<HttpRequest, HttpResponse>(SendPostXml));
            _interfaces.Add("sendChatMessage", new Action<HttpRequest, HttpResponse>(SendChatMessage));

            _interfaces.Add("setTitle", new Action<HttpRequest, HttpResponse>(SetTitle));
            _interfaces.Add("setDebugEnable", new Action<HttpRequest, HttpResponse>(SetDebugEnable));
            // interfaces.Add("changeScreenMode", new Action<HttpRequest, HttpResponse>(ChangeScreenMode));
            _interfaces.Add("setScreenIndexDisplay", new Action<HttpRequest, HttpResponse>(SetScreenIndexDisplay));
            _interfaces.Add("setLayoutMode", new Action<HttpRequest, HttpResponse>(SetLayoutMode));
            _interfaces.Add("setCustomLayoutMode", new Action<HttpRequest, HttpResponse>(SetCustomLayoutMode));

            _interfaces.Add("getVideoSources", new Action<HttpRequest, HttpResponse>(GetVideoSources));
            _interfaces.Add("startRendering", new Action<HttpRequest, HttpResponse>(StartRendering));
            _interfaces.Add("stopRendering", new Action<HttpRequest, HttpResponse>(StopRendering));
            _interfaces.Add("setAutoAssignRenderer", new Action<HttpRequest, HttpResponse>(SetAutoAssignRenderer));
            _interfaces.Add("setRendererContainerForceLayoutNum",
                new Action<HttpRequest, HttpResponse>(SetRendererContainerForceLayoutNum));

            _interfaces.Add("setAutoAnswer", new Action<HttpRequest, HttpResponse>(SetAutoAnswer));
            _interfaces.Add("saveTangoLoginInfo", new Action<HttpRequest, HttpResponse>(SaveTangoLoginInfo));
            // interfaces.Add("connectToTangoIM", new Action<HttpRequest, HttpResponse>(ConnectToTangoIM));
            // interfaces.Add("disconnectFromTangoIM", new Action<HttpRequest, HttpResponse>(DisconnectFromTangoIM));
            // interfaces.Add("sendMessageByTangoIM", new Action<HttpRequest, HttpResponse>(SendMessageByTangoIM));
            // interfaces.Add("sendSyncByTangoIM", new Action<HttpRequest, HttpResponse>(SendSyncByTangoIM));
            // interfaces.Add("tangoLogin", new Action<HttpRequest, HttpResponse>(TangoLogin));
            _interfaces.Add("addUserHardDeviceInfo", new Action<HttpRequest, HttpResponse>(AddUserHardDeviceInfo));
            _interfaces.Add("deleteUserHardDeviceInfo",
                new Action<HttpRequest, HttpResponse>(DeleteUserHardDeviceInfo));

            _interfaces.Add("getOSFile", new Action<HttpRequest, HttpResponse>(GetOSFile));
            _interfaces.Add("copyToClipboard", new Action<HttpRequest, HttpResponse>(CopyToClipboard));
            _interfaces.Add("startUrl", new Action<HttpRequest, HttpResponse>(StartUrl));
            _interfaces.Add("showDialog", new Action<HttpRequest, HttpResponse>(ShowDialog));
            _interfaces.Add("appActivate", new Action<HttpRequest, HttpResponse>(AppActivate));
            _interfaces.Add("appMinimize", new Action<HttpRequest, HttpResponse>(AppMinimize));
            _interfaces.Add("appExit", new Action<HttpRequest, HttpResponse>(AppExit));
        }

        public override void OnGet(HttpRequest request, HttpResponse response)
        {
            ///链接形式1:"http://localhost:4050/assets/styles/style.css"表示访问指定文件资源，
            ///此时读取服务器目录下的/assets/styles/style.css文件。

            ///链接形式1:"http://localhost:4050/assets/styles/"表示访问指定页面资源，
            ///此时读取服务器目录下的/assets/styles/style.index文件。

            //处理Get方法请求，否则处理文件请求
            if (HandleGetMethod(request, response)) return;

            //当文件不存在时应返回404状态码
            string requestURL = request.URL;
            requestURL = requestURL.Replace("/", @"\").Replace("\\..", "").TrimStart('\\');
            string requestFile = Path.Combine(ServerRoot, requestURL);

            //判断地址中是否存在扩展名
            string extension = Path.GetExtension(requestFile);

            //根据有无扩展名按照两种不同链接进行处
            if (extension != "")
            {
                //从文件中返回HTTP响应
                response = response.FromFile(requestFile);
            }
            else
            {
                //目录存在且不存在index页面时时列举目录
                if (Directory.Exists(requestFile) && !File.Exists(requestFile + "\\index.html"))
                {
                    requestFile = Path.Combine(ServerRoot, requestFile);
                    var content = ListDirectory(requestFile, requestURL);
                    response = response.SetContent(content, Encoding.UTF8);
                    response.Content_Type = "text/html; charset=UTF-8";
                }
                else
                {
                    //加载静态HTML页面
                    requestFile = Path.Combine(requestFile, "index.html");
                    response = response.FromFile(requestFile);
                    response.Content_Type = "text/html; charset=UTF-8";
                }
            }

            AddAllowCORSHeader(response);

            response.Send();
        }

        private void AddAllowCORSHeader(HttpResponse response)
        {
            response.SetHeader("Access-Control-Allow-Origin", "*");
            response.SetHeader("Access-Control-Allow-Credentials", "true");
            response.SetHeader("Access-Control-Allow-Methods", "GET,POST,OPTIONS,PUT,DELETE");
            response.SetHeader("Access-Control-Allow-Headers",
                "Content-Type,Content-Length,Accept-Encoding,X-Requested-with,Origin,X-Forwarded-Proto");
            response.SetHeader("X-Forwarded-Proto", "https");

            // response.Headers["Access-Control-Allow-Origin"] = "*";
            // response.Headers["Access-Control-Allow-Credentials"] = "true";
            // response.Headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS,PUT,DELETE";
            // response.Headers["Access-Control-Allow-Headers"] =
            //     "Content-Type,Content-Length,Accept-Encoding,X-Requested-with, Origin";
            // response.Headers["X-Forwarded-Proto"] = "https";
        }

        private bool HandleGetMethod(HttpRequest request, HttpResponse response)
        {
            int lastSeqIndex;
            if ((lastSeqIndex = request.URL.LastIndexOf("/", StringComparison.Ordinal)) != -1)
            {
                string methodUrl = request.URL.Substring(lastSeqIndex + 1);
                int questionIndex;
                if ((questionIndex = methodUrl.IndexOf("?", StringComparison.Ordinal)) != -1)
                {
                    string methodName = methodUrl.Substring(0, questionIndex);
                    if (_interfaces.TryGetValue(methodName, out var method))
                    {
                        response.Content_Encoding = "utf-8";
                        response.StatusCode = "200";
                        response.Content_Type = "text/html; charset=UTF-8";
                        AddAllowCORSHeader(response);
                        method.DynamicInvoke(request, response);
                        response.Send();
                        return true;
                    }
                }
            }

            return false;
        }

        public override void OnPost(HttpRequest request, HttpResponse response)
        {
            if (request == null || response == null)
            {
                return;
            }

            //构造响应报文
            //response.SetContent(content);
            response.Content_Encoding = "utf-8";
            response.StatusCode = "200";
            response.Content_Type = "text/html; charset=UTF-8";

            Dictionary<string, string> header = new Dictionary<string, string>();
            header["Server"] = "VideoService";
            header["Access-Control-Allow-Origin"] = "*";
            header["Access-Control-Allow-Credentials"] = "true";
            header["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS,PUT,DELETE";
            header["Access-Control-Allow-Headers"] =
                "Content-Type,Content-Length,Accept-Encoding,X-Requested-with, Origin";

            response.SetHeader("Server", "VideoService");
            request.SetHeader("Server", "VideoService");

            //解析调用方法名
            int lastSeqIndex = request.URL.LastIndexOf("/");

            string requestMethod = "unknown method";

            if (lastSeqIndex != -1)
            {
                requestMethod = request.URL.Substring(lastSeqIndex + 1);
            }

            //实现
            if (_interfaces.ContainsKey(requestMethod))
            {
                _interfaces[requestMethod].DynamicInvoke(request, response);
            }
            else
            {
                setResponse(response, -1, "unknown method");
            }

            //获取客户端传递的参数
            string data = request.Params == null
                ? ""
                : string.Join(";", request.Params.Select(x => x.Key + "=" + x.Value).ToArray());

            //设置返回信息
            string content = $"Post Method : {requestMethod}\r\nBody : \r\n{request.Body}";
            log(content);

            //发送响应
            response.Send();
        }

        public override void OnDefault(HttpRequest request, HttpResponse response)
        {
        }

        private string ConvertPath(string[] urls)
        {
            string html = string.Empty;
            int length = ServerRoot.Length;
            foreach (var url in urls)
            {
                var s = url.StartsWith("..") ? url : url.Substring(length).TrimEnd('\\');
                html += String.Format("<li><a href=\"{0}\">{0}</a></li>", s);
            }

            return html;
        }

        private string getParam(Dictionary<string, string> p, string key)
        {
            return null != p && p.ContainsKey(key) ? p[key] : null;
        }

        private string ListDirectory(string requestDirectory, string requestURL)
        {
            //列举子目录
            var folders = requestURL.Length > 1 ? new string[] { "../" } : new string[] { };
            folders = folders.Concat(Directory.GetDirectories(requestDirectory)).ToArray();
            var foldersList = ConvertPath(folders);

            //列举文件
            var files = Directory.GetFiles(requestDirectory);
            var filesList = ConvertPath(files);

            //构造HTML
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format("<html><head><title>{0}</title></head>", requestDirectory));
            builder.Append(string.Format("<body><h1>{0}</h1><br/><ul>{1}{2}</ul></body></html>",
                requestURL, filesList, foldersList));

            return builder.ToString();
        }

        private void setResponse(HttpResponse response, bool success)
        {
            setResponse(response, success ? 0 : -1, success ? "操作成功" : "操作失败");
        }

        private void setResponseData(HttpResponse response, int code, string data)
        {
            response.SetContent("{\"code\":" + code + ",\"data\":" + data + "}");
        }

        private void setResponse(HttpResponse response, int code, string msg)
        {
            response.SetContent("{\"code\":" + code + ",\"msg\":\"" + msg + "\"}");
        }

        //---------------------------------------------------------Http API------------------------------------------------------------------------------------

        private void OpenFolder(HttpRequest request, HttpResponse response)
        {
            string path = getParam(request.Params, "path");
            try
            {
                if (!String.IsNullOrEmpty(path))
                {
                    path = Http.UrlDecode(path);
                    Util.RunCmd($"explorer /e, {path}");
                    setResponse(response, true);
                }
                else
                {
                    setResponse(response, -1, "参数不能为空");
                }
            }
            catch (Exception e)
            {
                setResponse(response, -1, e.ToString());
            }
        }

        private void StartProcess(HttpRequest request, HttpResponse response)
        {
            string target = getParam(request.Params, "target");
            try
            {
                if (!String.IsNullOrEmpty(target))
                {
                    target = Http.UrlDecode(target);
                    Util.RunCmd($"start \"\" {target}");
                    setResponse(response, true);
                }
                else
                {
                    setResponse(response, -1, "参数不能为空");
                }
            }
            catch (Exception e)
            {
                setResponse(response, -1, e.ToString());
            }
        }

        private void GetVersion(HttpRequest request, HttpResponse response)
        {
            setResponseData(response, 0, "\"" + VideoManager.VERSION + "\"");
        }

        private void Join(HttpRequest request, HttpResponse response)
        {
            string portal = getParam(request.Params, "portal");
            string userName = getParam(request.Params, "userName");
            string password = getParam(request.Params, "password");
            string roomKey = getParam(request.Params, "roomKey");
            string roomName = getParam(request.Params, "roomName");
            string roomPin = getParam(request.Params, "roomPin");
            string displayName = getParam(request.Params, "displayName");

            if (String.IsNullOrEmpty(portal) || String.IsNullOrEmpty(roomKey) || String.IsNullOrEmpty(displayName))
            {
                setResponse(response, -1, "参数不能为空");
            }
            else
            {
                // userName = Http.UrlDecode(userName);
                _videoManager.Connect(
                    Http.UrlDecode(portal),
                    Http.UrlDecode(userName),
                    password,
                    roomKey,
                    Http.UrlDecode(roomName),
                    roomPin,
                    Http.UrlDecode(displayName));

                WaitHandle.WaitAll(new WaitHandle[] { _videoManager.CreateWaitHandle() }); //等待入会回调完成
                _videoManager.connectionEventHandle = null;

                StringBuilder connectionStatusJson = new StringBuilder();
                connectionStatusJson.Append("{");
                connectionStatusJson.Append("\"connectionStatus\":\"").Append(_videoManager.connectionStatus);
                connectionStatusJson.Append("\"}");
                setResponseData(response, _videoManager.isConnected ? 0 : -1,
                    connectionStatusJson.ToString());
            }
        }

        private void Leave(HttpRequest request, HttpResponse response)
        {
            setResponse(response, _videoManager.Disconnect());
        }

        private void OpenCefWindow(HttpRequest request, HttpResponse response)
        {
            string url = getParam(request.Params, "url");
            string title = getParam(request.Params, "title");
            if (!String.IsNullOrEmpty(url))
            {
                url = Http.UrlDecode(url);
                title = Http.UrlDecode(title);

                int id = new Random().Next(10, 9999);
                _videoManager.OpenCefWindow(id, url, title);

                setResponse(response, 0, "{\"id\":" + id + "}"); //返回窗口ID
            }
            else
            {
                setResponse(response, false);
            }
        }

        private void OpenTangoDrawWindow(HttpRequest request, HttpResponse response)
        {
            string url = getParam(request.Params, "url");
            string username = getParam(request.Params, "username");
            string room = getParam(request.Params, "room");
            string title = getParam(request.Params, "title");
            string isStarter = getParam(request.Params, "isStarter");
            if (!String.IsNullOrEmpty(url))
            {
                url = Http.UrlDecode(url);
                username = Http.UrlDecode(username);
                room = Http.UrlDecode(room);
                title = Http.UrlDecode(title);

                _videoManager.OpenTangoDrawWindow(url, username, room, title, string.Equals("true", isStarter));

                setResponse(response, true);
            }
            else
            {
                setResponse(response, false);
            }
        }

        private void OpenStartShareWindow(HttpRequest request, HttpResponse response)
        {
            _videoManager.OpenStartShareWindow();
            setResponse(response, true);
        }

        private void OpenSettingsWindow(HttpRequest request, HttpResponse response)
        {
            _videoManager.OpenSettingsWindow();
            setResponse(response, true);
        }

        private void OpenAVCheckWindow(HttpRequest request, HttpResponse response)
        {
            _videoManager.OpenAVCheckWindow();
            setResponse(response, true);
        }

        private void SetVideoLocationSize(HttpRequest request, HttpResponse response)
        {
            if (_videoManager.IsSmallWindowMode())
            {
                setResponse(response, -2, "当前已锁定为小窗模式，不允许操作视频大小和坐标！");
                return;
            }

            string x = getParam(request.Params, "x");
            string y = getParam(request.Params, "y");
            string width = getParam(request.Params, "width");
            string height = getParam(request.Params, "height");

            if (IsNumeric(x) && IsNumeric(y) && IsNumeric(width) && IsNumeric(height))
            {
                int intx = (int)float.Parse(x);
                int inty = (int)float.Parse(y);
                int intWidth = (int)float.Parse(width);
                int intHeight = (int)float.Parse(height);
                _videoManager.SetVideoLocationSize(intx, inty, intWidth, intHeight);
                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "参数错误");
            }
        }

        private void SetVideoVisible(HttpRequest request, HttpResponse response)
        {
            string visible = getParam(request.Params, "visible");

            if (!String.IsNullOrEmpty(visible))
            {
                _videoManager.SetVideoVisible("true".Equals(visible));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "参数错误");
            }
        }

        private void SetMicPrivacy(HttpRequest request, HttpResponse response)
        {
            string isPrivacy = getParam(request.Params, "isPrivacy");

            if (!String.IsNullOrEmpty(isPrivacy))
            {
                _videoManager.SetMicrophonePrivacy("true".Equals(isPrivacy));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "参数错误");
            }
        }

        private void SetSpeakerPrivacy(HttpRequest request, HttpResponse response)
        {
            string isPrivacy = getParam(request.Params, "isPrivacy");

            if (!String.IsNullOrEmpty(isPrivacy))
            {
                _videoManager.SetSpeakerPrivacy("true".Equals(isPrivacy));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "参数错误");
            }
        }

        private void SetCameraPrivacy(HttpRequest request, HttpResponse response)
        {
            string isPrivacy = getParam(request.Params, "isPrivacy");

            if (!String.IsNullOrEmpty(isPrivacy))
            {
                _videoManager.SetCameraPrivacy("true".Equals(isPrivacy));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "参数错误");
            }
        }

        public void ListCamera(HttpRequest request, HttpResponse response)
        {
            string camIndexStr = getParam(request.Params, "cameraIndex");

            if (IsNumeric(camIndexStr))
            {
                int cameraIndex = int.Parse(camIndexStr);

                string json = _videoManager.GetVideoDevicesListJson(cameraIndex);

                setResponseData(response, String.IsNullOrEmpty(json) ? -1 : 0, json);
            }
            else
            {
                setResponse(response, -1, "参数错误");
            }
        }

        public void AssignCamera(HttpRequest request, HttpResponse response)
        {
            string camIndex = getParam(request.Params, "cameraIndex");
            string camName = getParam(request.Params, "cameraName");

            if (IsNumeric(camIndex) && !String.IsNullOrEmpty(camName))
            {
                camName = Http.UrlDecode(camName);
                try
                {
                    int sourceType = int.Parse(camIndex);
                    _videoManager.AssignVideoDevice(sourceType, camName);
                }
                catch (Exception e)
                {
                    setResponse(response, false);
                    return;
                }

                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "参数错误");
            }
        }

        public void ConfigCamera(HttpRequest request, HttpResponse response)
        {
            string camIndexStr = getParam(request.Params, "cameraIndex");

            string resolution = getParam(request.Params, "resolution");
            string framerate = getParam(request.Params, "framerate");
            string resolutionProfile = getParam(request.Params, "resolutionProfile");
            string framerateProfile = getParam(request.Params, "framerateProfile");

            if (IsNumeric(camIndexStr) && IsNumeric(resolutionProfile) && IsNumeric(framerateProfile))
            {
                int cameraIndex = int.Parse(camIndexStr);
                int rp = int.Parse(resolutionProfile);
                int fp = int.Parse(framerateProfile);

                StringBuilder sb = new StringBuilder();
                if (SetCameraResolution(cameraIndex, resolution))
                {
                    sb.Append("设置摄像头" + cameraIndex + "分辨率成功。");
                }

                if (SetCameraFramerate(cameraIndex, framerate))
                {
                    sb.Append("设置摄像头" + cameraIndex + "帧率成功。");
                }

                if (rp > -1 && _videoManager.cameraManager.SetCameraResolutionProfile(cameraIndex, rp))
                {
                    sb.Append("设置摄像头" + cameraIndex + "分辨率策略成功。 value -> " + rp);
                }

                if (fp > -1 && _videoManager.cameraManager.SetCameraFramerateProfile(cameraIndex, fp))
                {
                    sb.Append("设置摄像头" + cameraIndex + "帧率策略成功。 value -> " + fp);
                }

                setResponse(response, sb.Length > 0 ? 0 : -1, sb.ToString());
            }
            else
            {
                setResponse(response, -1, "cameraIndex 参数错误");
            }
        }

        public void AssignMicrophone(HttpRequest request, HttpResponse response)
        {
            string micName = getParam(request.Params, "micName");

            if (!String.IsNullOrEmpty(micName))
            {
                micName = Http.UrlDecode(micName);

                _videoManager.microphoneManager.AssignDevice(micName);
                _videoManager.microphoneManager.SaveLastSelectedDevice(micName);

                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "参数错误");
            }
        }

        public void AssignSpeaker(HttpRequest request, HttpResponse response)
        {
            string speakerName = getParam(request.Params, "speakerName");

            if (!String.IsNullOrEmpty(speakerName))
            {
                speakerName = Http.UrlDecode(speakerName);

                _videoManager.speakerManager.AssignDevice(speakerName);
                _videoManager.speakerManager.SaveLastSelectedDevice(speakerName);

                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "参数错误");
            }
        }

        public void GetIODeviceState(HttpRequest request, HttpResponse response)
        {
            string statusJson = _videoManager.GetIODeviceStateJson();
            response.SetContent(statusJson);
        }

        public void GetSystemStatus(HttpRequest request, HttpResponse response)
        {
            string statusJson = _videoManager.GetSystemStatusJson();
            response.SetContent(statusJson);
        }

        public void SendPost(HttpRequest request, HttpResponse response)
        {
            string postUrl = getParam(request.Params, "url");
            if (!String.IsNullOrEmpty(postUrl))
            {
                setResponseData(response, 0, Http.SendPost(postUrl, request.Headers, request.Params));
            }
            else
            {
                setResponse(response, false);
            }
        }

        public void SendPostXml(HttpRequest request, HttpResponse response)
        {
            string postUrl = getParam(request.Params, "url");
            string postXml = getParam(request.Params, "data");
            if (!String.IsNullOrEmpty(postUrl) && !String.IsNullOrEmpty(postXml))
            {
                string result = Http.SendPostXml(postUrl, request.Headers, postXml);
                response.SetContent(result);
            }
            else
            {
                setResponse(response, -1, "请求sendPostXml失败，参数不能为空");
            }
        }

        public void SendChatMessage(HttpRequest request, HttpResponse response)
        {
            string message = getParam(request.Params, "message");
            if (!String.IsNullOrEmpty(message))
            {
                message = Http.UrlDecode(message);
                _videoManager.connector.SendChatMessage(message);
                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "请求sendChatMessage失败，message参数不能为空");
            }
        }

        public void SetTitle(HttpRequest request, HttpResponse response)
        {
            string title = getParam(request.Params, "title");
            if (!String.IsNullOrEmpty(title))
            {
                _videoManager.SetTitle(title);
                _videoManager.SetTitleVisible(true);
                setResponse(response, true);
            }
            else
            {
                setResponse(response, false);
            }
        }

        public void SetDebugEnable(HttpRequest request, HttpResponse response)
        {
            string enable = getParam(request.Params, "enable");
            if (!String.IsNullOrEmpty(enable))
            {
                _videoManager.SetDebugEnable("true".Equals(enable));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, false);
            }
        }

        // public void ChangeScreenMode(HttpRequest request, HttpResponse response)
        // {
        //     string mode = getParam(request.Params, "mode");
        //     if (!String.IsNullOrEmpty(mode))
        //     {
        //         if ("full".Equals(mode))
        //         {
        //             form.ChangeUiFullScreenMode();
        //         }
        //         else if ("small".Equals(mode))
        //         {
        //             form.ChangeUiSmallScreenMode();
        //         }
        //
        //         setResponse(response, true);
        //     }
        //     else
        //     {
        //         setResponse(response, false);
        //     }
        // }

        public void SetScreenIndexDisplay(HttpRequest request, HttpResponse response)
        {
            string visible = getParam(request.Params, "visible");
            if (!String.IsNullOrEmpty(visible))
            {
                _videoManager.layoutController.SetExtFormIndexDisplay("true".Equals(visible));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, false);
            }
        }

        /**
         * sdk内置布局或自定义布局
         */
        public void SetLayoutMode(HttpRequest request, HttpResponse response)
        {
            string mode = getParam(request.Params, "mode");
            if (!String.IsNullOrEmpty(mode))
            {
                _videoManager.layoutController.UseDefaultLayout("default".Equals(mode));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, false);
            }
        }

        public char SEPARATER_REMOTE_TYPE = '$';

        public void GetVideoSources(HttpRequest request, HttpResponse response)
        {
            if (!_videoManager.isConnected)
            {
                setResponse(response, -1, "调用失败，必须入会后才能调用");
                return;
            }

            string remoteListJson = _videoManager.GetVideoSourcesJson();

            response.SetContent(remoteListJson);
        }

        /**
         * 自定义布局下的布局方式
         * 默认布局：等分
         * 主讲人布局：画廊
         */
        public void SetCustomLayoutMode(HttpRequest request, HttpResponse response)
        {
            string layoutMode = getParam(request.Params, "layoutMode");
            string lecturerId = getParam(request.Params, "lecturerId");
            bool isByUser = String.Equals(getParam(request.Params, "isByUser"), "true");

            if (!String.IsNullOrEmpty(layoutMode) &&
                IsNumeric(layoutMode))
            {
                lock (_videoManager.layoutController._Lock)
                {
                    LayoutMode resultLayoutMode = LayoutMode.Mode_UNKNOWN;
                    int mode = int.Parse(layoutMode);
                    if (mode == (int)LayoutMode.MODE_NORMAL)
                    {
                        resultLayoutMode = LayoutMode.MODE_NORMAL;
                        _videoManager.layoutController.ChangeCustomizedLayoutModeOnMainWait(LayoutMode.MODE_NORMAL,
                            lecturerId);
                        setResponse(response, true);
                    }
                    else if (mode == (int)LayoutMode.MODE_LECTURE_FIXED_LECTURER)
                    {
                        if (!String.IsNullOrEmpty(lecturerId))
                        {
                            resultLayoutMode = LayoutMode.MODE_LECTURE_FIXED_LECTURER;
                            _videoManager.layoutController.ChangeCustomizedLayoutModeOnMainWait(
                                LayoutMode.MODE_LECTURE_FIXED_LECTURER,
                                lecturerId);
                            setResponse(response, true);
                        }
                        else
                        {
                            setResponse(response, -1, "设置主讲人布局失败，主讲人未知");
                        }
                    }
                    else if (mode == (int)LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER)
                    {
                        resultLayoutMode = LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER;
                        _videoManager.layoutController.ChangeCustomizedLayoutModeOnMainWait(
                            LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER,
                            lecturerId);
                        setResponse(response, true);
                    }
                    else
                    {
                        setResponse(response, -1, "切换模式失败，模式未知");
                    }

                    if (isByUser && resultLayoutMode != LayoutMode.Mode_UNKNOWN)
                    {
                        _videoManager.layoutController.SaveLayoutMode(resultLayoutMode);
                        _videoManager.layoutController.SaveLecturerId(_videoManager.layoutController.GetLecturerId());
                    }
                }
            }
            else
            {
                setResponse(response, false);
            }
        }

        public void StartRendering(HttpRequest request, HttpResponse response)
        {
            string sourceKey = getParam(request.Params, "sourceKey");
            string screenIndex = getParam(request.Params, "screenIndex"); //
            string rendererIndex = getParam(request.Params, "rendererIndex"); //
            if (!String.IsNullOrEmpty(sourceKey) && IsNumeric(screenIndex) && IsNumeric(rendererIndex))
            {
                sourceKey = Http.UrlDecode(sourceKey);
                bool result;
                if (int.Parse(screenIndex) < 0)
                {
                    lock (_videoManager.layoutController._Lock)
                    {
                        IAsyncResult asyncResult = _videoManager.layoutController.StartRenderingOnMain(sourceKey);
                        while (!asyncResult.IsCompleted)
                        {
                        }
                    }

                    result = true;
                }
                else
                {
                    if (int.Parse(rendererIndex) < 0)
                    {
                        lock (_videoManager.layoutController._Lock)
                        {
                            IAsyncResult asyncResult = _videoManager.layoutController.StartRenderingOnMain(
                                sourceKey,
                                int.Parse(screenIndex)
                            );
                            while (!asyncResult.IsCompleted)
                            {
                            }
                        }

                        result = true;
                    }
                    else
                    {
                        lock (_videoManager.layoutController._Lock)
                        {
                            IAsyncResult asyncResult = _videoManager.layoutController.StartRenderingOnMain(
                                sourceKey,
                                int.Parse(screenIndex),
                                int.Parse(rendererIndex)
                            );
                            while (!asyncResult.IsCompleted)
                            {
                            }
                        }

                        result = true;
                    }
                }

                setResponse(response, result);

                if (!result)
                {
                    _videoManager.socketService.SendVideoSourcesUpdatedEventToAll();
                }
            }
            else
            {
                setResponse(response, -1, "请检查参数");
            }
        }

        public void StopRendering(HttpRequest request, HttpResponse response)
        {
            string sourceKey = getParam(request.Params, "sourceKey");
            if (!String.IsNullOrEmpty(sourceKey))
            {
                string str = Http.UrlDecode(sourceKey);
                lock (_videoManager.layoutController._Lock)
                {
                    IAsyncResult asyncResult = _videoManager.layoutController.StopRenderingOnMain(str);
                    while (!asyncResult.IsCompleted)
                    {
                    }
                }

                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "sourceKey不能为空");
            }
        }

        public void SetAutoAssignRenderer(HttpRequest request, HttpResponse response)
        {
            string auto = getParam(request.Params, "auto");

            if (!String.IsNullOrEmpty(auto))
            {
                _videoManager.layoutController.SetAutoAssignRenderer("true".Equals(auto));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "参数错误");
            }
        }

        public void SetRendererContainerForceLayoutNum(HttpRequest request, HttpResponse response)
        {
            string containerIndex = getParam(request.Params, "containerIndex");
            string num = getParam(request.Params, "num");
            if (IsNumeric(containerIndex) && IsNumeric(num))
            {
                bool result = _videoManager.layoutController.SetRendererContainerForceLayoutNum(
                    int.Parse(containerIndex),
                    int.Parse(num));
                if (result)
                    _videoManager.layoutController.SaveScreenForceLayoutNum(int.Parse(containerIndex), int.Parse(num));
                setResponse(response, result ? 0 : -1, result ? "操作成功" : "请检查参数是否正确");
            }
            else
            {
                setResponse(response, -1, "sourceKey不能为空");
            }
        }

        public void SetAutoAnswer(HttpRequest request, HttpResponse response)
        {
            string auto = getParam(request.Params, "auto");

            if (!String.IsNullOrEmpty(auto))
            {
                _videoManager.SetAutoAnswer("true".Equals(auto));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "参数错误");
            }
        }

        public void SaveConfig(HttpRequest request, HttpResponse response)
        {
            string key = getParam(request.Params, "key");
            string value = getParam(request.Params, "value");
            if (!String.IsNullOrEmpty(key) && !String.IsNullOrEmpty(value))
            {
                key = Http.UrlDecode(key);
                value = Http.UrlDecode(value);
                Config.SetStringConfig(key, value);
                setResponse(response, true);
            }
            else
            {
                setResponse(response, false);
            }
        }

        public void SaveTangoLoginInfo(HttpRequest request, HttpResponse response)
        {
            string server = getParam(request.Params, "server");
            string username = getParam(request.Params, "username");
            string password = getParam(request.Params, "password");
            string isAutoLogin = getParam(request.Params, "isAutoLogin");
            string subsystemTangoDrawUrl = getParam(request.Params, "subsystemTangoDrawUrl");

            if (!String.IsNullOrEmpty(server))
            {
                server = Http.UrlDecode(server);
                Config.SetStringConfig("tango_server_url", server);
            }

            if (!String.IsNullOrEmpty(username))
            {
                Config.SetStringConfig("tango_username", username);
            }

            if (!String.IsNullOrEmpty(password))
            {
                Config.SetStringConfig("tango_password", password);
            }

            if (!String.IsNullOrEmpty(isAutoLogin))
            {
                Config.SetStringConfig("tango_auto_login", isAutoLogin);
            }

            if (!String.IsNullOrEmpty(subsystemTangoDrawUrl))
            {
                _videoManager.loginUsername = username;
                _videoManager.subsystemTangoDrawUrl = Http.UrlDecode(subsystemTangoDrawUrl);
            }

            setResponse(response, true);
        }

        // public void ConnectToTangoIM(HttpRequest request, HttpResponse response)
        // {
        //     string host = getParam(request.Params, "host");
        //     string port = getParam(request.Params, "port");
        //     string uid = getParam(request.Params, "uid");
        //     if (
        //         !String.IsNullOrEmpty(host) &&
        //         !String.IsNullOrEmpty(port) &&
        //         !String.IsNullOrEmpty(uid) &&
        //         IsNumeric(port) &&
        //         IsNumeric(uid)
        //     )
        //     {
        //         form.tangoService.Connect(host, ushort.Parse(port), uint.Parse(uid));
        //         setResponse(response, true);
        //     }
        //     else
        //     {
        //         setResponse(response, -1, "参数不能为空");
        //     }
        // }
        //
        // public void DisconnectFromTangoIM(HttpRequest request, HttpResponse response)
        // {
        //     form.tangoService.Disconnect();
        //     setResponse(response, true);
        // }
        //
        // public void SendMessageByTangoIM(HttpRequest request, HttpResponse response)
        // {
        //     string msg = getParam(request.Params, "msg");
        //     if (!string.IsNullOrEmpty(msg)) form.tangoService.SendMessage(Http.UrlDecode(msg));
        //     setResponse(response, !string.IsNullOrEmpty(msg));
        // }
        //
        // public void SendSyncByTangoIM(HttpRequest request, HttpResponse response)
        // {
        //     string msg = getParam(request.Params, "msg");
        //     if (!string.IsNullOrEmpty(msg)) form.tangoService.SendSync(Http.UrlDecode(msg));
        //     setResponse(response, !string.IsNullOrEmpty(msg));
        // }
        //
        // public void TangoLogin(HttpRequest request, HttpResponse response)
        // {
        //     string serverUrl = getParam(request.Params, "serverUrl");
        //     string username = getParam(request.Params, "username");
        //     string password = getParam(request.Params, "password");
        //     string autoLogin = getParam(request.Params, "autoLogin");
        //
        //     if (!String.IsNullOrEmpty(serverUrl) &&
        //         !String.IsNullOrEmpty(username) &&
        //         !String.IsNullOrEmpty(password))
        //     {
        //         serverUrl = Http.UrlDecode(serverUrl);
        //
        //         JObject result = form.tangoService.Login(serverUrl, username, password, autoLogin);
        //
        //         if (result != null)
        //         {
        //             response.SetContent(JsonConvert.SerializeObject(result));
        //         }
        //         else
        //         {
        //             setResponse(response, -1, "request tango login failed. result is null");
        //         }
        //     }
        //     else
        //     {
        //         setResponse(response, false);
        //     }
        // }

        public void AddUserHardDeviceInfo(HttpRequest request, HttpResponse response)
        {
            string name = getParam(request.Params, "name");
            string callNumber = getParam(request.Params, "callNumber");

            if (!String.IsNullOrEmpty(name) &&
                !String.IsNullOrEmpty(callNumber))
            {
                name = Http.UrlDecode(name);
                callNumber = Http.UrlDecode(callNumber);

                SipContactsManager.HardDevice hardDevice = new SipContactsManager.HardDevice();
                hardDevice.name = name;
                hardDevice.callNumber = callNumber;
                _videoManager._sipContactsManager.AddUserHardDeviceInfo(hardDevice);

                setResponse(response, true);
            }
            else
            {
                setResponse(response, false);
            }
        }

        public void DeleteUserHardDeviceInfo(HttpRequest request, HttpResponse response)
        {
            string name = getParam(request.Params, "name");
            string callNumber = getParam(request.Params, "callNumber");

            if (!String.IsNullOrEmpty(name) &&
                !String.IsNullOrEmpty(callNumber))
            {
                name = Http.UrlDecode(name);
                callNumber = Http.UrlDecode(callNumber);

                _videoManager._sipContactsManager.RemoveUserHardDeviceInfo(name);

                setResponse(response, true);
            }
            else
            {
                setResponse(response, false);
            }
        }

        private void GetOSFile(HttpRequest request, HttpResponse response)
        {
            string filePath = getParam(request.Params, "file");
            if (!String.IsNullOrEmpty(filePath))
            {
                // filePath = Http.UrlDecode(filePath);
                try
                {
                    //解决base64编码在url传递过程的特殊字符
                    filePath = filePath.Replace("$", "=");
                    filePath = filePath.Replace("|", "/");

                    byte[] bs = Convert.FromBase64String(filePath);
                    filePath = Encoding.UTF8.GetString(bs);
                }
                catch (Exception e)
                {
                    setResponse(response, -3, "参数异常！必须为Base64编码！");
                    return;
                }

                if (!File.Exists(filePath))
                {
                    setResponse(response, -1, "文件不存在");
                    return;
                }

                string fileName = Path.GetFileName(filePath);
                long fileLength = new FileInfo(filePath).Length;

                response.SetHeader("Content-Type", "application/x-msdownload");
                response.SetHeader("content-disposition", "attachment;filename=" + Http.UrlEncode(fileName));
                response.SetHeader("Content-Length", fileLength.ToString());

                response.FromFile(filePath);
                if (String.Equals(response.Content_Type, "image/pjpeg"))
                {
                    response.Content_Type = "image/jpeg";
                }
                else if (String.Equals(response.Content_Type, "image/x-png"))
                {
                    response.Content_Type = "image/png";
                }
            }
            else
            {
                setResponse(response, -2, "文件路径不能为空");
            }
        }

        public void CopyToClipboard(HttpRequest request, HttpResponse response)
        {
            string content = getParam(request.Params, "content");
            if (!String.IsNullOrEmpty(content))
            {
                content = Http.UrlDecode(content);
                _videoManager.RunOnUIThread((Action)(() => { Clipboard.SetDataObject(content); }));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "复制到剪切板失败，内容为空！");
            }
        }

        private void StartUrl(HttpRequest request, HttpResponse response)
        {
            string url = getParam(request.Params, "url");
            if (!String.IsNullOrEmpty(url))
            {
                url = Http.UrlDecode(url);
                _videoManager.RunOnUIThread((Action)(() => { Util.StartUrl(url); }));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "打开URL失败，URL不能为空！");
            }
        }

        private void ShowDialog(HttpRequest request, HttpResponse response)
        {
            string message = getParam(request.Params, "message");
            if (!String.IsNullOrEmpty(message))
            {
                message = Http.UrlDecode(message);
                _videoManager.RunOnUIThread((Action)(() => { MessageBox.Show(message); }));
                setResponse(response, true);
            }
            else
            {
                setResponse(response, -1, "打开对话框失败，内容不能为空！");
            }
        }

        private void AppActivate(HttpRequest request, HttpResponse response)
        {
            _logger.Info("Invoke AppActivate...");
            _videoManager.RunOnUIThread((Action)(() =>
            {
                SimpleEventBus.GetDefaultEventBus().Post("AppActivate", TimeSpan.Zero);
            }));
            setResponse(response, true);
        }

        public void AppMinimize(HttpRequest request, HttpResponse response)
        {
            _logger.Info("Invoke AppMinimize...");
            _videoManager.RunOnUIThread((Action)(() =>
            {
                SimpleEventBus.GetDefaultEventBus().Post("AppMinimize", TimeSpan.Zero);
            }));
            setResponse(response, true);
        }

        public void AppExit(HttpRequest request, HttpResponse response)
        {
            _logger.Info("Invoke AppExit...");
            _videoManager.RunOnUIThread((Action)(() =>
            {
                SimpleEventBus.GetDefaultEventBus().Post("AppExit", TimeSpan.Zero);
            }));
        }

        private bool SetCameraResolution(int cameraIndex, string resolutionKey)
        {
            if (String.IsNullOrEmpty(resolutionKey) ||
                !_videoManager.cameraManager.resolutions.ContainsKey(resolutionKey))
            {
                return false;
            }

            switch (cameraIndex)
            {
                case 1:
                    _videoManager.cameraManager.SaveCam1Resolution(resolutionKey);
                    return _videoManager.cameraManager.SetCam1Resolution(resolutionKey);
                case 2:
                    _videoManager.cameraManager.SaveCam2Resolution(resolutionKey);
                    return _videoManager.cameraManager.SetCam2Resolution(resolutionKey);
                case 3:
                    _videoManager.cameraManager.SaveCam3Resolution(resolutionKey);
                    return _videoManager.cameraManager.SetCam3Resolution(resolutionKey);
                case 4:
                    _videoManager.monitorShareManager.SaveResolution(resolutionKey);
                    return _videoManager.monitorShareManager.SetCam4Resolution(resolutionKey);
                case 5:
                    _videoManager.windowShareManager.SaveResolution(resolutionKey);
                    return _videoManager.windowShareManager.SetResolution(resolutionKey);
            }

            return false;
        }

        private bool SetCameraFramerate(int cameraIndex, string framerateKey)
        {
            if (String.IsNullOrEmpty(framerateKey) || !_videoManager.cameraManager.framerates.ContainsKey(framerateKey))
            {
                return false;
            }

            switch (cameraIndex)
            {
                case 1:
                    _videoManager.cameraManager.SaveCam1Framerate(framerateKey);
                    return _videoManager.cameraManager.SetCam1Framerate(framerateKey);
                case 2:
                    _videoManager.cameraManager.SaveCam2Framerate(framerateKey);
                    return _videoManager.cameraManager.SetCam2Framerate(framerateKey);
                case 3:
                    _videoManager.cameraManager.SaveCam3Framerate(framerateKey);
                    return _videoManager.cameraManager.SetCam3Framerate(framerateKey);
                case 4:
                    _videoManager.monitorShareManager.SaveFramerate(framerateKey);
                    return _videoManager.monitorShareManager.SetCam4Framerate(framerateKey);
                case 5:
                    _videoManager.windowShareManager.SaveFramerate(framerateKey);
                    return _videoManager.windowShareManager.SetFramerate(framerateKey);
            }

            return false;
        }

        private bool IsNumeric(string value)
        {
            return Regex.IsMatch(value, @"^[+-]?\d*[.]?\d*$");
        }

        private bool showLog = false;

        private void log(string content)
        {
            if (showLog) _logger.Info(content);
        }
    }
}