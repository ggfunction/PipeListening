namespace PipeListening
{
    using System;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {
        private const string PipeName = "Test1";
        private readonly CheapPipeServer server;

        public Form1()
        {
            this.InitializeComponent();

            this.Button1.Click += (s, e) =>
            {
                var client = new System.IO.Pipes.NamedPipeClientStream(PipeName);
                var writer = new System.IO.StreamWriter(client);
                {
                    try
                    {
                        client.Connect(100);
                        writer.WriteLine("{0} {1}", DateTime.Now, this.TextBox1.Text);
                        writer.Flush();
                        client.WaitForPipeDrain();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(". {0}", ex.Message);
                    }
                }
            };

            this.server = new CheapPipeServer(PipeName);
            this.server.Recieved += (s, e) =>
            {
                Console.Write(e.Reader.ReadToEnd());
            };
            this.server.Start();
        }

        public TextBox TextBox1 { get; private set; }

        public Button Button1 { get; private set; }
    }
}