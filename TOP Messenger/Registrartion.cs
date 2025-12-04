using System;
using System.Collections.Generic;
using System.Linq.Expressions;

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

        /*public void LoginAdmin()
        {
            adminLog.Add("server", "pAv0Pav183");
        }*/

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

            /*if (login == ServerAdminLogin)
            {
                return new RegistrationResult
                {
                    IsValid = false
                };
            }

            if (usersLog.ContainsKey(login))
            {
                return new RegistrationResult
                {
                    IsValid = false
                };
            }*/

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
    }
    public class RegistrationResult
    {
        public bool IsValid { get; set; }
        public bool IsGuest { get; set; }
        public bool IsServer { get; set; }
        public string Login { get; set; }
    }
}
