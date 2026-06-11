using System;

namespace VidyoClient
{
    public class ParticipantWrapper : Participant
    {
        private String _id;
        private String _name;
        private String _userId;

        private ParticipantApplicationType
            _applicationType = ParticipantApplicationType.ParticipantAPPLICATIONTYPE_None;

        // public ParticipantWrapper(IntPtr other) : base(other)
        // {
        // }

        public ParticipantWrapper(String id, String userId, String name) 
        {
            _id = id;
            _userId = userId;
            _name = name;
        }

        public override String GetId()
        {
            return _id;
        }

        public override String GetUserId()
        {
            return _userId;
        }

        public override String GetName()
        {
            return _name;
        }

        public override ParticipantApplicationType GetApplicationType()
        {
            return _applicationType;
        }
    }
}