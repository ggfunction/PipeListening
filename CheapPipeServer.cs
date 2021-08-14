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

        public CheapPipeServer()
            : this(string.Empty)
        {
        }

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

        public event EventHandler<RecievedEventArgs> Recieved;

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

            if (this.mutex != null)
            {
                try
                {
                    this.mutex.ReleaseMutex();
                }
                catch
                {
                }

                this.mutex.Close();
            }
        }

        public void Start()
        {
            this.waitToListen.Reset();
        }

        public void Stop()
        {
            this.waitToListen.Set();
        }

        protected virtual void OnRecieved(RecievedEventArgs e)
        {
            if (this.Recieved != null)
            {
                this.Recieved(this, e);
            }
        }

        private WaitHandle BeginWaitForConnection()
        {
            var serverStream = default(NamedPipeServerStream);
            var ar = default(IAsyncResult);

            try
            {
                serverStream = new NamedPipeServerStream(this.Name, PipeDirection.InOut, -1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                ar = serverStream.BeginWaitForConnection(this.ConnectCallback, serverStream);
                return ar.AsyncWaitHandle;
            }
            catch
            {
                if (ar != null)
                {
                    serverStream.EndWaitForConnection(ar);
                }

                if (serverStream != null)
                {
                    serverStream.Dispose();
                }
            }

            return new EventWaitHandle(true, EventResetMode.AutoReset);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                var serverStream = ar.AsyncState as NamedPipeServerStream;

                using (var eventArgs = new RecievedEventArgs())
                {
                    serverStream.EndWaitForConnection(ar);

                    const int BufferSize = 1024 * 64;
                    var bytes = new byte[BufferSize];
                    var count = 0;

                    do
                    {
                        count = serverStream.Read(bytes, 0, bytes.Length);
                        eventArgs.Content.Write(bytes, 0, count);
                    }
                    while (count == bytes.Length);

                    eventArgs.Content.Seek(0, 0);

                    this.OnRecieved(eventArgs);
                }
            }
            catch
            {
            }
        }

        private void WaitCallback(object data)
        {
            var waitHandles = new WaitHandle[]
            {
                this.waitToQuit,
                this.waitToListen,
            };

            var connections = new HashSet<WaitHandle>();

            for (var i = 0; i < this.ConcurrentRequests; i++)
            {
                connections.Add(this.BeginWaitForConnection());
            }

            while (true)
            {
                var waitHandlesArray = waitHandles.Concat(connections).ToArray();
                var index = WaitHandle.WaitAny(waitHandlesArray);

                if (index == Array.IndexOf(waitHandlesArray, this.waitToQuit))
                {
                    break;
                }

                if (index == Array.IndexOf(waitHandlesArray, this.waitToListen))
                {
                    continue;
                }

                connections.Remove(waitHandlesArray[index]);

                for (var i = connections.Count; i < this.ConcurrentRequests; i++)
                {
                    connections.Add(this.BeginWaitForConnection());
                }
            }
        }
    }
}