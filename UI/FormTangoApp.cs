using System;
using System.Drawing;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using NLog;
using VidyoClient;
using Application = System.Windows.Forms.Application;

namespace VideoClient.UI
{
    public partial class FormTangoApp : Form
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ChromiumWebBrowser _chromeBrowser;

        private String initAddress;

        public bool isClosed;

        public FormTangoApp(string initAddress)
        {
            this.initAddress = initAddress;

            InitializeComponent();

            FormClosing += Form_Closing;
            Activated += CefForm_Activated;
            Shown += CefForm_Shown;
            Resize += SyncFormSize;

            // Text = "操作窗口";
            Icon = new Icon(Application.StartupPath + "\\wwwroot\\res\\lss.ico");
            ShowInTaskbar = false;

            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            
            // Size = new Size((int)
            //     (Screen.FromControl(this).Bounds.Width * 0.7),
            //     (int)(Screen.FromControl(this).Bounds.Height * 0.7));
            // Location = new Point
            // (
            //     Screen.FromControl(this).Bounds.Width / 2 - Width / 2,
            //     Screen.FromControl(this).Bounds.Height / 2 - Height / 2
            // );
            // StartPosition = FormStartPosition.CenterScreen;

            InitializeChromium();
        }

        private void InitializeChromium()
        {
            _chromeBrowser = new ChromiumWebBrowser(initAddress);
            _chromeBrowser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
            _chromeBrowser.JavascriptObjectRepository.Register("JSProxy", new HomeJSProxy(), false,
                BindingOptions.DefaultBinder);
            _chromeBrowser.AddressChanged += OnBrowserAddressChanged;
            panel1.Controls.Add(_chromeBrowser);
            _chromeBrowser.BringToFront();
            _chromeBrowser.Left = 0;
            _chromeBrowser.Top = 0;
            _chromeBrowser.KeyboardHandler = new CEFKeyBoardHander();
            SyncCefSize();
        }

        private void OnBrowserAddressChanged(object sender, AddressChangedEventArgs e)
        {
            Log("OnBrowserAddressChanged -> {}", e);
        }

        private void SyncFormSize(object sender, EventArgs e)
        {
            Log("SyncFormTangoAppSize " + Width + " x " + Height + " , " + ClientRectangle.Width +
                "x" + ClientRectangle.Height);
            SyncCefSize();
        }

        private void SyncCefSize()
        {
            panel1.Width = ClientRectangle.Width;
            panel1.Height = ClientRectangle.Height;
            if (null != _chromeBrowser)
            {
                _chromeBrowser.Width = panel1.Width;
                _chromeBrowser.Height = panel1.Height;
            }
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            Log("On Close");

            Log("On Form_Closing");

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                _chromeBrowser.Dispose();
            }

            isClosed = true;
        }

        private void CefForm_Activated(object sender, EventArgs e)
        {
            Log("On Activated");
        }

        private void CefForm_Shown(object sender, EventArgs e)
        {
            Log("On Shown");
        }

        private void CefForm_Load(object sender, EventArgs e)
        {
            Log("On Load");
        }

        public new void Show()
        {
            isClosed = false;
            base.Show();
        }

        public void LoadUrl(string url)
        {
            _chromeBrowser.LoadUrl(url);
        }

        private IAsyncResult RunOnUIThread(Delegate method)
        {
            return BeginInvoke(method);
        }

        private void Log(string content)
        {
            _logger.Info("[FormTangoApp] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info($"[FormTangoApp] {content}", args);
        }
    }
}