using System;
using System.Threading;
using System.Windows.Forms;

namespace waninput2
{
    public partial class ServerForm : Form
    {
        private Exception error;

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

                //start thread so form can still be interacted with
                Thread th = new Thread(new ThreadStart(delegate () {
                    Server.Run(int.Parse(portText.Text), int.Parse(widthText.Text), int.Parse(heightText.Text), textLog);
                }));
                th.Start();
            }
            catch (Exception er)
            {
                error = er;
                warningText.Text = er.Message + " (click for details)";
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

        private void warningText_Click(object sender, EventArgs e)
        {
            if (error != null)
            {
                WarningDialog wd = new WarningDialog(error);
                wd.ShowDialog();
            }
        }
    }
}
