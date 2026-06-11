namespace VideoClient.Entity
{
    /**
     * 可用摄像头对象，字段按照Vue选择器设计
     */
    public class CameraObj
    {
        /**
         * 设备名，用于展示
         */
        public string label;

        /**
         * 设备ID，用于程序设置
         */
        public string value;

        /**
         * 是否可用，取值true或false
         */
        public bool disabled;

        public CameraObj(string label, string value)
        {
            this.label = label;
            this.value = value;
            this.disabled = false;
        }

        public CameraObj(string label, string value, bool disabled)
        {
            this.label = label;
            this.value = value;
            this.disabled = disabled;
        }
    }
}