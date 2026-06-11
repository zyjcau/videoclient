using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NLog;
using VideoClient.Util;

namespace VidyoClient
{
    public class MicrophoneListener : Connector.IRegisterLocalMicrophoneEventListener
    {
        // private Form form;
        private Control _control;
        private readonly Connector _connector;

        delegate void StringArgReturningVoidDelegate(string text);

        private const string DEVICE_NONE = "关闭";
        private const string DEVICE_AUTO = "auto";

        public Dictionary<string, LocalMicrophone> mics = new Dictionary<string, LocalMicrophone>();
        public String lastSelectedDeviceName;
        public String micInUse = DEVICE_NONE;

        public MicrophoneListener(Control control, Connector connector)
        {
            // this.form = form;
            _control = control;
            _connector = connector;

            mics.Add("关闭", null);

            lastSelectedDeviceName = Config.GetStringConfig("mic_last_selected_device_name");
        }

        public void OnLocalMicrophoneAdded(LocalMicrophone localMicrophone)
        {
            _logger.Info(
                "-----> On Local Microphone Added : id => {0} , name => {1} , signal_type => {2} , volume => {3} , boost => {4} , auto_gain => {5} , echo_cancellcation => {6}",
                localMicrophone.GetId(),
                localMicrophone.GetName(),
                localMicrophone.GetSignalType(),
                localMicrophone.GetVolume(),
                localMicrophone.GetBoost(),
                localMicrophone.GetAutoGain(),
                localMicrophone.GetEchoCancellation());

            // form.AddMicToMenu(getName(localMicrophone));
            AddMictoList(getName(localMicrophone), localMicrophone);
        }

        public void OnLocalMicrophoneRemoved(LocalMicrophone localMicrophone)
        {
            // form.RemoveMicFromMenu(getName(localMicrophone));
            RemoveMicFromList(getName(localMicrophone));
        }

        public void OnLocalMicrophoneSelected(LocalMicrophone localMicrophone)
        {
            // if (null != localMicrophone)
            // {
            //     micInUse = getName(localMicrophone);
            // }
        }

        public void OnLocalMicrophoneStateUpdated(LocalMicrophone localMicrophone, Device.DeviceState state)
        {
        }

        public string getName(LocalMicrophone localMicrophone)
        {
            return localMicrophone.GetName() + "$" + localMicrophone.GetId(); //id中存在-
        }

        public void AddMictoList(string key, LocalMicrophone mic)
        {
            mics.Add(key, mic);
        }

        public void RemoveMicFromList(string key)
        {
            mics.Remove(key);
        }

        public void SaveLastSelectedDevice(string micName)
        {
            lastSelectedDeviceName = micName;
            Config.SetStringConfig("mic_last_selected_device_name", micName);
        }

        public String AssignLastSelectedDevice()
        {
            if (String.IsNullOrEmpty(lastSelectedDeviceName)) return DEVICE_NONE;

            if (String.Equals(DEVICE_AUTO, lastSelectedDeviceName))
            {
                foreach (KeyValuePair<string, LocalMicrophone> pair in mics)
                {
                    String deviceName = pair.Key;
                    if (!String.Equals(DEVICE_NONE, deviceName))
                    {
                        AssignDevice(deviceName);
                        return deviceName;
                    }
                }
            }
            else if (String.Equals(DEVICE_NONE, lastSelectedDeviceName))
            {
                return DEVICE_NONE;
            }
            else if (mics.ContainsKey(lastSelectedDeviceName))
            {
                AssignDevice(lastSelectedDeviceName);
                return lastSelectedDeviceName;
            }
            else
            {
                foreach (KeyValuePair<string, LocalMicrophone> pair in mics)
                {
                    String deviceName = pair.Key;
                    if (!String.Equals(DEVICE_NONE, deviceName))
                    {
                        AssignDevice(deviceName);
                        return deviceName;
                    }
                }
            }

            return DEVICE_NONE;
        }

        public void AssignDevice(string deviceName)
        {
            // RunOnUIThread((Action)(() => { }));
            RunOnUIThread((Action)(() =>
            {
                if ("关闭".Equals(deviceName))
                {
                    // _connector.SetMicrophonePrivacy(true);
                    _connector.SelectLocalMicrophone(null);
                }
                else
                {
                    _connector.SelectLocalMicrophone(mics[deviceName]);
                    // _connector.SetMicrophonePrivacy(false);
                }

                micInUse = deviceName;

                Log("Assign Microphone ({})", micInUse);
            }));
        }

        public void Close()
        {
            AssignDevice(DEVICE_NONE);
        }

        public IAsyncResult RunOnUIThread(Delegate method)
        {
            return _control.BeginInvoke(method);
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private void Log(string content)
        {
            _logger.Info("[MicrophoneManager] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[MicrophoneManager] " + content, args);
        }
    }
}