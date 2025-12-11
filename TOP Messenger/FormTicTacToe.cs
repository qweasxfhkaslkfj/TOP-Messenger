using System;
using System.Windows.Forms;

namespace TOP_Messenger
{
    public partial class FormTicTacToe : Form
    {
        //true = X; false = O
        private bool isPlayerXTurn = true;
        private bool gameEnded = false;

        //Счет
        private int playerXScore = 0;   //Х
        private int playerOScore = 0;   //O

        // Счетчик ходов для определения ничьи
        private int moveCount = 0;

        //Имя другого игрока
        private string Player2 = "Оппонент";

        public FormTicTacToe()
        {
            InitializeComponent();
            ResetGame();
        }

        //Перезапуск игры
        private void ResetGame()
        {
            //Чистим кнопки
            button1.Text = button2.Text = button3.Text =
            button4.Text = button5.Text = button6.Text =
            button7.Text = button8.Text = button9.Text = "";

            //Включаем кнопки
            button1.Enabled = button2.Enabled = button3.Enabled =
            button4.Enabled = button5.Enabled = button6.Enabled =
            button7.Enabled = button8.Enabled = button9.Enabled = true;

            // Сбрасываем флаги
            isPlayerXTurn = true;
            gameEnded = false;
            moveCount = 0;

            // Обновляем отображение
            UpdateScoreDisplay();
            UpdateTurnDisplay();
        }

        //Показывает счета
        private void UpdateScoreDisplay()
        {
            lblMe.Text = $"Ваш счет: {playerXScore}";
            lblHe.Text = $"Счет {Player2}: {playerOScore}";
        }
        
        //Показывает кто ходит
        private void UpdateTurnDisplay()
        {
            if (isPlayerXTurn)
            {
                lblWalker.Text = "Ход: Крестики (Вы)";
            }
            else
            {
                lblWalker.Text = $"Ход: Нолики ({Player2})";
            }
        }

        //Общий обработчик кликов по кнопкам
        private void HandleButtonClick(Button button)
        {
            if (gameEnded || button.Text != "")
                return;

            moveCount++;

            if (isPlayerXTurn)
            {
                button.Text = "X";
            }
            else
            {
                button.Text = "O";
            }

            // Проверяем победителя
            string winner = Proverka();

            if (winner != "")
            {
                gameEnded = true;

                if (winner == "X")
                {
                    playerXScore++;
                    MessageBox.Show("Вы победили! Крестики выиграли!", "Победа!",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    playerOScore++;
                    MessageBox.Show($"{Player2} победил! Нолики выиграли!", "Победа!",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                UpdateScoreDisplay();
            }
            else if (moveCount == 9)
            {
                gameEnded = true;
                MessageBox.Show("Ничья! Все поле заполнено.", "Игра окончена",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                isPlayerXTurn = !isPlayerXTurn;
                UpdateTurnDisplay();
            }
        }

        //Проерка на победу
        private string Proverka()
        {
            //Строка
            if (button1.Text != "" && button1.Text == button2.Text && button2.Text == button3.Text)
            {
                HighlightWinner(button1, button2, button3);
                return button1.Text;
            }
            if (button4.Text != "" && button4.Text == button5.Text && button5.Text == button6.Text)
            {
                HighlightWinner(button4, button5, button6);
                return button4.Text;
            }
            if (button7.Text != "" && button7.Text == button8.Text && button8.Text == button9.Text)
            {
                HighlightWinner(button7, button8, button9);
                return button7.Text;
            }

            //Столбцы
            if (button1.Text != "" && button1.Text == button4.Text && button4.Text == button7.Text)
            {
                HighlightWinner(button1, button4, button7);
                return button1.Text;
            }
            if (button2.Text != "" && button2.Text == button5.Text && button5.Text == button8.Text)
            {
                HighlightWinner(button2, button5, button8);
                return button2.Text;
            }
            if (button3.Text != "" && button3.Text == button6.Text && button6.Text == button9.Text)
            {
                HighlightWinner(button3, button6, button9);
                return button3.Text;
            }

            //Диагональ
            if (button1.Text != "" && button1.Text == button5.Text && button5.Text == button9.Text)
            {
                HighlightWinner(button1, button5, button9);
                return button1.Text;
            }
            if (button3.Text != "" && button3.Text == button5.Text && button5.Text == button7.Text)
            {
                HighlightWinner(button3, button5, button7);
                return button3.Text;
            }

            return "";
        }

        //Подсветка выигрышной комбинации
        private void HighlightWinner(Button btn1, Button btn2, Button btn3)
        {
            btn1.BackColor = btn2.BackColor = btn3.BackColor = System.Drawing.Color.LightGreen;
        }

        private void button9_Click_1(object sender, EventArgs e)
        {
            HandleButtonClick(button9);
        }
        private void button8_Click_1(object sender, EventArgs e)
        {
            HandleButtonClick(button8);
        }
        private void button7_Click_1(object sender, EventArgs e)
        {
            HandleButtonClick(button7);
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            HandleButtonClick(button6);
        }
        private void button5_Click_1(object sender, EventArgs e)
        {
            HandleButtonClick(button5);
        }
        private void button4_Click_1(object sender, EventArgs e)
        {
            HandleButtonClick(button4);
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            HandleButtonClick(button3);
        }
        private void button2_Click_1(object sender, EventArgs e)
        {
            HandleButtonClick(button2);
        }
        private void button1_Click_1(object sender, EventArgs e)
        {
            HandleButtonClick(button1);
        }
    }
}