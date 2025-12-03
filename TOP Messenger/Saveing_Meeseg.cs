using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace TOP_Messenger
{
    public partial class Form1 : Form
    {
        private TcpListener server;
        private List<ClientHandler> clients = new List<ClientHandler>();
        private Thread serverThread;
        private bool isRunning = false;
        private string userName = "User1"; // Имя текущего пользователя

      
        // Класс для обработки каждого клиента
        public class ClientHandler
        {
            public TcpClient Client { get; set; }
            public string UserName { get; set; }
            public NetworkStream Stream { get; set; }
            public StreamReader Reader { get; set; }
            public StreamWriter Writer { get; set; }
            public Thread ClientThread { get; set; }
        }

        // Запуск сервера
        private void StartServer()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, 8888);
                server.Start();
                isRunning = true;

                Invoke((MethodInvoker)delegate
                {
                    lstMessages.Items.Add($"Сервер запущен на порту 8888");
                    lstMessages.Items.Add($"Ваше имя: {userName}");
                });

                while (isRunning)
                {
                    if (server.Pending())
                    {
                        TcpClient client = server.AcceptTcpClient();
                        ClientHandler handler = new ClientHandler
                        {
                            Client = client,
                            Stream = client.GetStream(),
                            UserName = "Unknown"
                        };
                        handler.Reader = new StreamReader(handler.Stream, Encoding.UTF8);
                        handler.Writer = new StreamWriter(handler.Stream, Encoding.UTF8);

                        // Получаем имя пользователя
                        handler.UserName = handler.Reader.ReadLine();

                        Invoke((MethodInvoker)delegate
                        {
                            lstMessages.Items.Add($"{handler.UserName} подключился");
                        });

                        // Добавляем в список клиентов
                        clients.Add(handler);

                        // Запускаем поток для чтения сообщений от этого клиента
                        handler.ClientThread = new Thread(() => HandleClient(handler));
                        handler.ClientThread.Start();
                    }
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Invoke((MethodInvoker)delegate
                {
                    lstMessages.Items.Add($"Ошибка сервера: {ex.Message}");
                });
            }
        }

        // Обработка сообщений от клиента
        private void HandleClient(ClientHandler handler)
        {
            try
            {
                while (isRunning && handler.Client.Connected)
                {
                    if (handler.Stream.DataAvailable)
                    {
                        string message = handler.Reader.ReadLine();
                        if (message != null)
                        {
                            // Отображаем сообщение
                            Invoke((MethodInvoker)delegate
                            {
                                lstMessages.Items.Add($"[{handler.UserName}]: {message}");
                                lstMessages.SelectedIndex = lstMessages.Items.Count - 1;
                            });

                            // Пересылаем сообщение всем клиентам
                            BroadcastMessage($"[{handler.UserName}]: {message}");
                        }
                    }
                    Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки клиента: {ex.Message}");
            }
            finally
            {
                // Удаляем клиента при отключении
                RemoveClient(handler);
            }
        }

        // Отправка сообщения всем клиентам
        private void BroadcastMessage(string message)
        {
            lock (clients)
            {
                foreach (var client in clients.ToList())
                {
                    try
                    {
                        if (client.Client.Connected)
                        {
                            client.Writer.WriteLine(message);
                            client.Writer.Flush();
                        }
                    }
                    catch
                    {
                        RemoveClient(client);
                    }
                }
            }
        }

        // Удаление клиента
        private void RemoveClient(ClientHandler handler)
        {
            lock (clients)
            {
                if (clients.Contains(handler))
                {
                    Invoke((MethodInvoker)delegate
                    {
                        lstMessages.Items.Add($"{handler.UserName} отключился");
                    });

                    clients.Remove(handler);
                    handler.Client.Close();
                }
            }
        }

        // Подключение к другому серверу (чтобы два пользователя могли общаться)
        private void ConnectToServer(string ipAddress, string userName)
        {
            Thread connectThread = new Thread(() =>
            {
                try
                {
                    TcpClient client = new TcpClient();
                    client.Connect(ipAddress, 8888);

                    NetworkStream stream = client.GetStream();
                    StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);

                    // Отправляем имя пользователя
                    writer.WriteLine(userName);
                    writer.Flush();

                    // Добавляем в список как клиента
                    ClientHandler handler = new ClientHandler
                    {
                        Client = client,
                        Stream = stream,
                        Reader = reader,
                        Writer = writer,
                        UserName = userName
                    };

                    lock (clients)
                    {
                        clients.Add(handler);
                    }

                    Invoke((MethodInvoker)delegate
                    {
                        lstMessages.Items.Add($"Подключились к {ipAddress} как {userName}");
                    });

                    // Читаем входящие сообщения
                    while (client.Connected)
                    {
                        try
                        {
                            if (stream.DataAvailable)
                            {
                                string message = reader.ReadLine();
                                if (message != null)
                                {
                                    Invoke((MethodInvoker)delegate
                                    {
                                        lstMessages.Items.Add(message);
                                        lstMessages.SelectedIndex = lstMessages.Items.Count - 1;
                                    });
                                }
                            }
                            Thread.Sleep(50);
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Invoke((MethodInvoker)delegate
                    {
                        lstMessages.Items.Add($"Ошибка подключения: {ex.Message}");
                    });
                }
            });
            connectThread.Start();
        }

        // Отправка сообщения
        private void SendMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                string fullMessage = $"[{userName}]: {message}";

                // Отображаем свое сообщение
                lstMessages.Items.Add(fullMessage);
                lstMessages.SelectedIndex = lstMessages.Items.Count - 1;

                // Отправляем всем подключенным клиентам
                BroadcastMessage(fullMessage);

                txtMessage.Clear();
                txtMessage.Focus();
            }
        }

        // Кнопка запуска сервера
        private void btnStartServer_Click(object sender, EventArgs e)
        {
            if (!isRunning)
            {
                serverThread = new Thread(StartServer);
                serverThread.Start();
                btnStartServer.Text = "Сервер запущен";
                btnStartServer.Enabled = false;
            }
        }

        // Кнопка подключения к серверу
        private void btnConnect_Click(object sender, EventArgs e)
        {
            string ip = txtIP.Text;
            if (!string.IsNullOrWhiteSpace(ip))
            {
                ConnectToServer(ip, userName);
            }
        }

        // Кнопка отправки сообщения
        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage(txtMessage.Text);
        }

        // Отправка по Enter
        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                SendMessage(txtMessage.Text);
                e.Handled = true;
            }
        }

        // Очистка при закрытии
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isRunning = false;

            // Отключаем всех клиентов
            lock (clients)
            {
                foreach (var client in clients)
                {
                    try
                    {
                        client.Client.Close();
                    }
                    catch { }
                }
                clients.Clear();
            }

            // Останавливаем сервер
            if (server != null)
            {
                server.Stop();
            }
        }

        // Смена имени пользователя
        private void btnChangeName_Click(object sender, EventArgs e)
        {
            string newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите новое имя:", "Смена имени", userName);

            if (!string.IsNullOrWhiteSpace(newName))
            {
                userName = newName;
                this.Text = "Чат - " + userName;
                lstMessages.Items.Add($"Имя изменено на: {userName}");
            }
        }
    }
}