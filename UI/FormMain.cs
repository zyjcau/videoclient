using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using EventBus;
using NLog;
using VideoClient.Entity;
using VideoClient.Manager;
using VideoClient.Util;
using VidyoClient;
using VidyoConnector.Properties;
using Application = System.Windows.Forms.Application;

//https://docs.microsoft.com/zh-cn/dotnet/api/system.drawing.imaging.pixelformat?view=dotnet-plat-ext-3.1

namespace VideoClient.UI
{
    public partial class FormMain : Form, Connector.IConnect, VideoManager.OnDeviceMuteListener,
        VideoManager.OnVideoDeviceChangedListener, CEFKeyBoardHander.IOnFastKeyClick
    {
        #region 声明

        private ChromiumWebBrowser _appBrowser;
        public VideoManager videoManager = VideoManager.GetInstance();

        string _title;

        public FormMain(string[] args)
        {
            InitializeComponent();

            _title = Config.GetStringConfig("app_name");
            RefreshTitleWithTopMost();
            Icon = new Icon(Application.StartupPath + "\\wwwroot\\res\\lss.ico");

            // ChangeUiWindowMaximizedMode();
            ChangeUiWindowNormalMode();

            videoManager.ResetLaunchParams(args);

            SimpleEventBus.GetDefaultEventBus().Register(this);
        }

        /**
         * 处理多次打开程序的情况，新进程会发送消息到旧进程，旧进程会把自己的窗口激活
         * 此方法无法实现窗口隐藏到托盘后继续接收消息，已采取thread message实现，见Program
         */
        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == Program.WM_COPYDATA)
            {
                CommandStruct param = (CommandStruct)Marshal.PtrToStructure(m.LParam, typeof(CommandStruct));
                Log($"wnd param -> {param.lpData}");

                // if (!String.IsNullOrEmpty(param.lpData))
                // {
                //     string[] args = JsonConvert.DeserializeObject<string[]>(param.lpData);
                //     SaveLaunchParams(args);
                //     ParseLaunchParameters(args);
                //     LoadAppPage();
                //     SetActivate();
                // }
                // else
                // {
                //     SetActivate();
                // }
            }

            base.DefWndProc(ref m);
        }

        [EventSubscriber]
        public void OnEventBusMsgReceived(string message)
        {
            Log($"OnEventBusMsgReceived -> {message}");
            // Log($"on event bus invoke {message}");
            if (String.Equals(message, "AppExit"))
            {
                RunOnUIThread((Action)(AppClose));
            }
            else if (String.Equals(message, "AppActivate"))
            {
                RunOnUIThread((Action)(SetActivate));
            }
            else if (String.Equals(message, "AppMinimize"))
            {
                RunOnUIThread((Action)(SetDeactivate));
            }
        }

        #endregion

        #region Form Lifecycle

        private void FormVideo_Load(object sender, EventArgs e)
        {
            //检测必要环境库
            if (!CheckRuntimeEnvironment()) return;
            //初始化Chromium
            InitCef();
            // //初始化小窗菜单
            // InitSmallWindowController();
            //初始化菜单栏（用于终端模式）
            InitMenuBar();
            //初始化UI主程序
            InitAppBrowser();
            //检测模块（网络服务）是否可用
            if (!CheckModuleAvailable()) return;
            //初始化视频组件
            InitVideoManager();
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            SyncMenuSize();
            SyncMinorStreamSettingsBrowserSize();

            if (null != videoManager && videoManager.IsFullscreenMode()) return;

            SyncAppBrowserSize();

            if (null != videoManager && videoManager.IsEndpointMode())
            {
                videoManager.SetVideoLocationSize(0, 0, ClientRectangle.Width, ClientRectangle.Height);
            }
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            Log($"Form_Closing reason -> {e.CloseReason}");

            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (videoManager.isConnected)
                {
                    MessageBox.Show("请您先挂断后，再关闭软件。");
                }
                else
                {
                    SetDeactivate();
                }

                e.Cancel = true;
                return;
            }

            if (null != videoManager && videoManager.leaveAndExitApp)
            {
                e.Cancel = false;
            }
        }

        private void AppClose()
        {
            Log("App Closing");

            _mouseHook?.Stop();

            _appBrowser?.Load("about:blank");

            if (null != videoManager)
            {
                videoManager.leaveAndExitApp = true;
                videoManager.Uninit();
            }

            Application.Exit();
        }

        #endregion

        #region Init

        private bool CheckRuntimeEnvironment()
        {
            Log("-----CheckRuntimeEnvironment begin");

            EnvCheckUtil.PrintEnvironmentInfo();

            //检测 操作系统是否兼容软件
            if (!EnvCheckUtil.IsOsCompatibleWithApp())
            {
                Log("-----CheckRuntimeEnvironment failed, Is os incompatible with app.");
                DialogResult result =
                    MessageBox.Show(Resources.alert_is_os_not_compatible_with_app, "", MessageBoxButtons.OK);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }

                return false;
            }

            //检测 DotNet运行时库是否安装
            var isInstallDotNet = EnvCheckUtil.IsInstallDotNet(EnvCheckUtil.IsWin10() ? "4.7.1" : "4.5.2");
            if (!isInstallDotNet)
            {
                Log("-----CheckRuntimeEnvironment failed, Is os not installed Dot Net Framework.");
                DialogResult result =
                    MessageBox.Show(Resources.alert_dot_net_framework_not_exist, "", MessageBoxButtons.OK);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }

                return false;
            }

            //检测 VC++运行时库是否安装
            var isInstallVc1519 = EnvCheckUtil.IsInstallVc(new[]
                { "C++", "2015-2019", Environment.Is64BitProcess ? "x64" : "x86" });
            var isInstallVc1522 = EnvCheckUtil.IsInstallVc(new[]
                { "C++", "2015-2022", Environment.Is64BitProcess ? "x64" : "x86" });
            if (!isInstallVc1519 && !isInstallVc1522)
            {
                Log("-----CheckRuntimeEnvironment failed, Is os not installed VC++.");
                DialogResult result =
                    MessageBox.Show(Resources.alert_vc_runtime_not_exist, "", MessageBoxButtons.OK);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }

                return false;
            }

            Log("---CheckRuntimeEnvironment completed, is everything ok!");
            return true;
        }

        private bool CheckModuleAvailable()
        {
            Log($"StartupPath -> {Application.StartupPath}");
            Log($"ExecutablePath -> {Application.ExecutablePath}");

            //检测端口是否被占用
            int portInt = Config.GetIntegerConfig("http_server_port");
            bool httpPortInUse = Util.Util.PortInUse(portInt);
            bool wsPortInUse = Util.Util.PortInUse(portInt + 1);

            Log("Http Port In Use -> {}", httpPortInUse);
            Log("WS Port In Use -> {}", wsPortInUse);

            if (httpPortInUse)
            {
                DialogResult result =
                    MessageBox.Show(portInt + Resources.alert_port_is_in_used, "", MessageBoxButtons.OK);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }

                return false;
            }

            if (wsPortInUse)
            {
                DialogResult result =
                    MessageBox.Show(portInt + Resources.alert_port_is_in_used, "", MessageBoxButtons.OK);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }

                return false;
            }

            return true;
        }

        public void ResetAndParseLaunchParams(string[] args)
        {
            videoManager.ResetAndParseLaunchParams(args);
        }

        /**
         * 使窗口到前台
         */
        public void SetActivate()
        {
            Show();
            Activate();
        }

        /**
         * 使窗口到后台
         */
        public void SetDeactivate()
        {
            Hide();
        }

        private MenuItem notifyIconMenuItemTopMost, notifyIconMenuItemMinimize;

        private void InitNotifyIcon()
        {
            components = new Container();

            ContextMenu notifyIconMenu = new ContextMenu();
            //根据启动参数，设置窗口置顶
            if (videoManager.IsLaunchParamTrue("topMost"))
            {
                SetTopMost(true);
            }

            //置顶
            notifyIconMenuItemTopMost = new MenuItem();
            notifyIconMenuItemTopMost.Index = 0;
            notifyIconMenuItemTopMost.Checked = TopMost;
            notifyIconMenuItemTopMost.Text = "设置窗口在最顶层显示";
            notifyIconMenuItemTopMost.Click += (o, args) => { ToggleTopMost(); };

            // //根据启动参数，设置窗口缩小
            // if (videoManager.IsLaunchParamTrue("smallMode"))
            // {
            //     ChangeUiWindowSmallMode();
            // }

            //缩小窗口
            notifyIconMenuItemMinimize = new MenuItem();
            notifyIconMenuItemMinimize.Visible = false;
            notifyIconMenuItemMinimize.Index = 1;
            notifyIconMenuItemMinimize.Text = "缩小窗口到屏幕右下";
            notifyIconMenuItemMinimize.Click += (o, args) => { ChangeUiWindowSmallMode(); };
            //显示窗口
            MenuItem notifyIconMenuItemShow = new MenuItem();
            notifyIconMenuItemShow.Index = 2;
            notifyIconMenuItemShow.Text = "显示";
            notifyIconMenuItemShow.Click += (o, args) => { SetActivate(); };
            //退出程序
            MenuItem notifyIconMenuItemExit = new MenuItem();
            notifyIconMenuItemExit.Index = 3;
            notifyIconMenuItemExit.Text = "退出";
            notifyIconMenuItemExit.Click += (o, args) =>
            {
                if (null != videoManager && videoManager.isConnected)
                {
                    MessageBox.Show("请先退出视频后再尝试...");
                    return;
                }

                SimpleEventBus.GetDefaultEventBus().Post("AppExit", TimeSpan.Zero);
            };
            notifyIconMenu.MenuItems.AddRange(new[]
            {
                notifyIconMenuItemTopMost,
                notifyIconMenuItemMinimize,
                notifyIconMenuItemShow,
                notifyIconMenuItemExit
            });

            NotifyIcon notifyIcon = new NotifyIcon(components);
            notifyIcon.Text = Text;
            notifyIcon.Icon = Icon;
            notifyIcon.Visible = true;
            notifyIcon.ContextMenu = notifyIconMenu;
            notifyIcon.DoubleClick += (o, args) => { SetActivate(); };
        }

        private void RefreshTitleWithTopMost()
        {
            Text = TopMost ? $"{_title} （已置顶）" : _title;
        }

        public void SetTopMost(bool enable)
        {
            TopMost = enable;
            if (null != notifyIconMenuItemTopMost) notifyIconMenuItemTopMost.Checked = TopMost;
            RefreshTitleWithTopMost();
        }

        private void ToggleTopMost()
        {
            TopMost = !TopMost;
            if (null != notifyIconMenuItemTopMost) notifyIconMenuItemTopMost.Checked = TopMost;
            RefreshTitleWithTopMost();
        }

        private void InitCef()
        {
            //配置cef
            CefSettings settings = new CefSettings();
            settings.Locale = "zh-CN";
            settings.AcceptLanguageList = "zh-CN";

            settings.CefCommandLineArgs.Add("disable-gpu", "1"); //禁用GPU，解决部分电脑显示比例不正常的情况
            settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1");

            settings.CefCommandLineArgs.Add("high-dpi-support", "1");
            settings.CefCommandLineArgs.Add("force-device-scale-factor", Util.Util.GetWinScaling(this).ToString());

            // settings.BackgroundColor = Util.Util.ColorToUInt(Color.FromArgb(0, 0, 0, 0));
            settings.CefCommandLineArgs.Add("--disable-application-cache", "1"); //
            settings.CefCommandLineArgs.Add("--disable-session-storage", "1"); //
            settings.CefCommandLineArgs.Add("--enable-media-stream", "1");
            settings.CefCommandLineArgs.Add("–-enable-experimental-web-platform-features", "1"); //启用窗口共享等特性
            settings.CefCommandLineArgs.Add("--disable-web-security", "1"); //关闭同源策略,允许跨域
            settings.CefCommandLineArgs.Add("--disable-site-isolation-trials", "1"); //关闭站点隔离策略,允许跨域
            settings.CefCommandLineArgs.Add("--ignore-urlfetcher-cert-requests", "1"); //忽略证书验证
            settings.CefCommandLineArgs.Add("--ignore-certificate-errors", "1"); //忽略证书验证
            //允许访问私有网络资源，资料：https://source.chromium.org/chromium/chromium/src/+/main:base/base_switches.cc?q=kDisableFeatures&ss=chromium
            //https://blog.csdn.net/weixin_44734310/article/details/125682240
            settings.CefCommandLineArgs.Add("--disable-features", "BlockInsecurePrivateNetworkRequests");
            settings.CefCommandLineArgs.Add("--allow-running-insecure-content",
                "1"); //---！！！测试发现，通过此参数可以实现https页面访问http资源
            // //启用访问本地文件
            // settings.CefCommandLineArgs.Add("allow-file-access-from-files", "1");
            Cef.Initialize(settings);
            //配置JS调用
            CefSharpSettings.WcfEnabled = true;
            // _appBrowser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;//test code 测试崩溃情况
        }

        private void InitAppBrowser()
        {
            //
            string webrootPath = Application.StartupPath + "\\wwwroot";
            string initAddress = webrootPath + "\\launch.html";

            _appBrowser = new ChromiumWebBrowser(initAddress);
            _appBrowser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
            _appBrowser.JavascriptObjectRepository.Register("JSProxy", new HomeJSProxy(), false,
                BindingOptions.DefaultBinder);
            _appBrowser.AddressChanged += OnBrowserAddressChanged;
            // Add it to the form and fill it to the form window.
            panel_cef.Controls.Add(_appBrowser);
            _appBrowser.Dock = DockStyle.Fill;
            _appBrowser.Left = 0;
            _appBrowser.Top = 0;
            // new CefForm(2, "性能分析工具", webrootPath + "\\libs\\logtool\\VideoLog.html", false)
            _appBrowser.KeyboardHandler = new CEFKeyBoardHander(this);

            // _appWindow = new FormTangoApp(initAddress);

            SyncAppBrowserSize();

            SetInternalCefVisible(true);
        }

        public void OnWindowsKeyClick(int windowsKeyCode)
        {
            if ((Keys)windowsKeyCode == Keys.F11)
            {
                RunOnUIThread((Action)(() =>
                {
                    string webrootPath = Application.StartupPath + "\\wwwroot";
                    Util.Util.StartUrl(webrootPath + "\\libs\\logtool\\VideoLog.html");
                }));
            }
            else if ((Keys)windowsKeyCode == Keys.F10)
            {
                Util.Util.OpenFolder(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                     "\\Laisaisi\\VideoClient\\");
            }
        }

        private void InitVideoManager()
        {
            // videoManager = VideoManager.GetInstance();
            videoManager.Init(
                panel_video,
                this,
                this,
                this);

            Task t = new Task(() =>
            {
                try
                {
                    videoManager.InitConfig();
                }
                catch (Exception exception)
                {
                    Log(exception.ToString());
                    throw;
                }

                try
                {
                    videoManager.InitVideoComponent(); //!!! 耗时严重 !!!
                }
                catch (Exception exception)
                {
                    Log(exception.ToString());
                    throw;
                }

                RunOnUIThread((Action)(() =>
                {
                    videoManager.InitManager();
                    videoManager.PrepareForLeave();
                    videoManager.StartSocketService();
                    videoManager.StartHttpService();

                    Log("---init app usage mode (NormalMode,GuestMode,EndpointMode) begin---");
                    if (videoManager.IsEndpointMode())
                    {
                        if (null != videoManager && videoManager.IsFullscreenMode())
                        {
                            ChangeUiFullScreenMode();
                            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                            Util.Util.AssignFormFullScreenToScreen(this, Screen.FromControl(this));
                        }

                        videoManager?.SetVideoVisible(false);
                        videoManager?.SetVideoLocationSize(
                            0,
                            0,
                            ClientRectangle.Width,
                            ClientRectangle.Height);

                        _mouseHook = new MouseHook();
                        _mouseHook.OnMouseActivity += OnMouseUpClick;
                        _mouseHook.Start();

                        Log("Mode : use endpoint mode.");
                    }
                    else
                    {
                        videoManager?.SetVideoVisible(false);

                        // SetInternalCefVisible(true);
                        Log("Mode : use normal mode.");
                    }

                    Log("---init app usage mode end---");

                    OnVideoManagerReady();
                }));
            });
            t.Start();
        }

        public void OnSuccess()
        {
            if (null != videoManager)
            {
                if (videoManager.IsEndpointMode())
                {
                    //显示下拉菜单按钮
                    RunOnUIThread((Action)(() =>
                    {
                        SetInternalCefVisible(false);
                        ShowMenuButton();
                    }));
                }
                else
                {
                    RunOnUIThread((Action)(() =>
                    {
                        notifyIconMenuItemMinimize.Visible = true;
                        if (videoManager.IsLaunchParamTrue("smallMode")) videoManager.ApplySmallWindowMode();
                    }));
                }
            }
        }

        public void OnFailure(Connector.ConnectorFailReason reason)
        {
            if (null != videoManager)
            {
                if (videoManager.IsEndpointMode())
                {
                    RunOnUIThread((Action)(() => { SetInternalCefVisible(true); }));
                }
                else
                {
                    RunOnUIThread((Action)(() => { notifyIconMenuItemMinimize.Visible = false; }));
                }
            }
        }

        public void OnDisconnected(Connector.ConnectorDisconnectReason reason)
        {
            if (null != videoManager)
            {
                if (videoManager.IsEndpointMode())
                {
                    //隐藏下拉菜单按钮
                    RunOnUIThread((Action)(() =>
                    {
                        SetInternalCefVisible(true);
                        DismissMenuButton();
                        DismissMenuPanel();
                        DismissMinorStreamSettingsBrowser();
                    }));
                }
                else
                {
                    RunOnUIThread((Action)(() => { notifyIconMenuItemMinimize.Visible = false; }));
                }
            }
        }

        private void OnVideoManagerReady()
        {
            Controls.SetChildIndex(panel_cef, 3);
            Controls.SetChildIndex(videoManager._smallWindowController.panelHeader, 2);
            Controls.SetChildIndex(videoManager._smallWindowController.panelFooter, 2);
            Controls.SetChildIndex(panel_video, 2);
            Controls.SetChildIndex(menuSwitch, 1);
            Controls.SetChildIndex(menuPanel, 0);
            //
            Resize += Form_Resize;
            FormClosing += Form_Closing;
            //加载启动参数
            videoManager.ParseLaunchParameters();
            videoManager.IsReady = true;
            //
            SyncCameraPinButtonState();
            //初始化mini任务栏
            InitNotifyIcon();
            //根据启动参数加载对应的界面
            LoadAppPage();
            //test code
            // SetInternalCefVisible(false);
        }

        public void LoadAppPage()
        {
            if (null != videoManager)
            {
                string indexPage = "index.html";
                if (videoManager.IsEndpointMode())
                {
                    indexPage = "home.html";
                }
                else if (videoManager.IsGuestMode())
                {
                    indexPage = "vue_app_guest_client/guest.html";
                }

                // _webRoot = $"http://{videoManager.httpServer.ServerIP}:{videoManager.httpServer.ServerPort}";
                string addr =
                    $"{videoManager.GetWebRoot()}/{indexPage}?{videoManager.GetLaunchUrlParam()}&openByClient=true";
                Log($"app load url : {addr}");
                _appBrowser.LoadUrl(addr);
            }
        }

        public void ReloadAppByArgs(string[] args)
        {
            if (null != videoManager && videoManager.isConnected)
            {
                Log("ReloadAppByArgs failed,because video is connected.");
                MessageBox.Show("请您先挂断后，再尝试调用软件。");
                return;
            }

            Log("ReloadAppByArgs Started.");

            // SaveLaunchParams(args);
            // 
            ResetAndParseLaunchParams(args);
            LoadAppPage();
            SetActivate();
        }

        private void SyncAppBrowserSize(bool isFullScreen = false)
        {
            int height = isFullScreen ? Screen.FromControl(this).Bounds.Height : ClientRectangle.Height;
            int width = ClientRectangle.Width;

            Log("SyncCefSize " + Width + " x " + Height + " , " + width +
                "x" + height + " , " + isFullScreen);

            panel_cef.Margin = new Padding(0, 0, 0, 0);
            panel_cef.Location = new Point(0, 0);
            panel_cef.Size = new Size(width, height);
            // panel_cef.Size = new Size(
            //     width,
            //     height - (_isStatusVisible ? statusStrip1.Height : 0));
            if (null != _appBrowser)
            {
                _appBrowser.Width = panel_cef.Width;
                _appBrowser.Height = panel_cef.Height;
            }
        }

        private void OnBrowserAddressChanged(object sender, AddressChangedEventArgs e)
        {
            // _logger.Info("OnBrowserAddressChanged -> {}", e);
        }

        #endregion

        #region EndpointMenu

        private Button menuButton, screenKeyboardButton;
        MouseHook _mouseHook;

        private FlowLayoutPanel menuPanel;
        private int menuPanelHeight = 65;

        private Button
            leaveMeetingButton;

        private ButtonImage2State cameraPinButton,
            menuSwitch,
            muteSpeakerButton,
            camera2Button,
            camera3Button,
            closeMenuButton;

        private ButtonImage3State muteMicButton, muteCameraButton;

        private Panel minorStreamSettingPanel;
        private ChromiumWebBrowser minorStreamSettingBrowser;
        private String minorStreamSettingsHtml = Application.StartupPath + "\\wwwroot\\start_minor_stream.html";

        private void InitMenuBar()
        {
            // btn_menu.Visible = false;

            menuSwitch = new ButtonImage2State(48, "icon_menu_2.png", "icon_menu_2.png", OnMenuClick);
            Controls.Add(menuSwitch);
            // Controls.SetChildIndex(menuSwitch, 1);
            DismissMenuButton();
            menuPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Location = new Point(0, 0),
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(0, 0, 0, 0),
                Size = new Size(ClientRectangle.Width, menuPanelHeight),
                BackColor = Color.FromArgb(0, 36, 36, 36),
                Visible = false
            };
            Controls.Add(menuPanel);
            // Controls.SetChildIndex(menuPanel, 0);
            // menuPanel.BringToFront();

            //关闭菜单 按钮
            closeMenuButton = AddImageButton("icon_close.png", "icon_close.png", OnMenuCloseClick);
            //退出会议 按钮
            leaveMeetingButton = AddMenuButton("退出会议", OnLeaveMeetingClick);
            //camera3 按钮
            camera3Button = AddImageButton(
                "icon_minor_stream_btn.png",
                "icon_minor_stream_sending.png",
                OnCamera3Click);
            //camera2 按钮
            camera2Button = AddImageButton(
                "icon_minor_stream_btn.png",
                "icon_minor_stream_sending.png",
                OnCamera2Click);
            //camera pin 按钮
            cameraPinButton = AddImageButton(
                "icon_pin_to_screen_off.png",
                "icon_pin_to_screen_on.png",
                OnCameraPinClick);
            //禁用摄像头 按钮
            muteCameraButton = AddImageButton(
                "icon_camera_mute.png",
                "icon_camera_open.png",
                "icon_camera_lock.png",
                OnMuteCameraClick);
            //禁用扬声器 按钮
            muteSpeakerButton = AddImageButton(
                "icon_speaker_mute.png",
                "icon_speaker_open.png",
                OnMuteSpeakerClick);
            //禁用麦克风 按钮
            muteMicButton = AddImageButton(
                "icon_mic_mute.png",
                "icon_mic_open.png",
                "icon_mic_lock.png",
                OnMuteMicClick);

            //
            SyncMenuSize();
            //---初始化辅流设置面板
            minorStreamSettingPanel = new Panel();
            // minorStreamSettingPanel.Visible = false;
            minorStreamSettingBrowser = new ChromiumWebBrowser();
            minorStreamSettingBrowser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
            minorStreamSettingBrowser.JavascriptObjectRepository.Register("MinorStreamSettingsJsProxy",
                new MinorStreamSettingsJsProxy(this), false,
                BindingOptions.DefaultBinder);
            minorStreamSettingBrowser.AddressChanged += OnMinorStreamSettingsBrowserAddressChanged;
            minorStreamSettingPanel.Controls.Add(minorStreamSettingBrowser);
            Controls.Add(minorStreamSettingPanel);
            minorStreamSettingPanel.SendToBack();
            SyncMinorStreamSettingsBrowserSize();
        }

        private void SyncMinorStreamSettingsBrowserSize()
        {
            minorStreamSettingPanel.Size = new Size(ClientRectangle.Width / 5, ClientRectangle.Height / 2);
            minorStreamSettingPanel.Location = new Point(
                ClientRectangle.Width - minorStreamSettingPanel.Width,
                menuPanel.Height);
            minorStreamSettingPanel.Margin = new Padding(0, 0, 0, 0);
            minorStreamSettingPanel.Padding = new Padding(0, 0, 0, 0);
            if (null != minorStreamSettingBrowser)
            {
                minorStreamSettingBrowser.Width = minorStreamSettingPanel.Width;
                minorStreamSettingBrowser.Height = minorStreamSettingPanel.Height;
            }
        }

        private void OnMinorStreamSettingsBrowserAddressChanged(object sender, AddressChangedEventArgs e)
        {
            Log("OnMinorStreamSettingsBrowserAddressChanged -> {}", e.Address);
            RunOnUIThread((Action)(() =>
            {
                if (!String.IsNullOrEmpty(e.Address) && !String.Equals("about:blank", e.Address))
                {
                    minorStreamSettingPanel.BringToFront();
                }
                else
                {
                    minorStreamSettingPanel.SendToBack();
                }
            }));
        }

        private Button AddMenuButton(String text, EventHandler handler)
        {
            Button button = new Button()
            {
                Size = new Size(150, menuPanelHeight),
                Margin = new Padding(8, 0, 8, 0),
                Text = text,
                ForeColor = Color.Brown,
                Font = new Font("宋体", 14, FontStyle.Bold)
            };
            button.Click += handler;
            menuPanel.Controls.Add(button);
            return button;
        }

        private ButtonImage2State AddImageButton(
            String state1Icon,
            String state2Icon,
            EventHandler handler)
        {
            ButtonImage2State button = new ButtonImage2State(menuPanelHeight, state1Icon, state2Icon, handler);
            menuPanel.Controls.Add(button);
            return button;
        }

        private ButtonImage3State AddImageButton(
            String state1Icon,
            String state2Icon,
            String state3Icon,
            EventHandler handler)
        {
            ButtonImage3State button =
                new ButtonImage3State(
                    menuPanelHeight,
                    state1Icon,
                    state2Icon,
                    state3Icon,
                    handler);
            menuPanel.Controls.Add(button);
            return button;
        }

        private void SyncMenuSize()
        {
            menuSwitch.Location = new Point(ClientRectangle.Width - menuSwitch.Width - 4, 40);

            menuPanel.Location = new Point(0, 0);
            menuPanel.Margin = new Padding(0, 0, 0, 0);
            menuPanel.Size = new Size(ClientRectangle.Width, menuPanelHeight);
        }

        private void ShowMenuButton()
        {
            menuSwitch.Visible = true;
        }

        private void DismissMenuButton()
        {
            menuSwitch.Visible = false;
        }

        private void ShowMenuPanel()
        {
            menuPanel.Visible = true;
            // menuPanel.BringToFront();
        }

        private void DismissMenuPanel()
        {
            menuPanel.Visible = false;
        }

        private void OnMenuClick(object sender, EventArgs e)
        {
            // ShowControlCefWindow();
            ShowMenuPanel();
        }

        private void OnMenuCloseClick(object sender, EventArgs e)
        {
            DismissMenuPanel();
        }

        private void OnLeaveMeetingClick(object sender, EventArgs e)
        {
            videoManager?.Disconnect();
        }

        private void OnCamera3Click(object sender, EventArgs e)
        {
            minorStreamSettingBrowser?.Load(minorStreamSettingsHtml + "?type=3");
        }

        private void OnCamera2Click(object sender, EventArgs e)
        {
            minorStreamSettingBrowser?.Load(minorStreamSettingsHtml + "?type=2");
        }

        private void OnCameraPinClick(object sender, EventArgs e)
        {
            videoManager?.ToggleMainCameraPin();
            SyncCameraPinButtonState();
        }

        private void DismissMinorStreamSettingsBrowser()
        {
            minorStreamSettingBrowser?.Load("about:blank");
        }

        private void OnMuteCameraClick(object sender, EventArgs e)
        {
            videoManager?.ToggleCameraPrivacy();
        }

        private void OnMuteSpeakerClick(object sender, EventArgs e)
        {
            videoManager?.ToggleSpeakerPrivacy();
        }

        private void OnMuteMicClick(object sender, EventArgs e)
        {
            videoManager?.ToggleMicrophonePrivacy();
        }

        private void SetMuteCameraButtonState(bool mute, bool locked)
        {
            RunOnUIThread((Action)(() =>
            {
                if (locked)
                {
                    muteCameraButton.UseState3();
                }
                else
                {
                    if (mute)
                    {
                        muteCameraButton.UseState1();
                    }
                    else
                    {
                        muteCameraButton.UseState2();
                    }
                }
            }));
        }

        private void SetMuteSpeakerButtonState(bool mute, bool locked)
        {
            RunOnUIThread((Action)(() => { muteSpeakerButton.UseState1(mute); }));
        }

        private void SetMuteMicButtonState(bool mute, bool locked)
        {
            RunOnUIThread((Action)(() =>
            {
                if (locked)
                {
                    muteMicButton.UseState3();
                }
                else
                {
                    if (mute)
                    {
                        muteMicButton.UseState1();
                    }
                    else
                    {
                        muteMicButton.UseState2();
                    }
                }
            }));
        }

        private void SyncCameraPinButtonState()
        {
            RunOnUIThread((Action)(() =>
            {
                cameraPinButton.UseState1(!videoManager.layoutController.isMainCameraPin);
            }));
        }

        private void SyncCamera2ButtonState()
        {
            RunOnUIThread((Action)(() => { camera2Button.UseState1(!videoManager.cameraManager.IsContentOpened()); }));
        }

        private void SyncCamera3ButtonState()
        {
            RunOnUIThread((Action)(() =>
            {
                camera3Button.UseState1(!videoManager.cameraManager.IsVirtualSourceOpened());
            }));
        }

        private void OnScreenKeyboardClick(object sennder, EventArgs e)
        {
            Process.Start(@"C:\WINDOWS\system32\osk.exe"); //调出屏幕键盘
        }

        private void VideoPanelMouseMove(object sender, MouseEventArgs e)
        {
            // Log("VideoPanelMouseMove -> {0},{1}", e.X, e.Y);
            if (null != videoManager && videoManager.isConnected && videoManager.IsEndpointMode())
            {
                if (null != menuButton) menuButton.Visible = (e.Y < ClientRectangle.Height / 3);
                if (null != screenKeyboardButton) screenKeyboardButton.Visible = (e.Y < ClientRectangle.Height / 3);
            }
        }

        private void OnMouseUpClick(object sennder, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle && e.Delta == MouseHook.WM_MBUTTONUP)
            {
                Process.Start(@"C:\WINDOWS\system32\osk.exe"); //调出屏幕键盘
            }
            else if (e.Button == MouseButtons.Right && e.Delta == MouseHook.WM_RBUTTONUP)
            {
                videoManager?.OpenTangoAppWindow();
            }
        }

        #endregion

        #region Handle UI

        public void ChangeUiWindowNormalMode()
        {
            if (InvokeRequired)
            {
                RunOnUIThread((Action)(ChangeUiWindowMaximizedMode));
            }
            else
            {
                StartPosition = FormStartPosition.CenterScreen;
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Normal;
            }
        }

        public void ChangeUiWindowMaximizedMode()
        {
            if (InvokeRequired)
            {
                RunOnUIThread((Action)(ChangeUiWindowMaximizedMode));
            }
            else
            {
                StartPosition = FormStartPosition.CenterScreen;
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Maximized;
            }
        }

        public void ChangeUiWindowSmallMode()
        {
            if (InvokeRequired)
            {
                RunOnUIThread((Action)(ChangeUiWindowSmallMode));
            }
            else
            {
                videoManager?.ApplySmallWindowMode();
            }
        }

        public void ChangeUiFullScreenMode()
        {
            RunOnUIThread((Action)(() =>
            {
                SyncAppBrowserSize(true);
                SyncMenuSize();
                Log("Change UI To Full Screen Mode.（Bounds : " + Screen.PrimaryScreen.Bounds + " , Scaling : " +
                    Util.Util.GetWinScaling(this) + " ） - " +
                    Screen.PrimaryScreen.DeviceName);
            }));
        }

        public void SetInternalCefVisible(bool visible)
        {
            RunOnUIThread((Action)(() => { panel_cef.Visible = visible; }));
        }

        public void OnDeviceMuteChanged(int deviceType, bool mute, bool locked)
        {
            switch (deviceType)
            {
                case 0: //camera
                    SetMuteCameraButtonState(mute, locked);
                    break;
                case 1: //mic
                    SetMuteMicButtonState(mute, locked);
                    break;
                case 2: //speaker
                    SetMuteSpeakerButtonState(mute, locked);
                    break;
            }
        }

        public void OnVideoDeviceChanged(int sourceType, string deviceId)
        {
            switch (sourceType)
            {
                case 2: //content
                    SyncCamera2ButtonState();
                    break;
                case 3: //third camera
                    SyncCamera3ButtonState();
                    break;
            }
        }

        #endregion

        #region 日志

        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private void Log(string content)
        {
            _logger.Info("[FormMain] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info($"[FormMain] {content}", args);
        }

        private IAsyncResult RunOnUIThread(Delegate method)
        {
            return BeginInvoke(method);
        }

        #endregion
    }
}