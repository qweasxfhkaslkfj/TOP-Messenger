using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace TOP_Messenger
{
    public class Registration
    {
        public static string CurrentLogin { get; private set; } = "";
        public static bool IsGuest { get; private set; } = false;
        public static bool IsServer { get; private set; } = false;

        public static string CurrentRole
        {
            get
            {
                if (IsServer) return "Server";
                if (IsGuest) return "Guest";
                return "User";
            }
        }

        private Dictionary<string, string> usersLog;
        private const string ServerAdminLogin = "server";
        private const string AdminPassword = "pAv0Pav183";

        public Registration()
        {
            usersLog = new Dictionary<string, string>();
            LoginUser();
        }

        private void LoginUser()
        {
            usersLog.Add("krs333", "krs123");
            usersLog.Add("Pagan821", "ars123");
            usersLog.Add("denden", "denzem123");
            usersLog.Add("cat_noir", "denzol123");
            usersLog.Add("lady_bug", "kerya123");
            usersLog.Add("tabeer", "alb123");
            usersLog.Add("lushPush", "ol123");
            usersLog.Add("Siles", "zah123");
            usersLog.Add("USF055", "usf123");
            usersLog.Add("vld666", "vld123");
            usersLog.Add("ananas", "nast123");
        }

        public RegistrationResult CheckLoginAndPassword(string login, string password)
        {
            // Сбрасываем предыдущую сессию
            ResetSession();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                return new RegistrationResult
                {
                    IsValid = false,
                };
            }

            // Проверка сервера
            if (login == ServerAdminLogin && password == AdminPassword)
            {
                CurrentLogin = login;
                IsServer = true;
                IsGuest = false;

                return new RegistrationResult
                {
                    IsValid = true,
                    Login = login,
                    IsServer = true,
                    IsGuest = false
                };
            }

            // Проверка обычных пользователей
            if (usersLog.ContainsKey(login) && usersLog[login] == password)
            {
                CurrentLogin = login;
                IsGuest = false;
                IsServer = false;

                return new RegistrationResult
                {
                    IsValid = true,
                    Login = login,
                    IsServer = false,
                    IsGuest = false
                };
            }

            // Неверные учетные данные
            return new RegistrationResult
            {
                IsValid = false
            };
        }

        //Проверка Гостя
        public RegistrationResult CheckGuestLogin(string login)
        {
            ResetSession();

            if (string.IsNullOrWhiteSpace(login))
            {
                return new RegistrationResult
                {
                    IsValid = false
                };
            }


            //Если логин гостя совпадает с логином Адимна
            if (login == ServerAdminLogin)
            {
                return new RegistrationResult
                {
                    IsValid = false
                };
            }

            //Если логин гостя совпадает с логином Клиента
            if (usersLog.ContainsKey(login))
            {
                return new RegistrationResult
                {
                    IsValid = false
                };
            }

            //Если количество символов больше 15 
            if (login.Length >= 15)
            {
                return new RegistrationResult
                {
                    IsValid = false
                };
            }

            CurrentLogin = login;
            IsGuest = true;
            IsServer = false;

            return new RegistrationResult
            {
                IsValid = true,
                Login = login,
                IsGuest = true
            };
        }

        private void ResetSession()
        {
            CurrentLogin = "";
            IsGuest = false;
            IsServer = false;
        }

        // Метод для проверки текущей роли (для других форм)
        public static bool IsCurrentUserServer() => IsServer;
        public static bool IsCurrentUserGuest() => IsGuest;
        public static bool IsCurrentUserRegular() => !IsGuest && !IsServer;
        public static string GetCurrentLogin() => CurrentLogin;

        public static string GetLocalIPAddress()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);

            try
            {
                socket.Connect("8.8.8.8", 65530);

                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;

                if (endPoint != null)
                {
                    string ipAddress = endPoint.Address.ToString();
                    if (ipAddress != null && ipAddress != "")
                    {
                        return ipAddress;
                    }
                }
                return "192.168.88.145";
            }
            catch (Exception)
            {
                string fallbackIp = GetLocalIPByUDP();

                if (fallbackIp != null && fallbackIp != "")
                {
                    return fallbackIp;
                }

                return "192.168.88.145";
            }
            finally
            {
                socket.Dispose();
            }
        }
        private static string GetLocalIPByUDP()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                try
                {
                    socket.Connect("10.255.255.255", 1);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;

                    if (endPoint != null)
                    {
                        IPAddress ipAddress = endPoint.Address;
                        if (ipAddress != null)
                        {
                            return ipAddress.ToString();
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                    return null;
                }
            }
        }
    }
    public class RegistrationResult
    {
        public bool IsValid { get; set; }
        public bool IsGuest { get; set; }
        public bool IsServer { get; set; }
        public string Login { get; set; }
    }
}
