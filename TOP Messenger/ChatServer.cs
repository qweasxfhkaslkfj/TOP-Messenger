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
            Stream = client.GetStream();
            Reader = new StreamReader(Stream, Encoding.Unicode);
            Writer = new StreamWriter(Stream, Encoding.Unicode) { AutoFlush = true };
        }

        public void Close()
        {
            try { Reader?.Close(); } catch { }
            try { Writer?.Close(); } catch { }
            try { Stream?.Close(); } catch { }
            try { Client?.Close(); } catch { }
        }
    }

    public class ChatServer
    {
        private TcpListener _server;
        private TcpListener _fileServer;
        private List<ClientConnection> _connectedClients = new List<ClientConnection>();
        private readonly object _clientsLock = new object();
        private readonly object _logFileLock = new object();
        private int _clientCounter = 0;
        private Thread _listenThread;
        private bool _isRunning = false;
        private bool _isFileServerRunning = false;
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

                // Запускаем сервер для файлов на отдельном порту (8889)
                _fileServer = new TcpListener(ip, 8889);
                _fileServer.Start();
                _isFileServerRunning = true;

                // Создаем файл лога только на сервере
                InitializeLogFile();

                LogMessage($"Сервер запущен на {ipAddress}:{port}");
                LogMessage("Сервер файлов запущен на порту 8889");
                LogMessage("Ожидание подключений...");

                _listenThread = new Thread(new ThreadStart(ListenForClients));
                _listenThread.IsBackground = true;
                _listenThread.Start();

                // Запускаем поток для приема файлов
                Thread fileThread = new Thread(new ThreadStart(ListenForFiles));
                fileThread.IsBackground = true;
                fileThread.Start();
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка запуска сервера: {ex.Message}");
                _isRunning = false;
            }
        }

        private void ListenForFiles()
        {
            try
            {
                while (_isFileServerRunning)
                {
                    TcpClient client = _fileServer.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleFileTransfer(client));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    LogMessage("Сервер файлов прекратил прослушивание.");
                }
                else if (_isFileServerRunning)
                {
                    LogMessage($"Ошибка прослушивания файловых подключений: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                if (_isFileServerRunning)
                {
                    LogMessage($"Общая ошибка в ListenForFiles: {ex.Message}");
                }
            }
        }

        // Метод для обработки передачи файла
        private void HandleFileTransfer(TcpClient client)
        {
            string senderLogin = "";

            try
            {
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream, Encoding.Unicode))
                {
                    // Читаем сигнал
                    string signal = reader.ReadString();

                    if (signal == "FILE_TRANSFER")
                    {
                        // Читаем логин отправителя
                        senderLogin = reader.ReadString();

                        // Сохраняем файл на сервере
                        // Предполагается, что FileTransfer.SaveFileFromClient существует
                        string serverFileName = FileTransfer.SaveFileFromClient(stream, senderLogin);

                        // Отправляем клиенту имя сохраненного файла
                        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode))
                        {
                            writer.Write(serverFileName);
                        }

                        LogMessage($"Файл от {senderLogin} сохранен на сервере: {serverFileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка обработки файла от {senderLogin}: {ex.Message}");
            }
            finally
            {
                try { client.Close(); } catch { }
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

        // Метод для сохранения сообщения в файл истории на сервере (С ШИФРОВАНИЕМ)
        public void SaveMessageToServerLog(string message, string sender = null, bool isFileMessage = false)
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

                    if (string.IsNullOrEmpty(sender) || sender == "Система" || isFileMessage)
                    {
                        // Системные сообщения и сообщения о файлах НЕ шифруем
                        messageToSave = $"[{timestamp}] {message}";
                    }
                    else
                    {
                        // Сообщение от пользователя - шифруем текст сообщения
                        string encryptedMessage = EncryptMessageForStorage(message);
                        messageToSave = $"[{timestamp}] [{sender}] {encryptedMessage}";
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

        // Шифрование сообщения для хранения в файле
        private string EncryptMessageForStorage(string message)
        {
            // Предполагается, что класс Encryption с методами Encrypt и Decrypt существует
            return Encryption.Encrypt(message);
        }

        // Дешифрование сообщения при чтении из файла
        private string DecryptMessageFromStorage(string encryptedMessage)
        {
            try
            {
                return Encryption.Decrypt(encryptedMessage);
            }
            catch (Exception)
            {
                // Если не удалось дешифровать, возвращаем оригинал
                return encryptedMessage;
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _isFileServerRunning = false;

            try
            {
                _server?.Stop();
                _fileServer?.Stop();
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
                clientConn.Writer.WriteLine($"COLOR:{clientColor.ToArgb()}");
                clientConn.Writer.Flush();

                // Отправляем всем сообщение о подключении нового пользователя
                string connectMessage = $"{currentClientName} подключился к чату";
                BroadcastColoredMessage(connectMessage, clientColor);

                // Сохраняем в историю на сервере (системное сообщение не шифруется)
                SaveMessageToServerLog(connectMessage, "Система");

                // Отправляем историю сообщений новому пользователю (дешифрованную)
                SendChatHistoryToClient(clientConn);

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
                        // Проверяем, является ли сообщение о файле
                        bool isFileMessage = message.Contains("[ФАЙЛ от") || message.Contains("[SERVER_FILE:");

                        if (isFileMessage)
                        {
                            // Обрабатываем файл на сервере (если содержит информацию о файле)
                            if (message.Contains("[SERVER_FILE:"))
                            {
                                ProcessFileOnServer(message, currentClientName);
                            }

                            // Сохраняем сообщение о файле в истории НЕ шифрованным
                            SaveMessageToServerLog(message, currentClientName, isFileMessage);
                        }
                        else
                        {
                            // Обычные сообщения сохраняем с шифрованием
                            SaveMessageToServerLog(message, currentClientName, false);
                        }

                        // Отправляем сообщение всем остальным клиентам
                        string coloredMessage = $"[{currentClientName}]: {message}";
                        BroadcastColoredMessage(coloredMessage, clientColor, clientConn);
                    }
                }

                // Сохраняем отключение в истории на сервере (системное сообщение)
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

        // Метод для обработки файла на сервере
        private void ProcessFileOnServer(string message, string sender)
        {
            try
            {
                if (message.Contains("[SERVER_FILE:"))
                {
                    int serverFileStart = message.IndexOf("[SERVER_FILE:") + 13;
                    int serverFileEnd = message.IndexOf("]", serverFileStart);

                    if (serverFileStart > 0 && serverFileEnd > serverFileStart)
                    {
                        string serverFileName = message.Substring(serverFileStart, serverFileEnd - serverFileStart);
                        string originalFileName = "";
                        long fileSize = 0;

                        // Извлекаем оригинальное имя файла и размер
                        if (message.Contains("]: "))
                        {
                            int nameStart = message.IndexOf("]: ") + 3;
                            int nameEnd = message.IndexOf(" (");

                            if (nameEnd > nameStart)
                            {
                                originalFileName = message.Substring(nameStart, nameEnd - nameStart).Trim();

                                // Извлекаем размер файла
                                int sizeStart = message.IndexOf(" (") + 2;
                                int sizeEnd = message.IndexOf(")", sizeStart);

                                if (sizeStart > 0 && sizeEnd > sizeStart)
                                {
                                    string sizeStr = message.Substring(sizeStart, sizeEnd - sizeStart);
                                    // Убираем единицы измерения
                                    sizeStr = sizeStr.Replace("Б", "").Replace("КБ", "").Replace("МБ", "").Replace("ГБ", "").Trim();
                                    string[] sizeParts = sizeStr.Split(' ');

                                    if (sizeParts.Length > 0 && double.TryParse(sizeParts[0], out double parsedSize))
                                    {
                                        // Конвертируем в байты
                                        if (sizeStr.Contains("КБ") || sizeStr.Contains("кб"))
                                            fileSize = (long)(parsedSize * 1024);
                                        else if (sizeStr.Contains("МБ") || sizeStr.Contains("мб"))
                                            fileSize = (long)(parsedSize * 1024 * 1024);
                                        else if (sizeStr.Contains("ГБ") || sizeStr.Contains("гб"))
                                            fileSize = (long)(parsedSize * 1024 * 1024 * 1024);
                                        else
                                            fileSize = (long)parsedSize;
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(serverFileName) && !string.IsNullOrEmpty(originalFileName))
                        {
                            // Сохраняем информацию о файле
                            string mappingFilePath = Path.Combine(FileTransfer.ServerFilesDirectory, "files_mapping.txt");
                            string mappingLine = $"{DateTime.Now:yyyy-MM-dd HH:mm}|{sender}|{originalFileName}|{serverFileName}|{fileSize}";

                            lock (_logFileLock)
                            {
                                File.AppendAllText(mappingFilePath, mappingLine + Environment.NewLine);
                            }

                            LogMessage($"Файл зарегистрирован: {originalFileName} → {serverFileName} ({fileSize} байт)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка обработки файла на сервере: {ex.Message}");
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

        // Отправка истории чата новому клиенту (с сервера) - ДЕШИФРУЕМ перед отправкой
        private void SendChatHistoryToClient(ClientConnection clientConn)
        {
            try
            {
                if (File.Exists(_messageLogFile))
                {
                    string[] lines = File.ReadAllLines(_messageLogFile);
                    int startIndex = Math.Max(0, lines.Length - 50);

                    for (int i = startIndex; i < lines.Length; i++)
                    {
                        // Пропускаем системные сообщения о сессиях
                        if (!lines[i].Contains("=== Сессия") && !lines[i].Contains("=== Начало лога"))
                        {
                            // Дешифруем сообщение перед отправкой (если оно зашифровано)
                            string processedLine = ProcessHistoryLineForSending(lines[i]);
                            if (!string.IsNullOrEmpty(processedLine))
                            {
                                clientConn.Writer.WriteLine(processedLine);
                            }
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

        // Обработка строки истории для отправки клиенту
        private string ProcessHistoryLineForSending(string line)
        {
            // Формат строки: [timestamp] [sender] encrypted_message
            // Или: [timestamp] message (для системных)

            try
            {
                // Если это системное сообщение (без отправителя)
                if (line.Contains("] ") && !line.Contains("] ["))
                {
                    int timestampEnd = line.IndexOf("] ") + 2;
                    if (timestampEnd > 0 && timestampEnd < line.Length)
                    {
                        string timestamp = line.Substring(0, timestampEnd);
                        string message = line.Substring(timestampEnd);

                        // Системные сообщения не шифруются
                        return $"{timestamp}{message}";
                    }
                }
                // Если это сообщение от пользователя
                else if (line.Contains("] ["))
                {
                    int timestampEnd = line.IndexOf("] ") + 2;
                    int senderStart = line.IndexOf("[", timestampEnd);
                    int senderEnd = line.IndexOf("]", senderStart);

                    if (timestampEnd > 0 && senderStart > 0 && senderEnd > senderStart)
                    {
                        string timestamp = line.Substring(0, timestampEnd);
                        string sender = line.Substring(senderStart + 1, senderEnd - senderStart - 1);
                        string encryptedMessage = line.Substring(senderEnd + 1).Trim();

                        // Дешифруем сообщение (если оно зашифровано)
                        string decryptedMessage = DecryptMessageFromStorage(encryptedMessage);

                        return $"{timestamp}[{sender}] {decryptedMessage}";
                    }
                }

                return line; // В случае ошибки возвращаем оригинал
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка обработки строки истории: {ex.Message}");
                return line; // В случае ошибки возвращаем оригинал
            }
        }

        // Метод для загрузки истории (используется в FormClient)
        public List<string> LoadChatHistoryFromFile(int maxLines = 100)
        {
            List<string> history = new List<string>();

            try
            {
                if (File.Exists(_messageLogFile))
                {
                    string[] lines = File.ReadAllLines(_messageLogFile);
                    int startIndex = Math.Max(0, lines.Length - maxLines);

                    for (int i = startIndex; i < lines.Length; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(lines[i]) &&
                            !lines[i].Contains("=== Начало лога") &&
                            !lines[i].Contains("=== Сессия"))
                        {
                            // Обрабатываем строку для отображения (дешифруем)
                            string processedLine = ProcessHistoryLineForDisplay(lines[i]);
                            history.Add(processedLine);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка загрузки истории: {ex.Message}");
            }

            return history;
        }

        // Обработка строки истории для отображения
        private string ProcessHistoryLineForDisplay(string line)
        {
            try
            {
                // Если это системное сообщение (без отправителя)
                if (line.Contains("] ") && !line.Contains("] ["))
                {
                    int timestampEnd = line.IndexOf("] ") + 2;
                    if (timestampEnd > 0 && timestampEnd < line.Length)
                    {
                        string timestamp = line.Substring(1, timestampEnd - 3); // Без скобок
                        string message = line.Substring(timestampEnd);

                        return $"[{timestamp}] {message}";
                    }
                }
                // Если это сообщение от пользователя
                else if (line.Contains("] ["))
                {
                    int timestampEnd = line.IndexOf("] ") + 2;
                    int senderStart = line.IndexOf("[", timestampEnd);
                    int senderEnd = line.IndexOf("]", senderStart);

                    if (timestampEnd > 0 && senderStart > 0 && senderEnd > senderStart)
                    {
                        string timestamp = line.Substring(1, timestampEnd - 3); // Без скобок
                        string sender = line.Substring(senderStart + 1, senderEnd - senderStart - 1);
                        string encryptedMessage = line.Substring(senderEnd + 1).Trim();

                        // Дешифруем сообщение
                        string decryptedMessage = DecryptMessageFromStorage(encryptedMessage);

                        return $"[{timestamp}] [{sender}] {decryptedMessage}";
                    }
                }

                return line;
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка обработки строки для отображения: {ex.Message}");
                return line;
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