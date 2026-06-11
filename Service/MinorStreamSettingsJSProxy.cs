using System;
using VideoClient.UI;

namespace VidyoClient
{
    public class MinorStreamSettingsJsProxy
    {
        private FormMain _formMain;

        public MinorStreamSettingsJsProxy(FormMain formMain)
        {
            _formMain = formMain;
        }

        public String GetSystemStatusJson()
        {
            return _formMain.videoManager.GetSystemStatusJson();
        }

        public String GetCameraJson(int sourceType)
        {
            return _formMain.videoManager.GetCameraJson(sourceType);
        }

        public void AssignCamera(int sourceType, String deviceName)
        {
            _formMain.videoManager.AssignVideoDevice(sourceType, deviceName);
        }
    }
}