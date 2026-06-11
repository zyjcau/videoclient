using System;
using System.Collections.Generic;
using NLog;
using VideoClient.Entity;
using VideoClient.Manager;
using VideoClient.Util;
using VideoClient.VidyoClient.Ext;
using VideoClient.VidyoClient.Ext.Manager;
using VidyoClient.Ext;

namespace VidyoClient
{
    public class CameraListener : Connector.IRegisterLocalCameraEventListener
    {
        #region 构造

        // const uint oneMb = 1024 * 1024;
        public const char CameraCustomIdSeparator = '$';

        private Connector _connector;
        private VideoManager _videoManager;
        private VideoSocketService _videoSocketService;
        private LayoutController _layoutController;
        public ThirdCameraManager _thirdCameraManager;

        public Dictionary<string, LocalCamera> cameras = new Dictionary<string, LocalCamera>();
        public List<CameraConfig> _cameraConfigs = new List<CameraConfig>();

        public Dictionary<string, uint[]> resolutions = new Dictionary<string, uint[]>();
        public Dictionary<string, uint> framerates = new Dictionary<string, uint>();

        public LocalCamera lastCamera, lastSecondCamera;

        public const string DEVICE_NONE = "关闭";
        public const string DEVICE_AUTO = "auto";

        public bool camera_1_name_use_display_name = true;
        public String displayName;

        public CameraListener(
            Connector connector,
            VideoSocketService videoSocketService,
            VideoManager videoManager)
        {
            _connector = connector;
            _videoSocketService = videoSocketService;
            _videoManager = videoManager;
            _thirdCameraManager = new ThirdCameraManager(_videoManager);
            Log("- create ThirdCameraManager");

            cameras.Add(DEVICE_NONE, null);

            camera_1_name_use_display_name = Config.GetBooleanConfig("camera_1_name_use_display_name");
            _cameraConfigs.Add(new CameraConfig(
                0,
                Config.GetStringConfig("camera_1_name"),
                Config.GetIntegerConfig("camera_1_position_of_screen"),
                Config.GetIntegerConfig("camera_1_position_of_renderer"),
                DEVICE_NONE,
                Config.GetStringConfig("camera_1_last_selected_device_name"),
                Config.GetStringConfig("camera_1_resolution"),
                (uint)Config.GetIntegerConfig("camera_1_framerate"),
                (uint)Config.GetIntegerConfig("SetTargetBitRate_1")));
            _cameraConfigs[0].isMirroring = Config.GetBooleanConfig("camera_1_mirroring");

            _cameraConfigs.Add(new CameraConfig(
                1,
                Config.GetStringConfig("camera_2_name"),
                Config.GetIntegerConfig("camera_2_position_of_screen"),
                Config.GetIntegerConfig("camera_2_position_of_renderer"),
                DEVICE_NONE,
                Config.GetStringConfig("camera_2_last_selected_device_name"),
                Config.GetStringConfig("camera_2_resolution"),
                (uint)Config.GetIntegerConfig("camera_2_framerate"),
                (uint)Config.GetIntegerConfig("SetTargetBitRate_2")));
            _cameraConfigs[1].isMirroring = Config.GetBooleanConfig("camera_2_mirroring");

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

        public LayoutController GetLayoutController()
        {
            if (_layoutController == null)
            {
                _layoutController = _videoManager.layoutController;
            }

            return _layoutController;
        }

        public CameraConfig GetVideoSourceConfig(int deviceIndex)
        {
            if (deviceIndex == 0 || deviceIndex == 1)
            {
                return _cameraConfigs[deviceIndex];
            }

            return deviceIndex == 2 ? _thirdCameraManager._cameraConfig : null;
        }

        #endregion

        #region 摄像头回调

        public void OnLocalCameraAdded(LocalCamera localCamera)
        {
            _logger.Info(
                "-----> On Local Camera Added : id => {0} , name => {1} , backlight_compensation => {2} , framerate_profile => {3} , resolution_profile => {4} , position => {5} , preview_label => {6}",
                localCamera.GetId(),
                localCamera.GetName(),
                localCamera.GetBacklightCompensation(),
                localCamera.GetFramerateTradeOffProfile(),
                localCamera.GetResolutionTradeOffProfile(),
                localCamera.GetPosition(),
                localCamera.GetPreviewLabel());
            if (null != localCamera.GetControlCapabilities())
            {
                CameraControlCapabilities capabilities = localCamera.GetControlCapabilities();
                _logger.Info($"-----| hasPhotoCapture => {capabilities.hasPhotoCapture}");
                _logger.Info($"-----| hasPresetSupport => {capabilities.hasPresetSupport}");
                _logger.Info($"-----| hasViscaSupport => {capabilities.hasViscaSupport}");
                _logger.Info($"-----| zoomHasNudge => {capabilities.zoomHasNudge}");
                _logger.Info($"-----| zoomHasRubberBand => {capabilities.zoomHasRubberBand}");
                _logger.Info($"-----| zoomHasContinuousMove => {capabilities.zooomHasContinuousMove}");
                _logger.Info($"-----| panTiltHasNudge => {capabilities.panTiltHasNudge}");
                _logger.Info($"-----| panTiltHasRubberBand => {capabilities.panTiltHasRubberBand}");
                _logger.Info($"-----| panTiltHasContinuousMove => {capabilities.panTiltHasContinuousMove}");
            }
            else
            {
                _logger.Info("-----| Camera Control Capabilities => null");
            }


            if (!IsNormalDevice(localCamera))
            {
                return;
            }

            AddCameraToList(getName(localCamera), localCamera);

            // _thirdCameraManager.InitCameras();

            _videoSocketService?.SendSystemStatusUpdatedEventToAll();
        }

        public void OnLocalCameraRemoved(LocalCamera localCamera)
        {
            if (!IsNormalDevice(localCamera))
            {
                return;
            }

            RemoveCameraFromList(getName(localCamera));

            // _thirdCameraManager.InitCameras();

            _videoSocketService?.SendSystemStatusUpdatedEventToAll();
        }

        public void OnLocalCameraSelected(LocalCamera localCamera)
        {
            if (localCamera != null)
            {
                Log("OnLocalCameraSelected Id -> {0},{1}", localCamera.GetId(), localCamera.GetName());

                if (!IsNormalDevice(localCamera))
                {
                    Log("select camera 1 failed. is not normal device!!!");
                    return;
                }

                SelectCamera(getName(localCamera), localCamera);
            }
            else
            {
                Log("OnLocalCameraSelected null...");
                SelectCamera(null, null);
            }
        }

        public void OnLocalCameraStateUpdated(LocalCamera localCamera, Device.DeviceState state)
        {
        }

        private String getName(LocalCamera localCamera)
        {
            string cid = "";

            if (localCamera.GetId().IndexOf("#") > 0)
            {
                int len = localCamera.GetId().LastIndexOf("#") - localCamera.GetId().IndexOf("#");
                cid = localCamera.GetId().Substring(localCamera.GetId().IndexOf("#"), len);
            }
            else if (localCamera.GetId().IndexOf("{") > 0)
            {
                int len = localCamera.GetId().IndexOf("}") - localCamera.GetId().IndexOf("{");
                cid = localCamera.GetId().Substring(localCamera.GetId().IndexOf("{"), len);
            }

            // form.log("摄像头ID：" + cid);
            return localCamera.GetName() + CameraCustomIdSeparator + cid;
        }

        private bool IsNormalDevice(LocalCamera localCamera)
        {
            if (null == localCamera || String.IsNullOrEmpty(localCamera.GetId()))
            {
                return false;
            }

            return localCamera.GetId().IndexOf("#") > 0 || localCamera.GetId().IndexOf("{") > 0;
        }

        private void AddCameraToList(string key, LocalCamera camera)
        {
            cameras.Add(key, camera);
        }

        private void RemoveCameraFromList(string key)
        {
            if (cameras.ContainsKey(key))
            {
                // connector.UnregisterLocalCameraFrameListener(cameras[key]);
            }

            cameras.Remove(key);
        }

        /// <summary>
        /// 获取摄像头对象列表，其中包含了摄像头被占用参数，便于UI层表示
        /// </summary>
        /// <param name="cameraIndex"></param>
        /// <returns></returns>
        public List<CameraObj> GetCameraObjectList(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex > 2) return null;

            //初始化占用列表（用于比对）
            var inUseList = new List<string>
            {
                _cameraConfigs[0].inUse, _cameraConfigs[1].inUse, _thirdCameraManager._cameraConfig.inUse
            };
            inUseList.RemoveAt(cameraIndex);

            List<CameraObj> cameraObjs = new List<CameraObj>();

            foreach (string key in cameras.Keys)
            {
                if (string.Equals(DEVICE_NONE, key))
                {
                    CameraObj obj = new CameraObj(DEVICE_NONE, DEVICE_NONE);
                    cameraObjs.Add(obj);
                }
                else
                {
                    var split = key.Split(CameraCustomIdSeparator);
                    if (split.Length == 2)
                    {
                        var contains = inUseList.Contains(key);
                        CameraObj obj = new CameraObj(split[0], key, contains);
                        cameraObjs.Add(obj);
                    }
                }
            }

            return cameraObjs;
        }

        #endregion

        #region 第一路

        public String GetLastCameraName()
        {
            return lastCamera == null ? DEVICE_NONE : getName(lastCamera);
        }

        private void SelectCamera(String name, LocalCamera camera)
        {
            if (!String.IsNullOrEmpty(name) && null != camera)
            {
                if (lastCamera != null)
                {
                    if (!GetLayoutController().IsUseDefaultLayout())
                    {
                        GetLayoutController().StopRenderingLocalCamera(lastCamera, true, _cameraConfigs[0]);
                    }
                }

                lastCamera = camera;

                //设置摄像头性能参数
                lastCamera.SetResolutionTradeOffProfile(
                    LocalCamera.LocalCameraTradeOffProfile.LocalcameratradeoffprofileHigh);
                lastCamera.SetFramerateTradeOffProfile(
                    LocalCamera.LocalCameraTradeOffProfile.LocalcameratradeoffprofileHigh);

                ulong framerate = 1000000000 / _cameraConfigs[0].framerate;

                if (camera_1_name_use_display_name && !String.IsNullOrEmpty(displayName))
                {
                    lastCamera.SetPreviewStreamLabel(displayName);
                }
                else
                {
                    lastCamera.SetPreviewStreamLabel(_cameraConfigs[0].previewLabel);
                }

                lastCamera.SetTargetBitRate(_cameraConfigs[0].bitrate);

                if (!GetLayoutController().IsUseDefaultLayout())
                {
                    lastCamera.SetMaxConstraint(_cameraConfigs[0].width, _cameraConfigs[0].height, framerate);
                    StartRenderingCamera();
                }

                Log("select camera 1 : ({0}x{1} , {2} , {3})",
                    _cameraConfigs[0].width, _cameraConfigs[0].height,
                    _cameraConfigs[0].framerate,
                    _cameraConfigs[0].bitrate);
            }
            else
            {
                if (!GetLayoutController().IsUseDefaultLayout() && null != lastCamera)
                {
                    GetLayoutController().StopRenderingLocalCamera(lastCamera, true, _cameraConfigs[0]);
                }

                lastCamera = null;
                // _cameraConfigs[0].isClosed = true;

                Log("select camera to null.");
            }

            _cameraConfigs[0].inUse = name;
        }

        public void StartRenderingCamera()
        {
            GetLayoutController().StartRenderingLocalCamera(lastCamera, _cameraConfigs[0]);
        }

        public void SaveLastSelectedCamera(string cameraName)
        {
            _cameraConfigs[0].lastSelectedDeviceName = cameraName;
            Config.SetStringConfig("camera_1_last_selected_device_name", cameraName);
        }

        public void SaveCam1Resolution(string resolutionKey)
        {
            Config.SetStringConfig("camera_1_resolution", resolutionKey);
        }

        public void SaveCam1Framerate(string framerateKey)
        {
            Config.SetStringConfig("camera_1_framerate", framerateKey);
        }

        public void SaveCam2Resolution(string resolutionKey)
        {
            Config.SetStringConfig("camera_2_resolution", resolutionKey);
        }

        public void SaveCam2Framerate(string framerateKey)
        {
            Config.SetStringConfig("camera_2_framerate", framerateKey);
        }

        public String AssignLastSelectedCamera()
        {
            if (String.IsNullOrEmpty(_cameraConfigs[0].lastSelectedDeviceName)) return DEVICE_NONE;

            if (String.Equals(DEVICE_AUTO, _cameraConfigs[0].lastSelectedDeviceName))
            {
                foreach (KeyValuePair<string, LocalCamera> pair in cameras)
                {
                    String cameraName = pair.Key;
                    if (!String.Equals(DEVICE_NONE, cameraName))
                    {
                        AssignCamera(cameraName);
                        return cameraName;
                    }
                }
            }
            else if (String.Equals(DEVICE_NONE, _cameraConfigs[0].lastSelectedDeviceName))
            {
                return DEVICE_NONE;
            }
            else if (cameras.ContainsKey(_cameraConfigs[0].lastSelectedDeviceName))
            {
                AssignCamera(_cameraConfigs[0].lastSelectedDeviceName);
                return _cameraConfigs[0].lastSelectedDeviceName;
            }
            else
            {
                foreach (KeyValuePair<string, LocalCamera> pair in cameras)
                {
                    String cameraName = pair.Key;
                    if (!String.Equals(DEVICE_NONE, cameraName))
                    {
                        AssignCamera(cameraName);
                        return cameraName;
                    }
                }
            }

            return DEVICE_NONE;
        }

        public LocalCamera AssignCamera(string cameraName)
        {
            LocalCamera camera = null;

            if (String.Equals(cameraName, DEVICE_NONE))
            {
                // _cameraConfigs[0].isClosed = true;
                _connector.SelectLocalCamera(null);

                Log("Assign camera 1 close completed.");
            }
            else
            {
                // _cameraConfigs[0].isClosed = false;

                camera = cameras[cameraName];

                // //设置摄像头性能参数
                // camera.SetResolutionTradeOffProfile(LocalCamera.LocalCameraTradeOffProfile
                //     .LocalcameratradeoffprofileHigh);
                // camera.SetFramerateTradeOffProfile(
                //     LocalCamera.LocalCameraTradeOffProfile.LocalcameratradeoffprofileHigh);
                // ulong framerate = 1000000000 / _cameraConfigs[0].framerate;
                // camera.SetMaxConstraint(_cameraConfigs[0].width, _cameraConfigs[0].height, framerate);
                // // lastCamera.SetPreviewLabel(_cameraConfigs[0].previewLabel);
                // camera.SetPreviewStreamLabel(_cameraConfigs[0].previewLabel);
                // // camera.SetOrientation(Device.DeviceOrientation.DeviceorientationRIGHT, true);
                // // lastCamera.SetPreviewStreamLabel("test");
                // camera.SetTargetBitRate(_cameraConfigs[0].bitrate);

                _connector.SelectLocalCamera(camera);

                Log("Assign Camera ({0}x{1} , {2} , {3})",
                    _cameraConfigs[0].width, _cameraConfigs[0].height,
                    _cameraConfigs[0].framerate,
                    _cameraConfigs[0].bitrate);
                Log("camera name -> {0}", camera.GetName());
            }

            _cameraConfigs[0].inUse = cameraName;

            return camera;
        }

        public void CloseCamera()
        {
            if (IsCameraOpened()) AssignCamera(DEVICE_NONE);
        }

        public bool IsCameraOpened()
        {
            return !String.Equals(_cameraConfigs[0].inUse, DEVICE_NONE);
        }

        // public void StartPreviewCamera(Panel panel)
        // {
        //     LocalCamera localCamera;
        //     
        //     String cameraName = AssignLastSelectedCamera();
        //
        //     if (String.Equals(DEVICE_NONE, cameraName))
        //     {
        //         Log("StartPreviewCamera None success.");
        //         return;
        //     }
        //
        //     localCamera = cameras[cameraName];
        //
        //     if (null != localCamera)
        //     {
        //         _connector.AssignViewToLocalCamera(panel.Handle, localCamera, false, true);
        //         _connector.ShowViewAt(panel.Handle, 0, 0,
        //             (uint) panel.Width,
        //             (uint) panel.Height);
        //         _previewCameraPanel = panel;
        //         Log("StartPreviewCamera success. -> ", cameraName);
        //     }
        //     else
        //     {
        //         _previewCameraPanel = null;
        //         Log("StartPreviewCamera failed. -> ", cameraName);
        //     }
        // }

        // /**
        //  * 选择摄像头
        //  * 选择后，会保存选择结果
        //  */
        // public bool SelectPreviewCamera(String cameraName)
        // {
        //     if (String.Equals(DEVICE_NONE, cameraName))
        //     {
        //         CloseCamera();
        //         SaveLastSelectedCamera(cameraName);
        //         Log("SelectPreviewCamera Close success.");
        //     }
        //     else
        //     {
        //         LocalCamera localCamera = AssignCamera(cameraName);
        //         if (null != localCamera)
        //         {
        //             SaveLastSelectedCamera(cameraName);
        //             Log("SelectPreviewCamera success. -> {}", cameraName);
        //         }
        //         else
        //         {
        //             Log("SelectPreviewCamera failed.");
        //             return false;
        //         }
        //     }
        //
        //     return true;
        // }

        // public void StopPreviewCamera(Panel panel)
        // {
        //     if (IsCameraPreviewed())
        //     {
        //         _connector.HideView(panel.Handle);
        //         CloseCamera();
        //         _previewCameraPanel = null;
        //         Log("StopPreviewCamera success.");
        //     }
        //     else
        //     {
        //         Log("StopPreviewCamera failed, not previewing.");
        //     }
        // }
        //
        // public bool IsCameraPreviewed()
        // {
        //     return _previewCameraPanel != null;
        // }

        #endregion

        #region 第二路

        public void SaveLastSelectedContent(string cameraName)
        {
            _cameraConfigs[1].lastSelectedDeviceName = cameraName;
            // Config.SetStringConfig("camera_2_last_selected_device_name", cameraName);
        }

        public void AssignLastSelectedContent()
        {
            if (String.IsNullOrEmpty(_cameraConfigs[1].lastSelectedDeviceName)) return;

            if (String.Equals(DEVICE_AUTO, _cameraConfigs[1].lastSelectedDeviceName))
            {
                foreach (KeyValuePair<string, LocalCamera> pair in cameras)
                {
                    string cameraName = pair.Key;
                    if (!String.Equals(DEVICE_NONE, cameraName))
                    {
                        AssignContent(cameraName);
                        return;
                    }
                }
            }
            else if (String.Equals(DEVICE_NONE, _cameraConfigs[1].lastSelectedDeviceName))
            {
                return;
            }
            else if (cameras.ContainsKey(_cameraConfigs[1].lastSelectedDeviceName))
            {
                AssignContent(_cameraConfigs[1].lastSelectedDeviceName);
            }
            else
            {
                foreach (KeyValuePair<string, LocalCamera> pair in cameras)
                {
                    string cameraName = pair.Key;
                    if (!String.Equals(DEVICE_NONE, cameraName))
                    {
                        AssignContent(cameraName);
                        return;
                    }
                }
            }
        }

        public void AssignContent(string cameraName)
        {
            if (String.Equals(cameraName, DEVICE_NONE))
            {
                // _cameraConfigs[1].isClosed = true;
                _connector.SelectVideoContentShare(null);
                if (!GetLayoutController().IsUseDefaultLayout())
                {
                    GetLayoutController().StopRenderingLocalCamera(lastSecondCamera, false, _cameraConfigs[1]);
                }

                lastSecondCamera = null;

                Log("close camera 2.");
            }
            else
            {
                if (lastSecondCamera != null)
                {
                    if (!GetLayoutController().IsUseDefaultLayout())
                    {
                        GetLayoutController().StopRenderingLocalCamera(lastSecondCamera, false, _cameraConfigs[1]);
                    }
                }

                lastSecondCamera = cameras[cameraName];
                // _cameraConfigs[1].isClosed = false;

                //设置摄像头性能参数
                SetCameraResolutionProfile(1, _cameraConfigs[1].resolutionProfile);
                SetCameraFramerateProfile(1, _cameraConfigs[1].framerateProfile);
                lastSecondCamera.SetPreviewLabel(_cameraConfigs[1].previewLabel);
                // lastSecondCamera.SetPreviewStreamLabel(_cameraConfigs[1].previewLabel);
                // lastSecondCamera.SetOrientation(Device.DeviceOrientation.DeviceorientationDOWN, false);
                ulong framerate = 1000000000 / _cameraConfigs[1].framerate;
                lastSecondCamera.SetMaxConstraint(_cameraConfigs[1].width, _cameraConfigs[1].height, framerate);
                lastSecondCamera.SetTargetBitRate(_cameraConfigs[1].bitrate);

                Log("Assign Content ({0}x{1} , {2} , {3})",
                    _cameraConfigs[1].width, _cameraConfigs[1].height,
                    _cameraConfigs[1].framerate,
                    _cameraConfigs[1].bitrate);
                Log("content name -> {0}", lastSecondCamera.GetName());

                _connector.SelectVideoContentShare(lastSecondCamera);

                if (!GetLayoutController().IsUseDefaultLayout())
                {
                    GetLayoutController().StartRenderingLocalCamera(lastSecondCamera, _cameraConfigs[1]);
                }
            }

            _cameraConfigs[1].inUse = cameraName;
        }

        public void CloseContent()
        {
            if (IsContentOpened()) AssignContent(DEVICE_NONE);
        }

        public bool IsContentOpened()
        {
            return !String.Equals(_cameraConfigs[1].inUse, DEVICE_NONE);
        }

        #endregion

        #region 第三路

        public void AssignVirtualSource(string cameraName)
        {
            _thirdCameraManager.AssignThirdCamera(cameraName);
        }

        public void AssignLastSelectedVirtualSource()
        {
            _thirdCameraManager.AssignLastSelectedCamera();
        }

        public void SaveLastSelectedVirtualSource(string cameraName)
        {
            _thirdCameraManager.SaveLastSelectedCamera(cameraName);
        }

        public void SaveCam3Resolution(string resolutionKey)
        {
            _thirdCameraManager.SaveCam3Resolution(resolutionKey);
        }

        public bool SetCam3Resolution(string resolutionKey)
        {
            return _thirdCameraManager.SetCam3Resolution(resolutionKey);
        }

        public void SaveCam3Framerate(string framerateKey)
        {
            _thirdCameraManager.SaveCam3Framerate(framerateKey);
        }

        public bool SetCam3Framerate(string framerateKey)
        {
            return _thirdCameraManager.SetCam3Framerate(framerateKey);
        }


        public void CloseVirtualSource()
        {
            _thirdCameraManager?.Close();
        }

        public bool IsVirtualSourceOpened()
        {
            return _thirdCameraManager.IsOpened();
        }

        #endregion

        #region 设置分辨率、帧率、配置

        //-------------------------------设置帧率-----------------------------------------------

        public bool SetCam1Framerate(string framerateKey)
        {
            _cameraConfigs[0].framerate = framerates[framerateKey];
            if (null != lastCamera)
            {
                ulong framerate = 1000000000 / _cameraConfigs[0].framerate;
                lastCamera.SetMaxConstraint(_cameraConfigs[0].width, _cameraConfigs[0].height, framerate);
                Log("set camera 1 framerate " + _cameraConfigs[0].framerate + " success.");
                return true;
            }
            else
            {
                Log("set camera 1 framerate  failed.");
            }

            return false;
        }

        public bool SetCam2Framerate(string framerateKey)
        {
            _cameraConfigs[1].framerate = framerates[framerateKey];
            if (null != lastSecondCamera)
            {
                ulong framerate = 1000000000 / _cameraConfigs[1].framerate;
                lastSecondCamera.SetMaxConstraint(_cameraConfigs[1].width, _cameraConfigs[1].height, framerate);
                Log("set camera 2 framerate " + _cameraConfigs[1].framerate);
                return true;
            }
            else
            {
                Log("set camera 2 framerate  failed.");
            }

            return false;
        }

        //-------------------------------设置分辨率-----------------------------------------------

        public bool SetCam1Resolution(string resolutionKey)
        {
            if (resolutionKey == "auto")
            {
                return true;
            }

            _cameraConfigs[0].resolution = resolutionKey;
            _cameraConfigs[0].width = resolutions[resolutionKey][0];
            _cameraConfigs[0].height = resolutions[resolutionKey][1];
            if (null != lastCamera)
            {
                ulong framerate = 1000000000 / _cameraConfigs[0].framerate;
                lastCamera.SetMaxConstraint(_cameraConfigs[0].width, _cameraConfigs[0].height, framerate);
                Log("set camera 1 resolution success. " + _cameraConfigs[0].width + "x" +
                    _cameraConfigs[0].height);
                return true;
            }
            else
            {
                Log("set camera 1 resolution failed. " + _cameraConfigs[0].width + "x" +
                    _cameraConfigs[0].height);
            }

            return false;
        }

        public bool SetCam2Resolution(string resolutionKey)
        {
            if (resolutionKey == "auto")
            {
                return true;
            }

            _cameraConfigs[1].resolution = resolutionKey;
            _cameraConfigs[1].width = resolutions[resolutionKey][0];
            _cameraConfigs[1].height = resolutions[resolutionKey][1];
            if (null != lastSecondCamera)
            {
                ulong framerate = 1000000000 / _cameraConfigs[1].framerate;
                lastSecondCamera.SetMaxConstraint(_cameraConfigs[1].width, _cameraConfigs[1].height, framerate);
                Log("set camera 2 resolution success. " + _cameraConfigs[1].width + "x" +
                    _cameraConfigs[1].height);
                return true;
            }
            else
            {
                Log("set camera 2 resolution failed. " + _cameraConfigs[1].width + "x" +
                    _cameraConfigs[1].height);
            }

            return false;
        }

        //-------------------------------设置摄像头-----------------------------------------------
        public bool SetCameraResolutionProfile(int cameraIndex, int profile)
        {
            switch (cameraIndex)
            {
                case 0:
                    lastCamera?.SetResolutionTradeOffProfile((LocalCamera.LocalCameraTradeOffProfile)profile);
                    break;
                case 1:
                    lastSecondCamera?.SetResolutionTradeOffProfile((LocalCamera.LocalCameraTradeOffProfile)profile);
                    break;
            }

            return true;
        }

        public bool SetCameraFramerateProfile(int cameraIndex, int profile)
        {
            switch (cameraIndex)
            {
                case 0:
                    lastCamera?.SetFramerateTradeOffProfile((LocalCamera.LocalCameraTradeOffProfile)profile);
                    break;
                case 1:
                    lastSecondCamera?.SetFramerateTradeOffProfile((LocalCamera.LocalCameraTradeOffProfile)profile);
                    break;
            }

            return true;
        }

        #endregion

        #region Log

        public readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private void Log(string content)
        {
            _logger.Info("[CameraManager] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[CameraManager] " + content, args);
        }

        #endregion
    };
}