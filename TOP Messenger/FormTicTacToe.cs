using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TOP_Messenger
{
    public partial class FormTicTacToe : Form
    {
        // true = X (игрок); false = O (робот)
        private bool isPlayerXTurn = true;
        private bool gameEnded = false;

        // Счет
        private int playerXScore = 0;   // Игрок (X)
        private int playerOScore = 0;   // Робот (O)

        // Счетчик ходов для определения ничьи
        private int moveCount = 0;

        // Имя робота
        private string robotName = "Робот";

        // Список всех кнопок для удобства
        private List<Button> allButtons;

        public FormTicTacToe()
        {
            InitializeComponent();

            // Инициализируем список кнопок
            allButtons = new List<Button>
            {
                button1, button2, button3,
                button4, button5, button6,
                button7, button8, button9
            };

            ResetGame();
        }

        // Перезапуск игры
        private void ResetGame()
        {
            // Чистим кнопки
            foreach (Button btn in allButtons)
            {
                btn.Text = "";
                btn.Enabled = true;
                btn.BackColor = System.Drawing.SystemColors.Control;
                btn.ForeColor = System.Drawing.Color.Black;
            }

            // Сбрасываем флаги
            isPlayerXTurn = true;
            gameEnded = false;
            moveCount = 0;

            // Обновляем отображение
            UpdateScoreDisplay();
            UpdateTurnDisplay();
        }

        // Показывает счета
        private void UpdateScoreDisplay()
        {
            lblMe.Text = $"Ваш счет: {playerXScore}";
            lblHe.Text = $"Счет {robotName}: {playerOScore}";
        }

        // Показывает кто ходит
        private void UpdateTurnDisplay()
        {
            if (isPlayerXTurn)
            {
                lblWalker.Text = "Ход: Крестики (Вы)";
                lblWalker.ForeColor = System.Drawing.Color.Red;
            }
            else
            {
                lblWalker.Text = $"Ход: Нолики ({robotName})";
                lblWalker.ForeColor = System.Drawing.Color.Blue;
            }
        }

        // Общий обработчик кликов по кнопкам (только для игрока)
        private void HandleButtonClick(Button button)
        {
            // Если игра завершена или не ход игрока - игнорируем
            if (gameEnded || !isPlayerXTurn || button.Text != "")
                return;

            // Ход игрока (крестики)
            MakeMove(button, "X");

            // Проверяем результат после хода игрока
            CheckGameResult();

            // Если игра продолжается и не кончилась - ходит робот
            if (!gameEnded && !isPlayerXTurn)
            {
                // Небольшая задержка для естественности
                Task.Delay(500).ContinueWith(_ =>
                {
                    // Вызываем ход робота в UI потоке
                    this.Invoke(new Action(RobotMove));
                }, TaskScheduler.Default);
            }
        }

        // Сделать ход
        private void MakeMove(Button button, string symbol)
        {
            button.Text = symbol;
            button.ForeColor = symbol == "X" ? System.Drawing.Color.Red : System.Drawing.Color.Blue;
            moveCount++;
        }

        // Ход робота
        private void RobotMove()
        {
            if (gameEnded || isPlayerXTurn)
                return;

            // 1. Попробовать выиграть (поставить O в выигрышную позицию)
            Button winningMove = FindWinningMove("O");
            if (winningMove != null)
            {
                MakeMove(winningMove, "O");
                CheckGameResult();
                return;
            }

            // 2. Попробовать заблокировать игрока (помешать X выиграть)
            Button blockingMove = FindWinningMove("X");
            if (blockingMove != null)
            {
                MakeMove(blockingMove, "O");
                CheckGameResult();
                return;
            }

            // 3. Если центр свободен - занять центр
            if (button5.Text == "")
            {
                MakeMove(button5, "O");
                CheckGameResult();
                return;
            }

            // 4. Занять любой свободный угол
            List<Button> corners = new List<Button> { button1, button3, button7, button9 };
            List<Button> freeCorners = corners.FindAll(b => b.Text == "");

            if (freeCorners.Count > 0)
            {
                Random rand = new Random();
                MakeMove(freeCorners[rand.Next(freeCorners.Count)], "O");
                CheckGameResult();
                return;
            }

            // 5. Занять любую свободную сторону
            List<Button> sides = new List<Button> { button2, button4, button6, button8 };
            List<Button> freeSides = sides.FindAll(b => b.Text == "");

            if (freeSides.Count > 0)
            {
                Random rand = new Random();
                MakeMove(freeSides[rand.Next(freeSides.Count)], "O");
                CheckGameResult();
                return;
            }
        }

        // Найти выигрышный ход для указанного символа
        private Button FindWinningMove(string symbol)
        {
            // Проверяем все свободные клетки
            foreach (Button btn in allButtons)
            {
                if (btn.Text == "")
                {
                    // Пробуем поставить символ
                    btn.Text = symbol;

                    // Проверяем, будет ли это выигрышный ход
                    string tempWinner = Proverka();

                    // Возвращаем клетку в исходное состояние
                    btn.Text = "";

                    // Если это выигрышный ход - возвращаем кнопку
                    if (tempWinner == symbol)
                        return btn;
                }
            }

            return null;
        }

        // Проверить результат игры и обновить состояние
        private void CheckGameResult()
        {
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
                    MessageBox.Show($"{robotName} победил! Нолики выиграли!", "Победа!",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                UpdateScoreDisplay();

                // Предложить новую игру
                AskForNewGame();
            }
            else if (moveCount == 9)
            {
                gameEnded = true;
                MessageBox.Show("Ничья! Все поле заполнено.", "Игра окончена",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Предложить новую игру
                AskForNewGame();
            }
            else
            {
                // Меняем ход
                isPlayerXTurn = !isPlayerXTurn;
                UpdateTurnDisplay();
            }
        }

        // Спросить, играть ли еще раз
        private void AskForNewGame()
        {
            DialogResult result = MessageBox.Show("Хотите сыграть еще раз?", "Новая игра",
                                                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                ResetGame();
            }
            else
            {
                // Отключаем все кнопки
                foreach (Button btn in allButtons)
                {
                    btn.Enabled = false;
                }
            }
        }

        // Проверка на победу (без изменений)
        private string Proverka()
        {
            // Строки
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

            // Столбцы
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

            // Диагонали
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

        // Подсветка выигрышной комбинации
        private void HighlightWinner(Button btn1, Button btn2, Button btn3)
        {
            btn1.BackColor = btn2.BackColor = btn3.BackColor = System.Drawing.Color.LightGreen;
        }

        // Обработчики кликов (оставляем как есть)
        private void button1_Click(object sender, EventArgs e) => HandleButtonClick(button1);
        private void button2_Click(object sender, EventArgs e) => HandleButtonClick(button2);
        private void button3_Click(object sender, EventArgs e) => HandleButtonClick(button3);
        private void button4_Click(object sender, EventArgs e) => HandleButtonClick(button4);
        private void button5_Click(object sender, EventArgs e) => HandleButtonClick(button5);
        private void button6_Click(object sender, EventArgs e) => HandleButtonClick(button6);
        private void button7_Click(object sender, EventArgs e) => HandleButtonClick(button7);
        private void button8_Click(object sender, EventArgs e) => HandleButtonClick(button8);
        private void button9_Click(object sender, EventArgs e) => HandleButtonClick(button9);

        // Кнопка новой игры (если добавите на форму)
        private void btnNewGame_Click(object sender, EventArgs e)
        {
            ResetGame();
        }

        // Кнопка сброса счета (если добавите на форму)
        private void btnResetScore_Click(object sender, EventArgs e)
        {
            playerXScore = 0;
            playerOScore = 0;
            ResetGame();
        }
    }
}