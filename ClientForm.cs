using System;
using System.Net;
using System.Windows.Forms;

namespace waninput2
{
    public partial class ClientForm : Form
    {
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
                using ClientWindow c = new ClientWindow(1280, 720, "Remote Play Client", 60f, new IPEndPoint(IPAddress.Parse(ipText.Text), int.Parse(portText.Text)));
                c.Run();
            }
            catch (FormatException)
            {
                warningText.Show();
            }
        }
    }
}
