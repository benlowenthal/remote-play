using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace waninput2
{
    public partial class ClientForm : Form
    {
        private Exception error;

        public ClientForm()
        {
            InitializeComponent();
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            warningText.Hide();
        }

        private void accept_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress.Parse(ipText.Text);
                int.Parse(portText.Text);

                Hide();
                ClientWindow c = new ClientWindow(1280, 720, "Remote Play Client", 60f, new IPEndPoint(IPAddress.Parse(ipText.Text), int.Parse(portText.Text)));
                c.Run();
                c.Dispose();
                Show();
            }
            catch (Exception er)
            {
                error = er;
                warningText.Text = er.Message + " (click for details)";
                warningText.Show();
                Show();
            }
        }

        private void warningText_Click(object sender, EventArgs e)
        {
            WarningDialog wd = new WarningDialog(error);
            wd.ShowDialog();
        }
    }
}
