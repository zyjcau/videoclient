using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("Remote Consultation Installer")]
[assembly: AssemblyDescription("VideoClientWindows Installer")]
[assembly: AssemblyCompany("远程会诊")]
[assembly: AssemblyProduct("VideoClientWindows")]
[assembly: AssemblyCopyright("Copyright (C) 2026 远程会诊")]
[assembly: AssemblyVersion("1.4.14.0")]
[assembly: AssemblyFileVersion("1.04.14.0")]

internal static class InstallerBootstrapper
{
    private const string AppName = "远程会诊";
    private const string ExeName = "VideoClient.exe";
    private const string Version = "1.04.14";
    private static readonly byte[] Marker = Encoding.ASCII.GetBytes("<<VIDEOCLIENT_PAYLOAD_ZIP_LENGTH:");

    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();

        try
        {
            if (args.Length > 0 && args[0].Equals("/uninstall", StringComparison.OrdinalIgnoreCase))
            {
                EnsureAdministrator("/uninstall");
                Uninstall();
                return 0;
            }

            EnsureAdministrator(null);
            string installDir = ChooseInstallDirectory();
            if (string.IsNullOrEmpty(installDir))
            {
                return 0;
            }

            Install(installDir);
            MessageBox.Show(AppName + " 安装完成。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, AppName + " 安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static void EnsureAdministrator(string arg)
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        if (principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo(Application.ExecutablePath);
        startInfo.Verb = "runas";
        startInfo.UseShellExecute = true;
        if (!string.IsNullOrEmpty(arg))
        {
            startInfo.Arguments = arg;
        }

        Process.Start(startInfo);
        Environment.Exit(0);
    }

    private static string ChooseInstallDirectory()
    {
        string defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppName);
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = "请选择 " + AppName + " 的安装目录";
            dialog.SelectedPath = defaultDir;
            dialog.ShowNewFolderButton = true;
            return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
        }
    }

    private static void Install(string installDir)
    {
        if (Directory.Exists(installDir))
        {
            Directory.Delete(installDir, true);
        }

        Directory.CreateDirectory(installDir);

        string tempZip = Path.Combine(Path.GetTempPath(), "VideoClientWindows_" + Guid.NewGuid().ToString("N") + ".zip");
        try
        {
            ExtractPayloadZip(tempZip);
            ZipFile.ExtractToDirectory(tempZip, installDir);
        }
        finally
        {
            if (File.Exists(tempZip))
            {
                File.Delete(tempZip);
            }
        }

        string appExe = Path.Combine(installDir, ExeName);
        string uninstallExe = Path.Combine(installDir, "Uninstall.exe");
        File.Copy(Application.ExecutablePath, uninstallExe, true);

        CreateShortcut(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk"),
            appExe,
            installDir);

        string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", AppName);
        Directory.CreateDirectory(startMenuDir);
        CreateShortcut(Path.Combine(startMenuDir, AppName + ".lnk"), appExe, installDir);
        CreateShortcut(Path.Combine(startMenuDir, "卸载 " + AppName + ".lnk"), uninstallExe, installDir, "/uninstall");

        RegisterUninstall(installDir);
        RegisterTangoProtocol(appExe);
    }

    private static void Uninstall()
    {
        string installDir = GetInstalledDirectory();
        string desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk");
        if (File.Exists(desktopShortcut))
        {
            File.Delete(desktopShortcut);
        }

        string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", AppName);
        if (Directory.Exists(startMenuDir))
        {
            Directory.Delete(startMenuDir, true);
        }

        Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\VideoClientWindows", false);
        Registry.ClassesRoot.DeleteSubKeyTree(@"tango", false);

        if (Directory.Exists(installDir))
        {
            string currentExe = Path.GetFullPath(Application.ExecutablePath);
            string installRoot = Path.GetFullPath(installDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (currentExe.StartsWith(installRoot, StringComparison.OrdinalIgnoreCase))
            {
                ProcessStartInfo deleteInfo = new ProcessStartInfo("cmd.exe");
                deleteInfo.Arguments = "/c timeout /t 2 /nobreak >nul & rmdir /s /q \"" + installDir + "\"";
                deleteInfo.CreateNoWindow = true;
                deleteInfo.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(deleteInfo);
            }
            else
            {
                Directory.Delete(installDir, true);
            }
        }

        MessageBox.Show(AppName + " 已卸载。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string GetInstalledDirectory()
    {
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\VideoClientWindows"))
        {
            string installDir = key == null ? null : key.GetValue("InstallLocation") as string;
            if (!string.IsNullOrEmpty(installDir))
            {
                return installDir;
            }
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppName);
    }

    private static void RegisterUninstall(string installDir)
    {
        using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\VideoClientWindows"))
        {
            key.SetValue("DisplayName", AppName);
            key.SetValue("DisplayVersion", Version);
            key.SetValue("Publisher", AppName);
            key.SetValue("InstallLocation", installDir);
            key.SetValue("DisplayIcon", Path.Combine(installDir, ExeName));
            key.SetValue("UninstallString", "\"" + Path.Combine(installDir, "Uninstall.exe") + "\" /uninstall");
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }
    }

    private static void RegisterTangoProtocol(string appExe)
    {
        using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"tango"))
        {
            key.SetValue("", "tango Protocol");
            key.SetValue("URL Protocol", "");
        }

        using (RegistryKey iconKey = Registry.ClassesRoot.CreateSubKey(@"tango\DefaultIcon"))
        {
            iconKey.SetValue("", "\"" + appExe + "\",0");
        }

        using (RegistryKey commandKey = Registry.ClassesRoot.CreateSubKey(@"tango\shell\open\command"))
        {
            commandKey.SetValue("", "\"" + appExe + "\" \"%1\"");
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string arguments = "")
    {
        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        object shell = Activator.CreateInstance(shellType);
        object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
        Type shortcutType = shortcut.GetType();
        shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
        shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
        shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath + ",0" });
        if (!string.IsNullOrEmpty(arguments))
        {
            shortcutType.InvokeMember("Arguments", BindingFlags.SetProperty, null, shortcut, new object[] { arguments });
        }

        shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
    }

    private static void ExtractPayloadZip(string destination)
    {
        string self = Application.ExecutablePath;
        byte[] bytes = File.ReadAllBytes(self);
        int markerIndex = LastIndexOf(bytes, Marker);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException("安装包缺少客户端文件载荷。");
        }

        int lengthStart = markerIndex + Marker.Length;
        int lengthEnd = Array.IndexOf(bytes, (byte)'>', lengthStart);
        if (lengthEnd < 0 || lengthEnd + 1 >= bytes.Length || bytes[lengthEnd + 1] != (byte)'>')
        {
            throw new InvalidOperationException("安装包载荷标记损坏。");
        }

        string lengthText = Encoding.ASCII.GetString(bytes, lengthStart, lengthEnd - lengthStart);
        long zipLength = long.Parse(lengthText);
        long zipStart = markerIndex - zipLength;
        if (zipStart < 0)
        {
            throw new InvalidOperationException("安装包载荷长度无效。");
        }

        using (FileStream output = File.Create(destination))
        {
            output.Write(bytes, (int)zipStart, (int)zipLength);
        }
    }

    private static int LastIndexOf(byte[] source, byte[] pattern)
    {
        for (int i = source.Length - pattern.Length; i >= 0; i--)
        {
            bool matched = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j])
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return i;
            }
        }

        return -1;
    }
}
