namespace PipeListening
{
    using System;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {
        private void InitializeComponent()
        {
            this.TextBox1 = new TextBox
            {
                Left = 8,
                Top = 8,
                Width = 200,
            };

            this.Button1 = new Button
            {
                Text = "Send",
                Left = this.TextBox1.Left,
                Top = this.TextBox1.Top + this.TextBox1.Height + 8,
                Width = 100,
            };

            this.Text = Application.ProductName;
            this.Controls.AddRange(new Control[] { this.TextBox1, this.Button1 });
        }
    }
}