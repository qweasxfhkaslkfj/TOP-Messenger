using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace public_chat_server
{
    public partial class Form1 : Form
    {
        private ChatServer _chatServer;
        delegate void AddListItemDelegate(string item);

        public Form1()
        {
            InitializeComponent();
            _chatServer = new ChatServer();
            _chatServer.MessageLogged += AddListItem;
            this.FormClosing += Form1_FormClosing;
        }

        private void AddListItem(string item)
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new AddListItemDelegate(AddListItem), item);
            }
            else
            {
                listBox1.Items.Add(item);
                listBox1.TopIndex = listBox1.Items.Count - 1;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
         
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(textBox1.Text);
                int port = Convert.ToInt32(textBox2.Text);

                _chatServer.Start(ipAddress, port);

                button1.Enabled = false;
            }
            catch (SocketException se)
            {
                MessageBox.Show($"Ошибка сокета: {se.Message}");
                AddListItem($"Ошибка сокета при запуске: {se.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Общая ошибка при запуске сервера: {ex.Message}");
                AddListItem($"Ошибка при запуске сервера: {ex.Message}");
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e) { }

        private void textBox1_TextChanged(object sender, EventArgs e) { }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) { }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _chatServer.Stop();
            button1.Enabled = true;
        }
    }
}