using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NLog;
using VideoClient.Util;

namespace VidyoClient
{
    public class SpeakerListener : Connector.IRegisterLocalSpeakerEventListener
    {
        private Control _control;
        private Connector _connector;

        delegate void StringArgReturningVoidDelegate(string text);

        private const string DEVICE_NONE = "关闭";
        private const string DEVICE_AUTO = "auto";

        public Dictionary<string, LocalSpeaker> speakers = new Dictionary<string, LocalSpeaker>();
        public String lastSelectedDeviceName;
        public string spkInUse = DEVICE_NONE;

        public SpeakerListener(Control control, Connector connector)
        {
            _control = control;
            _connector = connector;

            speakers.Add("关闭", null);

            lastSelectedDeviceName = Config.GetStringConfig("speaker_last_selected_device_name");
        }

        public void OnLocalSpeakerAdded(LocalSpeaker localSpeaker)
        {
            _logger.Info(
                "-----> On Local Speaker Added : id => {0} , name => {1} , stream_type => {2} , volume => {3}",
                localSpeaker.GetId(),
                localSpeaker.GetName(),
                localSpeaker.GetStreamType(),
                localSpeaker.GetVolume());

            // form.AddSpeakerToMenu(getName(localSpeaker));
            AddSpeakertoList(getName(localSpeaker), localSpeaker);
        }

        public void OnLocalSpeakerRemoved(LocalSpeaker localSpeaker)
        {
            // form.RemoveSpeakerFromMenu(getName(localSpeaker));
            RemoveSpeakerFromList(getName(localSpeaker));
        }

        public void OnLocalSpeakerSelected(LocalSpeaker localSpeaker)
        {
            // if (null != localSpeaker)
            // {
            //     spkInUse = getName(localSpeaker);
            // }
        }

        public void OnLocalSpeakerStateUpdated(LocalSpeaker localSpeaker, Device.DeviceState state)
        {
        }

        public string getName(LocalSpeaker localSpeaker)
        {
            return localSpeaker.GetName() + "$" + localSpeaker.GetId();
        }

        public void AddSpeakertoList(string key, LocalSpeaker speaker)
        {
            speakers.Add(key, speaker);
        }

        public void RemoveSpeakerFromList(string key)
        {
            speakers.Remove(key);
        }

        public void SaveLastSelectedDevice(string deviceName)
        {
            lastSelectedDeviceName = deviceName;
            Config.SetStringConfig("speaker_last_selected_device_name", deviceName);
        }

        public String AssignLastSelectedDevice()
        {
            if (String.IsNullOrEmpty(lastSelectedDeviceName)) return DEVICE_NONE;

            if (String.Equals(DEVICE_AUTO, lastSelectedDeviceName))
            {
                foreach (KeyValuePair<string, LocalSpeaker> pair in speakers)
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
            else if (speakers.ContainsKey(lastSelectedDeviceName))
            {
                AssignDevice(lastSelectedDeviceName);
                return lastSelectedDeviceName;
            }
            else
            {
                foreach (KeyValuePair<string, LocalSpeaker> pair in speakers)
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
            RunOnUIThread((Action)(() =>
            {
                if ("关闭".Equals(deviceName))
                {
                    // _connector.SetSpeakerPrivacy(true);
                    _connector.SelectLocalSpeaker(null);
                }
                else
                {
                    _connector.SelectLocalSpeaker(speakers[deviceName]);
                    // _connector.SetSpeakerPrivacy(false);
                }

                spkInUse = deviceName;

                Log("Assign Speaker ({})", spkInUse);
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
            _logger.Info("[SpeakerManager] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[SpeakerManager] " + content, args);
        }
    }
}