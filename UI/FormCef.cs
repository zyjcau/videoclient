using System;
using System.Drawing;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using NLog;
using VidyoClient;

namespace VideoClient.UI
{
    public partial class CefForm : Form
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ChromiumWebBrowser _chromeBrowser;
        public IRequestHandler _requestHandler;

        public int id;
        private String initAddress;

        private bool isLoadOnce;
        public bool isClosed;

        public CefForm(int id, string title, string initAddress, bool isLoadOnce)
        {
            this.id = id;
            this.initAddress = initAddress;
            this.isLoadOnce = isLoadOnce;

            InitializeComponent();

            FormClosing += Form_Closing;
            Activated += CefForm_Activated;
            Shown += CefForm_Shown;
            Resize += SyncFormSize;

            Text = title;
            ShowInTaskbar = true;

            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            // SetSizeRatioAndCenterInScreen(0.7, 0.7);

            InitializeChromium();
        }

        public void SetSizeRatioAndCenterInScreen(double widthRatio, double heightRatio)
        {
            Size = new Size((int)
                (Screen.FromControl(this).Bounds.Width * widthRatio),
                (int)(Screen.FromControl(this).Bounds.Height * heightRatio));
            Location = new Point
            (
                Screen.FromControl(this).Bounds.Width / 2 - Width / 2,
                Screen.FromControl(this).Bounds.Height / 2 - Height / 2
            );
        }

        private void InitializeChromium()
        {
            // Create a browser component
            _chromeBrowser = new ChromiumWebBrowser(initAddress);

            // var browserSettings = new BrowserSettings()
            // {
            //     FileAccessFromFileUrls = CefState.Enabled,
            //     UniversalAccessFromFileUrls = CefState.Enabled,
            // };
            // _chromeBrowser.BrowserSettings = browserSettings;

            _chromeBrowser.AddressChanged += OnBrowserAddressChanged;

            // Add it to the form and fill it to the form window.
            panel1.Controls.Add(_chromeBrowser);
            // chromeBrowser.Dock = DockStyle.Fill;
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
            Log("SyncCefFormSize " + Width + " x " + Height + " , " + ClientRectangle.Width +
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
            Log("On Form_Closing");
        }

        private void CefForm_Activated(object sender, EventArgs e)
        {
            Log("On Activated");
            if (!isLoadOnce) _chromeBrowser.Load(initAddress);
        }

        private void CefForm_Shown(object sender, EventArgs e)
        {
            Log("On Shown");
            // Size = new Size((int)
            //     (Screen.FromControl(this).Bounds.Width * 0.6),
            //     (int) (Screen.FromControl(this).Bounds.Height * 0.6));
            // StartPosition = FormStartPosition.CenterScreen;

            // _chromeBrowser.Load(initAddress);
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

        /*
         * 实现隐藏关闭按钮
         */
        // private const int CP_NOCLOSE_BUTTON = 0x200;
        //
        // protected override CreateParams CreateParams
        // {
        //     get
        //     {
        //         CreateParams myCp = base.CreateParams;
        //         myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
        //         return myCp;
        //     }
        // }

        private IAsyncResult RunOnUIThread(Delegate method)
        {
            return BeginInvoke(method);
        }

        private void Log(string content)
        {
            _logger.Info("[FormCef - " + Text + "] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info($"[FormCef - " + Text + "] {content}", args);
        }
    }
}