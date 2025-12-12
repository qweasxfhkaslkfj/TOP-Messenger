using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

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
        private readonly object _logFileLock = new object();
        private int _clientCounter = 0;
        private Thread _listenThread;
        private bool _isRunning = false;
        private string _messageLogFile;

        public event Action<string> MessageLogged;
        public bool IsRunning => _isRunning;

        // Конструктор - инициализируем путь к файлу истории
        public ChatServer()
        {
            // Файл истории будет находиться на компьютере сервера
            _messageLogFile = GetServerHistoryFilePath();
        }

        // Метод для получения пути к файлу истории на сервере
        private string GetServerHistoryFilePath()
        {
            try
            {
                string appDirectory = Application.StartupPath;
                string dataDirectory = Path.Combine(appDirectory, "ChatData");

                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }

                return Path.Combine(dataDirectory, "chat_history.txt");
            }
            catch (Exception)
            {
                return "chat_history.txt"; // путь по умолчанию
            }
        }

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

                // Создаем файл лога только на сервере
                InitializeLogFile();

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

        private void InitializeLogFile()
        {
            lock (_logFileLock)
            {
                try
                {
                    if (!File.Exists(_messageLogFile))
                    {
                        using (StreamWriter sw = File.CreateText(_messageLogFile))
                        {
                            sw.WriteLine($"=== Начало лога чата {DateTime.Now} ===");
                            sw.WriteLine($"Сервер запущен");
                            sw.WriteLine();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Ошибка создания файла лога: {ex.Message}");
                }
            }
        }

        // Метод для сохранения сообщения в файл истории на сервере
        public void SaveMessageToServerLog(string message, string sender = null)
        {
            lock (_logFileLock)
            {
                try
                {
                    if (!File.Exists(_messageLogFile))
                    {
                        InitializeLogFile();
                    }

                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    string messageToSave;

                    if (string.IsNullOrEmpty(sender))
                    {
                        messageToSave = $"[{timestamp}] {message}";
                    }
                    else
                    {
                        messageToSave = $"[{timestamp}] [{sender}] {message}";
                    }

                    using (StreamWriter sw = File.AppendText(_messageLogFile))
                    {
                        sw.WriteLine(messageToSave);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Ошибка сохранения сообщения в файл сервера: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;

            try
            {
                _server?.Stop();
                SaveMessageToServerLog("Сервер остановлен", "Система");
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
                        clientColor = GetRandomDarkBrightColor();
                    }
                    else
                    {
                        clientColor = GetUserColorByName(currentClientName);
                    }

                    clientConn.ClientColor = clientColor;
                }

                // Отправляем цвет пользователю
                clientConn.Writer.WriteLine($"YOUR_COLOR:{clientColor.ToArgb()}");
                clientConn.Writer.Flush();

                // Отправляем всем сообщение о подключении нового пользователя
                string connectMessage = $"{currentClientName} подключился к чату";
                BroadcastColoredMessage(connectMessage, clientColor);

                // Сохраняем в историю на сервере
                SaveMessageToServerLog(connectMessage, "Система");

                // Отправляем историю сообщений новому пользователю
                SendChatHistoryToClient(clientConn);

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
                        // Сохраняем сообщение в истории на сервере
                        SaveMessageToServerLog(message, currentClientName);

                        // Отправляем сообщение всем остальным клиентам
                        string coloredMessage = $"[{currentClientName}]: {message}";
                        BroadcastColoredMessage(coloredMessage, clientColor, clientConn);
                    }
                }

                // Сохраняем отключение в истории на сервере
                SaveMessageToServerLog($"Пользователь {currentClientName} отключился", "Система");
            }
            catch (IOException)
            {
                LogMessage($"[{currentClientName}] неожиданно отключился.");
                SaveMessageToServerLog($"Неожиданное отключение: {currentClientName}", "Система");
            }
            catch (ObjectDisposedException)
            {
                LogMessage($"[{currentClientName}] отключился (ресурсы освобождены).");
                SaveMessageToServerLog($"Отключение: {currentClientName}", "Система");
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
                    SaveMessageToServerLog(disconnectMessage, "Система");
                }

                clientConn.Close();
                LogMessage($"[{currentClientName}] отключен.");
            }
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

        // Отправка истории чата новому клиенту (с сервера)
        private void SendChatHistoryToClient(ClientConnection clientConn)
        {
            try
            {
                if (File.Exists(_messageLogFile))
                {
                    string[] lines = File.ReadAllLines(_messageLogFile);
                    int startIndex = Math.Max(0, lines.Length - 50); // Последние 50 сообщений

                    for (int i = startIndex; i < lines.Length; i++)
                    {
                        // Пропускаем системные сообщения о сессиях
                        if (!lines[i].Contains("=== Сессия") && !lines[i].Contains("=== Начало лога"))
                        {
                            clientConn.Writer.WriteLine($"HISTORY:{lines[i]}");
                        }
                    }
                    clientConn.Writer.Flush();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка отправки истории: {ex.Message}");
            }
        }

        // Добавление отсутствующих методов для рассылки сообщений
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

        private void LogMessage(string message)
        {
            MessageLogged?.Invoke(message);
        }

        // Метод для получения пути к файлу лога сервера
        public string GetLogFilePath()
        {
            return _messageLogFile;
        }

        // Метод для очистки лога на сервере
        public void ClearServerLog()
        {
            lock (_logFileLock)
            {
                try
                {
                    File.Delete(_messageLogFile);
                    InitializeLogFile();
                    LogMessage("Лог сообщений на сервере очищен");
                }
                catch (Exception ex)
                {
                    LogMessage($"Ошибка очистки лога на сервере: {ex.Message}");
                }
            }
        }
    }
}