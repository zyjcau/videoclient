using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CrashReporterDotNET;
using Newtonsoft.Json;
using NLog;
using VideoClient.Entity;
using VideoClient.UI;
using VideoClient.Util;
using Application = System.Windows.Forms.Application;

namespace VideoClient
{
    static class Program
    {
        #region 程序构造和初始化

        public static int mainThreadId;
        private static FormMain _formMain;

        private static readonly string CrashDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                                  "\\Laisaisi\\VideoClient\\crash\\";

        [STAThread]
        static void Main(string[] args)
        {
            if (IsAppRunning())
            {
                bool isCompleted = NotifyAppActivate(args);
                // MessageBox.Show(isCompleted ? "消息发送成功" : "失败");
                return;
            }

            Launch(args);
        }

        private static void Launch(string[] args)
        {
            if (_formMain == null)
            {
// #if DEBUG
//                 //...
// #else
//             ExceptionHelper.AddGlobalObserve();
//             ExceptionHelper.ExceptionOver += ExceptionHandler;
// #endif
                
                //异常捕获
                Application.ThreadException += (sender, ags) => SendReport(ags.Exception);
                AppDomain.CurrentDomain.UnhandledException += (sender, ags) =>
                {
                    SendReport((Exception)ags.ExceptionObject);
                };                
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                _reportCrash = new ReportCrash("luoj@hwari.com.cn")
                {
                    Silent = true,
                    ShowScreenshotTab = true,
                    IncludeScreenshot = true,
                    // DeveloperMessage = "请点击发送按钮后，会自动将崩溃信息发送给我们，以便于我们改善软件。",
                    #region Optional Configuration
                    // WebProxy = new WebProxy("Web proxy address, if needed"),
                    // AnalyzeWithDoctorDump = true,
                    // DoctorDumpSettings = new DoctorDumpSettings
                    // {
                    //     ApplicationID = new Guid("Application ID you received from DrDump.com"),
                    //     OpenReportInBrowser = true
                    // }
                    #endregion
                };
                _reportCrash.RetryFailedReports();
                
                mainThreadId = Thread.CurrentThread.ManagedThreadId;
                MessageFilter messageFilter = new MessageFilter();
                Application.AddMessageFilter(messageFilter);

                Application.Run(_formMain = new FormMain(args));

                //test code
                // Application.Run(new FormTest());
                // Application.Run(
                //     new FormPictureView(
                //         System.Windows.Forms.Application.UserAppDataPath + "\\Temp\\视频源1_1920x1080_1686034848.bmp"));
            }
            else
            {
                _formMain.ResetAndParseLaunchParams(args);
                _formMain.LoadAppPage();
                _formMain.SetActivate();
            }
        }

        #endregion

        #region 只启动一个进程，且激活已启动的进程

        [DllImport("user32.dll", EntryPoint = "ShowWindow")]
        public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
        public static extern int SetForegroundWindow(IntPtr hwnd);

        //通过窗口的标题来查找窗口的句柄 
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, ref CommandStruct lParam);

        public const int WM_COPYDATA = 0x004A;

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostThreadMessage(int threadId, uint msg, IntPtr wParam, IntPtr lParam);

        public const int WM_LAUNCH_APP = 0x80F0;

        static Process GetInRunningAppProcess()
        {
            Process cur = Process.GetCurrentProcess();
            foreach (Process p in Process.GetProcesses())
            {
                if (p.Id == cur.Id) continue;
                if (p.ProcessName == cur.ProcessName)
                {
                    return p;
                }
            }

            return null;
        }

        private static bool IsAppRunning()
        {
            return GetInRunningAppProcess() != null;
        }

        private static bool NotifyAppActivate(string[] args)
        {
            string text = "null";
            if (null != args && args.Length > 0)
            {
                text = JsonConvert.SerializeObject(args);
            }

            Config.SetStringConfig("launch_parameters_cache", text);
            PostThreadMessage(GetInRunningAppProcess().Threads[0].Id, WM_LAUNCH_APP, IntPtr.Zero, IntPtr.Zero);

            return true;
        }

        private static CommandStruct ConvertArgsToStruct(string[] args)
        {
            string text = "";
            if (null != args && args.Length > 0)
            {
                text = JsonConvert.SerializeObject(args);
            }

            CommandStruct param = default;
            param.dwData = (IntPtr)1; //自定义数据，4字节整数，0-15
            param.lpData = text; //发送字符串
            param.cbData = Encoding.Default.GetBytes(text).Length + 1; //发送字符串的长度
            return param;
        }

        public class MessageFilter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message msg)
            {
                if (msg.Msg == WM_LAUNCH_APP && _formMain != null)
                {
                    // CommandStruct param = (CommandStruct)Marshal.PtrToStructure(msg.LParam, typeof(CommandStruct));
                    // Log($"PreFilterMessage wnd param -> {param.lpData}");
                    string launchParametersCache = Config.GetStringConfig("launch_parameters_cache");
                    if (!String.IsNullOrEmpty(launchParametersCache) && !String.Equals("null", launchParametersCache))
                    {
                        string[] args = JsonConvert.DeserializeObject<string[]>(launchParametersCache);
                        _formMain.ReloadAppByArgs(args);
                    }
                    else
                    {
                        _formMain.SetActivate();
                    }

                    Log("PreFilterMessage -> {0},{1}", msg.Msg, launchParametersCache);
                }

                //识别消息并处理
                //return true;//吞掉消息，不派发
                return false; //进入下一步派发到对应窗口过程
            }
        }

        private static IntPtr FindWindowHandle()
        {
            IntPtr mainWindowHandle = default;

            Process cur = Process.GetCurrentProcess();
            foreach (Process p in Process.GetProcesses())
            {
                if (p.Id == cur.Id) continue;
                if (p.ProcessName == cur.ProcessName)
                {
                    mainWindowHandle = p.MainWindowHandle;
                }
            }

            if (mainWindowHandle == IntPtr.Zero)
            {
                mainWindowHandle = (IntPtr)FindWindow("", Config.GetStringConfig("app_name"));
            }

            return mainWindowHandle;
        }

        private static void TestPtrToStruct()
        {
            string text = "abc";
            CommandStruct param = default;
            param.dwData = (IntPtr)1; //自定义数据，4字节整数，0-15
            param.lpData = text; //发送字符串
            param.cbData = Encoding.Default.GetBytes(text).Length + 1; //发送字符串的长度

            int size = Marshal.SizeOf(param);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(param, ptr, true);

            CommandStruct structure = (CommandStruct)Marshal.PtrToStructure(ptr, typeof(CommandStruct));
            Console.WriteLine($"p -> {structure.lpData}");
        }

        #endregion

        #region 日志

        readonly static NLog.Logger _logger = LogManager.GetCurrentClassLogger();

        private static void Log(string content)
        {
            _logger.Info("[Program] " + content);
        }

        private static void Log(string content, params object[] args)
        {
            _logger.Info($"[Program] {content}", args);
        }

        #endregion

        #region 异常处理

        private static ReportCrash _reportCrash;
        
        public static void SendReport(Exception exception, string developerMessage = "")
        {
            _reportCrash.DeveloperMessage = developerMessage;
            _reportCrash.Silent = false;
            _reportCrash.Send(exception);
        }

        public static void SendReportSilently(Exception exception, string developerMessage = "")
        {
            _reportCrash.DeveloperMessage = developerMessage;
            _reportCrash.Silent = true;
            _reportCrash.Send(exception);
        }
        
        static void ExceptionHandler(int crashType, Exception ex)
        {
            if (!Directory.Exists(CrashDir))
            {
                Directory.CreateDirectory(CrashDir);
            }

            string fileName = CrashDir + "crash_" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss") + ".log";

            FileStream createdFile = File.Create(fileName);
            createdFile.Close();

            FileStream fs = new FileStream(fileName, FileMode.Open);
            StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("UTF-8"));
            sw.Write(ex.ToString());
            sw.Flush();
            sw.Close();
        }

        #endregion
    }
}