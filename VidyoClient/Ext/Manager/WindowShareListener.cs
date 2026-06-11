using System;
using System.Collections.Generic;
using NLog;
using VideoClient.Util;
using VideoClient.VidyoClient.Ext;
using VidyoClient.Ext;

namespace VidyoClient
{
    public class WindowShareListener : Connector.IRegisterLocalWindowShareEventListener
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Connector _connector;
        private VideoSocketService _videoSocketService;

        public LayoutController _layoutController;

        public CameraConfig _cameraConfig;

        public Dictionary<string, LocalWindowShare> windowShares = new Dictionary<string, LocalWindowShare>();

        public const string DEVICE_NONE = "关闭";
        private LocalWindowShare _lastWindowShare;
        private bool _startedWindowShare;

        public Dictionary<string, uint[]> resolutions = new Dictionary<string, uint[]>();
        public Dictionary<string, uint> framerates = new Dictionary<string, uint>();

        public WindowShareListener(Connector connector, VideoSocketService videoSocketService)
        {
            _connector = connector;
            _videoSocketService = videoSocketService;
            _cameraConfig = new CameraConfig(
                4,
                Config.GetStringConfig("camera_5_name"),
                Config.GetIntegerConfig("camera_5_position_of_screen"),
                Config.GetIntegerConfig("camera_5_position_of_renderer"),
                DEVICE_NONE,
                Config.GetStringConfig("camera_5_last_selected_device_name"),
                Config.GetStringConfig("camera_5_resolution"),
                (uint)Config.GetIntegerConfig("camera_5_framerate"),
                (uint)Config.GetIntegerConfig("SetTargetBitRate_5"));

            windowShares.Add(DEVICE_NONE, null);

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
        }

        public void OnLocalWindowShareAdded(LocalWindowShare localWindowShare)
        {
            _logger.Info(
                "-----> On Local WindowShare Added : id => {0} , name => {1} , app_name => {2} , state => {3}",
                localWindowShare.GetId(),
                localWindowShare.GetName(),
                localWindowShare.GetApplicationName(),
                localWindowShare.GetWindowState());

            // form.AddWindowShareToMenu(getName(localWindowShare));
            AddWindowShareToList(GetName(localWindowShare), localWindowShare);
            _videoSocketService?.SendSystemStatusUpdatedEventToAll();
            //form.log("OnLocalWindowShareAdded -> "+ localWindowShare.GetName());
        }

        public void OnLocalWindowShareRemoved(LocalWindowShare localWindowShare)
        {
            // form.RemoveWindowShareFromMenu(getName(localWindowShare));
            RemoveWindowShareFromList(GetName(localWindowShare));
            _videoSocketService?.SendSystemStatusUpdatedEventToAll();
            //form.log("OnLocalWindowShareRemoved -> " + localWindowShare.GetName());
        }

        public void OnLocalWindowShareSelected(LocalWindowShare localWindowShare)
        {
            //form.log("OnLocalWindowShareSelected -> " + localWindowShare.GetName());
            if (null != localWindowShare)
            {
                WindowShareSelected(GetName(localWindowShare), localWindowShare);
            }

            _layoutController?.SetStatusUiVisible(null != localWindowShare);

            _videoSocketService?.SendSystemStatusUpdatedEventToAll();
        }

        public void OnLocalWindowShareStateUpdated(LocalWindowShare localWindowShare, Device.DeviceState state)
        {
            //form.log("OnLocalWindowShareStateUpdated -> " + localWindowShare.GetName());
        }

        private static string GetName(LocalWindowShare localWindowShare)
        {
            string legitimateJson = localWindowShare.GetName().Replace("\\", "/").Replace("\"", "'");
            return $"{legitimateJson}({localWindowShare.GetId()})";
        }

        private void AddWindowShareToList(String name, LocalWindowShare localWindowShare)
        {
            if (!windowShares.ContainsKey(name))
            {
                windowShares.Add(name, localWindowShare);
            }
        }

        private void RemoveWindowShareFromList(String name)
        {
            windowShares.Remove(name);
        }

        private void WindowShareSelected(String name, LocalWindowShare lastWindowShare)
        {
            _lastWindowShare = lastWindowShare;
            _cameraConfig.inUse = name;
        }

        public bool SetFramerate(string framerateKey)
        {
            _cameraConfig.framerate = framerates[framerateKey];
            if (!DEVICE_NONE.Equals(_cameraConfig.inUse))
            {
                windowShares[_cameraConfig.inUse].SetBoundsConstraints(
                    _cameraConfig.framerate, _cameraConfig.framerate,
                    _cameraConfig.width, _cameraConfig.width,
                    _cameraConfig.height, _cameraConfig.height);
            }

            _logger.Info("select window share framerate " + _cameraConfig.framerate);
            return true;
        }

        public bool SetResolution(string resolutionKey)
        {
            _cameraConfig.resolution = resolutionKey;
            _cameraConfig.width = resolutions[resolutionKey][0];
            _cameraConfig.height = resolutions[resolutionKey][1];
            _logger.Info("select window share resolution " + _cameraConfig.width + "x" + _cameraConfig.height);
            return true;
        }

        public void SaveLastSelectedWindow(string deviceName)
        {
            _cameraConfig.lastSelectedDeviceName = deviceName;
            // Config.SetStringConfig("camera_5_last_selected_device_name", deviceName);
        }

        public void SaveResolution(string resolutionKey)
        {
            Config.SetStringConfig("camera_5_resolution", resolutionKey);
        }

        public void SaveFramerate(string framerateKey)
        {
            Config.SetStringConfig("camera_5_framerate", framerateKey);
        }

        public void AssignWindowShare(string monitor)
        {
            _cameraConfig.inUse = monitor;

            if (DEVICE_NONE.Equals(monitor))
            {
                _connector.SelectLocalWindowShare(null);
                _startedWindowShare = false;
            }
            else
            {
                ulong framerate = 1000000000 / _cameraConfig.framerate;
                _lastWindowShare = windowShares[monitor];

                // _lastWindowShare.SetBoundsConstraints(
                //     framerate, framerate,
                //     _cameraConfig.width, _cameraConfig.width,
                //     _cameraConfig.height, _cameraConfig.height);
                // _connector.SelectLocalWindowShare(_lastWindowShare);
                ConnectorShareOptions options = new ConnectorShareOptions(IntPtr.Zero);
                options.enableAudio = true;
                options.enableHighFramerate = true;
                _connector.SelectLocalWindowShareAdvanced(_lastWindowShare, options);

                _startedWindowShare = true;
            }
        }

        public void Close()
        {
            if (_startedWindowShare) AssignWindowShare(DEVICE_NONE);
        }

        public bool IsInUsed()
        {
            return !string.Equals(_cameraConfig.inUse, DEVICE_NONE);
        }
    };
}