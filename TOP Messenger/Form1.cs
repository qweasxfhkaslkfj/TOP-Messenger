using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TOP_Messenger
{
   
    public partial class FormClient : Form
    {
        public FormClient()
        {
            InitializeComponent();
        }


        public void AddFileToHistory(string fileName)
        {
            FlowLayoutPanel filePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 5)
            };

            Label fileLabel = new Label
            {
                Text = fileName,
                AutoSize = true,
                Margin = new Padding(5, 0, 5, 0),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            Button downloadButton = new Button
            {
                Text = "Скачать",
                Tag = fileName, 
                Margin = new Padding(5, 0, 5, 0),
                Padding = new Padding(5)
            };

            downloadButton.Click += (sender, e) =>
            {
                string fileToDownload = (string)((Button)sender).Tag;

                MessageBox.Show($"Скачивание файла: {fileToDownload}");
            };

            filePanel.Controls.Add(fileLabel);
            filePanel.Controls.Add(downloadButton);

            panelHistoryFiles.Controls.Add(filePanel);
        }

       
    }
}
