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
                    if (textBoxLogin.Text.Length >= 15)
                        MessageBox.Show("Логин не может быть длинее 15 символов");
                    else
                        MessageBox.Show("Ошибка входа!");
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
            if (e.KeyCode == Keys.Enter)
            {
                if (!textBoxPassword.Enabled)
                {
                    Enter();
                    buttonVhod.Focus();
                }
                else
                {
                    textBoxPassword.Focus();
                }
            }
        }

        private void textBoxPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Enter();
                e.SuppressKeyPress = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBoxPassword.PasswordChar == '\0')
                textBoxPassword.PasswordChar = '*';
            else
                textBoxPassword.PasswordChar = '\0';
        }

        private void textBoxPassword_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Enter();
                buttonVhod.Focus();
            }
        }
    }
}
