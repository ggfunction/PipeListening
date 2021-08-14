namespace PipeListening
{
    using System;
    using System.Collections.Generic;
    using System.IO.Pipes;
    using System.Linq;
    using System.Threading;

    public class CheapPipeServer : IDisposable
    {
        private readonly Mutex mutex;

        private readonly EventWaitHandle waitToListen;

        private readonly EventWaitHandle waitToQuit;

        private int concurrentRequests;

        public CheapPipeServer(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = Guid.NewGuid().ToString();
            }

            this.Name = name;

            bool createNew;
            this.mutex = new Mutex(true, name, out createNew);

            this.waitToListen = new EventWaitHandle(true, EventResetMode.ManualReset);
            this.waitToQuit = new EventWaitHandle(false, EventResetMode.AutoReset);

            this.ConcurrentRequests = Environment.ProcessorCount;

            ThreadPool.QueueUserWorkItem(this.WaitCallback, null);
        }

        public int ConcurrentRequests
        {
            get
            {
                return this.concurrentRequests;
            }

            set
            {
                this.concurrentRequests = Math.Min(Math.Max(1, value), Environment.ProcessorCount);
            }
        }

        public bool IsListening
        {
            get { return !this.waitToListen.WaitOne(0); }
        }

        public string Name { get; private set; }

        public void Close()
        {
            this.Stop();
            this.waitToQuit.Set();
        }

        public void Dispose()
        {
            this.Close();
        }

        public void Start()
        {
            this.waitToListen.Reset();
        }

        public void Stop()
        {
            this.waitToListen.Set();
        }

        private WaitHandle BeginWaitForConnection()
        {
            throw new NotImplementedException();
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            throw new NotImplementedException();
        }

        private void WaitCallback(object data)
        {
            var waitHandles = new WaitHandle[]
            {
                this.waitToQuit,
                this.waitToListen,
            };
        }
    }
}