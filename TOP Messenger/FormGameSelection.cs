using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
