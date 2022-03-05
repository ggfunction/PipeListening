namespace PipeListening
{
    using System;
    using System.Collections.Generic;
    using System.IO.Pipes;
    using System.Linq;
    using System.Threading;

    public class CheapPipeServer : IDisposable
    {
        private readonly object lockObject = new object();

        private readonly Semaphore semaphore;

        private readonly List<NamedPipeServerStream> streams;

        private readonly EventWaitHandle listeningEvent;

        private readonly EventWaitHandle stopEvent;

        private int concurrentRequests;

        public CheapPipeServer()
            : this(string.Empty)
        {
        }

        public CheapPipeServer(string name)
        {
            this.Name = string.IsNullOrEmpty(name) ?
                Guid.NewGuid().ToString() : name;

            this.semaphore = new Semaphore(1, 1, this.Name);
            this.streams = new List<NamedPipeServerStream>();

            this.Priority = this.semaphore.WaitOne(0) ?
                Priority.High : Priority.None;

            this.listeningEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
            this.stopEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

            this.ConcurrentRequests = Environment.ProcessorCount;
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler PriorityChanged;

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

        public Priority Priority { get; private set; }

        public bool IgnorePriority { get; set; }

        public bool IsListening
        {
            get { return this.listeningEvent.WaitOne(0); }
        }

        public string Name { get; private set; }

        public void Close()
        {
            this.Stop();
            this.stopEvent.Set();
            this.semaphore.Close();
        }

        public void Dispose()
        {
            this.Close();
        }

        public void Start()
        {
            if (!this.IsListening)
            {
                this.listeningEvent.Set();
                ThreadPool.QueueUserWorkItem(this.WaitCallback);
            }
        }

        public void Stop()
        {
            if (!this.IsListening)
            {
                return;
            }

            this.stopEvent.Set();
        }

        protected virtual void OnMessageReceived(MessageReceivedEventArgs e)
        {
            if (this.MessageReceived != null)
            {
                this.MessageReceived(this, e);
            }
        }

        protected virtual void OnPriorityChanged(EventArgs e)
        {
            if (this.PriorityChanged != null)
            {
                this.PriorityChanged(this, e);
            }
        }

        private WaitHandle BeginWaitForConnection()
        {
            var ar = default(IAsyncResult);
            var ss = default(NamedPipeServerStream);

            try
            {
                if (!this.IgnorePriority && this.Priority == Priority.None)
                {
                    throw new InvalidOperationException(Priority.None.ToString());
                }

                ss = new NamedPipeServerStream(
                    this.Name,
                    PipeDirection.InOut,
                    -1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                ar = ss.BeginWaitForConnection(
                    this.ConnectCallback,
                    ss);

                this.streams.Add(ss);

                return ar.AsyncWaitHandle;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return Memorandum.Threading.Delay.Signal(100);
        }

        private WaitHandle BeginWaitOne()
        {
            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

            ThreadPool.QueueUserWorkItem(
                state =>
            {
                if (this.Priority == Priority.None)
                {
                    if (this.semaphore.WaitOne())
                    {
                        this.Priority = Priority.High;
                        this.OnPriorityChanged(EventArgs.Empty);
                    }
                }

                ((EventWaitHandle)state).Set();
            },
                waitHandle);

            return waitHandle;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            var ss = ar.AsyncState as NamedPipeServerStream;
            var e = default(MessageReceivedEventArgs);

            try
            {
                ss.EndWaitForConnection(ar);
                e = new MessageReceivedEventArgs(ss);
                this.OnMessageReceived(e);
            }
            catch (System.IO.IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                if (e != null)
                {
                    e.Dispose();
                }

                this.streams.Remove(ss);
                ss.Dispose();
            }
        }

        private void WaitCallback(object state)
        {
            var requests = new HashSet<WaitHandle>
            {
                this.Priority == Priority.High ?
                    this.BeginWaitForConnection() :
                    this.BeginWaitOne(),
            };

            while (true)
            {
                if (this.Priority == Priority.High)
                {
                    for (var i = requests.Count; i < this.ConcurrentRequests; i++)
                    {
                        requests.Add(this.BeginWaitForConnection());
                    }
                }

                var waitHandles = requests.Prepend(this.stopEvent).ToArray();
                var index = WaitHandle.WaitTimeout;
                bool exit;

                try
                {
                    index = WaitHandle.WaitAny(waitHandles);
                    exit = index == Array.IndexOf(waitHandles, this.stopEvent);
                }
                catch (Exception ex)
                {
                    exit = true;
                    Console.WriteLine(ex);
                }

                if (exit)
                {
                    break;
                }

                try
                {
                    var waitHandle = waitHandles[index];
                    requests.Remove(waitHandle);
                    waitHandle.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            this.streams.ForEach(x => x.Dispose());
            this.streams.Clear();

            if (this.Priority == Priority.High)
            {
                this.semaphore.Release();
                this.Priority = Priority.None;
            }

            this.listeningEvent.Reset();
        }
    }
}