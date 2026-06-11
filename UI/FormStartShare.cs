using System;
using System.Drawing;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using NLog;
using VideoClient.Manager;
using VidyoClient;
using VidyoClient.Ext;
using Application = System.Windows.Forms.Application;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace VideoClient.UI
{
    public partial class FormStartShare : Form, IKeyboardHandler
    {
        public bool isClosed;

        private readonly VideoManager _videoManager;

        private ChromiumWebBrowser _browser;

        public FormStartShare(VideoManager videoManager)
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
            //     (Screen.FromControl(this).Bounds.Width * 0.6),
            //     (int)(Screen.FromControl(this).Bounds.Height * 0.6));
            // Location = new Point
            // (
            //     Screen.FromControl(this).Bounds.Width / 2 - Width / 2,
            //     Screen.FromControl(this).Bounds.Height / 2 - Height / 2
            // );
            // Text = "共享我的屏幕";
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

            string webRoot = Application.StartupPath + "\\wwwroot\\";
            // string webRoot = $"http://{_videoManager.httpServer.ServerIP}:{_videoManager.httpServer.ServerPort}/";
            string initAddress = $"{webRoot}start_share.html";

            _browser = new ChromiumWebBrowser(initAddress);
            _browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
            _browser.JavascriptObjectRepository.Register(
                "StartShareJsProxy",
                new StartShareJsProxy(this, _videoManager),
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
        }

        private void Form_Close(object sender, FormClosingEventArgs e)
        {
            Log("Form_Close");
        }

        private void Form_Activated(object sender, EventArgs e)
        {
        }

        private void Form_Deactivate(object sender, EventArgs e)
        {
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

        public void Dismiss()
        {
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


        public class StartShareJsProxy
        {
            private FormStartShare _formStartShare;
            private VideoManager _videoManager;

            public StartShareJsProxy(FormStartShare formStartShare, VideoManager videoManager)
            {
                _formStartShare = formStartShare;
                _videoManager = videoManager;
            }

            public void CloseWindow()
            {
                _formStartShare.Dismiss();
            }

            public String GetWindowsJson()
            {
                return _videoManager.GetSystemStatusJson();
            }

            public String GetScreenJson()
            {
                return _videoManager.GetCameraJson(3);
            }

            public void AssignMonitorShare(String deviceName)
            {
                _videoManager.AssignVideoDevice(VideoSourceType.TYPE_LOCAL_MONITOR_INT, deviceName);
            }

            public void AssignWindowShare(String deviceName)
            {
                _videoManager.AssignVideoDevice(VideoSourceType.TYPE_LOCAL_WINDOW_INT, deviceName);
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
            _logger.Info("[FormStartShare] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info($"[FormStartShare] {content}", args);
        }

        #endregion
    }
}