using System;
using System.IO;
using System.Text;

namespace EncryptionTesting
{
    public static class Encryption
    {
        private const string ENCRYPTED_FILE_PATH = "encrypted_messages.txt";

        public static string Encrypt(string input)
        {
            if (input == null) return null;
            if (input == "") return "";

            string result = ShiftText(input, 3);

            try
            {
                LogEncryptedMessage(input, result);
            }
            catch
            {
                // Игнорируем ошибки записи в лог, как указано в тесте
            }

            return result;
        }

        public static string Decrypt(string input)
        {
            if (input == null) return null;
            if (input == "") return "";

            return ShiftText(input, -3);
        }

        private static string ShiftText(string text, int shift)
        {
            StringBuilder result = new StringBuilder();

            foreach (char c in text)
            {
                if (c >= 'a' && c <= 'z')
                {
                    char shifted = (char)(c + shift);
                    if (shifted > 'z')
                        shifted = (char)(shifted - 26);
                    else if (shifted < 'a')
                        shifted = (char)(shifted + 26);
                    result.Append(shifted);
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    char shifted = (char)(c + shift);
                    if (shifted > 'Z')
                        shifted = (char)(shifted - 26);
                    else if (shifted < 'A')
                        shifted = (char)(shifted + 26);
                    result.Append(shifted);
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        private static void LogEncryptedMessage(string original, string encrypted)
        {
            try
            {
                string logEntry = $"Original: {original} -> Encrypted: {encrypted}{Environment.NewLine}";
                File.AppendAllText(ENCRYPTED_FILE_PATH, logEntry, Encoding.UTF8);
            }
            catch
            {
                // Обрабатываем исключения, как требуется в тесте
            }
        }

        public static string GetEncryptedMessagesLog()
        {
            if (!File.Exists(ENCRYPTED_FILE_PATH))
            {
                return "Файл с зашифрованными сообщениями не найден.";
            }

            return File.ReadAllText(ENCRYPTED_FILE_PATH, Encoding.UTF8);
        }

        public static void ClearEncryptedMessagesLog()
        {
            if (File.Exists(ENCRYPTED_FILE_PATH))
            {
                File.Delete(ENCRYPTED_FILE_PATH);
            }
        }
    }
}