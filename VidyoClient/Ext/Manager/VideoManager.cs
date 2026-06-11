using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using VideoClient.Entity;
using VideoClient.UI;
using VideoClient.Util;
using VideoClient.VidyoClient.Ext;
using VidyoClient;
using VidyoClient.Ext;
using Application = System.Windows.Forms.Application;

namespace VideoClient.Manager
{
    public class VideoManager : Connector.IConnect, Connector.IRegisterReconnectEventListener,
        Connector.IRegisterMessageEventListener, Connector.IRegisterModerationCommandEventListener,
        Connector.IRegisterModerationResultEventListener, Connector.IRegisterRecorderInCallEventListener,
        Connector.IRegisterErrorEventListener
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        #region Variable

        public static string VERSION = "V1.04.14" + (Environment.Is64BitProcess ? "(x64)" : "(x86)");
        public static int VERSION_NUMBER = 10414;

        public bool IsReady { get; set; }

        public bool isConnected;
        public int connectionStatusCode; // -2连接丢失、-1连接失败、0断开连接、1连接成功、2正在重连、3重连成功
        public string connectionStatus;

        private Panel _videoPanel;
        public SmallWindowController _smallWindowController;

        private string apiCode = "YWRtOkxzc3ZjQDIwMzA=";
        public Connector connector;
        public Connector.IConnect connectionListener;
        private OnDeviceMuteListener onDeviceMuteListener;
        private OnVideoDeviceChangedListener onVideoDeviceChangedListener;
        public String portal, username, password, roomKey, roomName, roomPin, displayName;
        private string logFilter;
        private bool enableVideoDebug;
        public bool cameraPrivacyOnAppLaunch, micPrivacyOnAppLaunch, speakerPrivacyOnAppLaunch;
        public bool isLockMuteCamera, isLockMuteMic, isLockMuteSpeaker;
        public bool leaveAndExitApp;
        uint maxSendBitRate;
        uint maxReceiveBitRate;
        uint maxBitRate;

        public CameraListener cameraManager; //第一、第二摄像头管理器，vidyo api对于前两路采用同一类型表示（LocalCamera）
        public VirtualSourceListener virtualSourceListener; //第三路
        public MonitorShareListener monitorShareManager; //第四路
        public WindowShareListener windowShareManager;
        public MicrophoneListener microphoneManager;
        public SpeakerListener speakerManager;
        public ParticipantListener participantManager;

        public LayoutController layoutController;
        public LayoutControllerConfig _layoutrControllerConfig;
        public float _winScaling;

        public SipContactsManager _sipContactsManager;

        //网络服务
        public VideoHttpService httpServer;
        public VideoSocketService socketService;
        private string httpServiceWebRootDir = Application.StartupPath + "\\wwwroot";
        private String httpServiceIp;
        private int httpServicePort;

        //tango
        public string loginUsername; //用做supabase对象存储的目录
        public string subsystemTangoDrawUrl; //登录后被赋值，有值则表示服务可用
        public string drawRoomKey = "vQtBfbLmpoq7iGTC5hNg7A";

        //windows
        private FormTangoApp _appWindow;

        private bool _isAppWindowLoaded;

        // private bool isTangoDrawStarter = false;
        private CefForm _tangoDrawWindow;
        private CefForm _avCheckWindow;
        private FormSettings _settingsWindow;
        private FormStartShare _startShareWindow;
        private FormMessage _formMessage;

        //启动参数
        private string[] _args;
        private string _launchUrlParams = "isWin=true";
        Dictionary<string, string> _launchParams = new Dictionary<string, string>();

        #endregion

        #region 初始化

        private static VideoManager _instance;

        public static VideoManager GetInstance()
        {
            return _instance ?? (_instance = new VideoManager());
        }

        private VideoManager()
        {
        }

        public void Init(
            Panel videoPanel,
            Connector.IConnect connectionListener,
            OnDeviceMuteListener onDeviceMuteListener,
            OnVideoDeviceChangedListener onVideoDeviceChangedListener
        )
        {
            // this.form = form;
            _videoPanel = videoPanel;
            this.connectionListener = connectionListener;
            this.onDeviceMuteListener = onDeviceMuteListener;
            this.onVideoDeviceChangedListener = onVideoDeviceChangedListener;
            // this.videoSourcePlayer1 = videoSourcePlayer1;
            //初始化小窗菜单
            InitSmallWindowController();
        }

        public void InitConfig()
        {
            //布局控制配置
            _layoutrControllerConfig = new LayoutControllerConfig();
            //read configs
            _layoutrControllerConfig.UseDefaultLayout = Config.GetBooleanConfig("UseDefaultLayout");
            _layoutrControllerConfig.EnableEndpointMode = Config.GetBooleanConfig("enableEndpointMode");
            _layoutrControllerConfig.EnableGuestMode = Config.GetBooleanConfig("enableGuestMode");
            _layoutrControllerConfig.EndpointModeWindowMode = Config.GetStringConfig("endpointModeWindowMode");
            _layoutrControllerConfig.EndpointModeWindowNumber = Config.GetIntegerConfig("endpointModeWindowNumber");

            _layoutrControllerConfig.EndpointModeCameraPin = Config.GetBooleanConfig("endpointModeCameraPin");
            _layoutrControllerConfig.EndpointModeCameraPinWindowIndex =
                Config.GetIntegerConfig("endpointModeCameraPinWindowIndex");

            _layoutrControllerConfig.DisplayGuestVideo = Config.GetBooleanConfig("displayGuestVideo");
            _layoutrControllerConfig.DisplayGatewayVideo = Config.GetBooleanConfig("displayGatewayVideo");
            _layoutrControllerConfig.DisplaySelfViewPip = Config.GetBooleanConfig("displaySelfViewPIP");
            _layoutrControllerConfig.DisplaySelfViewInMeeting = Config.GetBooleanConfig("displaySelfViewInMeeting");
            _layoutrControllerConfig.DisplayZoomBtnOnLocalRenderer =
                Config.GetBooleanConfig("displayZoomBtnOnLocalRenderer");
            _layoutrControllerConfig.DisplayZoomBtnOnRemoteRenderer =
                Config.GetBooleanConfig("displayZoomBtnOnRemoteRenderer");
            _layoutrControllerConfig.DisplayZoomBtnOnLecturerRenderer =
                Config.GetBooleanConfig("displayZoomBtnOnLecturerRenderer");
            _layoutrControllerConfig.DisplaySnapshotBtn = Config.GetBooleanConfig("displaySnapshotBtn");
            _layoutrControllerConfig.AllowCameraCopyRendering = Config.GetBooleanConfig("allowCameraCopyRendering");
            _layoutrControllerConfig.RemoteParticipants = Config.GetIntegerConfig("RemoteParticipants");

            _layoutrControllerConfig.AutoAssignRenderer = Config.GetBooleanConfig("AutoAssignRenderer");

            _layoutrControllerConfig.ForceLayoutNum.Clear();
            for (int i = 1; i <= 4; i++)
            {
                _layoutrControllerConfig.ForceLayoutNum.Add(Config.GetIntegerConfig("Screen" + i + "ForceLayoutNum"));
            }

            _layoutrControllerConfig.VoiceActivatedPositionOfScreens =
                Config.GetIntegerConfig("VoiceActivatedPositionOfScreens");
            _layoutrControllerConfig.VoiceActivatedPositionOfRenderers =
                Config.GetIntegerConfig("VoiceActivatedPositionOfRenderers");
            //视频组件配置
            logFilter = Config.GetStringConfig("LogFilter");
            enableVideoDebug = Config.GetBooleanConfig("enableVideoDebug");
            cameraPrivacyOnAppLaunch = Config.GetBooleanConfig("cameraPrivacyOnAppLaunch");
            micPrivacyOnAppLaunch = Config.GetBooleanConfig("micPrivacyOnAppLaunch");
            speakerPrivacyOnAppLaunch = Config.GetBooleanConfig("speakerPrivacyOnAppLaunch");
            uint oneMb = 1024 * 1024;
            maxSendBitRate = (uint)Config.GetIntegerConfig("SetMaxSendBitRate") * oneMb;
            maxReceiveBitRate = (uint)Config.GetIntegerConfig("SetMaxReceiveBitRate") * oneMb;
            maxBitRate = (uint)Config.GetIntegerConfig("SetMaxBitRate") * oneMb;
            //
            httpServicePort = Config.GetIntegerConfig("http_server_port");
            Log("LoadConfig");
        }

        public void InitVideoComponent()
        {
            if (connector != null) return;

            Log("---------------Video Component Initialize begin----------------");

            try
            {
                ConnectorPKG.Initialize();
            }
            catch (Exception e)
            {
                Log("ConnectorPKG.Initialize failed. \n{0}", e.ToString());
                throw;
            }

            connector = new Connector(
                // videoPanel.Handle,
                IntPtr.Zero,
                Connector.ConnectorViewStyle.ConnectorviewstyleDefault,
                (uint)_layoutrControllerConfig.RemoteParticipants,
                logFilter,
                // "logs\\video_log.txt",
                // $"{Application.UserAppDataPath}\\logs\\video_log.txt",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                "\\Laisaisi\\VideoClient\\logs\\sdk_logs.log",
                0);

            if (enableVideoDebug)
            {
                connector.EnableDebug(7776, logFilter);
            }
            else
            {
                connector.DisableDebug();
            }

            connector.SetCameraPrivacy(cameraPrivacyOnAppLaunch);

            connector.SelectLocalCamera(null);

            connector.SetMicrophonePrivacy(micPrivacyOnAppLaunch);
            connector.SetSpeakerPrivacy(speakerPrivacyOnAppLaunch);

            // connector.SetWebProxyTransport(true);
            // connector.SetWebProxyTransportAddress("222.249.239.217", 23333);

            connector.SetAutoReconnect(true);

            Log("Video SDK version -> " + connector.GetVersion());
            Log("connector(remote participant:{0})", _layoutrControllerConfig.RemoteParticipants);

            connector.SetCpuTradeOffProfile(Connector.ConnectorTradeOffProfile.ConnectortradeoffprofileHigh);

            //set network config
            connector.SetMaxSendBitRate(maxSendBitRate);
            Log("connector.SetMaxSendBitRate - > " + maxSendBitRate);
            connector.SetMaxReceiveBitRate(maxReceiveBitRate);
            Log("connector.SetMaxReceiveBitRate - > " + maxReceiveBitRate);
            connector.SetMaxBitRate(maxBitRate);
            Log("connector.SetMaxBitRate - > " + maxBitRate);

            connector.ReportLocalParticipantOnJoined(true);
            Log("connector.ReportLocalParticipantOnJoined(true)");

            connector.RegisterReconnectEventListener(this);

            connector.RegisterMessageEventListener(this);

            connector.RegisterModerationCommandEventListener(this);
            connector.RegisterModerationResultEventListener(this);

            connector.RegisterRecorderInCallEventListener(this);

            connector.RegisterErrorEventListener(this);

            Log("---------------Video Component Initialize end----------------");
        }

        #region SmallWindow

        private void InitSmallWindowController()
        {
            Panel headerPanel = new Panel
            {
                Location = new Point(0, 0),
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(0, 0, 0, 0),
                Size = new Size(1, 1),
                BackColor = Color.FromArgb(36, 36, 36),
                Visible = false
            };
            _videoPanel.FindForm()?.Controls.Add(headerPanel);
            Panel footerPanel = new Panel
            {
                Location = new Point(0, 0),
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(0, 0, 0, 0),
                Size = new Size(1, 1),
                BackColor = Color.FromArgb(36, 36, 36),
                Visible = false
            };
            _videoPanel.FindForm()?.Controls.Add(footerPanel);
            _smallWindowController = new SmallWindowController(headerPanel, _videoPanel, footerPanel);
        }

        #endregion

        public void StartHttpService()
        {
            Log("---------------HttpServer----------------");
            Log("Web Root Dir -> " + httpServiceWebRootDir);
            Log("Server Port -> " + httpServicePort);
            Thread serviceThread = new Thread(() => { httpServer.Start(); });
            serviceThread.Start();
        }

        public void StartSocketService()
        {
            int portInt = httpServicePort + 1;
            Log("---------------SocketServer----------------");
            Log("Server Port -> " + portInt);
            Thread serviceThread = new Thread(() => { socketService.Start(); });
            serviceThread.Start();
        }

        public void InitManager()
        {
            Log("----- Init Manager Begin -----");
            //初始化Web服务，用于UI层调用接口。
            //当终端模式时，使用局域网IP地址，实现局域网远程控制。
            httpServiceIp = IPAddress.Loopback.ToString();
            if (_layoutrControllerConfig.EnableEndpointMode)
            {
                //todo 如果有多网卡，则会出现问题
                httpServiceIp = Util.Util.GetLocalIp();
            }

            Log($"| http server ip : {httpServiceIp}");

            httpServer = new VideoHttpService(this, httpServiceIp, httpServicePort);
            httpServer.SetRoot(httpServiceWebRootDir);
            httpServer.Logger = new ConsoleLogger();
            Log("| create VideoHttpService");
            //
            socketService = new VideoSocketService(this, (ushort)(httpServicePort + 1));
            Log("| create VideoSocketService");
            //
            _sipContactsManager = new SipContactsManager();
            Log("| create SipContactsManager");
            //
            virtualSourceListener = new VirtualSourceListener();
            Log("| create VirtualSourceListener");
            //
            participantManager = new ParticipantListener(socketService);
            Log("| create ParticipantListener");
            //外设管理器
            cameraManager = new CameraListener(
                connector,
                socketService,
                this);
            Log("| create CameraListener");
            // thirdCameraManager = new ThirdCameraManager(connector, this, virtualSourceListener);
            // Log("- create ThirdCameraManager");
            monitorShareManager = new MonitorShareListener(connector, socketService);
            Log("| create MonitorShareListener");
            windowShareManager = new WindowShareListener(connector, socketService);
            Log("| create WindowShareListener");
            microphoneManager = new MicrophoneListener(_videoPanel, connector);
            Log("| create MicrophoneListener");
            speakerManager = new SpeakerListener(_videoPanel, connector);
            Log("| create SpeakerListener");
            //布局控制器
            layoutController = new LayoutController(
                _videoPanel, _layoutrControllerConfig, connector,
                socketService, participantManager);
            participantManager.loudestParticipantChangedListener = layoutController;
            monitorShareManager._layoutController = layoutController;
            windowShareManager._layoutController = layoutController;
            Log("| create LayoutController");


            //register listener
            connector.RegisterLocalCameraEventListener(cameraManager);
            connector.RegisterLocalMicrophoneEventListener(microphoneManager);
            connector.RegisterLocalSpeakerEventListener(speakerManager);
            connector.RegisterLocalWindowShareEventListener(windowShareManager);
            connector.RegisterLocalMonitorEventListener(monitorShareManager);
            Log("| register Local DeviceEventListener");

            connector.RegisterRemoteCameraEventListener(layoutController);
            connector.RegisterRemoteWindowShareEventListener(layoutController);
            Log("| register Remote EventListener");

            connector.RegisterVirtualVideoSourceEventListener(virtualSourceListener);
            Log("| register VirtualVideoSourceEventListener");
            connector.RegisterParticipantEventListener(participantManager);
            Log("| register ParticipantEventListener");

            _winScaling = Util.Util.GetWinScaling(_videoPanel);
            //
            layoutController.Init();
            //
            StartPreviewCameraAtExtForm();
            // Log("- layoutController.Init()");
            Log("----- Init Manager End -----");
        }

        public void Uninit()
        {
            SetVideoVisible(false);
            StopPreviewCameraFromExtForm();

            cameraManager.CloseVirtualSource();

            httpServer?.Stop();
            socketService?.Dispose();

            // RunOnUIThread((Action)(UninitVidyo));
            UninitVidyo();
        }

        private void UninitVidyo()
        {
            //close vidyo
            if (null != connector)
            {
                if (isConnected) connector.Disconnect();

                if (virtualSourceListener.mVirtualShare != null)
                {
                    connector.DestroyVirtualVideoSource(virtualSourceListener.mVirtualShare);
                }

                connector.UnregisterLocalCameraEventListener();
                connector.UnregisterLocalMicrophoneEventListener();
                connector.UnregisterLocalSpeakerEventListener();
                connector.UnregisterLocalWindowShareEventListener();
                connector.UnregisterLocalMonitorEventListener();

                connector.UnregisterRemoteCameraEventListener();
                connector.UnregisterRemoteWindowShareEventListener();
                connector.UnregisterVirtualVideoSourceEventListener();
                connector.UnregisterParticipantEventListener();

                connector.UnregisterModerationCommandEventListener();
                connector.UnregisterModerationResultEventListener();
                connector.UnregisterRecorderInCallEventListener();
                connector.UnregisterErrorEventListener();

                //-----解决使用sdk布局退出app时发生内存访问错误的问题-----
                connector.HideView(_videoPanel.Handle);
                connector.AssignViewToCompositeRenderer(IntPtr.Zero,
                    Connector.ConnectorViewStyle.ConnectorviewstyleDefault,
                    16);
                //-----↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑-----
                connector.Disable();
                connector = null;
            }
        }

        #endregion

        #region API

        public bool LaunchParamsContains(string key)
        {
            return _launchParams.ContainsKey(key);
        }

        public bool IsLaunchParamTrue(string key)
        {
            return LaunchParamsContains(key) && String.Equals(_launchParams[key], "true");
        }

        public void ResetAndParseLaunchParams(string[] args)
        {
            ResetLaunchParams(args);
            ParseLaunchParameters(args);
        }

        public void ResetLaunchParams(string[] args)
        {
            _args = args;
            _launchUrlParams = "isWin=true";
            _launchParams.Clear(); //刷新args后，需要把解析参数集初始化
        }

        public void ParseLaunchParameters()
        {
            ParseLaunchParameters(_args);
        }

        private void ParseLaunchParameters(string[] args)
        {
            //parse launch parameters
            Log("-----Start parse launch parameters-----");
            Log($"args -> {String.Join(" , ", args)}");
            // bool launchByParams = false;

            if (null != args && args.Length > 0)
            {
                //convert web launch parameters
                string joinScheme = "tango://join";
                if (args[0].StartsWith(joinScheme))
                {
                    string argsUrl = args[0].Substring(args[0].IndexOf("?", StringComparison.Ordinal) + 1);
                    args = argsUrl.Split('&');
                }

                //parse parameters
                foreach (string s in args)
                {
                    Log("origin : {0}", s);
                    if (!s.Contains("="))
                    {
                        Log("启动参数异常，格式必须为：key=value");
                        break;
                    }

                    try
                    {
                        string key = s.Substring(0, s.IndexOf("="));
                        string value = s.Substring(s.IndexOf("=") + 1, s.Length - s.IndexOf("=") - 1);
                        _launchParams.Add(key, value);
                        _launchUrlParams += $"&{key}={value}";
                        Log("parse : key->{0} , value->{1}", key, value);
                    }
                    catch (Exception exception)
                    {
                        Log(exception.ToString());
                        throw;
                    }
                }
            }

            //解析启动参数，设置guest标志
            if (_launchParams.ContainsKey("enableGuestMode"))
            {
                SetGuestMode(String.Equals(_launchParams["enableGuestMode"], "true"));
                Log("set guest mode true by launch params,now mode is {}",
                    _layoutrControllerConfig.EnableGuestMode);
            }

            Log("-----End   parse launch parameters-----");
        }

        public String GetWebRoot()
        {
            return $"http://{httpServer.ServerIP}:{httpServer.ServerPort}";
        }

        public String GetLaunchUrlParam()
        {
            return _launchUrlParams;
        }

        public bool IsUseDefaultLayout()
        {
            if (layoutController?.config == null) return false;
            return layoutController.config.UseDefaultLayout;
        }

        public bool IsEndpointMode()
        {
            if (layoutController?.config == null) return false;
            return layoutController.config.EnableEndpointMode;
        }

        public bool IsGuestMode()
        {
            if (layoutController?.config == null) return false;
            return layoutController.config.EnableGuestMode;
        }

        public void SetGuestMode(bool isGuestMode)
        {
            _layoutrControllerConfig.EnableGuestMode = isGuestMode;
        }

        public bool IsFullscreenMode()
        {
            if (layoutController?.config == null) return false;
            return String.Equals("fullscreen", layoutController.config.EndpointModeWindowMode);
        }

        public bool IsSmallWindowMode()
        {
            return _smallWindowController.IsApplied();
        }

        public void ApplySmallWindowMode()
        {
            _smallWindowController.Apply();
        }

        public void Connect(
            String portal,
            String un,
            String pwd,
            String roomKey,
            String roomName,
            String roomPin,
            String displayName
        )
        {
            this.portal = portal;
            this.username = un;
            this.password = pwd;
            this.roomKey = roomKey;
            this.roomName = roomName;
            this.roomPin = roomPin;
            this.displayName = displayName;

            if (Config.GetBooleanConfig("ProxyEnable"))
            {
                connector.SetWebProxyTransport(true);
                String proxyHost = Config.GetStringConfig("ProxyHost");
                int proxyPort = Config.GetIntegerConfig("ProxyPort");
                if (!String.IsNullOrEmpty(proxyHost) && proxyPort > 0)
                {
                    connector.SetWebProxyTransportAddress(proxyHost, (uint)proxyPort);
                    _logger.Info("Enable Video Proxy success. (" + proxyHost + ":" + proxyPort + ")");
                }
                else
                {
                    _logger.Info("Enable Video Proxy failed. (" + proxyHost + ":" + proxyPort + ")");
                }
            }
            else
            {
                connector.SetWebProxyTransport(false);
                connector.SetWebProxyTransportAddress(null, 0);
                _logger.Info("Disable Video Proxy.");
            }

            bool connectWithAccount = !String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password);

            //提前调整好本地设备，以便入会
            cameraManager.displayName = connectWithAccount ? "我" : displayName + "（我）";
            PrepareForJoin();

            if (connectWithAccount)
            {
                connector.ConnectToRoomWithKey(this.portal, username, password, this.roomKey,
                    this.roomPin,
                    this);
                Log("ConnectToRoomWithKey({0},{1},{2},{3},{4})",
                    this.portal, username, password, this.roomKey, this.roomPin);
            }
            else
            {
                Config.SetStringConfig("guest_server_url", this.portal);
                // Config.SetStringConfig("guest_room_key", this.roomKey);//在UI层调用存储，可能会存储号码方式
                Config.SetStringConfig("guest_display_name", this.displayName);
                subsystemTangoDrawUrl = Config.GetStringConfig("guest_subsystemTangoDrawUrl");
                connector.ConnectToRoomAsGuest(this.portal, this.displayName, this.roomKey, this.roomPin,
                    this);
                Log("ConnectToRoomAsGuest({0},{1},{2},{3})",
                    this.portal, this.displayName, this.roomKey, this.roomPin);
            }
        }

        public bool Disconnect()
        {
            return connector.Disconnect();
        }

        public Panel GetVideoPanel()
        {
            return _videoPanel;
        }

        /**
         * sourceType：参见“VideoSourceType”类，常量从1开始，与配置文件中对应。方便配置人员理解顺序
         */
        public void AssignVideoDevice(int sourceType, string deviceId)
        {
            switch (sourceType) //等价于cameraIndex
            {
                case VideoSourceType.TYPE_LOCAL_CAMERA_INT: //第一路视频源
                    SelectCamera(deviceId);
                    break;
                case VideoSourceType.TYPE_LOCAL_CAMERA2_INT: //第二路视频源
                    cameraManager.AssignContent(deviceId);
                    cameraManager.SaveLastSelectedContent(deviceId);
                    break;
                case VideoSourceType.TYPE_VIRTUAL_SOURCE_INT: //第三路视频源
                    cameraManager.AssignVirtualSource(deviceId);
                    cameraManager.SaveLastSelectedVirtualSource(deviceId);
                    break;
                case VideoSourceType.TYPE_LOCAL_MONITOR_INT: //共享显示器
                case VideoSourceType.TYPE_LOCAL_WINDOW_INT: //共享窗口
                    StartShare(sourceType, deviceId);
                    break;
            }

            onVideoDeviceChangedListener.OnVideoDeviceChanged(sourceType, deviceId);
        }

        public void SetVideoSourceResolution(int sourceType, String resolutionKey)
        {
            switch (sourceType) //等价于cameraIndex
            {
                case VideoSourceType.TYPE_LOCAL_CAMERA_INT: //第一路视频源
                    cameraManager.SetCam1Resolution(resolutionKey);
                    break;
                case VideoSourceType.TYPE_LOCAL_CAMERA2_INT: //第二路视频源
                    cameraManager.SetCam2Resolution(resolutionKey);
                    break;
                case VideoSourceType.TYPE_VIRTUAL_SOURCE_INT: //第三路视频源
                    cameraManager.SetCam3Resolution(resolutionKey);
                    break;
                case VideoSourceType.TYPE_LOCAL_MONITOR_INT: //共享显示器
                case VideoSourceType.TYPE_LOCAL_WINDOW_INT: //共享窗口
                    break;
            }
        }

        public void SetVideoSourceFrameRate(int sourceType, String frameRateKey)
        {
            switch (sourceType) //等价于cameraIndex
            {
                case VideoSourceType.TYPE_LOCAL_CAMERA_INT: //第一路视频源
                    cameraManager.SetCam1Framerate(frameRateKey);
                    break;
                case VideoSourceType.TYPE_LOCAL_CAMERA2_INT: //第二路视频源
                    cameraManager.SetCam2Framerate(frameRateKey);
                    break;
                case VideoSourceType.TYPE_VIRTUAL_SOURCE_INT: //第三路视频源
                    cameraManager.SetCam3Framerate(frameRateKey);
                    break;
                case VideoSourceType.TYPE_LOCAL_MONITOR_INT: //共享显示器
                case VideoSourceType.TYPE_LOCAL_WINDOW_INT: //共享窗口
                    break;
            }
        }

        public void OpenCamera()
        {
            var cameraName = cameraManager.AssignLastSelectedCamera();
            UpdatePreviewCameraStateAtExtForm(cameraName);
        }

        public void CloseCamera()
        {
            UpdatePreviewCameraStateAtExtForm(CameraListener.DEVICE_NONE);
            cameraManager.CloseCamera();
        }

        private bool SelectCamera(String cameraName)
        {
            if (CameraListener.DEVICE_NONE.Equals(cameraName))
            {
                UpdatePreviewCameraStateAtExtForm(cameraName);
            }

            cameraManager.AssignCamera(cameraName);
            cameraManager.SaveLastSelectedCamera(cameraName);
            Log("SelectPreviewCamera success. -> {}", cameraName);

            if (!CameraListener.DEVICE_NONE.Equals(cameraName))
            {
                UpdatePreviewCameraStateAtExtForm(cameraName);
            }

            return true;
        }

        private void UpdatePreviewCameraStateAtExtForm(String cameraName)
        {
            //刷新扩展屏的自视画面
            if (IsEndpointMode() && !isConnected)
            {
                foreach (FormVideoExt formVideoExt in layoutController._forms)
                {
                    formVideoExt?.ChangePreviewCamera(this, cameraName);
                }
            }
        }

        private void StartPreviewCameraAtExtForm()
        {
            RunOnUIThread((Action)(() =>
            {
                if (IsEndpointMode() && !isConnected)
                {
                    foreach (FormVideoExt formVideoExt in layoutController._forms)
                    {
                        formVideoExt?.StartPreviewLastSelectedCamera(this);
                    }
                }
            }));
        }

        private void StopPreviewCameraFromExtForm()
        {
            foreach (FormVideoExt formVideoExt in layoutController._forms)
            {
                formVideoExt?.StopPreviewCamera(this);
            }
            // if (IsEndpointMode()) //终端模式入会时会停止渲染扩展屏的摄像头预览，为了入会能重新打开摄像头（触发onSelected），需要先关闭摄像头，之后按固定逻辑，会打开最后一次选择的摄像头
            // {
            //     cameraManager.CloseCamera();
            // }
        }

        private void StopPreviewCameraFromExtFormOnMain()
        {
            RunOnUIThread((Action)(StopPreviewCameraFromExtForm));
        }

        private bool StartShare(int sourceType, string deviceId)
        {
            bool started = false;

            if (sourceType == VideoSourceType.TYPE_LOCAL_MONITOR_INT)
            {
                monitorShareManager.AssignMonitorShare(deviceId);
                monitorShareManager.SaveLastSelectedCamera(deviceId);
                started = true;
            }
            else if (sourceType == VideoSourceType.TYPE_LOCAL_WINDOW_INT)
            {
                windowShareManager.AssignWindowShare(deviceId);
                windowShareManager.SaveLastSelectedWindow(deviceId);
                started = true;
            }

            //开始共享时，如果是个人模式，开启小窗 并 置顶
            if (started)
            {
                if (!string.Equals(deviceId, WindowShareListener.DEVICE_NONE) &&
                    !IsEndpointMode())
                {
                    RunOnUIThread((Action)(() =>
                    {
                        _smallWindowController.Apply();
                        ((FormMain)_videoPanel.FindForm())?.SetTopMost(true);
                    }));
                }

                return true;
            }

            return false;
        }

        public void StopShare()
        {
            if (windowShareManager.IsInUsed())
            {
                AssignVideoDevice(VideoSourceType.TYPE_LOCAL_WINDOW_INT, WindowShareListener.DEVICE_NONE);
            }
            else if (monitorShareManager.IsInUsed())
            {
                AssignVideoDevice(VideoSourceType.TYPE_LOCAL_MONITOR_INT, MonitorShareListener.DEVICE_NONE);
            }
        }

        public bool IsShareStarted()
        {
            return windowShareManager.IsInUsed() || monitorShareManager.IsInUsed();
        }

        public void SelectMicrophone(String deviceName)
        {
            microphoneManager.SaveLastSelectedDevice(deviceName);
            if (isConnected) microphoneManager.AssignDevice(deviceName);
            Log("SelectMicrophone success. -> {}", deviceName);
        }

        public void SelectSpeaker(String deviceName)
        {
            speakerManager.SaveLastSelectedDevice(deviceName);
            if (isConnected) speakerManager.AssignDevice(deviceName);
            Log("SelectSpeaker success. -> {}", deviceName);
        }

        public void SetMicrophonePrivacy(bool privacy)
        {
            if (isLockMuteMic) return;

            connector.SetMicrophonePrivacy(privacy);
            micPrivacyOnAppLaunch = privacy;

            onDeviceMuteListener.OnDeviceMuteChanged(1, micPrivacyOnAppLaunch, isLockMuteMic);
            socketService.SendSystemStatusUpdatedEventToAll();

            Log("SetMicrophonePrivacy -> {}", privacy);
        }

        public bool ToggleMicrophonePrivacy()
        {
            SetMicrophonePrivacy(!micPrivacyOnAppLaunch);
            return micPrivacyOnAppLaunch;
        }

        public void SetSpeakerPrivacy(bool privacy)
        {
            if (isLockMuteSpeaker) return;

            connector.SetSpeakerPrivacy(privacy);
            speakerPrivacyOnAppLaunch = privacy;

            onDeviceMuteListener.OnDeviceMuteChanged(2, speakerPrivacyOnAppLaunch, isLockMuteSpeaker);
            socketService.SendSystemStatusUpdatedEventToAll();

            Log("SetSpeakerPrivacy -> {}", privacy);
        }

        public bool ToggleSpeakerPrivacy()
        {
            SetSpeakerPrivacy(!speakerPrivacyOnAppLaunch);
            return speakerPrivacyOnAppLaunch;
        }

        public void SetCameraPrivacy(bool privacy)
        {
            if (isLockMuteCamera) return;

            connector.SetCameraPrivacy(privacy);
            cameraPrivacyOnAppLaunch = privacy;

            onDeviceMuteListener.OnDeviceMuteChanged(0, cameraPrivacyOnAppLaunch, isLockMuteCamera);
            socketService.SendSystemStatusUpdatedEventToAll();

            Log("SetCameraPrivacy -> {}", privacy);
        }

        public bool ToggleCameraPrivacy()
        {
            SetCameraPrivacy(!cameraPrivacyOnAppLaunch);
            return cameraPrivacyOnAppLaunch;
        }

        public bool SetMainCameraPin(bool pin)
        {
            return layoutController.SetMainCameraPin(pin);
        }

        public bool ToggleMainCameraPin()
        {
            return SetMainCameraPin(!layoutController.isMainCameraPin);
        }

        //--------------------------------------------------------------------------------------------------------------
        /**
         * AppWindow作为终端模式的控制页面，创建后的对象会一直存在，便于复用再次打开。
         */
        public void OpenTangoAppWindow()
        {
            RunOnUIThread((Action)(() =>
            {
                if (null == _appWindow)
                {
                    string initAddress = Application.StartupPath + "\\wwwroot\\launch.html";
                    _appWindow = new FormTangoApp(initAddress);
                }

                if (!_isAppWindowLoaded)
                {
                    string indexPage = "index.html";
                    if (IsGuestMode())
                    {
                        indexPage = "vue_app_guest_client/guest.html";
                    }

                    _appWindow.LoadUrl($"{GetWebRoot()}/{indexPage}?{_launchUrlParams}");
                    _isAppWindowLoaded = true;
                }

                bool isOpen = false;
                foreach (Form form in Application.OpenForms)
                {
                    if (form is FormTangoApp && !((FormTangoApp)form).isClosed)
                    {
                        form.Activate();
                        form.WindowState = FormWindowState.Normal;
                        // form.StartPosition = FormStartPosition.CenterScreen;
                        isOpen = true;
                        break;
                    }
                }

                if (!isOpen)
                {
                    _appWindow.WindowState = FormWindowState.Normal;
                    // _appWindow.StartPosition = FormStartPosition.CenterScreen;
                    _appWindow.Show();
                }
            }));
        }

        public void DismissTangoAppWindow()
        {
            _appWindow.Hide();
        }

        public void OpenCefWindow(int id, string url, string title)
        {
            RunOnUIThread((Action)(() =>
            {
                CefForm cef = new CefForm(
                    id,
                    String.IsNullOrEmpty(title) ? "" : title,
                    url,
                    true)
                {
                    // _requestHandler = new CefRequestHandler(this)
                };
                cef.Show();
            }));
        }

        public void OpenTangoDrawWindow(string url, string nickName, string roomId, string windowTitle,
            bool isTangoDrawStarter = false)
        {
            if (null != _tangoDrawWindow)
            {
                Log("- OpenTangoDrawWindow: failed,window has already opened.");
                return;
            }

            RunOnUIThread((Action)(() =>
            {
                string fullUrl = url + "?username=" + nickName + "#room=" + roomId + "," + drawRoomKey;
                Log("- OpenTangoDrawWindow: {}", fullUrl);
                _tangoDrawWindow = new CefForm(
                    1,
                    String.IsNullOrEmpty(windowTitle) ? "" : windowTitle,
                    fullUrl,
                    true);
                _tangoDrawWindow.Closed += (sender, args) =>
                {
                    _tangoDrawWindow = null;
                    if (isTangoDrawStarter)
                    {
                        Dictionary<string, string> json = new Dictionary<string, string>
                        {
                            { "action", "closeTangoDrawWindow" },
                            { "roomId", roomId }
                        };
                        connector.SendChatMessage("[action]$" + JsonConvert.SerializeObject(json));
                        Log("- OpenTangoDrawWindow: SendChatMessage('closeTangoDrawWindow') to Room {}", roomId);
                    }
                };
                _tangoDrawWindow.Show();
                if (isTangoDrawStarter)
                {
                    Dictionary<string, string> json = new Dictionary<string, string>
                    {
                        { "action", "openTangoDrawWindow" },
                        { "url", url },
                        { "roomId", roomId }
                    };
                    connector.SendChatMessage("[action]$" + JsonConvert.SerializeObject(json));
                    Log("- OpenTangoDrawWindow: SendChatMessage('openTangoDrawWindow') to Room {}", roomId);
                }
            }));
        }

        /**
         * 打开当前房间的TangoDraw窗口
         *
         * imgPath: 必须是网络地址
         */
        public void OpenTangoDrawWindowWithImage(string imgPath)
        {
            string base63Path = Convert.ToBase64String(Encoding.UTF8.GetBytes(imgPath));
            if (base63Path.IndexOf("=", StringComparison.Ordinal) != -1)
            {
                base63Path = base63Path.Replace("=", "$");
            }

            if (base63Path.IndexOf("/", StringComparison.Ordinal) != -1)
            {
                base63Path = base63Path.Replace("/", "|");
            }

            // http://localhost:port/getOSFile?file=base64(imgPath).replace('=','$');//url param不允许'='字符，所以替换成'$'
            string fullImagePath =
                Http.UrlEncode($"http://{httpServiceIp}:{httpServicePort}/getOSFile?file={base63Path}");
            OpenTangoDrawWindow(
                subsystemTangoDrawUrl,
                displayName + "&images=" + fullImagePath,
                roomKey,
                "TangoDraw",
                true);
        }

        public void OpenStartShareWindow()
        {
            RunOnUIThread((Action)(() =>
            {
                _startShareWindow = new FormStartShare(this);
                _startShareWindow.TopMost = _videoPanel.FindForm().TopMost;
                Log($"弹出共享窗口时，跟随主窗口置顶窗台同步 TopMost -> {_startShareWindow.TopMost}");

                _startShareWindow.ShowDialog(_videoPanel);
            }));
        }

        public void OpenAVCheckWindow()
        {
            RunOnUIThread((Action)(() =>
            {
                // String url =
                //     $"http://{_videoManager.httpServer.ServerIP}:{_videoManager.httpServer.ServerPort}/vue_app_av_check/av_check.html";
                String url =
                    $"{Application.StartupPath}\\wwwroot\\vue_app_av_check\\av_check.html";

                _avCheckWindow = new CefForm(666, "音视频设备检测", url, true);
                // _avCheckWindow.SetSizeRatioAndCenterInScreen(0.5, 0.7);
                if (_videoPanel?.FindForm() != null)
                {
                    _avCheckWindow.TopMost = _videoPanel.FindForm().TopMost;
                    Log($"弹出音视频检测窗口时，跟随主窗口置顶窗台同步 TopMost -> {_avCheckWindow.TopMost}");
                }

                _avCheckWindow.ShowDialog(_videoPanel);
            }));
        }

        public void DismissAVCheckWindow()
        {
            RunOnUIThread((Action)(() => { _avCheckWindow?.Hide(); }));
        }

        public void OpenSettingsWindow()
        {
            RunOnUIThread((Action)(() =>
            {
                _settingsWindow = new FormSettings(this);
                _settingsWindow.MinimumSize = new Size(975, 500);
                if (_videoPanel?.FindForm() != null)
                {
                    _settingsWindow.TopMost = _videoPanel.FindForm().TopMost;
                    Log($"弹出设置窗口时，跟随主窗口置顶窗台同步 TopMost -> {_settingsWindow.TopMost}");
                }

                _settingsWindow.ShowDialog(_videoPanel);
            }));
        }

        public void OpenMessageWindow(string message)
        {
            if (null == _formMessage)
            {
                RunOnUIThread((Action)(() =>
                {
                    _formMessage = new FormMessage();
                    _formMessage.ShowWithParent(_videoPanel.FindForm(), message);
                }));
            }
            else
            {
                RunOnUIThread((Action)(() => { _formMessage.RefreshMessage(message); }));
            }
        }

        public void DismissMessageWindow()
        {
            if (null != _formMessage)
            {
                RunOnUIThread((Action)(() =>
                {
                    _formMessage.Hide();
                    _formMessage = null;
                }));
            }
        }


        public void OpenTencentRooms()
        {
            string appName = "C:\\Program Files\\Tencent\\TencentMeetingRooms\\TencentMeetingRooms.exe"; // 要检测的应用
            string appPath = Util.Util.CheckIfAppInstalled(appName);

            if (!string.IsNullOrEmpty(appPath))
            {
                // MessageBox.Show($"应用已安装: {appName}\n路径: {appPath}", "提示");
                try
                {
                    Process.Start(appPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法启动应用: {ex.Message}", "错误");
                }
            }
            else
            {
                MessageBox.Show($"未检测到应用: {appName}", "提示");
            }
        }
        //--------------------------------------------------------------------------------------------------------------

        public void SetVideoVisible(bool visible)
        {
            RunOnUIThread((Action)(() => { _videoPanel.Visible = visible; }));
        }

        public void SetVideoLocationSize(int x, int y, int width, int height)
        {
            RunOnUIThread((Action)(() => { layoutController.SetRendererContainerSize(x, y, width, height); }));
        }

        public void SetTitle(string title)
        {
            // RunOnUIThread((Action)(() => { videoPanel.Visible = visible; }));
        }

        public void SetTitleVisible(bool visible)
        {
        }

        public void SetDebugEnable(bool enable)
        {
            enableVideoDebug = enable;

            RunOnUIThread((Action)(() =>
            {
                if (enable)
                {
                    connector.EnableDebug(7776, Config.GetStringConfig("LogFilter"));
                }
                else
                {
                    connector.DisableDebug();
                }
            }));
            Log("SetDebugEnable(enable:{0})", enable);
        }

        public void SetAutoAnswer(bool auto)
        {
            Config.SetStringConfig("tango_auto_answer", auto ? "true" : "false");
            Log("SetAutoAnswer(auto:{0})", auto);
        }

        #endregion

        #region ParseInfo2Json

        public string GetVideoDevicesListJson(int cameraIndex)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");

            string[] items;

            if (cameraIndex == 0 || cameraIndex == 1 || cameraIndex == 2)
            {
                items = cameraManager.cameras.Keys.ToArray();
            }
            // else if (cameraIndex == 2)
            // {
            //     items = cameraManager.cameras.Keys.ToArray();
            // }
            else if (cameraIndex == 3)
            {
                items = monitorShareManager.monitorShares.Keys.ToArray();
            }
            else if (cameraIndex == 4)
            {
                items = windowShareManager.windowShares.Keys.ToArray();
            }
            else
            {
                return null;
            }

            for (var i = 0; i < items.Length; i++)
            {
                string camName = items[i];
                sb.Append("\"").Append(camName).Append("\"");
                if (i != items.Length - 1)
                {
                    sb.Append(",");
                }
            }

            sb.Append("]");

            return sb.ToString();
        }

        public string GetCameraObjectListJson(int cameraIndex)
        {
            if (cameraIndex >= 0 && cameraIndex < 3)
            {
                List<CameraObj> cameraObjs = cameraManager.GetCameraObjectList(cameraIndex);
                return null != cameraObjs ? JsonConvert.SerializeObject(cameraObjs) : "[]";
            }

            return "[]";
        }

        public string GetSpeakersListJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");

            String[] items = speakerManager.speakers.Keys.ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                string spkName = items[i];
                sb.Append("\"").Append(spkName).Append("\"");
                if (i != items.Length - 1)
                {
                    sb.Append(",");
                }
            }

            sb.Append("]");

            return sb.ToString();
        }

        public String GetSpeakerJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{")
                .Append("\"inUse\":\"").Append(speakerManager.lastSelectedDeviceName).Append("\",")
                .Append("\"list\":").Append(GetSpeakersListJson())
                .Append("}");
            return sb.ToString();
        }

        public string GetMicrophonesListJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");

            String[] items = microphoneManager.mics.Keys.ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                string micName = items[i];
                sb.Append("\"").Append(micName).Append("\"");
                if (i != items.Length - 1)
                {
                    sb.Append(",");
                }
            }

            sb.Append("]");

            return sb.ToString();
        }

        public String GetMicrophoneJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{")
                .Append("\"inUse\":\"").Append(microphoneManager.lastSelectedDeviceName).Append("\",")
                .Append("\"list\":").Append(GetMicrophonesListJson())
                .Append("}");
            return sb.ToString();
        }

        /**
         * 生成视频源数据
         */
        public string GetVideoSourcesJson()
        {
            string json = JsonConvert.SerializeObject(layoutController.GetAvailableVideoSources());
            return "{\"code\":" + 0 + ",\"data\":" + json + "}";
        }

        public string GetIODeviceStateJson()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{");

            sb.Append("\"isMuteCamera\":").Append(cameraPrivacyOnAppLaunch ? "true" : "false").Append(",");
            sb.Append("\"isMuteMic\":").Append(micPrivacyOnAppLaunch ? "true" : "false").Append(",");
            sb.Append("\"isMuteSpeaker\":").Append(speakerPrivacyOnAppLaunch ? "true" : "false").Append(",");

            sb.Append("\"isLockMuteCamera\":").Append(isLockMuteCamera ? "true" : "false").Append(",");
            sb.Append("\"isLockMuteMic\":").Append(isLockMuteMic ? "true" : "false").Append(",");
            sb.Append("\"isLockMuteSpeaker\":").Append(isLockMuteSpeaker ? "true" : "false");

            sb.Append("}");

            return "{\"code\":" + 0 + ",\"data\":" + sb + "}";
        }

        public string GetSystemStatusJson()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{");

            //系统状态

            sb.Append("\"sdkVersion\":\"").Append(connector.GetVersion()).Append("\",");
            sb.Append("\"appVersionCode\":\"").Append(VERSION).Append("\",");
            sb.Append("\"appVersionNumber\":").Append(VERSION_NUMBER).Append(",");
            sb.Append("\"appName\":\"").Append(Config.GetStringConfig("app_name")).Append("\",");
            sb.Append("\"clientName\":\"").Append(Config.GetStringConfig("client_name")).Append("\",");
            sb.Append("\"localIp\":\"").Append(Util.Util.GetLocalIp()).Append("\",");
            sb.Append("\"winScaling\":\"").Append(_winScaling).Append("\",");
            sb.Append("\"windowNumber\":").Append(layoutController.GetWindowNumber()).Append(",");
            sb.Append("\"enableDebug\":").Append(enableVideoDebug ? "true" : "false").Append(",");
            sb.Append("\"isConnected\":").Append(isConnected ? "true" : "false").Append(",");
            sb.Append("\"connectionStatusCode\":").Append(connectionStatusCode).Append(",");
            sb.Append("\"snapshotFolder\":\"").Append(Http.UrlEncode(VideoFrameListener._savePath)).Append("\",");

            sb.Append("\"apiCode\":\"").Append(apiCode).Append("\",");

            //入会状态
            sb.Append("\"curRoomKey\":\"").Append(roomKey).Append("\",");
            sb.Append("\"curRoomName\":\"").Append(roomName).Append("\",");

            sb.Append("\"gatewayPrefixH323\":\"").Append(Config.GetStringConfig("GatewayPrefixH323")).Append("\",");
            sb.Append("\"gatewayPrefixSIP\":\"").Append(Config.GetStringConfig("GatewayPrefixSIP")).Append("\",");

            sb.Append("\"isMuteCamera\":")
                .Append(cameraPrivacyOnAppLaunch ? "true" : "false")
                .Append(",");
            sb.Append("\"isMuteMic\":")
                .Append(micPrivacyOnAppLaunch ? "true" : "false")
                .Append(",");
            sb.Append("\"isMuteSpeaker\":")
                .Append(speakerPrivacyOnAppLaunch ? "true" : "false")
                .Append(",");

            sb.Append("\"isLockMuteCamera\":")
                .Append(isLockMuteCamera ? "true" : "false")
                .Append(",");
            sb.Append("\"isLockMuteMic\":")
                .Append(isLockMuteMic ? "true" : "false")
                .Append(",");
            sb.Append("\"isLockMuteSpeaker\":")
                .Append(isLockMuteSpeaker ? "true" : "false")
                .Append(",");


            sb.Append("\"enableRecording\":").Append(Config.GetStringConfig("enableRecording")).Append(",");
            sb.Append("\"enableEndpointMode\":").Append(Config.GetStringConfig("enableEndpointMode")).Append(",");
            sb.Append("\"useDefaultLayout\":").Append(Config.GetStringConfig("UseDefaultLayout")).Append(",");
            sb.Append("\"isAutoAssignRenderer\":")
                .Append(layoutController.config.AutoAssignRenderer ? "true" : "false")
                .Append(",");
            sb.Append("\"displayGuestVideo\":")
                .Append(layoutController.config.DisplayGuestVideo ? "true" : "false")
                .Append(",");
            sb.Append("\"displayGatewayVideo\":")
                .Append(layoutController.config.DisplayGatewayVideo ? "true" : "false")
                .Append(",");
            sb.Append("\"displaySelfViewInMeeting\":")
                .Append(layoutController.config.DisplaySelfViewInMeeting ? "true" : "false")
                .Append(",");

            //web配置
            sb.Append("\"web_ui_title\":\"").Append(Config.GetStringConfig("web_ui_title")).Append("\",");
            sb.Append("\"web_ui_room_label_display_name\":\"")
                .Append(Config.GetStringConfig("web_ui_room_label_display_name")).Append("\",");
            sb.Append("\"web_ui_usage_mode_title_visible\":")
                .Append(Config.GetStringConfig("web_ui_usage_mode_title_visible")).Append(",");
            sb.Append("\"web_ui_module_contacts_visible\":")
                .Append(Config.GetStringConfig("web_ui_module_contacts_visible")).Append(",");
            sb.Append("\"web_ui_module_rooms_visible\":")
                .Append(Config.GetStringConfig("web_ui_module_rooms_visible")).Append(",");
            sb.Append("\"web_ui_module_callsip323_visible\":")
                .Append(Config.GetStringConfig("web_ui_module_callsip323_visible")).Append(",");
            sb.Append("\"web_ui_module_profile_visible\":")
                .Append(Config.GetStringConfig("web_ui_module_profile_visible")).Append(",");
            sb.Append("\"web_ui_contact_detail_join_btn_visible\":")
                .Append(Config.GetStringConfig("web_ui_contact_detail_join_btn_visible")).Append(",");
            sb.Append("\"web_ui_create_room_btn_visible\":")
                .Append(Config.GetStringConfig("web_ui_create_room_btn_visible")).Append(",");
            sb.Append("\"web_ui_logo\":\"").Append(Config.GetStringConfig("web_ui_logo")).Append("\",");
            sb.Append("\"web_webrtc_site\":\"").Append(Config.GetStringConfig("web_webrtc_site")).Append("\",");
            // sb.Append("\"web_ui_usage_mode_default\":\"").Append(Config.GetStringConfig("web_ui_usage_mode_default"))
            //     .Append("\",");

            //摄像头
            sb.Append("\"camera1\":")
                .Append(GetCameraJson(0))
                .Append(",");
            sb.Append("\"camera2\":")
                .Append(GetCameraJson(1))
                .Append(",");
            sb.Append("\"camera3\":")
                .Append(GetCameraJson(2))
                .Append(",");
            sb.Append("\"camera4\":")
                .Append(GetCameraJson(3))
                .Append(",");
            sb.Append("\"camera5\":")
                .Append(GetCameraJson(4))
                .Append(",");

            //麦克风
            sb.Append("\"microphones\":{")
                .Append("\"inUse\":\"").Append(microphoneManager.micInUse).Append("\",")
                .Append("\"list\":").Append(GetMicrophonesListJson())
                .Append("},");

            //扬声器
            sb.Append("\"speakers\":{")
                .Append("\"inUse\":\"").Append(speakerManager.spkInUse).Append("\",")
                .Append("\"list\":").Append(GetSpeakersListJson())
                .Append("},");

            //显示器数据
            sb.Append("\"displays\":[");
            for (int i = 0;
                 i < layoutController._rendererContainers.Count;
                 i++)
            {
                VideoRendererContainer container = layoutController._rendererContainers[i];
                sb.Append("{");
                string deviceName = i == 0
                    ? "主窗口"
                    : "扩展窗口" + i;
                sb.Append("\"deviceName\":\"").Append(deviceName + "\",");
                sb.Append("\"index\":").Append(i).Append(",");
                sb.Append("\"width\":").Append(container._container.Width).Append(",");
                sb.Append("\"height\":").Append(container._container.Height).Append(",");
                sb.Append("\"maxRenderersNum\":").Append(container.GetAvailableRendererNumber())
                    .Append(",");
                sb.Append("\"supportedGridLayoutNumber\":").Append(container.GetSupportedGridLayoutNumberJson())
                    .Append(",");
                sb.Append("\"forceRenderingNum\":").Append(container.curFixedLayoutNumber);
                sb.Append("}");
                if (i != (layoutController._rendererContainers.Count - 1)) sb.Append(",");
            }

            sb.Append("]");

            //Tango
            sb.Append(",\"tango\":{")
                // .Append("\"isLoggedIn\":").Append(form.tangoService.loggedIn ? "true" : "false").Append(",")
                // .Append("\"connected\":").Append(form.tangoService.connected ? "true" : "false").Append(",")
                // .Append("\"session\":")
                // .Append(String.IsNullOrEmpty(form.tangoService.sessionJson) ? "null" : form.tangoService.sessionJson)
                // .Append(",")
                .Append("\"isAutoLogin\":").Append(Config.GetStringConfig("tango_auto_login")).Append(",")
                .Append("\"isAutoAnswer\":").Append(Config.GetStringConfig("tango_auto_answer")).Append(",")
                .Append("\"isLectureModeExcept\":").Append(Config.GetStringConfig("tango_lecture_mode_except"))
                .Append(",")
                .Append("\"server\":\"").Append(Config.GetStringConfig("tango_server_url")).Append("\",")
                .Append("\"serverHostScheme\":\"").Append(Config.GetBooleanConfig("tango_use_https") ? "https" : "http")
                .Append("\",")
                .Append("\"username\":\"").Append(Config.GetStringConfig("tango_username")).Append("\",")
                .Append("\"password\":\"").Append(Config.GetStringConfig("tango_password")).Append("\",")
                .Append("\"userHardDeviceList\":")
                .Append(JsonConvert.SerializeObject(_sipContactsManager.userHardDeviceList))
                .Append("}");

            //Guest
            sb.Append(",\"guest\":{")
                .Append("\"server\":\"").Append(Config.GetStringConfig("guest_server_url")).Append("\",")
                .Append("\"roomKey\":\"").Append(Config.GetStringConfig("guest_room_key")).Append("\",")
                .Append("\"displayName\":\"").Append(Config.GetStringConfig("guest_display_name")).Append("\",")
                .Append("\"subsystemTangoDrawUrl\":\"").Append(Config.GetStringConfig("guest_subsystemTangoDrawUrl"))
                .Append("\"")
                .Append("}");

            sb.Append("}");

            return "{\"code\":" + 0 + ",\"data\":" + sb.ToString() + "}";
        }

        public string GetCameraJson(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex > 4) return null;
            StringBuilder sb = new StringBuilder();
            CameraConfig cameraConfig = null;

            switch (cameraIndex)
            {
                case 0:
                case 1:
                case 2:
                    cameraConfig = cameraManager.GetVideoSourceConfig(cameraIndex);
                    break;
                case 3:
                    cameraConfig = monitorShareManager._cameraConfig;
                    break;
                case 4:
                    cameraConfig = windowShareManager._cameraConfig;
                    break;
            }

            if (null == cameraConfig) return null;

            int sourceType = (cameraIndex + 1); //配置中的索引按生活序号
            sb.Append("{")
                .Append("\"index\":").Append(sourceType).Append(",")
                .Append("\"sourceType\":").Append(sourceType).Append(",")
                .Append("\"name\":\"").Append(Config.GetStringConfig("camera_" + sourceType + "_name")).Append("\",")
                .Append("\"enable\":").Append(Config.GetStringConfig("camera_" + sourceType + "_enable")).Append(",")
                .Append("\"width\":").Append(cameraConfig.width).Append(",")
                .Append("\"height\":").Append(cameraConfig.height).Append(",")
                .Append("\"resolution\":\"").Append(cameraConfig.resolution).Append("\",")
                .Append("\"framerate\":").Append(cameraConfig.framerate).Append(",")
                .Append("\"resolutionProfile\":").Append(cameraConfig.resolutionProfile).Append(",")
                .Append("\"framerateProfile\":").Append(cameraConfig.framerateProfile).Append(",")
                .Append("\"inUse\":\"").Append(cameraConfig.inUse).Append("\",")
                .Append("\"list\":").Append(GetVideoDevicesListJson(cameraIndex)).Append(",")
                .Append("\"listObj\":").Append(GetCameraObjectListJson(cameraIndex))
                .Append("}");
            return sb.ToString();
        }

        #endregion

        #region 房间进出回调处理

        private void PrepareForJoin()
        {
            // RunOnUIThread((Action)(() =>
            // {
            StopPreviewCameraFromExtFormOnMain();

            var nLastCameraName = cameraManager.GetLastCameraName();

            var nAssignLastSelectedCamera = cameraManager.AssignLastSelectedCamera();
            if (IsEndpointMode())
            {
                OpenMessageWindow("正在加入房间中...");
                //终端入会时，
                if (!String.Equals(CameraListener.DEVICE_NONE, nAssignLastSelectedCamera) &&
                    String.Equals(nLastCameraName, nAssignLastSelectedCamera))
                {
                    cameraManager.StartRenderingCamera();
                }
            }

            cameraManager.AssignLastSelectedVirtualSource();
            cameraManager.AssignLastSelectedContent();

            microphoneManager.AssignLastSelectedDevice();
            speakerManager.AssignLastSelectedDevice();
            // }));

            onDeviceMuteListener.OnDeviceMuteChanged(0, cameraPrivacyOnAppLaunch, isLockMuteCamera);
            onDeviceMuteListener.OnDeviceMuteChanged(1, micPrivacyOnAppLaunch, isLockMuteMic);
            onDeviceMuteListener.OnDeviceMuteChanged(2, speakerPrivacyOnAppLaunch, isLockMuteSpeaker);
        }

        public void PrepareForLeave()
        {
            StartPreviewCameraAtExtForm();

            //关闭234路源
            if (IsEndpointMode())
            {
                DismissMessageWindow();
            }
            else
            {
                cameraManager.CloseCamera();
            }

            cameraManager.CloseContent();
            cameraManager.CloseVirtualSource();
            monitorShareManager.Close();
            windowShareManager.Close();

            microphoneManager.Close();
            speakerManager.Close();

            isLockMuteCamera = false;
            isLockMuteMic = false;
            isLockMuteSpeaker = false;
            socketService.SendSystemStatusUpdatedEventToAll();
        }

        public void OnSuccess()
        {
            _logger.Info("-----> Connection On Success");

            isConnected = true;

            // PrepareForJoin();//调整到连接入会前，入会后再调用的话，会因为语音激励同时触发导致显示逻辑混乱。

            layoutController.RegisterParticipantsChangedListener();
            layoutController.DismissExtWindowTips();

            if (IsEndpointMode())
            {
                DismissMessageWindow();
                SetVideoVisible(true);
            }
            // else
            // {
            //     if (IsLaunchParamTrue("smallMode")) _smallWindowController.Apply();
            // }

            connectionStatus = "Connected";
            SetConnectionEventHandleCompleted();

            connectionStatusCode = 1;
            socketService?.SendVidyoConnectionStatusEventToAll(connectionStatusCode);

            connectionListener?.OnSuccess();
        }

        public void OnFailure(Connector.ConnectorFailReason reason)
        {
            _logger.Info("-----> Connection On Failure - " + reason.ToString());

            isConnected = false;

            PrepareForLeave();

            connectionStatus = "Failed - " + reason.ToString();
            SetConnectionEventHandleCompleted();

            connectionStatusCode = -1;
            socketService?.SendVidyoConnectionStatusEventToAll(connectionStatusCode);

            connectionListener?.OnFailure(reason);
        }

        public void OnDisconnected(Connector.ConnectorDisconnectReason reason)
        {
            if (reason == Connector.ConnectorDisconnectReason.ConnectordisconnectreasonDisconnected)
            {
                Log("-----> onDisconnected: successfully disconnected, reason = ", reason.ToString());
            }
            else
            {
                // ！！！ 被挂断应该触发Terminated，但windows触发MiscLocalError ！！！
                if (reason == Connector.ConnectorDisconnectReason.ConnectordisconnectreasonTerminated ||
                    reason == Connector.ConnectorDisconnectReason.ConnectordisconnectreasonMiscLocalError)
                {
                    var promptKey = LaunchParameters.PromptForTerminated;
                    var text = "您已被管理员挂断";
                    if (_launchParams.ContainsKey(promptKey) &&
                        !String.IsNullOrEmpty(_launchParams[promptKey]))
                    {
                        text = _launchParams[promptKey];
                        text = Http.UrlDecode(text);
                    }

                    MessageBox.Show(text);
                }

                Log("-----> onDisconnected: unexpected disconnection, reason = ", reason.ToString());
            }

            isConnected = false;

            layoutController.UnregisterParticipantsChangedListener();

            PrepareForLeave();

            layoutController.Restore();
            layoutController.ShowExtWindowTips();
            if (IsEndpointMode()) SetVideoVisible(false);

            connectionStatusCode = 0;
            socketService?.SendVidyoConnectionStatusEventToAll(connectionStatusCode);

            connectionListener?.OnDisconnected(reason);
        }

        public void OnReconnecting(uint attempt, uint attemptTimeout, Connector.ConnectorFailReason reason)
        {
            //status code : 2
            Log("-----> Connection On OnReconnecting attempt->{0},attemptTimeout->{1} reason->{2}",
                attempt,
                attemptTimeout,
                reason.ToString());

            if (IsEndpointMode())
            {
                OpenMessageWindow("网络异常，正在尝试重新连接...");
            }

            connectionStatusCode = 2;
            socketService?.SendVidyoConnectionStatusEventToAll(connectionStatusCode);
        }

        public void OnReconnected()
        {
            //status code : 3
            Log("-----> Connection On Reconnected");

            if (IsEndpointMode())
            {
                DismissMessageWindow();
            }

            connectionStatusCode = 3;
            socketService?.SendVidyoConnectionStatusEventToAll(connectionStatusCode);
        }

        public void OnConferenceLost(Connector.ConnectorFailReason reason)
        {
            //status code : -2
            Log("-----> Connection On Conference Lost reason->{0}", reason.ToString());

            if (IsEndpointMode())
            {
                OpenMessageWindow("重连失败，正在继续尝试...");
            }

            connectionStatusCode = -2;
            socketService?.SendVidyoConnectionStatusEventToAll(connectionStatusCode);
        }

        public void OnChatMessageReceived(Participant participant, ChatMessage chatMessage)
        {
            Log("-----> {0} said : {1}", participant.GetName(), chatMessage.body);
            string chatStr = chatMessage.body;
            if (!String.IsNullOrEmpty(chatStr))
            {
                if (chatStr.StartsWith("[action]$"))
                {
                    HandleActionMessage(chatStr);
                }
                else
                {
                    HandleChatMessage(participant.GetName(), chatStr);
                }
            }
        }

        private void HandleActionMessage(string message)
        {
            string[] sp = message.Split('$');
            if (sp.Length >= 2)
            {
                string json = sp[1];
                JObject jobj;
                // Dictionary<string, string> map = JsonConvert.DeserializeObject(json) as Dictionary<string, string>;
                try
                {
                    jobj = (JObject)JsonConvert.DeserializeObject(json);
                }
                catch (Exception e)
                {
                    Error($"Error! Received a bad json message.\n{e.ToString()}");
                    return;
                }

                if (jobj != null)
                {
                    string action = (string)jobj["action"];

                    if (!String.IsNullOrEmpty(action) && action.Equals("openTangoDrawWindow"))
                    {
                        string url = (string)jobj["url"];
                        string roomId = (string)jobj["roomId"];
                        if (!String.IsNullOrEmpty(url) && !String.IsNullOrEmpty(roomId))
                            OpenTangoDrawWindow(url, displayName, roomId, "电子白板", false);
                    }

                    if (!String.IsNullOrEmpty(action) && action.Equals("closeTangoDrawWindow"))
                    {
                        if (null != _tangoDrawWindow && !_tangoDrawWindow.isClosed)
                        {
                            RunOnUIThread((Action)(() => { _tangoDrawWindow.Close(); }));
                        }
                    }

                    if (!String.IsNullOrEmpty(action) && action.Equals("updateRecordingState"))
                    {
                        socketService?.SendFunctionStateEventToAll(true);
                    }
                }
            }
        }

        private void HandleChatMessage(string sender, string message)
        {
            socketService?.SendChatMessageEventToAll(new ChatMessageObj(sender, message));
        }

        public void OnModerationCommandReceived(
            Device.DeviceType deviceType,
            Room.RoomModerationType moderationType,
            bool state)
        {
            Log($"-----> On Moderation Command Received ({deviceType},{moderationType},{state})");
            // 
            if (deviceType == Device.DeviceType.DevicetypeLocalCamera)
            {
                if (moderationType == Room.RoomModerationType.RoommoderationtypeHardMute)
                {
                    isLockMuteCamera = state;
                    onDeviceMuteListener.OnDeviceMuteChanged(0, state, isLockMuteCamera);
                }
                else if (moderationType == Room.RoomModerationType.RoommoderationtypeSoftMute)
                {
                    onDeviceMuteListener.OnDeviceMuteChanged(0, state, isLockMuteCamera);
                }
            }
            else if (deviceType == Device.DeviceType.DevicetypeLocalMicrophone)
            {
                if (moderationType == Room.RoomModerationType.RoommoderationtypeHardMute)
                {
                    isLockMuteMic = state;
                    onDeviceMuteListener.OnDeviceMuteChanged(1, state, isLockMuteMic);
                }
                else if (moderationType == Room.RoomModerationType.RoommoderationtypeSoftMute)
                {
                    onDeviceMuteListener.OnDeviceMuteChanged(1, state, isLockMuteMic);
                }
            }

            socketService.SendModerationCommandEventToAll(deviceType, moderationType, state);
        }

        public void OnModerationResult(
            Participant participant,
            Connector.ConnectorModerationResult result,
            Connector.ConnectorModerationActionType action,
            string requestId)
        {
            Log($"-----> On Moderation Result ({participant},{result},{action},{requestId})");
        }

        public void RecorderInCall(bool hasRecorder, bool isPaused)
        {
            Log($"-----> Recorder In Call (hasRecorder : {hasRecorder} , isPaused : {isPaused})");
        }

        public void OnError(Connector.ConnectorErrorCode error, string apiName)
        {
            Log($"-----> On VideoSDK Error ({error},{apiName})");
        }

        #endregion

        #region WaitHandle

        public AutoResetEvent connectionEventHandle;

        public AutoResetEvent CreateWaitHandle()
        {
            connectionEventHandle = new AutoResetEvent(false);
            return connectionEventHandle;
        }

        /**
         * 用于Http请求入会的响应同步
         */
        public void SetConnectionEventHandleCompleted()
        {
            connectionEventHandle?.Set();
            // connectionEventHandle = null;
        }

        #endregion

        public IAsyncResult RunOnUIThread(Delegate method)
        {
            return _videoPanel.BeginInvoke(method);
        }

        private void Log(string content)
        {
            _logger.Info("[VideoManager] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[VideoManager] " + content, args);
        }

        private void Error(string content)
        {
            _logger.Error("[VideoManager] " + content);
        }

        public interface OnDeviceMuteListener
        {
            /*
             * deviceType: 0-camera 1-mic 2-speaker
             */
            void OnDeviceMuteChanged(int deviceType, bool mute, bool locked);
        }

        public interface OnVideoDeviceChangedListener
        {
            void OnVideoDeviceChanged(int sourceType, String deviceId);
        }
    }
}