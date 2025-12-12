using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOP_Messenger
{
    internal class Encryption
    {
        // Сдвиг для шифра Цезаря
        private const int SHIFT = 3;

        /// <summary>
        /// Шифрует сообщение с использованием шифра Цезаря
        /// </summary>
        /// <param name="message">Исходное сообщение</param>
        /// <returns>Зашифрованное сообщение</returns>
        public static string Encrypt(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            StringBuilder encrypted = new StringBuilder();

            foreach (char c in message)
            {
                // Шифруем только буквы, цифры и пробелы оставляем как есть
                if (char.IsLetter(c))
                {
                    char offset = char.IsUpper(c) ? 'A' : 'a';
                    char encryptedChar = (char)(((c + SHIFT - offset) % 26) + offset);
                    encrypted.Append(encryptedChar);
                }
                else
                {
                    // Не буквы оставляем без изменений
                    encrypted.Append(c);
                }
            }

            return encrypted.ToString();
        }

        /// <summary>
        /// Расшифровывает сообщение, зашифрованное шифром Цезаря
        /// </summary>
        /// <param name="encryptedMessage">Зашифрованное сообщение</param>
        /// <returns>Расшифрованное сообщение</returns>
        public static string Decrypt(string encryptedMessage)
        {
            if (string.IsNullOrEmpty(encryptedMessage))
                return encryptedMessage;

            StringBuilder decrypted = new StringBuilder();

            foreach (char c in encryptedMessage)
            {
                if (char.IsLetter(c))
                {
                    char offset = char.IsUpper(c) ? 'A' : 'a';
                    // Для дешифровки используется обратный сдвиг 
                    char decryptedChar = (char)(((c - SHIFT - offset + 26) % 26) + offset);
                    decrypted.Append(decryptedChar);
                }
                else
                {
                    decrypted.Append(c);
                }
            }

            return decrypted.ToString();
        }
    }
}