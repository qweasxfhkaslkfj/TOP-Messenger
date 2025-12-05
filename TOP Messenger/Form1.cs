using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TOP_Messenger
{
    public partial class FormClient : Form
    {
        private ChatServer chatServer;
        private TcpClient tcpClient;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread receiveThread;
        private bool isConnected = false;
        private string serverIP = "192.168.88.128";
        private int serverPort = 8888;

        public FormClient()
        {
            InitializeComponent();
            SetupInterfaceByRole();

            // Проверка на сервер
            if (Registration.IsCurrentUserServer())
            {
                chatServer = new ChatServer();
                chatServer.MessageLogged += (message) =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action<string>(msg => AddServerLog(msg)), message);
                    }
                    else
                    {
                        AddServerLog(message);
                    }
                };
                chatServer.MessageReceived += (message) =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action<string>(msg => AddChatMessage(msg)), message);
                    }
                    else
                    {
                        AddChatMessage(message);
                    }
                };

                // Сервер запускается на указанном IP
                string localIP = "192.168.88.128";
                serverIP = localIP;
                chatServer.Start(localIP, serverPort);
                AddServerLog($"Сервер запущен на {localIP}:{serverPort}");

                ConnectToServer();
            }
        }

        private void AddServerLog(string message)
        {
            listBoxChat.TopIndex = listBoxChat.Items.Count - 1;
        }

        private void AddChatMessage(string message)
        {
            listBoxChat.Items.Add(message);
            listBoxChat.TopIndex = listBoxChat.Items.Count - 1;
        }

        private void AddOwnMessage(string message)
        {
            string ownMessage = $"[Вы]: {message}";
            listBoxChat.Items.Add(ownMessage);
            listBoxChat.TopIndex = listBoxChat.Items.Count - 1;
        }

        public void AddFileToHistory(string fileName)
        {
            FlowLayoutPanel filePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 5)
            };

            Label fileLabel = new Label
            {
                Text = fileName,
                AutoSize = true,
                Margin = new Padding(5, 0, 5, 0),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            Button downloadButton = new Button
            {
                Text = "Скачать",
                Tag = fileName,
                Margin = new Padding(5, 0, 5, 0),
                Padding = new Padding(5)
            };

            downloadButton.Click += (sender, e) =>
            {
                string fileToDownload = (string)((Button)sender).Tag;
                MessageBox.Show($"Скачивание файла: {fileToDownload}");
            };

            filePanel.Controls.Add(fileLabel);
            filePanel.Controls.Add(downloadButton);

            panelHistoryFiles.Controls.Add(filePanel);
        }

        private void SetupInterfaceByRole()
        {
            bool isServer = Registration.IsCurrentUserServer();
            bool isGuest = Registration.IsCurrentUserGuest();
            string login = Registration.GetCurrentLogin();

            buttonPlayGame.Enabled = !isGuest && !isServer;
            buttonFile.Enabled = !isGuest;

            // Проверка на сервер или гостя или логин
            btnUserAnanas.Enabled = !isServer && !isGuest && login != "ananas";
            btnUserCat_Noir.Enabled = !isServer && !isGuest && login != "cat_noir";
            btnUserDenden.Enabled = !isServer && !isGuest && login != "denden";
            btnUserKrs.Enabled = !isServer && !isGuest && login != "krs333";
            btnUserLady_Bug.Enabled = !isServer && !isGuest && login != "lady_bug";
            btnUserLushPush.Enabled = !isServer && !isGuest && login != "lushPush";
            btnUserPagan.Enabled = !isServer && !isGuest && login != "Pagan821";
            btnUserSiles.Enabled = !isServer && !isGuest && login != "Siles";
            btnUserTabeer.Enabled = !isServer && !isGuest && login != "tabeer";
            btnUserVld.Enabled = !isServer && !isGuest && login != "vld666";
            buttonUserUSF.Enabled = !isServer && !isGuest && login != "USF055";
        }

        private void ConnectToServer()
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(serverIP, serverPort);

                NetworkStream stream = tcpClient.GetStream();
                reader = new StreamReader(stream, Encoding.Unicode);
                writer = new StreamWriter(stream, Encoding.Unicode) { AutoFlush = true };

                // Отправляем имя пользователя серверу
                writer.WriteLine(Registration.GetCurrentLogin());

                isConnected = true;

                // Запускаем поток для получения сообщений
                receiveThread = new Thread(new ThreadStart(ReceiveMessages));
                receiveThread.IsBackground = true;
                receiveThread.Start();

                AddChatMessage("Подключено к серверу чата");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
                isConnected = false;
            }
        }

        private void ReceiveMessages()
        {
            try
            {
                while (isConnected)
                {
                    string message = reader.ReadLine();
                    if (message == null) break;

                    if (InvokeRequired)
                    {
                        Invoke(new Action<string>(AddChatMessage), message);
                    }
                    else
                    {
                        AddChatMessage(message);
                    }
                }
            }
            catch (IOException)
            {
                if (isConnected)
                {
                    Invoke(new Action(() => AddChatMessage("Соединение с сервером потеряно")));
                }
            }
            catch (Exception ex)
            {
                if (isConnected)
                {
                    Invoke(new Action(() => AddChatMessage($"Ошибка получения: {ex.Message}")));
                }
            }
            finally
            {
                isConnected = false;
            }
        }

        private void SendMessageToServer()
        {
            if (!isConnected || writer == null)
            {
                MessageBox.Show("Нет подключения к серверу", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string message = textBoxMessage.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            try
            {
                // Добавляем сообщение локально (клиент видит его сразу)
                AddOwnMessage(message);

                // Отправляем на сервер для рассылки другим клиентам
                writer.WriteLine(message);

                textBoxMessage.Clear();
                textBoxMessage.Focus();
            }
            catch (Exception ex)
            {
                AddChatMessage($"Ошибка отправки: {ex.Message}");
                isConnected = false;
            }
        }

        private void FormClient_Load(object sender, EventArgs e)
        {
            if (!Registration.IsCurrentUserServer())
            {
                ConnectToServer();
            }
        }

        private void FormClient_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (isConnected)
            {
                try
                {
                    if (writer != null)
                        writer.WriteLine("EXIT");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }

                isConnected = false;
                reader?.Close();
                writer?.Close();
                tcpClient?.Close();
            }

            if (chatServer != null)
                chatServer.Stop();

            Application.Exit();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            SendMessageToServer();
        }

        private void textBoxMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                SendMessageToServer();
            }
        }
    }
}