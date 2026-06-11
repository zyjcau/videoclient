using System;
using System.Drawing;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using NLog;
using VideoClient.Manager;
using VidyoClient;
using VidyoClient.Ext;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace VideoClient.UI
{
    public partial class FormSettings : Form, IKeyboardHandler
    {
        public bool isClosed;

        private readonly VideoManager _videoManager;

        private ChromiumWebBrowser _browser;

        public FormSettings(VideoManager videoManager)
        {
            _videoManager = videoManager;

            InitializeComponent();

            Load += Form_Load;
            FormClosing += Form_Close;
            Resize += Form_Resize;

            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            
            // Size = new Size((int)
            //     (Screen.FromControl(this).Bounds.Width * 0.85),
            //     // 1140,
            //     (int)(Screen.FromControl(this).Bounds.Height * 0.7)
            //     // 640
            // );
            // Location = new Point
            // (
            //     Screen.FromControl(this).Bounds.Width / 2 - Width / 2,
            //     Screen.FromControl(this).Bounds.Height / 2 - Height / 2
            // );
            // Text = "摄像头设置";
        }

        private void Form_Load(object sender, EventArgs e)
        {
            panel_video.Size = new Size(1, 1);
            panel_video.Location = new Point(0, 0);
            panel_video.BackColor = Color.Black;
            panel_video.Visible = true;
            panel_video.BringToFront();

            //配置JS调用
            CefSharpSettings.WcfEnabled = true;

            // string webRoot = Application.StartupPath + "\\wwwroot\\";
            string webRoot = $"http://{_videoManager.httpServer.ServerIP}:{_videoManager.httpServer.ServerPort}/";
            string initAddress = $"{webRoot}vue_app_settings/settings.html";

            _browser = new ChromiumWebBrowser(initAddress);
            _browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
            _browser.JavascriptObjectRepository.Register(
                "SettingsJsProxy",
                new SettingsJsProxy(this, _videoManager),
                false,
                BindingOptions.DefaultBinder);
            // _browser.AddressChanged += OnBrowserAddressChanged;
            _browser.KeyboardHandler = this;
            // Add it to the form and fill it to the form window.
            panel_cef.Controls.Add(_browser);
            _browser.Dock = DockStyle.Fill;
            _browser.Left = 0;
            _browser.Top = 0;

            SyncCefSize();

            StartPreviewLastSelectedCamera();
        }

        private void Form_Close(object sender, FormClosingEventArgs e)
        {
            Log("Form_Close");

            StopPreviewCamera();
            // isClosed = true;
            // Hide();
            // e.Cancel = true;

            // DialogResult = DialogResult.OK;
        }

        private void Form_Activated(object sender, EventArgs e)
        {
            StartPreviewLastSelectedCamera();
        }

        private void Form_Deactivate(object sender, EventArgs e)
        {
            StopPreviewCamera();
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            SyncCefSize();
        }

        //用于实现不在Alt+Tab中显示
        // protected override CreateParams CreateParams
        // {
        //     get
        //     {
        //         const int WS_EX_APPWINDOW = 0x40000;
        //         const int WS_EX_TOOLWINDOW = 0x80;
        //         CreateParams cp = base.CreateParams;
        //         cp.ExStyle &= (~WS_EX_APPWINDOW);
        //         cp.ExStyle |= WS_EX_TOOLWINDOW;
        //         return cp;
        //     }
        // }

        private void OnBrowserAddressChanged(object sender, AddressChangedEventArgs e)
        {
        }

        public new void Show()
        {
            isClosed = false;
            base.Show();
        }

        public new void Show(IWin32Window owner)
        {
            isClosed = false;
            base.Show(owner);
        }

        public new void ShowDialog(IWin32Window owner)
        {
            isClosed = false;
            base.ShowDialog(owner);
        }

        public void Dismiss()
        {
            // RunOnUIThread((Action) (() => { WindowState = FormWindowState.Minimized; }));
            RunOnUIThread((Action)(() => { Close(); }));
        }

        private void SyncCefSize()
        {
            Log("SyncCefSize " + Width + " x " + Height + " , " + ClientRectangle.Width +
                "x" + ClientRectangle.Height);
            panel_cef.Margin = new Padding(0, 0, 0, 0);
            panel_cef.Location = new Point(0, 0);
            panel_cef.Size = new Size(ClientRectangle.Width, ClientRectangle.Height);
            if (null != _browser)
            {
                _browser.Width = panel_cef.Width;
                _browser.Height = panel_cef.Height;
            }
        }

        private Panel _previewCameraPanel; //空表示停止状态，有值则表示预览中

        public void StartPreviewLastSelectedCamera()
        {
            // if (!_videoManager.isConnected) //只能在没有入会的情况下预览
            // {
            // LocalCamera localCamera;

            String cameraName = _videoManager.cameraManager.AssignLastSelectedCamera();

            if (String.Equals(CameraListener.DEVICE_NONE, cameraName))
            {
                Log("StartPreviewCamera None success.");
                return;
            }

            StartPreviewCamera(cameraName);
            // }
        }

        private void StartPreviewCamera(String cameraName)
        {
            LocalCamera localCamera = _videoManager.cameraManager.cameras[cameraName];
            if (null != localCamera)
            {
                _videoManager.connector?.AssignViewToLocalCamera(panel_video.Handle, localCamera, false, true);
                _videoManager.connector?.ShowViewAt(panel_video.Handle, 0, 0,
                    (uint)panel_video.Width,
                    (uint)panel_video.Height);
                _previewCameraPanel = panel_video;
                Log("StartPreviewCamera success. -> {}", cameraName);
            }
            else
            {
                _previewCameraPanel = null;
                Log("StartPreviewCamera failed. -> ", cameraName);
            }
        }

        public void StopPreviewCamera()
        {
            if (IsCameraPreviewed())
            {
                _videoManager.connector?.HideView(panel_video.Handle);

                //只有在未入会的情况下才关闭摄像头
                if (!_videoManager.isConnected)
                {
                    // if (!_videoManager.IsEndpointMode() && !_videoManager.IsGuestMode())
                    // {//桌面账号模式下，
                    _videoManager.cameraManager.CloseCamera();
                    // }
                }

                _previewCameraPanel = null;
                Log("StopPreviewCamera success.");
            }
            else
            {
                Log("StopPreviewCamera failed, not previewing.");
            }
        }

        private bool IsCameraPreviewed()
        {
            return _previewCameraPanel != null;
        }

        public void SelectCamera1(String cameraName)
        {
            RunOnUIThread((Action)(() =>
            {
                _videoManager.AssignVideoDevice(VideoSourceType.TYPE_LOCAL_CAMERA_INT, cameraName);

                if (String.Equals(CameraListener.DEVICE_NONE, cameraName))
                {
                    StopPreviewCamera();
                }
                else
                {
                    if (IsCameraPreviewed())
                    {
                        StopPreviewCamera();
                    }

                    // _videoManager.cameraManager.AssignCamera(cameraName);
                    StartPreviewCamera(cameraName);
                }

                Log("SelectPreviewCamera({})", cameraName);
            }));
        }

        public void SetVideoPanelLocationSize(int x, int y, int width, int height)
        {
            RunOnUIThread((Action)(() =>
            {
                panel_video.Location = new Point(x, y);
                panel_video.Size = new Size(width, height);
                _videoManager.connector?.ShowViewAt(panel_video.Handle, 0, 0, (uint)panel_video.Width,
                    (uint)panel_video.Height);
            }));
        }

        public void SetVideoPanelVisible(bool visible)
        {
            RunOnUIThread((Action)(() => { panel_video.Visible = visible; }));
        }

        public class SettingsJsProxy
        {
            private FormSettings _formSettings;
            private VideoManager _videoManager;

            private float _winScaling = 0;

            public SettingsJsProxy(FormSettings formSettings, VideoManager videoManager)
            {
                _formSettings = formSettings;
                _videoManager = videoManager;
            }

            public bool IsVideoConnected()
            {
                return _videoManager.isConnected;
            }

            public float GetWinScaling()
            {
                return Util.Util.GetWinScaling(_formSettings);
            }

            public void CloseSettingWindow()
            {
                _formSettings.Dismiss();
            }

            public String GetCamera1Json()
            {
                return _videoManager.GetCameraJson(0);
            }

            public String GetCamera2Json()
            {
                return _videoManager.GetCameraJson(1);
            }

            public String GetCamera3Json()
            {
                return _videoManager.GetCameraJson(2);
            }

            public void SwitchCamera1(String cameraName)
            {
                _formSettings.SelectCamera1(cameraName);
            }

            public void SetVideoSourceResolution(int sourceType, String resolutionKey)
            {
                _videoManager.SetVideoSourceResolution(sourceType, resolutionKey);
            }

            public void SetVideoSourceFrameRate(int sourceType, String frameRateKey)
            {
                _videoManager.SetVideoSourceFrameRate(sourceType, frameRateKey);
            }

            public void AssignVideoDevice(int sourceType, String deviceName)
            {
                _videoManager.AssignVideoDevice(sourceType, deviceName);
            }

            public void SetVideoPanelVisible(bool visible)
            {
                _formSettings.SetVideoPanelVisible(visible);
            }

            public void SetVideoPanelLocationSize(int x, int y, int width, int height)
            {
                if (_winScaling == 0)
                {
                    _winScaling = Util.Util.GetWinScaling(_formSettings);
                }

                x = (int)(x * _winScaling);
                y = (int)(y * _winScaling);
                width = (int)(width * _winScaling);
                height = (int)(height * _winScaling);
                _formSettings.SetVideoPanelLocationSize(x, y, width, height);
            }

            public String GetMicrophoneJson()
            {
                return _videoManager.GetMicrophoneJson();
            }

            public void AssignMicrophone(string microphone)
            {
                _videoManager.SelectMicrophone(microphone);
            }

            public String GetSpeakerJson()
            {
                return _videoManager.GetSpeakerJson();
            }

            public void AssignSpeaker(string speaker)
            {
                _videoManager.SelectSpeaker(speaker);
            }

            public bool IsUseDefaultLayout()
            {
                return _videoManager.IsUseDefaultLayout();
            }
        }

        private IAsyncResult RunOnUIThread(Delegate method)
        {
            return BeginInvoke(method);
        }

        #region 键盘响应

        public bool OnPreKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode,
            int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            return false;
        }

        public bool OnKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode,
            int nativeKeyCode,
            CefEventFlags modifiers, bool isSystemKey)
        {
            if (type == KeyType.KeyUp && Enum.IsDefined(typeof(Keys), windowsKeyCode))
            {
                var key = (Keys)windowsKeyCode;
                switch (key)
                {
                    case Keys.F12:
                        if (modifiers == CefEventFlags.ControlDown) browser.ShowDevTools();
                        break;

                    case Keys.F5:
                        if (modifiers == CefEventFlags.ControlDown)
                        {
                            browser.Reload(true); //强制忽略缓存
                        }
                        else
                        {
                            browser.Reload();
                        }

                        break;
                }
            }

            return false;
        }

        #endregion

        #region 日志

        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private void Log(string content)
        {
            _logger.Info("[FormSettings] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info($"[FormSettings] {content}", args);
        }

        #endregion
    }
}