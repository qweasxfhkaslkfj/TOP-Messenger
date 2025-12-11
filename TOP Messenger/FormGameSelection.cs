using System;
using System.Windows.Forms;

namespace TOP_Messenger
{
    public partial class FormGameSelection : Form
    {
        public FormGameSelection()
        {
            InitializeComponent();
        }

        private void btnTicTacToe_Click(object sender, EventArgs e)
        {
            FormTicTacToe formTicTacToe = new FormTicTacToe();
            formTicTacToe.ShowDialog();
            this.Close();
        }
    }
}
