using System.Web.UI.WebControls;
using VideoClient.Manager;
using VideoClient.UI;
using VideoClient.Util;

namespace VidyoClient
{
    public class HomeJSProxy
    {
        private VideoManager _videoManager;

        public HomeJSProxy()
        {
            _videoManager = VideoManager.GetInstance();
        }

        public string GetWebServiceUrl()
        {
            return Util.GetLocalIp() + ":" + Config.GetIntegerConfig("http_server_port");
        }

        public void OpenAppWindow()
        {
            _videoManager.OpenTangoAppWindow();
        }

        public void OpenSettingWindow()
        {
            _videoManager.OpenSettingsWindow();
        }

        public void OpenAVCheckWindow()
        {
            _videoManager.OpenAVCheckWindow();
        }

        public void DismissAVCheckWindow()
        {
            _videoManager.DismissAVCheckWindow();
        }

        public void OpenTencentRooms()
        {
            _videoManager.OpenTencentRooms();
        }

        public bool IsCameraOpened()
        {
            return _videoManager.cameraManager.IsCameraOpened();
        }

        public void CloseCamera()
        {
            _videoManager.CloseCamera();
        }

        public void OpenCamera()
        {
            _videoManager.OpenCamera();
        }

    }
}