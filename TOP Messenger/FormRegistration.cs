using System;
using System.Windows.Forms;

namespace TOP_Messenger
{
    public partial class FormRegistration : Form
    {
        private Registration registration;

        public FormRegistration()
        {
            InitializeComponent();
            registration = new Registration();
        }

        private void buttonZahodGosta_Click(object sender, EventArgs e)
        {
            textBoxPassword.Text = "";
            textBoxPassword.Enabled = false;
            textBoxLogin.Focus();
        }

        private void buttonVhod_Click(object sender, EventArgs e)
        {
            Enter();
        }

        private void Enter()
        {
            if (!textBoxPassword.Enabled)
            {
                var result = registration.CheckGuestLogin(textBoxLogin.Text);

                if (result.IsValid)
                {
                    FormClient formClient = new FormClient();
                    formClient.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Error!");
                }
            }
            else
            {
                var result = registration.CheckLoginAndPassword(
                    textBoxLogin.Text,
                    textBoxPassword.Text);


                if (result.IsValid)
                {
                    FormClient formClient = new FormClient();
                    formClient.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Error!");
                }
            }
        }

        private void textBoxLogin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                if (!textBoxPassword.Enabled)
                {
                    Enter();
                    e.SuppressKeyPress = true;
                }
                else
                {
                    textBoxPassword.Focus();
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void textBoxPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                Enter();
                e.SuppressKeyPress = true;
            }
        }
    }
}
