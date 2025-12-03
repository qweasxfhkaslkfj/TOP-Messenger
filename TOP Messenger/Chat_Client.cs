using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TOP_Messenger
{
    internal class Chat_Client
    private TcpClient client;
    private StreamReader reader;
    private StreamWriter writer;
    private Thread receiveThread;
    private string clientName;
    private bool isConnected = false;
    public ClientForm()
    {
        InitializeComponent();
        this.FormClosing += ClientForm_FormClosing;

        buttonDisconnect.Enabled = false;
        textBoxMessage.Enabled = false;
        buttonSend.Enabled = false;
    }
    delegate void AddChatMessageDelegate(string message);
    private void AddChatMessage(string message)
    {
        if (listBoxChat.InvokeRequired)
        {
            listBoxChat.Invoke(new AddChatMessageDelegate(AddChatMessage), message);
        }
        else
        {
            listBoxChat.Items.Add(message);

            listBoxChat.TopIndex = listBoxChat.Items.Count - 1;
        }
    }
    private void ReceiveMessages()
    {
        try
        {
            string message;
            while (isConnected && (message = reader.ReadLine()) != null)
            {
                AddChatMessage(message);

                if (message.Contains("Сервер останавливается."))
                {
                    this.Invoke((MethodInvoker)delegate { Disconnect(); });
                    break;
                }
            }
        }
        catch (IOException)
        {
            AddChatMessage("Соединение с сервером потеряно.");
            this.Invoke((MethodInvoker)delegate { Disconnect(); });
        }
        catch (ObjectDisposedException)
        {
            AddChatMessage("Соединение закрыто.");
        }
        catch (Exception ex)
        {
            AddChatMessage($"Ошибка приема сообщения: {ex.Message}");
            this.Invoke((MethodInvoker)delegate { Disconnect(); });
        }
    }
    private void Disconnect()
    {
        if (!isConnected) return;

        try
        {
            if (writer != null)
            {
                writer.WriteLine("EXIT");
            }
        }
        catch (Exception ex)
        {
            AddChatMessage($"Ошибка при отправке EXIT: {ex.Message}");
        }
        finally
        {
            try { reader?.Close(); } catch { }
            try { writer?.Close(); } catch { }
            try { client?.Close(); } catch { }

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(100);
            }

            isConnected = false;
            AddChatMessage("Отключено от сервера.");
            UpdateUIState(false);
        }
    }
    private void SendMessage()
    {
        if (!isConnected || writer == null)
        {
            MessageBox.Show("Вы не подключены к серверу.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string message = textBoxMessage.Text.Trim();
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        try
        {
            writer.WriteLine(message);
            textBoxMessage.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка отправки сообщения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            AddChatMessage($"Ошибка отправки: {ex.Message}");
            Disconnect();
        }
    }

    private void UpdateUIState(bool connected)
    {
        if (this.InvokeRequired)
        {
            this.Invoke((MethodInvoker)delegate { UpdateUIState(connected); });
            return;
        }

        textBoxServerIP.Enabled = !connected;
        textBoxServerPort.Enabled = !connected;
        textBoxNickname.Enabled = !connected;
        buttonConnect.Enabled = !connected;

        buttonDisconnect.Enabled = connected;
        textBoxMessage.Enabled = connected;
        buttonSend.Enabled = connected;
    }
}