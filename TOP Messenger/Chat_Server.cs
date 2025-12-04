using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace public_chat_server
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

        public event Action<string> MessageLogged;

        public ChatServer()
        {
        
        }

        public void Start(IPAddress ipAddress, int port)
        {
            if (_server != null && _server.Server.IsBound)
            {
                LogMessage("Сервер уже запущен.");
                return;
            }

            _server = new TcpListener(ipAddress, port);
            _server.Start();
            LogMessage("Сервер запущен...");
            LogMessage($"Ожидание подключений на {ipAddress}:{port}");

            Thread listenThread = new Thread(new ThreadStart(ListenForClients));
            listenThread.IsBackground = true;
            listenThread.Start();
        }

        public void Stop()
        {
            if (_server != null)
            {
                try
                {
                    _server.Stop();
                    LogMessage("Сервер остановлен.");
                }
                catch (Exception ex)
                {
                    LogMessage($"Ошибка при остановке сервера: {ex.Message}");
                }
                _server = null;
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
                    catch (Exception ex)
                    {
                        LogMessage($"Ошибка при закрытии клиента {client.ClientName}: {ex.Message}");
                    }
                }
                _connectedClients.Clear();
            }
        }

        private void ListenForClients()
        {
            try
            {
                while (_server != null && _server.Server.IsBound)
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
                else
                {
                    LogMessage($"Ошибка прослушивания клиентов: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Общая ошибка в ListenForClients: {ex.Message}");
            }
        }

        private void HandleClient(ClientConnection clientConn)
        {
            string currentClientName = clientConn.ClientName;

            try
            {
                string initialMessage = clientConn.Reader.ReadLine();
                if (!string.IsNullOrEmpty(initialMessage))
                {
                    currentClientName = initialMessage.Trim();
                    clientConn.ClientName = currentClientName;
                    LogMessage($"Клиент {currentClientName} зарегистрировал имя.");
                }
                else
                {
                    LogMessage($"Клиент {clientConn.ClientName} не предоставил имя, используя временное ID.");
                }

                BroadcastMessage($"[{currentClientName} подключился к чату]");

                string message;
                while ((message = clientConn.Reader.ReadLine()) != null)
                {
                    message = message.Trim();

                    LogMessage($"[{currentClientName}]: {message}");

                    if (message.ToUpper() == "EXIT")
                    {
                        LogMessage($"[{currentClientName}] отключается по команде.");
                        break;
                    }

                    SendMessageToOthers($"[{currentClientName}]: {message}", clientConn);
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
                BroadcastMessage($"[{currentClientName} покинул чат]");
                LogMessage($"[{currentClientName}] отключен.");
            }
        }

        private void BroadcastMessage(string message)
        {
            LogMessage($"Рассылка: {message}");
            List<ClientConnection> clientsToCleanup = new List<ClientConnection>();

            lock (_clientsLock)
            {
                foreach (var client in _connectedClients)
                {
                    try
                    {
                        client.Writer.WriteLine(message);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Ошибка отправки сообщения клиенту {client.ClientName}: {ex.Message}. Отмечаем на удаление.");
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

        private void SendMessageToOthers(string message, ClientConnection senderClient)
        {
            List<ClientConnection> clientsToCleanup = new List<ClientConnection>();

            lock (_clientsLock)
            {
                foreach (var client in _connectedClients)
                {
                    if (client != senderClient)
                    {
                        try
                        {
                            client.Writer.WriteLine(message);
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Ошибка отправки сообщения клиенту {client.ClientName} (исключая отправителя): {ex.Message}. Отмечаем на удаление.");
                            clientsToCleanup.Add(client);
                        }
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
        private void LogMessage(string message)
        {
            MessageLogged?.Invoke(message);
        }
    }
}
