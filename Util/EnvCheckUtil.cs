using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Microsoft.Win32;
using NLog;

namespace VideoClient.Util
{
    /// <summary>
    /// 检测.Net环境
    /// </summary>
    public class EnvCheckUtil
    {
        /// <summary>
        /// 判断.Net Framework的Version是否符合需要 (.Net Framework 版本在2.0及以上)
        /// </summary>
        /// <param name="version">
        /// 需要的版本 version = 4.5
        /// </param>
        /// <returns>
        /// </returns>
        public static bool IsInstallDotNet(string version)
        {
            string oldname = "0";
            using (RegistryKey ndpKey = RegistryKey.OpenRemoteBaseKey(
                           RegistryHive.LocalMachine,
                           ""
                       )
                       .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
            {
                if (ndpKey != null)
                    foreach (string versionKeyName in ndpKey.GetSubKeyNames())
                    {
                        if (versionKeyName.StartsWith("v"))
                        {
                            RegistryKey versionKey = ndpKey.OpenSubKey(versionKeyName);
                            if (versionKey != null)
                            {
                                string newname = (string)versionKey.GetValue(
                                    "Version",
                                    ""
                                );
                                if (string.CompareOrdinal(
                                        newname,
                                        oldname
                                    ) >
                                    0)
                                {
                                    oldname = newname;
                                }

                                if (newname != "")
                                {
                                    continue;
                                }

                                foreach (string subKeyName in versionKey.GetSubKeyNames())
                                {
                                    RegistryKey subKey = versionKey.OpenSubKey(subKeyName);
                                    if (subKey != null)
                                        newname = (string)subKey.GetValue(
                                            "Version",
                                            ""
                                        );
                                    if (string.CompareOrdinal(
                                            newname,
                                            oldname
                                        ) >
                                        0)
                                    {
                                        oldname = newname;
                                    }
                                }
                            }
                        }
                    }
            }

            return string.CompareOrdinal(
                       oldname,
                       version
                   ) >
                   0;
        }

        public static bool IsInstallVc(string[] keywords)
        {
            string runtimeArchitecture;
            if (TryGetVcRuntimeArchitecture(
                    keywords,
                    out runtimeArchitecture
                ) &&
                IsInstallVcRuntime14OrLater(runtimeArchitecture))
            {
                return true;
            }

            List<string> lists = new List<string>();
            RegistryKey key = Registry.LocalMachine;
            string str = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
            if (!Environment.Is64BitOperatingSystem)
            {
                str = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            }

            GetRegistry(
                key,
                str,
                ref lists
            );
            if (lists.Count > 0)
            {
                string vcName = null;
                foreach (var s1 in lists.Where(x =>
                             keywords.All(keyword =>
                                 x.IndexOf(
                                     keyword,
                                     StringComparison.OrdinalIgnoreCase
                                 ) >=
                                 0)))
                {
                    vcName = s1;
                    break;
                }

                Console.WriteLine(vcName);
                return !string.IsNullOrEmpty(vcName);
            }

            return false;
        }

        private static bool TryGetVcRuntimeArchitecture(string[] keywords, out string architecture)
        {
            architecture = null;
            if (keywords == null)
            {
                return false;
            }

            if (keywords.Any(x => string.Equals(
                    x,
                    "x64",
                    StringComparison.OrdinalIgnoreCase
                )))
            {
                architecture = "x64";
                return true;
            }

            if (keywords.Any(x => string.Equals(
                    x,
                    "x86",
                    StringComparison.OrdinalIgnoreCase
                )))
            {
                architecture = "x86";
                return true;
            }

            return false;
        }

        private static bool IsInstallVcRuntime14OrLater(string architecture)
        {
            string subKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\" + architecture;
            return IsInstallVcRuntime14OrLater(
                       RegistryView.Registry64,
                       subKey
                   ) ||
                   IsInstallVcRuntime14OrLater(
                       RegistryView.Registry32,
                       subKey
                   );
        }

        private static bool IsInstallVcRuntime14OrLater(RegistryView registryView, string subKey)
        {
            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(
                           RegistryHive.LocalMachine,
                           registryView
                       ))
                using (RegistryKey runtimeKey = baseKey.OpenSubKey(subKey))
                {
                    if (runtimeKey == null)
                    {
                        return false;
                    }

                    object installedValue = runtimeKey.GetValue("Installed");
                    object majorValue = runtimeKey.GetValue("Major");
                    int installed;
                    int major;
                    return int.TryParse(
                               installedValue?.ToString(),
                               out installed
                           ) &&
                           installed == 1 &&
                           int.TryParse(
                               majorValue?.ToString(),
                               out major
                           ) &&
                           major >= 14;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void GetRegistry
        (
            RegistryKey keyR,
            string str,
            ref List<string> list
        )
        {
            RegistryKey aimdir = keyR.OpenSubKey(str);
            if (aimdir != null)
            {
                var subvalueNames = aimdir.GetValueNames();
                foreach (string valueName in subvalueNames)
                {
                    if (valueName.ToLower().Equals("displayname") || valueName.ToLower().Equals("productname"))
                    {
                        if (aimdir.GetValue(valueName) != null && aimdir.GetValue(valueName).ToString().ToLower()
                                .Contains("microsoft visual c++"))
                        {
                            list.Add(aimdir.GetValue(valueName).ToString());
                        }
                    }
                }

                var subkeyNames = aimdir.GetSubKeyNames();
                foreach (string keyName in subkeyNames)
                {
                    GetRegistry(
                        aimdir,
                        keyName,
                        ref list
                    );
                }
            }
        }

        public static bool IsWin78()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (Environment.OSVersion.Version.Major == 6 &&
                    (
                        Environment.OSVersion.Version.Minor == 1 ||
                        Environment.OSVersion.Version.Minor == 2 ||
                        Environment.OSVersion.Version.Minor == 3
                    )
                   )
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsWin10()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (Environment.OSVersion.Version.Major == 10)
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetDotNetVersion()
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            var xmlAttributeCollection = xmlDoc.SelectSingleNode("//startup//supportedRuntime")?.Attributes;
            if (xmlAttributeCollection != null)
            {
                var targetFramework = xmlAttributeCollection["sku"]?.Value;
                return targetFramework;
            }

            return "";
        }

        public static bool IsSpecifiedDotNetVersion(string version)
        {
            var dotNetVersion = GetDotNetVersion();
            return dotNetVersion.Contains(version);
        }

        public static bool IsApp32Bit()
        {
            return IntPtr.Size == 4;
        }

        public static bool IsApp64Bit()
        {
            return IntPtr.Size == 8;
        }

        public static bool IsOsAppSameBit()
        {
            return Environment.Is64BitOperatingSystem == Environment.Is64BitProcess;
        }

        public static bool IsOsCompatibleWithApp()
        {
            //情况一
            if (IsWin10())
            {
                return true;
            }

            //情况二
            if (IsWin78() && IsSpecifiedDotNetVersion("4.5") && IsOsAppSameBit())
            {
                return true;
            }

            return false;
        }

        public static string GetEnvironmentInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\n---------------------------------------------").Append("\n");

            sb.Append(
                    $@"| OS  {Environment.OSVersion} , {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}")
                .Append("\n");
            sb.Append($@"| OS  Arch {(Environment.Is64BitOperatingSystem ? "64bit" : "32bit")}").Append("\n");
            sb.Append($@"| App Arch {(Environment.Is64BitProcess ? "64bit" : "32bit")}").Append("\n");
            sb.Append($@"| App .Net Framework > {GetDotNetVersion()}").Append("\n");
            sb.Append($@"| Env Installed .Net452 > {IsInstallDotNet("4.5.2")}").Append("\n");
            sb.Append($@"| Env Installed .Net471 > {IsInstallDotNet("4.7.1")}").Append("\n");

            var isInstallVc1519X86 = IsInstallVc(new[] { "C++", "2015-2019", "x86" });
            var isInstallVc1519X64 = IsInstallVc(new[] { "C++", "2015-2019", "x64" });
            var isInstallVc1522X86 = IsInstallVc(new[] { "C++", "2015-2022", "x86" });
            var isInstallVc1522X64 = IsInstallVc(new[] { "C++", "2015-2022", "x64" });
            sb.Append($@"| Env Installed VC++15-19 x86 > {isInstallVc1519X86}").Append("\n");
            sb.Append($@"| Env Installed VC++15-19 x64 > {isInstallVc1519X64}").Append("\n");
            sb.Append($@"| Env Installed VC++15-22 x86 > {isInstallVc1522X86}").Append("\n");
            sb.Append($@"| Env Installed VC++15-22 x64 > {isInstallVc1522X64}").Append("\n");

            sb.Append("---------------------------------------------").Append("\n");
            return sb.ToString();
        }

        public static void PrintEnvironmentInfo()
        {
            Log("\n---------------------------------------------");

            Log(
                $@"| OS  {Environment.OSVersion} , {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}");
            Log($@"| OS  Arch {(Environment.Is64BitOperatingSystem ? "64bit" : "32bit")}");
            Log($@"| App Arch {(Environment.Is64BitProcess ? "64bit" : "32bit")}");
            Log($@"| App .Net Framework > {GetDotNetVersion()}");
            Log($@"| Env Installed .Net452 > {IsInstallDotNet("4.5.2")}");
            Log($@"| Env Installed .Net471 > {IsInstallDotNet("4.7.1")}");

            var isInstallVc1519X86 = IsInstallVc(new[] { "C++", "2015-2019", "x86" });
            var isInstallVc1519X64 = IsInstallVc(new[] { "C++", "2015-2019", "x64" });
            var isInstallVc1522X86 = IsInstallVc(new[] { "C++", "2015-2022", "x86" });
            var isInstallVc1522X64 = IsInstallVc(new[] { "C++", "2015-2022", "x64" });
            Log($@"| Env Installed VC++15-19 x86 > {isInstallVc1519X86}");
            Log($@"| Env Installed VC++15-19 x64 > {isInstallVc1519X64}");
            Log($@"| Env Installed VC++15-22 x86 > {isInstallVc1522X86}");
            Log($@"| Env Installed VC++15-22 x64 > {isInstallVc1522X64}");

            Log("---------------------------------------------");
        }

        /// <summary>
        /// 打开系统软件卸载页面
        /// </summary>
        public static void OpenAppUninstallPage()
        {
            Process.Start("appwiz.cpl");
        }

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static void Log(string content)
        {
            logger.Info("[EnvCheck] " + content);
        }
    }
}
