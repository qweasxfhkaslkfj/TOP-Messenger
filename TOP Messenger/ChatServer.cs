using System;
using System.Collections.Generic;
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
        public StreamWriter Writer { get; private set; }
        public StreamReader Reader { get; private set; }
        public string ClientName { get; set; }

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
        public event Action<string> MessageReceived;


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

        public bool IsRunning => _isRunning;

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

            try
            {
                // Первое сообщение от клиента - его имя
                string initialMessage = clientConn.Reader.ReadLine();
                if (!string.IsNullOrEmpty(initialMessage))
                {
                    currentClientName = initialMessage.Trim();
                    clientConn.ClientName = currentClientName;
                    LogMessage($"Клиент {currentClientName} зарегистрировал имя.");
                }

                BroadcastMessage($"[{currentClientName} подключился к чату]", null);
                //MessageReceived?.Invoke($"[{currentClientName} подключился к чату]");

                string message;
                while (_isRunning && (message = clientConn.Reader.ReadLine()) != null)
                {
                    message = message.Trim();

                    if (message.ToUpper() == "EXIT")
                    {
                        LogMessage($"[{currentClientName}] отключается по команде.");
                        break;
                    }

                    string formattedMessage = $"[{currentClientName}]: {message}";
                    LogMessage(formattedMessage);
                    //MessageReceived?.Invoke(formattedMessage);
                    SendMessageToOthers(formattedMessage, clientConn);
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

                clientConn.Close();

                string disconnectMessage = $"[{currentClientName} покинул чат]";
                BroadcastMessage(disconnectMessage, null);
                //MessageReceived?.Invoke(disconnectMessage);

                LogMessage($"[{currentClientName}] отключен.");
            }
        }
        public void SendMessageToAll(string message)
        {
            if (!_isRunning) return;

            BroadcastMessage(message, null);
            //MessageReceived?.Invoke(message);
        }
        private void BroadcastMessage(string message, ClientConnection sender)
        {
            List<ClientConnection> clientsToCleanup = new List<ClientConnection>();

            lock (_clientsLock)
            {
                foreach (var client in _connectedClients)
                {
                    if (client == sender) continue;

                    try
                    {
                        client.Writer.WriteLine(message);
                    }
                    catch (Exception)
                    {
                        clientsToCleanup.Add(client);
                    }
                }

                foreach (var client in clientsToCleanup)
                {
                    _connectedClients.Remove(client);
                    client.Close();
                    LogMessage($"Клиент {client.ClientName} удален из списка подключенных (неактивен).");
                }
            }
        }
        private void SendMessageToOthers(string message, ClientConnection sender)
        {
            BroadcastMessage(message, sender);
        }
        private void LogMessage(string message)
        {
            MessageLogged?.Invoke(message);
        }
    }
}