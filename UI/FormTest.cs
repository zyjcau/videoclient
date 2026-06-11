using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using NLog;
using VideoClient.VidyoClient.Ext;

namespace VideoClient.UI
{
    public partial class FormTest : Form
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private VideoRenderer renderer;

        private FormMessage nFormMessage;

        public FormTest()
        {
            InitializeComponent();

            Resize += FormTest_Resize;

            Panel renderPanel = new Panel();
            renderPanel.Size = new Size(ClientRectangle.Width, ClientRectangle.Height);
            renderPanel.BackColor = Color.Brown;
            renderPanel.Name = "lecturer_";
            renderPanel.Visible = true;
            renderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            renderPanel.BorderStyle = BorderStyle.FixedSingle;
            // Controls.Add(renderPanel);

            renderer = new VideoRenderer(0, 0, renderPanel);

            renderer.EnableRecord();
            renderer.EnableSnapshot();

            renderer.EnableMaximizeBtn();
            renderer.EnableExchangeBtn();
            renderer.EnablePinBtn();

            renderer.funcPanelAtLeft.Controls.Add(renderer.snapshotBtn);
            renderer.ShowTools();

            var layout = new FlowLayoutPanel();
            layout.Size = Size;
            layout.FlowDirection = FlowDirection.LeftToRight;
            Controls.Add(layout);

            AddButton(layout.Controls, "Crash Simulate",
                (sender, args) => throw new NullReferenceException("测试一下崩溃报告机制。"));
            
            AddButton(layout.Controls, "启动Connect",
                (sender, args) =>
                {
                    RunCmd(
                        "start \"\" \"vidyo://join?portal=dev.lssvc.cn&roomKey=WlUxVwo3Sx&displayName=test1&isCustom=true&loginMod=0&welcomePage=1\"");
                });
            
            AddButton(layout.Controls, "测试WebRTC",
                (sender, args) =>
                {
                    //test code:测试webrtc
                    CefSettings settings = new CefSettings();
                    settings.CefCommandLineArgs.Add("enable-media-stream", "1");
                    settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream", "1");
                    settings.CefCommandLineArgs.Add("enable-system-flash", "1"); //flash
                    settings.CefCommandLineArgs.Add("enable-speech-input", "1"); //语音输入
                    Cef.Initialize(settings);
                    CefForm cef = new CefForm(
                        0,
                        "webrtc",
                        "https://webrtc.lssvc.cn",
                        true)
                    {
                        // _requestHandler = new CefRequestHandler(this)
                    };
                    // var browserSettings = new BrowserSettings();
                    // browserSettings.
                    // cef._chromeBrowser.BrowserSettings = browserSettings;
                    cef.Show();
                });
            
            AddButton(layout.Controls, "验证消息弹窗",
                (sender, args) =>
                {
                     if (null == nFormMessage)
                     {
                         nFormMessage = new FormMessage();
                         nFormMessage.ShowWithParent(this, "网络异常，正在尝试重新连接...");
                    
                         _logger.Debug("click....");
                         Task.Run(async () =>
                         {
                             _logger.Debug("wait....");
                             await Task.Delay(3000);
                             _logger.Debug("run on ui....");
                             RunOnUIThread(() =>
                             {
                                 _logger.Debug("hide....");
                                 if (null != nFormMessage) nFormMessage.Hide();
                                 nFormMessage = null;
                             });
                         });
                     }
                     else
                     {
                         nFormMessage.RefreshMessage("刷新消息");
                     }
                });
            
            TextBox nTextBox = new TextBox()
            {
                Size = new Size(300, 300)
            };
            layout.Controls.Add(nTextBox);

            List<string> nList = GetIPv4Addresses();
            foreach (string d in nList)
            {
                nTextBox.AppendText($"{d}\n");
            }

            CenterToScreen();

            //test code : parse base64
            // byte[] bs = Convert.FromBase64String("QzpcVXNlcnNcbHVvamluZ1xQaWN0dXJlc1wxLmpwZw==");
            // String filePath = Encoding.UTF8.GetString(bs);
            // _logger.Info($"filePath ---> {filePath}");

            //test code : upload image
            // string uploadFileName = "123.bmp";
            // Thread nThread = new Thread(() =>
            // {
            //     Thread.Sleep(2000);
            //     RestResponse result =
            //         VideoManager.GetInstance().UploadImage(
            //                 @"C:\Users\luojing\AppData\Roaming\Laisaisi\VideoClient\1.61\Temp\视频源1_1920x1080_1686202669.bmp",
            //                 uploadFileName)
            //             .Result;
            //     _logger.Info("reuslt ---> " + result.Content);
            //     // Console.WriteLine("reuslt ---> " + result.Content);
            // });
            // nThread.Start();

            //test code : download image
            // new FormPictureView(subsystemSupabaseUrl + "/storage/v1/object/image/files/users/" + loginUsername + "/" +
            //                     uploadFileName)
            //     .Show();
        }

        private void AddButton(Control.ControlCollection controls, string buttonText, EventHandler click)
        {
            Button button = new Button
            {
                Text = buttonText,
                Size = new Size(120, 60)
            };
            button.Click += click;
            controls.Add(button);
        }

        private void FormTest_Resize(object sender, EventArgs e)
        {
            renderer.SetSize(0, 0, ClientRectangle.Width, ClientRectangle.Height);
        }

        public static string RunCmd(string cmd)
        {
            cmd = cmd.Trim().TrimEnd('&') + "&exit"; //说明：不管命令是否成功均执行exit命令，否则当调用ReadToEnd()方法时，会处于假死状态
            using (Process p = new Process())
            {
                p.StartInfo.FileName = @"C:\Windows\System32\cmd.exe";
                p.StartInfo.UseShellExecute = false; //是否使用操作系统shell启动
                p.StartInfo.RedirectStandardInput = true; //接受来自调用程序的输入信息
                p.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
                p.StartInfo.RedirectStandardError = true; //重定向标准错误输出
                p.StartInfo.CreateNoWindow = true; //不显示程序窗口
                p.Start(); //启动程序

                //向cmd窗口写入命令
                p.StandardInput.WriteLine(cmd);
                p.StandardInput.AutoFlush = true;

                //获取cmd窗口的输出信息
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(); //等待程序执行完退出进程
                p.Close();

                return output;
            }
        }

        public static List<string> GetIPv4Addresses()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            List<string> ipv4Addresses = new List<string>();

            foreach (IPAddress address in addresses)
            {
                // 判断是否为IPv4地址
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipv4Addresses.Add(address.ToString());
                }
            }

            return ipv4Addresses;
        }

        private IAsyncResult RunOnUIThread(Action method)
        {
            return BeginInvoke(method);
        }
    }
}