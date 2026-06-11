using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using VideoClient.UI;
using VidyoClient.Ext;

namespace VidyoClient
{
    public class ParticipantListener : Connector.IRegisterParticipantEventListener
    {
        VideoSocketService _videoSocketService;

        public VideoParticipant _virtualParticipantMe;

        public Dictionary<string, VideoParticipant>
            _participants = new Dictionary<string, VideoParticipant>(); //key使用userId

        public IOnLoudestParticipantChangedListener loudestParticipantChangedListener { get; set; }
        public IOnParticipantsChangedListener participantsChangedListener { get; set; }

        public ParticipantListener(VideoSocketService videoSocketService)
        {
            _videoSocketService = videoSocketService;
            _virtualParticipantMe = new VideoParticipant(new ParticipantWrapper("0", "user_0", "我"));
        }

        public void OnParticipantJoined(Participant participant)
        {
            AddParticipant(participant);
            // form.log("app type->"+participant.GetApplicationType());
            //todo participants param is null
            participantsChangedListener?.OnParticipantsChanged(true, participant, null, _participants.Count <= 1);
        }

        public void OnParticipantLeft(Participant participant)
        {
            RemoveParticipant(participant);

            //todo participants param is null
            participantsChangedListener?.OnParticipantsChanged(false, participant, null, _participants.Count <= 1);
        }

        public void OnDynamicParticipantChanged(List<Participant> participants)
        {
            foreach (Participant p in participants)
            {
                Console.WriteLine(p.GetName());
            }
        }

        public void OnLoudestParticipantChanged(Participant participant, bool audioOnly)
        {
            loudestParticipantChangedListener?.OnLoudestParticipantChanged(participant, audioOnly);
        }

        private void AddParticipant(Participant participant)
        {
            // Log("OnParticipantJoined({0},{1},{2})",
            //     participant.GetId(),
            //     participant.GetUserId(),
            //     participant.GetApplicationType());

            VideoParticipant p = new VideoParticipant(participant);
            _participants.Add(p.GetUserId(), p);

            _videoSocketService.SendParticipantOnAddedEventToAll(p);
        }

        private void RemoveParticipant(Participant participant)
        {
            // Log("OnParticipantLeft({0},{1},{2})",
            //     participant.GetId(),
            //     participant.GetUserId(),
            //     participant.GetApplicationType());

            if (IsParticipantExist(participant))
            {
                _videoSocketService.SendParticipantOnRemovedEventToAll(_participants[participant.GetUserId()]);
                _participants.Remove(participant.GetUserId());
            }
        }

        public bool IsParticipantExist(Participant participant)
        {
            if (null != participant)
            {
                return _participants.ContainsKey(participant.GetUserId());
            }

            return false;
        }

        /// <summary>
        /// 判断主讲人是否在会中
        /// </summary>
        /// <param name="lecturerId">为Participant.getUserId()后半部分，通过VideoParticipant.parseId()解析</param>
        /// <returns></returns>
        public bool IsLecturerExist(string lecturerId)
        {
            if (IsLecturerMe(lecturerId))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(lecturerId))
            {
                foreach (var pair in _participants)
                {
                    if (string.Equals(lecturerId, pair.Value.GetId()))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsLecturerMe(string lecturerId)
        {
            if (null != _virtualParticipantMe && string.Equals(lecturerId, _virtualParticipantMe.GetId()))
            {
                return true;
            }

            return false;
        }

        public bool AddVideoSource(VideoSource videoSource)
        {
            if (null == videoSource?.@from) return false;

            //添加本地视频源
            if (videoSource.IsLocalCamera() || videoSource.IsLocalCamera2() || videoSource.IsVirtualSource())
            {
                _virtualParticipantMe.AddVideoSource(videoSource.GetSourceKey(), videoSource.type);
                return true;
            }

            //添加其他参会人远端源
            String userId = videoSource.from.GetUserId();
            if (_participants.ContainsKey(userId))
            {
                _participants[userId].AddVideoSource(videoSource.GetSourceKey(), videoSource.type);
                return true;
            }

            return false;
        }

        public void RemoveVideoSource(String userId, String sourceKey)
        {
            if (String.IsNullOrEmpty(sourceKey)) return;

            if (_participants.ContainsKey(userId))
            {
                _participants[userId].RemoveVideoSource(sourceKey);
            }
        }

        public void RemoveMyVideoSource(String sourceKey)
        {
            _virtualParticipantMe.RemoveVideoSource(sourceKey);
        }

        public string GetParticipantListJson()
        {
            return "{\"code\":" + 0 + ",\"data\":" + JsonConvert.SerializeObject(_participants) + "}";
        }

        public VideoParticipant QueryByVideoSourceKey(string sourceKey)
        {
            foreach (KeyValuePair<string, VideoParticipant> kv in _participants)
            {
                if (kv.Value.ContainsVideoSourceKey(sourceKey))
                {
                    return kv.Value;
                }
            }

            return null;
        }

        public string QueryNameByVideoSourceKey(string sourceKey)
        {
            VideoParticipant fromWho = QueryByVideoSourceKey(sourceKey);
            return fromWho != null ? fromWho.name : "unknown";
        }

        private void Log(string content)
        {
            _logger.Info("[ParticipantsManager] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[ParticipantsManager] " + content, args);
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }

    public interface IOnLoudestParticipantChangedListener
    {
        void OnLoudestParticipantChanged(Participant participant, bool audioOnly);
    }

    public interface IOnParticipantsChangedListener
    {
        /// <summary>
        /// 监听参会人变化
        /// </summary>
        /// <param name="isJoinedEvent">true表示join事件，反之表示left事件</param>
        /// <param name="participants"></param>
        /// <param name="isJustMe"></param>
        void OnParticipantsChanged(bool isJoinedEvent, Participant participant, List<Participant> participants,
            bool isJustMe);
    }
}