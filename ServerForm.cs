using System;
using System.Threading;
using System.Windows.Forms;

namespace waninput2
{
    public partial class ServerForm : Form
    {
        public ServerForm()
        {
            InitializeComponent();
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {
            warningText.Hide();
            closeButton.Enabled = false;
        }

        private void accept_Click(object sender, EventArgs e)
        {
            try
            {
                int.Parse(widthText.Text);
                int.Parse(heightText.Text);
                int.Parse(portText.Text);

                warningText.Text = "Server running...";
                warningText.Show();
                closeButton.Enabled = true;
                openButton.Enabled = false;

                Thread th = new Thread(new ThreadStart(delegate () { Server.Run(int.Parse(portText.Text), int.Parse(widthText.Text), int.Parse(heightText.Text)); }));
                th.Start();
            }
            catch (FormatException)
            {
                warningText.Show();
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Server.Close();
            warningText.Hide();
            openButton.Enabled = true;
            closeButton.Enabled = false;
        }
    }
}
