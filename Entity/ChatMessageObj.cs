namespace VideoClient.Entity
{
    /**
     * 聊天对象，字段按照Vue选择器设计
     */
    public class ChatMessageObj
    {
        public string from;
        public string content;
        public bool fromMe;

        public ChatMessageObj(string from, string content)
        {
            this.from = from;
            this.content = content;
            this.fromMe = false;
        }

        public ChatMessageObj(string from, string content, bool fromMe)
        {
            this.from = from;
            this.content = content;
            this.fromMe = fromMe;
        }
    }
}