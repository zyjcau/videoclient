using System;
using System.Threading;

namespace VideoClient.VidyoClient.Ext
{
    public abstract class AsyncResultExt : IAsyncResult
    {
        public bool IsCompleted { get; }
        public WaitHandle AsyncWaitHandle { get; }
        public object AsyncState { get; }
        public bool CompletedSynchronously { get; }
    }
}