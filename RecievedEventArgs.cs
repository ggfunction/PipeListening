namespace PipeListening
{
    using System;
    using System.IO;

    public class RecievedEventArgs : EventArgs, IDisposable
    {
        public RecievedEventArgs()
        {
            this.Content = new MemoryStream();
            this.Reader = new StreamReader(this.Content);
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
    }
}