using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VideoClient.Util
{
    public class Util
    {
        public static void OpenFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;

            Process process = new Process();
            ProcessStartInfo psi = new ProcessStartInfo("Explorer.exe")
            {
                Arguments = folderPath
            };
            process.StartInfo = psi;

            try
            {
                process.Start();
            }
            finally
            {
                process.Close();
            }
        }

        /*
         * 获取本地的IP地址
         */
        public static string GetLocalIp()
        {
            string addressIp = string.Empty;
            foreach (IPAddress ipAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ipAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    addressIp = ipAddress.ToString();
                }
            }

            return addressIp;
        }

        public static void AssignFormFullScreenToScreen(Form form, Screen screen)
        {
            form.WindowState = FormWindowState.Normal;
            form.FormBorderStyle = FormBorderStyle.None;
            form.StartPosition = FormStartPosition.Manual;
            form.DesktopBounds = screen.Bounds;
            form.Left = screen.Bounds.X;
            form.Top = screen.Bounds.Y;
            form.Size = new System.Drawing.Size(screen.Bounds.Width, screen.Bounds.Height);
        }

        public static float GetWinScaling(Control control)
        {
            int dpiX;
            Graphics graphics = control.CreateGraphics();
            dpiX = (Int32)graphics.DpiX;
            // log("graphics.DpiX -> " + graphics.DpiX);
            if (dpiX == 96)
            {
                return 1;
            }
            else if (dpiX == 120)
            {
                return 1.25f;
            }
            else if (dpiX == 144)
            {
                return 1.5f;
            }
            else if (dpiX == 168)
            {
                return 1.75f;
            }
            else if (dpiX == 192)
            {
                return 2f;
            }
            else if (dpiX == 216)
            {
                return 2.25f;
            }
            else if (dpiX == 240)
            {
                return 2.5f;
            }
            else if (dpiX == 288)
            {
                return 3f;
            }
            else if (dpiX == 336)
            {
                return 3.5f;
            }
            else
            {
                return 1;
            }
        }

        public static bool PortInUse(int port)
        {
            bool inUse = false;
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    break;
                }
            }

            return inUse;
        }

        public static bool IsNumeric(string value)
        {
            return Regex.IsMatch(value, @"^[+-]?\d*[.]?\d*$");
        }

        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            //TimeSpan ts = DateTime.Now - new DateTime(0, 0, 0, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds);
        }

        /// <summary>
        /// 开机自启
        /// </summary>
        public static void SetAutoStart(string appName, bool isAutoRun = true)
        {
            try
            {
                string subPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

                string separatorChar = @"\";
                char[] separator = separatorChar.ToCharArray();
                string[] myPaths = subPath.Split(separator);

                RegistryKey reg = Registry.CurrentUser;
                RegistryKey regKey2 = Registry.CurrentUser;

                for (int i = 0; i < myPaths.Length; i++)
                {
                    regKey2 = reg.OpenSubKey(myPaths[i], true);
                    if (regKey2 == null)
                        reg = reg.CreateSubKey(myPaths[i]);
                    else
                        reg = regKey2;
                }


                // Registry
                // Key reg = Registry.CurrentUser.OpenSubKey(@"\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                // if (reg == null)
                // {
                //     reg = Registry.CurrentUser.CreateSubKey(@"\SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                // }

                Console.Out.WriteLine("获取Registry成功");
                if (reg != null)
                {
                    if (isAutoRun)
                    {
                        if (null != reg.GetValue(appName))
                        {
                            reg.DeleteValue(appName, false);
                            Console.Out.WriteLine("软件开机启动已存在，现在删除。");
                        }

                        reg.SetValue(appName, System.Windows.Forms.Application.ExecutablePath);
                        Console.Out.WriteLine("设置软件开机启动成功！");
                    }
                    else
                    {
                        if (null != reg.GetValue(appName))
                        {
                            reg.DeleteValue(appName, false);
                            Console.Out.WriteLine("取消软件开机启动成功！");
                        }
                    }

                    // run.Close();
                    reg.Close();
                    regKey2.Close();
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("设置开机启动注册表失败!");
                Console.Out.WriteLine(e);
                // MessageBox.Show("开机自动启动服务注册被拒绝!请确认有系统管理员权限!");
            }
        }

        public static uint ColorToUInt(Color color)
        {
            return (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));
        }

        public static void StartUrl(String url)
        {
            Process.Start(url);
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

        public static string CheckIfAppInstalled(string appName)
        {
            // // 方式 1: 检查注册表中的已安装程序列表
            // string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            // using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey))
            // {
            //     if (key != null)
            //     {
            //         foreach (string subKeyName in key.GetSubKeyNames())
            //         {
            //             if (subKeyName.Equals(appName))
            //             {
            //                 return key.OpenSubKey(subKeyName)?.GetValue("DisplayIcon") as string;
            //             }
            //
            //             using (RegistryKey subKey = key.OpenSubKey(subKeyName))
            //             {
            //                 string displayName = subKey?.GetValue("DisplayName") as string;
            //                 string installLocation = subKey?.GetValue("InstallLocation") as string;
            //
            //                 if (!string.IsNullOrEmpty(displayName) && displayName.Contains(appName))
            //                 {
            //                     // // 优先返回安装路径
            //                     // if (!string.IsNullOrEmpty(installLocation))
            //                     //     return installLocation;
            //
            //                     // 也可以返回执行路径
            //                     string exePath = subKey?.GetValue("DisplayIcon") as string;
            //                     if (!string.IsNullOrEmpty(exePath))
            //                         return exePath;
            //                 }
            //             }
            //         }
            //     }
            // }

            // 方式 2: 检查默认安装路径
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), appName);
            if (Directory.Exists(defaultPath))
            {
                return defaultPath;
            }

            if (File.Exists(appName))
            {
                return appName;
            }

            return null; // 未找到
        }
    }
}