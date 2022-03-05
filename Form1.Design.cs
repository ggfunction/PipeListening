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

            this.Button2 = new Button
            {
                Text = "Start",
                Left = this.TextBox1.Left,
                Top = this.Button1.Top + this.Button1.Height + 8,
                Width = 100,
            };

            this.Button3 = new Button
            {
                Text = "Stop",
                Left = this.Button2.Left + this.Button2.Width + 8,
                Top = this.Button2.Top,
                Width = 100,
            };

            this.Text = Application.ProductName;
            this.Controls.AddRange(new Control[] { this.TextBox1, this.Button1, this.Button2, this.Button3 });
        }
    }
}