using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOP_Messenger
{
    public class Registration
    {
        public static bool isGuest = false;
        public static bool isServer = false;
        public static string login = "";

        private Dictionary<string, string> usersLog;
        private Dictionary<string, string> adminLog;

        public Registration()
        {
            usersLog = new Dictionary<string, string>();
            adminLog = new Dictionary<string, string>();

            LoginUser();
            LoginAdmin();
        }

        public void LoginUser()
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

        public void LoginAdmin()
        {
            adminLog.Add("server", "pAv0Pav183");
        }

        //Проверка на админа
        /*public void CheckLoginAndPassword(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Логин не может быть пустым"
                };
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Пароль не может быть пустым"
                };
            }

            // Проверка учетных данных
            if (login == ServerAdminLogin && password == AdminPassword)
            {
                // Серверный администратор
                return new ValidationResult
                {
                    IsValid = true,
                    IsServer = true,
                    UserRole = "ServerAdmin"
                };
            }
            else if (login == AdminLogin && password == AdminPassword)
            {
                // Обычный администратор
                return new ValidationResult
                {
                    IsValid = true,
                    IsServer = false,
                    UserRole = "Admin"
                };
            }
            else
            {
                // Проверка для обычных пользователей (можно добавить базу данных)
                // В этом примере просто блокируем вход
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Неверный логин или пароль"
                };
            }
        }*/
    }
}
