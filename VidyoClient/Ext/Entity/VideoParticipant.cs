using System;
using System.Collections.Generic;

namespace VidyoClient
{
    /// <summary>
    /// 视频参会人（扩展于SDK参会人，保存更多参会人信息）
    /// </summary>
    public class VideoParticipant
    {
        public string id; //对应userId下划线后部分，等价于entityID
        public string userId; //由用户类型+下划线+entityID组成
        public string name;
        public string type; //用户类型：User、Guest
        public int appType; //对应vidyo的applicationType
        
        /**
         * 只存储视频源的key，视频源对象可以通过key在LayoutContoller中获取
         */
        public Dictionary<string, string> _videoSources = new Dictionary<string, string>();

        public Participant participant;

        public VideoParticipant(Participant participant)
        {
            userId = participant.GetUserId();
            if (!String.IsNullOrEmpty(userId))
            {
                string[] split = userId.Split('_');
                if (split?.Length == 2)
                {
                    id = split[1];
                    type = split[0];
                }
            }

            name = participant.GetName();
            switch (participant.GetApplicationType())
            {
                case Participant.ParticipantApplicationType.ParticipantAPPLICATIONTYPE_None:
                    appType = 0;
                    break;
                case Participant.ParticipantApplicationType.ParticipantAPPLICATIONTYPE_Recorder:
                    appType = 1;
                    break;
                case Participant.ParticipantApplicationType.ParticipantAPPLICATIONTYPE_Gateway:
                    appType = 2;
                    break;
            }

            this.participant = participant;
        }

        /**
         * 参会人ID解析
         * 因为Vidyo的参会人用户id包含类型数据（形如"user_1"），但实际系统内部通信依赖的是id部分
         */
        public static string ParseId(Participant participant)
        {
            if (!String.IsNullOrEmpty(participant.GetUserId()))
            {
                string[] split = participant.GetUserId().Split('_');
                if (split?.Length == 2)
                {
                    return split[1];
                }
            }

            return null;
        }

        public static string ParseUserType(Participant participant)
        {
            if (!String.IsNullOrEmpty(participant.GetUserId()))
            {
                string[] split = participant.GetUserId().Split('_');
                if (split?.Length == 2)
                {
                    return split[0];
                }
            }

            return null;
        }

        public string GetId()
        {
            return id;
        }

        public string GetUserId()
        {
            return userId;
        }

        public string GetName()
        {
            return name;
        }

        public string GetParticipantType()
        {
            return type;
        }

        public void AddVideoSource(string sourceKey, string remoteType)
        {
            if (!_videoSources.ContainsKey(sourceKey))
            {
                _videoSources.Add(sourceKey, remoteType);
            }
        }

        public void RemoveVideoSource(string sourceKey)
        {
            if (_videoSources.ContainsKey(sourceKey))
            {
                _videoSources.Remove(sourceKey);
            }
        }

        public bool ContainsVideoSourceKey(string sourceKey)
        {
            return _videoSources.ContainsKey(sourceKey);
        }

        public static bool IsLecturer(Participant participant, string lecturerId)
        {
            string id = ParseId(participant);
            return String.Equals(lecturerId, id);
        }
    }
}