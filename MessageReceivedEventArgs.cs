namespace PipeListening
{
    using System;
    using System.IO;

    public class MessageReceivedEventArgs : EventArgs, IDisposable
    {
        public MessageReceivedEventArgs()
        {
            this.Content = new MemoryStream();
            this.Reader = new StreamReader(this.Content);
        }

        public MessageReceivedEventArgs(System.IO.Pipes.PipeStream stream)
            : this()
        {
            this.CopyStream(stream, this.Content);
            this.Content.Seek(0, 0);
        }

        public MemoryStream Content { get; private set; }

        public StreamReader Reader { get; private set; }

        public void Dispose()
        {
            if (this.Content != null)
            {
                this.Content.Dispose();
            }
        }

        private void CopyStream(Stream source, Stream destination)
        {
            const int BufferSize = 1024;
            var buffer = new byte[BufferSize];
            int count;
            do
            {
                count = source.Read(buffer, 0, buffer.Length);
                destination.Write(buffer, 0, count);
            }
            while (count == BufferSize);
        }
    }
}