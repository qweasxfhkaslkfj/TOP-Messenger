using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TOP_Messenger
{
    public class ClientConnection
    {
        public TcpClient Client { get; private set; }
        public NetworkStream Stream { get; set; }
        public StreamWriter Writer { get; private set; }
        public StreamReader Reader { get; private set; }
        public string ClientName { get; set; }
        public Color ClientColor { get; set; }

        public ClientConnection(TcpClient client, string tempId)
        {
            Client = client;
            ClientName = tempId;
            NetworkStream stream = client.GetStream();
            Reader = new StreamReader(stream, Encoding.Unicode);
            Writer = new StreamWriter(stream, Encoding.Unicode) { AutoFlush = true };
        }
        public void Close()
        {
            try { Reader?.Close(); } catch { }
            try { Writer?.Close(); } catch { }
            try { Client?.Close(); } catch { }
        }
    }

    public class ChatServer
    {
        private TcpListener _server;
        private List<ClientConnection> _connectedClients = new List<ClientConnection>();
        private readonly object _clientsLock = new object();
        private int _clientCounter = 0;
        private Thread _listenThread;
        private bool _isRunning = false;

        public event Action<string> MessageLogged;
        public bool IsRunning => _isRunning;

        public void Start(string ipAddress, int port)
        {
            if (_isRunning)
            {
                LogMessage("Сервер уже запущен.");
                return;
            }

            try
            {
                IPAddress ip = IPAddress.Parse(ipAddress);
                _server = new TcpListener(ip, port);
                _server.Start();
                _isRunning = true;

                LogMessage($"Сервер запущен на {ipAddress}:{port}");
                LogMessage("Ожидание подключений...");

                _listenThread = new Thread(new ThreadStart(ListenForClients));
                _listenThread.IsBackground = true;
                _listenThread.Start();
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка запуска сервера: {ex.Message}");
                _isRunning = false;
            }
        }
        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;

            try
            {
                _server?.Stop();
                LogMessage("Сервер остановлен.");
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при остановке сервера: {ex.Message}");
            }

            lock (_clientsLock)
            {
                foreach (var client in _connectedClients)
                {
                    try
                    {
                        client.Writer.WriteLine("Сервер останавливается. Вы были отключены.");
                        client.Close();
                    }
                    catch { }
                }
                _connectedClients.Clear();
            }
        }

        private void ListenForClients()
        {
            try
            {
                while (_isRunning)
                {
                    TcpClient client = _server.AcceptTcpClient();
                    Interlocked.Increment(ref _clientCounter);
                    string tempClientId = $"Guest#{_clientCounter}";
                    LogMessage($"Подключен новый клиент (временный ID: {tempClientId})");

                    ClientConnection clientConn = new ClientConnection(client, tempClientId);

                    lock (_clientsLock)
                    {
                        _connectedClients.Add(clientConn);
                    }

                    Thread clientThread = new Thread(() => HandleClient(clientConn));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    LogMessage("Сервер прекратил прослушивание новых подключений.");
                }
                else if (_isRunning)
                {
                    LogMessage($"Ошибка прослушивания клиентов: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    LogMessage($"Общая ошибка в ListenForClients: {ex.Message}");
                }
            }
        }

        private void HandleClient(ClientConnection clientConn)
        {
            string currentClientName = clientConn.ClientName;
            Color clientColor = Color.Black;

            try
            {
                // Получаем логин пользователя от клиента
                string initialMessage = clientConn.Reader.ReadLine();
                if (!string.IsNullOrEmpty(initialMessage))
                {
                    currentClientName = initialMessage.Trim();
                    clientConn.ClientName = currentClientName;

                    // Определяем цвет для пользователя
                    if (currentClientName.StartsWith("Guest#"))
                    {
                        // Для гостей - случайный ярко-темный цвет
                        clientColor = GetRandomDarkBrightColor();
                    }
                    else
                    {
                        // Для обычных пользователей - цвет на основе хэша имени
                        clientColor = GetUserColorByName(currentClientName);
                    }

                    clientConn.ClientColor = clientColor;

                    LogMessage($"Клиент {currentClientName} зарегистрировал имя. Цвет: {clientColor.Name}");
                }

                // Отправляем цвет пользователю
                clientConn.Writer.WriteLine($"YOUR_COLOR:{clientColor.ToArgb()}");
                clientConn.Writer.Flush();

                // Отправляем всем сообщение о подключении нового пользователя
                string connectMessage = $"{currentClientName} подключился к чату";
                BroadcastColoredMessage(connectMessage, clientColor);

                // Основной цикл обработки сообщений от клиента
                string message;
                while (_isRunning && (message = clientConn.Reader.ReadLine()) != null)
                {
                    message = message.Trim();

                    if (message.ToUpper() == "EXIT")
                    {
                        LogMessage($"[{currentClientName}] отключается по команде.");
                        break;
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        // Отправляем сообщение всем остальным клиентам
                        string coloredMessage = $"[{currentClientName}]: {message}";
                        BroadcastColoredMessage(coloredMessage, clientColor, clientConn);
                    }
                }
            }
            catch (IOException)
            {
                LogMessage($"[{currentClientName}] неожиданно отключился.");
            }
            catch (ObjectDisposedException)
            {
                LogMessage($"[{currentClientName}] отключился (ресурсы освобождены).");
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка обработки клиента {currentClientName}: {ex.Message}");
            }
            finally
            {
                lock (_clientsLock)
                {
                    _connectedClients.Remove(clientConn);
                }

                // Отправляем сообщение об отключении
                if (!string.IsNullOrEmpty(currentClientName))
                {
                    string disconnectMessage = $"{currentClientName} покинул чат";
                    BroadcastColoredMessage(disconnectMessage, clientColor);
                }

                clientConn.Close();
                LogMessage($"[{currentClientName}] отключен.");
            }
        }

        private void BroadcastColoredMessage(string message, Color color)
        {
            string formattedMessage = $"COLOR:{color.ToArgb()}|{message}";
            BroadcastMessage(formattedMessage);
        }

        private void BroadcastColoredMessage(string message, Color color, ClientConnection excludeClient)
        {
            string formattedMessage = $"COLOR:{color.ToArgb()}|{message}";
            BroadcastMessage(formattedMessage, excludeClient);
        }

        private void BroadcastMessage(string message)
        {
            BroadcastMessage(message, null);
        }

        private void BroadcastMessage(string message, ClientConnection excludeClient)
        {
            lock (_clientsLock)
            {
                foreach (var client in _connectedClients)
                {
                    // Пропускаем исключенного клиента, если указан
                    if (excludeClient != null && client == excludeClient)
                        continue;

                    try
                    {
                        client.Writer.WriteLine(message);
                        client.Writer.Flush();
                    }
                    catch
                    {
                        // Игнорируем ошибки отправки
                    }
                }
            }
        }

        private Color GetUserColorByName(string userName)
        {
            int hash = Math.Abs(userName.GetHashCode());

            Color[] darkBrightColors = new Color[]
            {
                Color.DarkRed,
                Color.DarkBlue,
                Color.DarkGreen,
                Color.DarkMagenta,
                Color.DarkCyan,
                Color.DarkOrange,
                Color.DarkViolet,
                Color.DarkSlateBlue,
                Color.MidnightBlue,
                Color.Maroon,
                Color.Purple,
                Color.Teal,
                Color.Navy,
                Color.OliveDrab,
                Color.SaddleBrown,
                Color.DarkSlateGray
            };

            int colorIndex = hash % darkBrightColors.Length;
            return darkBrightColors[colorIndex];
        }

        private Color GetRandomDarkBrightColor()
        {
            Random random = new Random(Guid.NewGuid().GetHashCode());

            Color[] guestColors = new Color[]
            {
                Color.DarkRed,
                Color.DarkBlue,
                Color.DarkGreen,
                Color.DarkMagenta,
                Color.DarkCyan,
                Color.DarkOrange,
                Color.DarkViolet,
                Color.DarkSlateBlue,
                Color.MidnightBlue,
                Color.Maroon,
                Color.Purple,
                Color.Teal,
                Color.Navy,
                Color.OliveDrab,
                Color.SaddleBrown,
                Color.DarkSlateGray
            };

            return guestColors[random.Next(guestColors.Length)];
        }

        private void LogMessage(string message)
        {
            MessageLogged?.Invoke(message);
        }
    }
}