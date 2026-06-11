namespace VideoClient.VidyoClient.Ext
{
    public enum LayoutMode
    {
        Mode_UNKNOWN = -1,
        MODE_NORMAL = 0, //网格布局，等分大小
        MODE_LECTURE_FIXED_LECTURER = 1, //画廊布局，固定主讲人（用户指定、会议指定）
        MODE_LECTURE_DYNAMIC_LECTURER = 2, //画廊布局，动态主讲人（语音激励）
        MODE_LECTURE_ONLY_LECTURER = 3, //仅看主讲人
    }
}