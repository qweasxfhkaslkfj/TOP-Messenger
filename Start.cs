using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net; // Для IPAddress
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace передача_файлов
{
    public partial class Start : Form
    {
        public Start()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Выберите файл";
                openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Изображения (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Все файлы (*.*)|*.*";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    textBox1.Text = filePath;
                }
                else
                {
                    textBox1.Text = "Выбор файла отменен.";
                }
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;

            try
            {
                var dirPath = @"C:\Users\sander.GD\Desktop\test\";

                if (!Directory.Exists(dirPath))
                {
                    MessageBox.Show($"Локальная директория не найдена: {dirPath}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // --- НАСТРОЙКИ TCP СЕРВЕРА (ИЗМЕНИ ЭТО!) ---
                var serverIp = "192.168.88.142"; // IP-адрес  TCP-сервера (например, localhost)
                var serverPort = 23;     // Порт, на котором слушает TCP-сервер

                TcpFileClient tcpClient = new TcpFileClient(serverIp, serverPort);

                string[] files = Directory.GetFiles(dirPath, "*.*");
                if (files.Length == 0)
                {
                    MessageBox.Show($"В директории {dirPath} нет файлов для отправки.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                foreach (string file in files)
                {
                    try
                    {
                        await tcpClient.SendFileAsync(file);
                        MessageBox.Show($"Файл {Path.GetFileName(file)} успешно отправлен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (TcpClientException ex)
                    {
                        MessageBox.Show($"Ошибка при отправке файла {Path.GetFileName(file)}: {ex.Message}", "Ошибка TCP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        
                    }
                }

                if (string.IsNullOrEmpty(textBox1.Text))
                {
                    MessageBox.Show("Gelieve uw naam in te geven !", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("Все доступные файлы были обработаны.", "Завершено", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (TcpClientException ex)
            {
                MessageBox.Show($"Произошла ошибка TCP-клиента: {ex.Message}", "Ошибка TCP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}", "Общая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button2.Enabled = true;
            }
        }
    }
}