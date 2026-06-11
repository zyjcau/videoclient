using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NLog;
using VideoClient.Util;
using VideoClient.VidyoClient.Ext;
using VidyoClient.Ext;

namespace VidyoClient
{
    public class MonitorShareListener : Connector.IRegisterLocalMonitorEventListener
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Connector _connector;
        private VideoSocketService _videoSocketService;
        public LayoutController _layoutController;

        public LocalMonitor lastMonitor;
        public Dictionary<string, LocalMonitor> monitorShares = new Dictionary<string, LocalMonitor>();
        public CameraConfig _cameraConfig;

        public Dictionary<string, uint[]> resolutions = new Dictionary<string, uint[]>();
        public Dictionary<string, uint> framerates = new Dictionary<string, uint>();

        public bool startedMonitorShare = false;

        public const string DEVICE_NONE = "关闭";

        public MonitorShareListener(Connector connector, VideoSocketService videoSocketService)
        {
            _connector = connector;
            _videoSocketService = videoSocketService;

            _cameraConfig = new CameraConfig(
                3,
                Config.GetStringConfig("camera_4_name"),
                Config.GetIntegerConfig("camera_4_position_of_screen"),
                Config.GetIntegerConfig("camera_4_position_of_renderer"),
                DEVICE_NONE,
                Config.GetStringConfig("camera_4_last_selected_device_name"),
                Config.GetStringConfig("camera_4_resolution"),
                (uint)Config.GetIntegerConfig("camera_4_framerate"),
                (uint)Config.GetIntegerConfig("SetTargetBitRate_4"));

            // framerates.Add("5", 5);
            // framerates.Add("10", 10);
            // framerates.Add("15", 15);
            // framerates.Add("30", 30);
            // framerates.Add("60", 60);
            framerates.Add("5", 5);
            framerates.Add("10", 10);
            framerates.Add("15", 15);
            framerates.Add("25", 25);
            framerates.Add("30", 30);
            // framerates.Add("40", 40);
            framerates.Add("50", 50);
            framerates.Add("60", 60);

            resolutions.Add("auto", new uint[] { 1280, 720 });
            resolutions.Add("480p", new uint[] { 720, 480 });
            resolutions.Add("540p", new uint[] { 960, 540 });
            resolutions.Add("720p", new uint[] { 1280, 720 });
            resolutions.Add("1080p", new uint[] { 1920, 1080 });
            resolutions.Add("2K", new uint[] { 2560, 1440 });
            resolutions.Add("4K", new uint[] { 3840, 2160 });

            monitorShares.Add(DEVICE_NONE, null);
        }

        public void OnLocalMonitorAdded(LocalMonitor localMonitor)
        {
            _logger.Info(
                "-----> On Local Monitor Added : id => {0} , name => {1}",
                localMonitor.GetId(),
                localMonitor.GetName());

            // form.AddMonitorShareToMenu(getName(localMonitor));
            AddMonitorShareToList(GetName(localMonitor), localMonitor);

            _videoSocketService?.SendSystemStatusUpdatedEventToAll();
        }

        public void OnLocalMonitorRemoved(LocalMonitor localMonitor)
        {
            // form.RemoveMonitorShareFromMenu(getName(localMonitor));
            RemoveMonitorShareFromList(GetName(localMonitor));

            _videoSocketService?.SendSystemStatusUpdatedEventToAll();
        }

        public void OnLocalMonitorSelected(LocalMonitor localMonitor)
        {
            if (null != localMonitor)
            {
                MonitorShareSelected(GetName(localMonitor), localMonitor);
            }

            _layoutController?.SetStatusUiVisible(null != localMonitor);

            _videoSocketService?.SendSystemStatusUpdatedEventToAll();
        }

        public void OnLocalMonitorStateUpdated(LocalMonitor localMonitor, Device.DeviceState state)
        {
        }

        private static string GetName(LocalMonitor localMonitor)
        {
            int len = localMonitor.GetId().LastIndexOf("#") - localMonitor.GetId().IndexOf("#");
            //bug substring 引发 OutOfRangeException
            string cid = localMonitor.GetId().Substring(localMonitor.GetId().IndexOf("#"), len);
            return localMonitor.GetName() + "$" + cid;
        }

        private void AddMonitorShareToList(String name, LocalMonitor localMonitor)
        {
            if (!monitorShares.ContainsKey(name))
            {
                monitorShares.Add(name, localMonitor);
                // log("monitor num -> " + getScreenNum());
            }
        }

        private void RemoveMonitorShareFromList(String name)
        {
            monitorShares.Remove(name);
            _logger.Info("monitor num -> " + Screen.AllScreens.Length);
        }

        private void MonitorShareSelected(String name, LocalMonitor monitor)
        {
            lastMonitor = monitor;
            _cameraConfig.inUse = name;
        }

        public bool SetCam4Framerate(string framerateKey)
        {
            _cameraConfig.framerate = framerates[framerateKey];
            if (!DEVICE_NONE.Equals(_cameraConfig.inUse))
            {
                monitorShares[_cameraConfig.inUse].SetBoundsConstraints(
                    _cameraConfig.framerate, _cameraConfig.framerate,
                    _cameraConfig.width, _cameraConfig.width,
                    _cameraConfig.height, _cameraConfig.height);
            }

            _logger.Info("select 4 camera framerate " + _cameraConfig.framerate);
            return true;
        }

        public bool SetCam4Resolution(string resolutionKey)
        {
            _cameraConfig.resolution = resolutionKey;
            _cameraConfig.width = resolutions[resolutionKey][0];
            _cameraConfig.height = resolutions[resolutionKey][1];
            _logger.Info("select camera 4 resolution " + _cameraConfig.width + "x" + _cameraConfig.height);
            return true;
        }

        public void SaveLastSelectedCamera(string cameraName)
        {
            _cameraConfig.lastSelectedDeviceName = cameraName;
            // Config.SetStringConfig("camera_4_last_selected_device_name", cameraName);
        }

        public void SaveResolution(string resolutionKey)
        {
            Config.SetStringConfig("camera_4_resolution", resolutionKey);
        }

        public void SaveFramerate(string framerateKey)
        {
            Config.SetStringConfig("camera_4_framerate", framerateKey);
        }

        public void AssignMonitorShare(string monitor)
        {
            _cameraConfig.inUse = monitor;

            if (DEVICE_NONE.Equals(monitor))
            {
                _connector.SelectLocalMonitor(null);
                startedMonitorShare = false;
            }
            else
            {
                ulong framerate = 1000000000 / _cameraConfig.framerate;
                lastMonitor = monitorShares[monitor];

                // lastMonitor.SetBoundsConstraints(
                //     framerate, framerate,
                //     _cameraConfig.width, _cameraConfig.width,
                //     _cameraConfig.height, _cameraConfig.height);
                // _connector.SelectLocalMonitor(lastMonitor);
                ConnectorShareOptions options = new ConnectorShareOptions(IntPtr.Zero);
                options.enableAudio = false;//设备上如果没有音频设备，会导致无法共享
                options.enableHighFramerate = true;
                _connector.SelectLocalMonitorAdvanced(lastMonitor, options);
                // _connector.SelectLocalMonitor(lastMonitor);

                startedMonitorShare = true;
            }
        }

        public void Close()
        {
            if (startedMonitorShare) AssignMonitorShare(DEVICE_NONE);
        }

        public bool IsInUsed()
        {
            return !string.Equals(_cameraConfig.inUse, DEVICE_NONE);
        }
    };
}