namespace PixivFSUWP.Data.DownloadJobs
{
    /// <summary>
    /// 下载任务状态
    /// </summary>
    public enum DownloadJobStatus
    {
        Created,    // 任务被创建
        Ready,      // 任务就绪
        Running,    // 任务正在进行
        Pause,      // 任务暂停
        Cancel,     // 任务取消
        Finished,   // 任务完成
        Failed      // 任务出现错误
    }
    //          +<=====================================+
    // Created =+=> Ready => Running =+=> Finished    =+
    //                                +=> Failed      =+
    //                                +=> Cancel      =+
    //                                +=> Pause       =+
}