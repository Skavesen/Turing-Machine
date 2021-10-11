using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace Algorithm
{
    public partial class TuringMachine : Form
    {
        private static List<char> Q = new List<char>() { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'z' }; //состояния
        private static List<char> First = new List<char>();//массив первой ленты
        private static List<char> Second = new List<char>();//массив второй ленты
        private static List<char> Third = new List<char>();//массив третьей ленты
        private static short qn = 0;//Текущее состояние
        private static int fn = 0;//Текущая позиция на первой ленте
        private static int sn = 0;//Текущая позиция на второй ленте
        private static int thn = 0;//Текущая позиция на второй ленте

        private static long tn = -1;//Количество тактов в данном слове
        private static long tnmax = 0;//Максимальное количество тактов для слов определенной длины

        private static bool Activation = false;//Активация подробной прорисовки алгоритма
        private static Thread Algo; //Дополнительный поток
        private static int Sleep_time = 500; //Время остановки на одной ячейке

        private static int Words_all = 0; //Количество всех слов
        private static int Words_count = 0;//Текущее слово по номеру
        private static bool Check = false; //Показатель того, подходит слово алфавиту или нет

        public TuringMachine()
        {
            InitializeComponent();
            //Инициализация лент на 10 ячеек
            for (int i = 0; i < 10; i++)
            {
                dataGridView1.RowHeadersVisible = false;
                dataGridView1.ColumnHeadersVisible = false;
                dataGridView1.Columns.Add("", "");
                dataGridView1[i, 0].Value = "λ";
                dataGridView1.Columns[i].Width = 40;
                dataGridView1.Rows[0].Height = 40;

                dataGridView2.RowHeadersVisible = false;
                dataGridView2.ColumnHeadersVisible = false;
                dataGridView2.Columns.Add("", "");
                dataGridView2[i, 0].Value = "λ";
                dataGridView2.Columns[i].Width = 40;
                dataGridView2.Rows[0].Height = 40;

            }
        }
        //Чтение из файла
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //Читаем данные из файла
                if (!File.Exists("C:\\Word.txt"))
                    throw new Exception("Ошибка доступа к файлу!");
                FileStream file1 = new FileStream("C:\\Word.txt", FileMode.Open);
                StreamReader sr = new StreamReader(file1);
                String Word = sr.ReadToEnd();
                file1.Close();
                //Перенос считанных данных в ленты
                ReadValue(Word);
                label1.Text = "Чтение выполнено!";
            }
            catch (Exception ex)
            {
                label1.Text = ex.Message;
            }
        }
        //Занесение слова
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                //Занесение считанных данных в ленты
                ReadValue(textBox1.Text);
                label1.Text = "Занесение выполнено!";
            }
            catch (Exception ex)
            {
                label1.Text = ex.Message;
            }
        }

        //Чтение данных
        void ReadValue(String Word)
        {
            //Если слово пустое, то вывести очищенную ленту
            if (Word.Length == 0)
                ClearTape();
            else
            {
                //Проверка на правильность данных
                for (int k = 0; k < Word.Length; k++)
                {
                        if (Word[k] != '0' && Word[k] != '1' && Word[k] != '*')
                        throw new Exception("Нельзя вводить такие символы!");
                }
                //Очистка лент
                ClearTape();
                //Проверка на абстрактную длину ленты
                if (Word.Length >= dataGridView1.ColumnCount - 1)
                {
                    dataGridView1.ColumnCount = 0;
                    dataGridView2.ColumnCount = 0;
                    for (int i = 0; i < Word.Length + 2; i++)
                    {
                        dataGridView1.Columns.Add("", "");
                        dataGridView1.Columns[i].Width = 40;
                        dataGridView1[i, 0].Value = "λ";
                        dataGridView1.Rows[0].Height = 40;
                        dataGridView2.Columns.Add("", "");
                        dataGridView2.Columns[i].Width = 40;
                        dataGridView2[i, 0].Value = "λ";
                        dataGridView2.Rows[0].Height = 40;

                    }
                }
                //заполнение ленты
                for (int i = 0; i < Word.Length; i++)
                    dataGridView1[i + 1, 0].Value = Word[i];
            }
        }
        //Полная очистка лент
        void ClearTape()
        {
            for (int i = 0; i < dataGridView1.ColumnCount; i++)
            {
                dataGridView1[i, 0].Style.BackColor = System.Drawing.Color.White;
                dataGridView2[i, 0].Style.BackColor = System.Drawing.Color.White;
                dataGridView1[i, 0].Value = "λ";
                dataGridView2[i, 0].Value = "λ";

            }
        }
        //Выполнение алгоритма на 1 ленте
        private void button2_Click(object sender, EventArgs e)
        {
            textBox3.Clear();
            Algo = new Thread(AlgorithmForOne);
            Algo.Start();
        }


        //Постройка графика из другого потока        
        void BuildChart(int lenght, long max)
        {
            chart1.Series[0].Points.AddXY(lenght, tnmax);
        }

        //Запись в label с другого потока
        void WriteLabel(string Text)
        {
            label1.Text = Text;
        }
        //Запись в textBox3 с другого потока
        void WriteTextBox3(string Text)
        {
            textBox3.Text = Text;
        }
       
        //Установка максимального значения для progressbar с другого потока
        void ProgressBarSetMax()
        {
            progressBar1.Maximum = Words_all;
        }
        
        //Установка текущего значения для progressbar с другого потока
        void ProgressBarSetValue(int count)
        {
            progressBar1.Value = count;
        }

        //Изменение значения остановки на ячейке при перемещении ползунка trackbar
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            Sleep_time = trackBar1.Value;
            textBox4.Text = Convert.ToString(trackBar1.Value);
        }
    
        //Блокировка кнопок 
        void BlockButtons()
        {
            if (button1.Enabled == true) button1.Enabled = false; else button1.Enabled = true;
            if (button2.Enabled == true) button2.Enabled = false; else button2.Enabled = true;
            if (button3.Enabled == true) button3.Enabled = false; else button3.Enabled = true;
            if (button4.Enabled == true) button4.Enabled = false; else button4.Enabled = true;
            if (button5.Enabled == true) button5.Enabled = false; else button5.Enabled = true;
            if (button7.Enabled == true) button7.Enabled = false; else button7.Enabled = true;
        }

        //Чтение данных с лент в массивы для алгоритма
        void ReRead()
        {
            //Полная очистка отрисовки цветом лент
            for (int j = 0; j < dataGridView1.ColumnCount; j++)
            {                                                           //System.Drawing.
                dataGridView1[j, 0].Style.BackColor = Color.White;
                dataGridView2[j, 0].Style.BackColor = Color.White;
            }
            //Подготовка для входа в алгоритм
            qn = 0;
            fn = 1;
            sn = 1;
            thn = 1;
            tn = -1;
            tnmax = 0;

            //Очистка массивов лент
            First.Clear();
            Second.Clear();
            Third.Clear();

            //Занесение данных первой ленты в массив
            First.Add('λ');
            int i = 1;
            for (; i < dataGridView1.ColumnCount; i++)
            {
                if (Convert.ToString(dataGridView1[i, 0].Value) != "λ")
                    First.Add(Convert.ToChar(dataGridView1[i, 0].Value));
                else
                    break;
            }
            First.Add('λ');

            //Занесение во второй массив пустых символов
            for (int k = 0; k < First.Count; k++)
                Second.Add('λ');
            //Занесение в третий массив пустых символов
            for (int k = 0; k < First.Count; k++)
                Third.Add('λ');
        }

        // Занесение данных на ленту после работы алгоритма с массивов
        void ReWrite()
        {
            //Полная очистка отрисовки лент цветом
            for (int i = 0; i < dataGridView1.ColumnCount; i++)
            {
                dataGridView1[i, 0].Style.BackColor = Color.White;
                dataGridView2[i, 0].Style.BackColor = Color.White;
            }

            //Занесение данных на ленты
            for (int i = 0; i < First.Count - 1; i++)
                dataGridView1[i, 0].Value = First[i];
            for (int i = 0; i < Second.Count - 1; i++)
                dataGridView2[i, 0].Value = Second[i];
        }

        //Перезапись лент и прорисовка текущей ячейки на каждом такте
        void ReWriteTape()
        {
            //Затирание старого выделения
            for (int i = 0; i < dataGridView1.ColumnCount; i++)
            {
                dataGridView1[i, 0].Style.BackColor = Color.White;
                dataGridView2[i, 0].Style.BackColor = Color.White;
            }

            //Перезапись на ленту новых данных
            for (int i = 0; i < First.Count; i++)
                dataGridView1[i, 0].Value = First[i];

            //Новое выделение
            dataGridView1[fn, 0].Style.BackColor = Color.Blue;
  

            //Вывод текущего состояния
            label1.Invoke(new Action<string>(WriteLabel), "Текущее состояние: q" + Convert.ToString(Q[qn]));
        }
        void ReWriteTape2()
        {//Затирание старого выделения
            for (int i = 0; i < dataGridView1.ColumnCount; i++)
            {
                dataGridView1[i, 0].Style.BackColor = Color.White;
                dataGridView2[i, 0].Style.BackColor = Color.White;
            }

            //Перезапись на ленту новых данных
            for (int i = 0; i < First.Count; i++)
                dataGridView1[i, 0].Value = First[i];

            //Новое выделение
            dataGridView1[fn, 0].Style.BackColor = Color.Blue;


            //Вывод текущего состояния
            label1.Invoke(new Action<string>(WriteLabel), "Текущее состояние: q1" + Convert.ToString(Q[qn]));
        }
        void ReWriteTape3()
        {
            //Затирание старого выделения
            for (int i = 0; i < dataGridView1.ColumnCount; i++)
            {
                dataGridView1[i, 0].Style.BackColor = Color.White;
                dataGridView2[i, 0].Style.BackColor = Color.White;
            }

            //Перезапись на ленту новых данных
            for (int i = 0; i < First.Count; i++)
                dataGridView1[i, 0].Value = First[i];
            for (int i = 0; i < Second.Count; i++)
                dataGridView2[i, 0].Value = Second[i];

            //Новое выделение
            dataGridView1[fn, 0].Style.BackColor = Color.Blue;
            dataGridView2[sn, 0].Style.BackColor = Color.Green;

            //Вывод текущего состояния
            label1.Invoke(new Action<string>(WriteLabel), "Текущее состояние: q" + Convert.ToString(Q[qn]));
        }
        void ReWriteTape4()
        {
            //Затирание старого выделения
            for (int i = 0; i < dataGridView1.ColumnCount; i++)
            {
                dataGridView1[i, 0].Style.BackColor = Color.White;
                dataGridView2[i, 0].Style.BackColor = Color.White;
            }

            //Перезапись на ленту новых данных
            for (int i = 0; i < First.Count; i++)
                dataGridView1[i, 0].Value = First[i];
            for (int i = 0; i < Second.Count; i++)
                dataGridView2[i, 0].Value = Second[i];

            //Новое выделение
            dataGridView1[fn, 0].Style.BackColor = Color.Blue;
            dataGridView2[sn, 0].Style.BackColor = Color.Green;

            //Вывод текущего состояния
            label1.Invoke(new Action<string>(WriteLabel), "Текущее состояние: q1" + Convert.ToString(Q[qn]));
        }
        //Вывод трассировки
        void WriteTracing()
        {
            textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + "--------------------------------------" + Environment.NewLine + "Первая:    ");
            for (int i = 0; i < First.Count; i++)
                textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + First[i] + " ");
            textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + "    Позиция: " + (fn + 1));   
            textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + Environment.NewLine + "Состояние: q" + Q[qn] + Environment.NewLine + "Такт:" + (tn + 1) + Environment.NewLine);
        }
        void WriteTracing2()
        {
            textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + "--------------------------------------" + Environment.NewLine + "Первая:    ");
            for (int i = 0; i < First.Count; i++)
                textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + First[i] + " ");
            textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + "    Позиция: " + (fn + 1));
            textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + Environment.NewLine + "Состояние: q1" + Q[qn] + Environment.NewLine + "Такт:" + (tn + 1) + Environment.NewLine);
        }
        void WriteTracing3()
        {
            {
                textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + "--------------------------------------" + Environment.NewLine + "Первая:    ");
                for (int i = 0; i < First.Count; i++)
                    textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + First[i] + " ");
                textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + "    Позиция: " + (fn + 1) + Environment.NewLine + "Вторая:    ");
                for (int i = 0; i < Second.Count; i++)
                    textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + Second[i] + " ");
                textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + "    Позиция: " + (sn + 1)  + Environment.NewLine +
                    "Состояние: q" + Q[qn] + Environment.NewLine + "Такт:" + (tn + 1) + Environment.NewLine);
            }
        }
        void WriteTracing4()
        {
            {
                textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + "--------------------------------------" + Environment.NewLine + "Первая:    ");
                for (int i = 0; i < First.Count; i++)
                    textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + First[i] + " ");
                textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + "    Позиция: " + (fn + 1) + Environment.NewLine + "Вторая:    ");
                for (int i = 0; i < Second.Count; i++)
                    textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + Second[i] + " ");
                textBox3.Invoke(new Action<string>(WriteTextBox3), textBox3.Text + "    Позиция: " + (sn + 1) + Environment.NewLine +
                    "Состояние: q1" + Q[qn] + Environment.NewLine + "Такт:" + (tn + 1) + Environment.NewLine);
            }
        }

        //Алгоритм для 1 ленты (подготовка на запуск и очистка лишних данных после работы)
        void AlgorithmForOne()
        {
            //chart1.Invoke(new Action(BuildChart));            
            button1.Invoke(new Action(BlockButtons));
            label1.Invoke(new Action<string>(WriteLabel), "Алгоритм начал работу!");
            File.WriteAllText("C:\\Log_one.txt", "");
            Activation = true;
            ReRead();
            Algorithm();
            Algorithm2();
            ReWrite();
            Activation = false;
            Check = false;
            label1.Invoke(new Action<string>(WriteLabel), "Алгоритм выполнен!");
            button1.Invoke(new Action(BlockButtons));
        }
        void Algorithm()
        {
            if (Activation == true)
            {
                ReWriteTape();
                WriteTracing();
                Log();
                Algo.Join(Sleep_time);
            }
            tn++;
            switch (Q[qn])
            {
                case '0':
                    if (First[fn] == '0')
                    {
                        First[fn] = '0';
                        fn++;
                        qn = 1;
                        Algorithm();
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = '1';
                        fn++;
                        qn = 1;
                        Algorithm();
                    }
                    else if (First[fn] == '*')//1ое слово прошло проверку->сост.где проверяется на нечётность 2ое слово
                    {
                        First[fn] = '*';
                        fn++;
                        qn = 9;
                        Algorithm();
                    }
                    else if (First[fn] == 'λ')
                    {
                        First[fn] = '+';
                        Check = true;
                    }
                    break;
                case '1':
                    if (First[fn] == '0')
                    {
                        First[fn] = '0';
                        fn++;
                        qn = 2;
                        Algorithm();
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = '1';
                        fn++;
                        qn = 2;
                        Algorithm();
                    }
                    else if (First[fn] == '*')//1ое слово не прошло проверку->сост. где мы идём в конец->сост. где мы всё стираем влево
                    {
                        First[fn] = '*';
                        fn++;
                        qn = 3;
                        Algorithm();
                    }
                    else if (First[fn] == 'λ')//1ое слово не прошло проверку->сост. где мы всё стираем влево
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 4;
                        Algorithm();
                    }
                    break;
                case '2':
                    if (First[fn] == '1')
                    {
                        First[fn] = '1';
                        fn++;
                        qn = 1;
                        Algorithm();
                    }
                    else if (First[fn] == '0')
                    {
                        First[fn] = '0';
                        fn++;
                        qn = 1;
                        Algorithm();
                    }
                    else if (First[fn] == '*')//1 слово прошло проверку->сост. где мы меняем на "а"
                    {
                        First[fn] = '*';
                        fn--;
                        qn = 5;
                        Algorithm();
                    }
                    else if (First[fn] == 'λ')//1ое слово не прошло проверку->сост. где мы всё стираем влево
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 4;
                        Algorithm();
                    }
                    break;
                case '3':
                    if (First[fn] == '0')
                    {
                        First[fn] = '0';
                        fn++;
                        qn = 3;
                        Algorithm();
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = '1';
                        fn++;
                        qn = 3;
                        Algorithm();
                    }
                    else if (First[fn] == 'λ')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 4;
                        Algorithm();
                    }
                    break;
                case '4'://состояние стирания влево
                    if (First[fn] == '1')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 4;
                        Algorithm();
                    }
                    else if (First[fn] == '0')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 4;
                        Algorithm();
                    }
                    else if (First[fn] == '*')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 4;
                        Algorithm();
                    }
                    else if (First[fn] == 'a')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 4;
                        Algorithm();
                    }
                    else if (First[fn] == 'λ')
                    {
                        First[fn] = '-';
                    }
                    break;
                case '5':
                    if (First[fn] == '0')
                    {
                        First[fn] = 'a';
                        fn++;
                        qn = 6;
                        Algorithm();
                    }
                    else if (First[fn] == 'a')
                    {
                        First[fn] = 'a';
                        fn--;
                        qn = 5;
                        Algorithm();
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = 'a';
                        fn++;
                        qn = 6;
                        Algorithm();
                    }
                    else if (First[fn] == '*')
                    {
                        First[fn] = '*';
                        fn--;
                        qn = 5;
                        Algorithm();
                    }
                    else if (First[fn] == 'λ')//пример вида 0000*11111
                    {
                        First[fn] = 'λ';
                        fn++;
                        qn = 9;
                        Algorithm();
                    }
                    break;
                case '6':
                    if (First[fn] == '0')
                    {
                        First[fn] = 'a';
                        fn--;
                        qn = 5;
                        Algorithm();
                    }
                    else if (First[fn] == 'a')
                    {
                        First[fn] = 'a';
                        fn++;
                        qn = 6;
                        Algorithm();
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = 'a';
                        fn--;
                        qn = 5;
                        Algorithm();
                    }
                    else if (First[fn] == '*')
                    {
                        First[fn] = '*';
                        fn++;
                        qn = 6;
                        Algorithm();
                    }
                    else if (First[fn] == 'λ')//пример вида 0000*111
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 7;
                        Algorithm();
                    }
                    break;
                case '7':
                    if (First[fn] == 'a')
                    {
                        First[fn] = 'a';
                        fn--;
                        qn = 8;
                        Algorithm();
                    }
                    else if (First[fn] == '*')
                    {
                        First[fn] = '*';
                        fn++;
                        qn = 2;
                        Algorithm2();
                    }
                    break;
                case '8':
                    if (First[fn] == 'a')
                    {
                        First[fn] = 'a';
                        fn--;
                        qn = 7;
                        Algorithm();
                    }
                    else if (First[fn] == '*')
                    {
                        First[fn] = '*';
                        fn++;
                        qn = 4;
                        Algorithm2();
                    }
                    break;
                case '9':
                    if (First[fn] == 'a')
                    {
                        First[fn] = 'a';
                        fn++;
                        qn = 9;
                        Algorithm();
                    }
                    else if (First[fn] == '0')
                    {
                        First[fn] = '0';
                        fn++;
                        qn = 0;
                        Algorithm2();
                    }
                    else if (First[fn] == '*')
                    {
                        First[fn] = '*';
                        fn++;
                        qn = 9;
                        Algorithm();
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = '1';
                        fn++;
                        qn = 0;
                        Algorithm2();
                    }
                    else if (First[fn] == 'λ')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 4;
                        Algorithm();
                    }
                    break;
                case 'z':
                    break;

            }
        }
        void Algorithm2()
        {
            if (Activation == true)
            {
                ReWriteTape2();
                WriteTracing2();
                Log2();
                Algo.Join(Sleep_time);
            }
            tn++;
            switch (Q[qn])
            {
                case '0':
                    if (First[fn] == '1')
                    {
                        First[fn] = '1';
                        fn++;
                        qn = 1;
                        Algorithm2();
                    }
                    else if (First[fn] == 'λ')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 4;
                        Algorithm();
                    }
                    else if (First[fn] == '0')
                    {
                        First[fn] = '0';
                        fn++;
                        qn = 1;
                        Algorithm2();
                    }
                    break;
                case '1':
                    if (First[fn] == '1')
                    {
                        First[fn] = '1';
                        fn++;
                        qn = 5;
                        Algorithm2();
                    }
                    else if (First[fn] == 'λ')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 4;
                        Algorithm();
                    }
                    else if (First[fn] == '0')
                    {
                        First[fn] = '0';
                        fn++;
                        qn = 5;
                        Algorithm2();
                    }
                    break;
                case '2':
                    if (First[fn] == 'a')
                    {
                        First[fn] = 'a';
                        fn++;
                        qn = 2;
                        Algorithm2();
                    }
                    else if (First[fn] == 'λ')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 4;
                        Algorithm();
                    }
                    break;
                case '3':
                    if (First[fn] == 'λ')
                    {
                        First[fn] = '+';
                        Check = true;
                    }
                    else if (First[fn] == 'a')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 3;
                        Algorithm2();
                    }
                    else if (First[fn] == '0')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 3;
                        Algorithm2();
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 3;
                        Algorithm2();
                    }
                    else if (First[fn] == '*')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 3;
                        Algorithm2();
                    }
                    break;
                case '4':
                    if (First[fn] == 'λ')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 3;
                        Algorithm2();
                    }
                    else if (First[fn] == 'a')
                    {
                        First[fn] = 'a';
                        fn++;
                        qn = 4;
                        Algorithm2();
                    }
                    break;
                case '5':
                    if (First[fn] == '1')
                    {
                        First[fn] = '1';
                        fn++;
                        qn = 1;
                        Algorithm2();
                    }
                    else if (First[fn] == 'λ')
                    {
                        First[fn] = 'λ';
                        fn--;
                        qn = 3;
                        Algorithm2();
                    }
                    else if (First[fn] == '0')
                    {
                        First[fn] = '0';
                        fn++;
                        qn = 1;
                        Algorithm2();
                    }
                    break;
            }
        }
        //Алгоритм для 3 лент
        private void button7_Click(object sender, EventArgs e)
        {
            textBox3.Clear();
            Algo = new Thread(AlgorithmForThree);
            Algo.Start();
        }
        void AlgorithmForThree()
        {
            button1.Invoke(new Action(BlockButtons));
            label1.Invoke(new Action<string>(WriteLabel), "Алгоритм начал работу!");
            File.WriteAllText("C:\\Log_two.txt", "");
            Activation = true;
            ReRead();
            Algorithm3();
            ReWrite();
            Activation = false;
            Check = false;
            label1.Invoke(new Action<string>(WriteLabel), "Алгоритм выполнен!");
            button1.Invoke(new Action(BlockButtons));
        }
       void Algorithm3()
        {
            if (Activation == true)
            {
                ReWriteTape3();
                WriteTracing3();
                Log3();
                Algo.Join(Sleep_time);
            }
            tn++;
            switch (Q[qn])
            {
                case '0':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = '1';
                        fn++; sn++;
                        qn = 1;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = '1';
                        fn++; sn++;
                        qn = 1;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = '+'; Second[sn] = 'λ';
                        Check = true;
                        Algorithm3();
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        fn++;
                        qn = 3;
                        Algorithm3();
                    }
                    break;
                case '1':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = '1';
                        fn++; sn++;
                        qn = 2;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = '1';
                        fn++; sn++;
                        qn = 2;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--; sn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        sn--;
                        qn = 9;
                        Algorithm3();
                    }
                    break;
                case '2':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = '1';
                        fn++; sn++;
                        qn = 1;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = '1';
                        fn++; sn++;
                        qn = 1;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--; sn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        fn++; sn--;
                        qn = 3;
                        Algorithm3();
                    }
                    break;
                case '3':
                    if (First[fn] == '0' && Second[sn] == '1')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        fn++; sn--;
                        qn = 4;
                        Algorithm3();
                    }
                    else if(First[fn] == '1' && Second[sn] == '1')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        fn++; sn--;
                        qn = 4;
                        Algorithm3();
                    }
                    else if(First[fn] == 'λ' && Second[sn] == '1')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--; sn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == '*' && Second[sn] == '1')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        sn--;
                        qn = 9;
                        Algorithm3();
                    }
                    else if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = '*';
                        fn++;
                        qn = 4;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = '*';
                        fn++;
                        qn = 4;
                        Algorithm3();
                    }             
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 9;
                        Algorithm3();
                    }
                    break;
                case '4':
                    if (First[fn] == '0' && Second[sn] == '1')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        fn++; sn--;
                        qn = 5;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == '1')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        fn++; sn--;
                        qn = 5;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == '1')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--; sn--;
                        qn = 1;
                        Algorithm4();
                    }
                    else if (First[fn] == '*' && Second[sn] == '1')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        sn--;
                        qn = 9;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == '*')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--; sn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == '0' && Second[sn] == '*')
                    {
                        First[fn] = '0'; Second[sn] = '0';
                        fn++; 
                        qn = 5;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == '*')
                    {
                        First[fn] = '1'; Second[sn] = '0';
                        fn++;
                        qn = 5;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == '0')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 1;
                        Algorithm4();
                    }
                    else if (First[fn] == '0' && Second[sn] == '0')
                    {
                        First[fn] = '0'; Second[sn] = '0';
                        fn++;
                        qn = 5;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == '0')
                    {
                        First[fn] = '1'; Second[sn] = '0';
                        fn++;
                        qn = 5;
                        Algorithm3();
                    }
                    break;
                case '5':
                    if (First[fn] == '0' && Second[sn] == '1')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        fn++; sn--;
                        qn = 4;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == '1')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        fn++; sn--;
                        qn = 4;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == '1')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--; sn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 0;
                        Algorithm4();
                    }                    
                    else if (First[fn] == '0' && Second[sn] == '0')
                    {
                        First[fn] = '0'; Second[sn] = '0';
                        fn++;
                        qn = 4;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == '0')
                    {
                        First[fn] = '1'; Second[sn] = '0';
                        fn++;
                        qn = 4;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == '0')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--; sn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        fn++;
                        qn = 6;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        fn++;
                        qn = 6;
                        Algorithm3();
                    }
                    break;
                case '6':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        fn++;
                        qn = 7;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        fn++;
                        qn = 7;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 9;
                        Algorithm3();
                    }
                    break;
                case '7':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        fn++;
                        qn = 8;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        fn++;
                        qn = 8;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 9;
                        Algorithm3();
                    }
                    break;
                case '8':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        fn++;
                        qn = 7;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        fn++;
                        qn = 7;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 1;
                        Algorithm4();
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 9;
                        Algorithm3();
                    }
                    break;
                case '9':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        fn++;
                        qn = 9;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        fn++;
                        qn = 9;
                        Algorithm3();
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        fn++;
                        qn = 9;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == '0' && Second[sn] == '1')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        fn++; sn--;
                        qn = 9;
                        Algorithm3();
                    }
                    else if (First[fn] == '1' && Second[sn] == '1')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        fn++; sn--;
                        qn = 9;
                        Algorithm3();
                    }
                    else if (First[fn] == '*' && Second[sn] == '1')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        fn++; sn--;
                        qn = 9;
                        Algorithm3();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        sn--;
                        qn = 9;
                        Algorithm3();
                    }
                    break;

            }
        }
        void Algorithm4()
        {
            if (Activation == true)
            {
                ReWriteTape4();
                WriteTracing4();
                Log4();
                Algo.Join(Sleep_time);
            }
            tn++;
            switch (Q[qn])
            {
                
                case '0':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 0;
                        Algorithm4();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = '-'; Second[sn] = 'λ';
                        Algorithm4();
                    }
                    break;
                case '1':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 1;
                        Algorithm4();
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 1;
                        Algorithm4();
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--;
                        qn = 1;
                        Algorithm4();
                    }
                    else if (First[fn] == '1' && Second[sn] == '1')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--; sn--;
                        qn = 1;
                        Algorithm4();
                    }
                    else if (First[fn] == '*' && Second[sn] == '1')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--; sn--;
                        qn = 1;
                        Algorithm4();
                    }
                     else if (First[fn] == '0' && Second[sn] == '1')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        fn--; sn--;
                        qn = 1;
                        Algorithm4();
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = '+'; Second[sn] = 'λ';
                        Check = true;
                        Algorithm4();
                    }
                    break;
            }
        }
        //Запуск перебора
        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox2.Text.Length == 0)
                    throw new Exception("Не введена максимальная длина слова!");
                if (Convert.ToInt32(textBox2.Text) > 10)
                    throw new Exception("Нельзя вводить такую большую длину!");
                //Очистка перед стартом графика
                chart1.Series[0].Points.Clear();
                //Запуск перебора
                Algo = new Thread(Bust);
                Algo.Start();
            }
            catch (Exception ex)
            {
                label1.Text = ex.Message;
            }
        }


        //Проверка вводимых данных на число
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Char.IsDigit(e.KeyChar)) return;
            else if (e.KeyChar == 8) return;
            else e.Handled = true;
        }

        //Алгоритм
        //
        void Algorithm6()
        {
            if (Activation == true)
            {
                ReWriteTape();
                WriteTracing();
                Log();
                Algo.Join(Sleep_time);
            }
            tn++;
            switch (Q[qn])
            {
                case '0':
                    if (First[fn] == '0')
                    {
                        First[fn] = '0'; 
                        fn++; 
                        qn = 2;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = '1';
                        fn++;
                        qn = 2;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*')
                    {
                        First[fn] = '*'; 
                        fn++; 
                        qn = 2;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == 'λ')
                    {
                        First[fn] = 'λ'; 
                        qn = 7; Check = true;
                        Algorithm6(); break;
                    }
                    break;
                case '1':
                    if (First[fn] == '0')
                    {
                        First[fn] = '0'; 
                        qn = 2;
                        fn++; 
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1' )
                    {
                        First[fn] = '1'; 
                        qn = 2;
                        fn++; 
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '*'; 
                        qn = 2;
                        fn++; 
                        Algorithm6(); break;
                    }
                    else if (First[fn] == 'λ' )
                    {
                        First[fn] = 'λ'; 
                        qn = 4;
                        fn--; 
                        Algorithm6(); break;
                    }
                    break;
                case '2':
                    if (First[fn] == '0' )
                    {
                        First[fn] = '0'; 
                        qn = 1;
                        fn++;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1' )
                    {
                        First[fn] = '1';
                        qn = 1;
                        fn++;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '*';
                        qn = 1;
                        fn++;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == 'λ' )
                    {
                        First[fn] = 'λ';
                        qn = 3;
                        fn--;
                        Algorithm6(); break;
                    }
                    break;
                case '3':
                    if (First[fn] == '0' )
                    {
                        First[fn] = '0'; 
                        qn = 3;
                        fn--; 
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = '1'; 
                        qn = 3;
                        fn--; 
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*')
                    {
                        First[fn] = '*'; 
                        qn = 3;
                        fn--; 
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '0' )
                    {
                        First[fn] = '-'; 
                        qn = 7;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = '-'; 
                        qn = 7;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '-';
                        qn = 7;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == 'λ' )
                    {
                        First[fn] = '-';
                        qn = 7;
                        Algorithm6(); break;
                    }
                    break;
                case '4':
                    if (First[fn] == '0' )
                    {
                        First[fn] = '0'; 
                        qn = 4;
                        fn--; 
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1' )
                    {
                        First[fn] = '1'; 
                        qn = 4;
                        fn--; 
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '-'; 
                        qn = 4;
                        fn--;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '0' )
                    {
                        First[fn] = '0'; 
                        qn = 4;
                        fn--;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1' )
                    {
                        First[fn] = '1'; 
                        qn = 4;
                        fn--;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '*'; 
                        qn = 4;
                        fn--;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == 'λ' )
                    {
                        First[fn] = 'λ'; 
                        qn = 5;
                        fn++; 
                        Algorithm6(); break;
                    }
                    break;
                case '5':
                    if (First[fn] == '0' )
                    {
                        First[fn] = '0'; 
                        qn = 5;
                        fn++; 
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1' )
                    {
                        First[fn] = '1'; 
                        qn = 5;
                        fn++; 
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*')
                    {
                        First[fn] = '*'; 
                        qn = 5;
                        fn++; sn++;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '0')
                    {
                        First[fn] = '+'; 
                        qn = 7; Check = true;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1' )
                    {
                        First[fn] = '+';
                        qn = 7; Check = true;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '+'; 
                        qn = 7; Check = true;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '0')
                    {
                        First[fn] = '0'; 
                        qn = 6;
                         fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = '1';
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '*'; 
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '0')
                    {
                        First[fn] = '0'; 
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '*'; 
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    break;
                case '6':
                    if (First[fn] == '0' )
                    {
                        First[fn] = '0'; 
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1' )
                    {
                        First[fn] = '1'; 
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '*'; 
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '0' )
                    {
                        First[fn] = '0';
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1' )
                    {
                        First[fn] = '1';
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '*';
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '0' )
                    {
                        First[fn] = '0'; 
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1' )
                    {
                        First[fn] = '1'; 
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '*'; 
                        qn = 6;
                        fn++;//s
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '0')
                    {
                        First[fn] = '-'; 
                        qn = 7;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '1' )
                    {
                        First[fn] = '-';
                        qn = 7;
                        Algorithm6(); break;
                    }
                    else if (First[fn] == '*' )
                    {
                        First[fn] = '-'; 
                        qn = 7;
                        Algorithm6(); break;
                    }
                    break;
                case 'z':
                    return;
            }
        }
        //
        void Algorithm5()
        {
            if (Activation == true)
            {
                ReWriteTape();
                WriteTracing();
                Log();
                Algo.Join(Sleep_time);
            }
            tn++;
            switch (Q[qn])
            {
                case '0':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = '1';
                        fn++; sn++;
                        qn = 2;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = '1';
                        fn++; sn++;
                        qn = 2;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = '1';
                        fn++; sn++;
                        qn = 2;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = '+';
                        qn = 7; Check = true;
                        Algorithm5(); break;
                    }
                    break;
                case '1':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = '1';
                        qn = 2;
                        fn++; sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = '1';
                        qn = 2;
                        fn++; sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = '1';
                        qn = 2;
                        fn++; sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        qn = 4;
                        fn--; sn--;
                        Algorithm5(); break;
                    }
                    break;
                case '2':
                    if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        qn = 1;
                        fn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        qn = 1;
                        fn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 1;
                        fn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        qn = 3;
                        fn--; sn--;
                        Algorithm5(); break;
                    }
                    break;
                case '3':
                    if (First[fn] == '0' && Second[sn] == '1')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        qn = 3;
                        fn--; sn--;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == '1')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        qn = 3;
                        fn--; sn--;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == '1')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 3;
                        fn--; sn--;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = '-';
                        qn = 7;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = '-';
                        qn = 7;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = '-';
                        qn = 7;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = '-';
                        qn = 7;
                        Algorithm5(); break;
                    }
                    break;
                case '4':
                    if (First[fn] == '0' && Second[sn] == '1')
                    {
                        First[fn] = '0'; Second[sn] = '0';
                        qn = 4;
                        fn--; sn--;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == '1')
                    {
                        First[fn] = '1'; Second[sn] = '1';
                        qn = 4;
                        fn--; sn--;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == '1')
                    {
                        First[fn] = '*'; Second[sn] = '*';
                        qn = 4;
                        fn--; sn--;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        qn = 4;
                        fn--;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        qn = 4;
                        fn--;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 4;
                        fn--;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == 'λ' && Second[sn] == 'λ')
                    {
                        First[fn] = 'λ'; Second[sn] = 'λ';
                        qn = 5;
                        fn++; sn++;
                        Algorithm5(); break;
                    }
                    break;
                case '5':
                    if (First[fn] == '0' && Second[sn] == '0')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        qn = 5;
                        fn++; sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == '1')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        qn = 5;
                        fn++; sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == '*')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 5;
                        fn++; sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = '+';
                        qn = 7; Check = true;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = '+';
                        qn = 7; Check = true;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = '+';
                        qn = 7; Check = true;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '0' && Second[sn] == '1')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == '*')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == '0')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '0' && Second[sn] == '*')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == '0')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == '1')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    break;
                case '6':
                    if (First[fn] == '0' && Second[sn] == '0')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == '1')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == '*')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '0' && Second[sn] == '1')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == '0')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == '0')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '0' && Second[sn] == '*')
                    {
                        First[fn] = '0'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == '*')
                    {
                        First[fn] = '1'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == '1')
                    {
                        First[fn] = '*'; Second[sn] = 'λ';
                        qn = 6;
                        sn++;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '0' && Second[sn] == 'λ')
                    {
                        First[fn] = '0'; Second[sn] = '-';
                        qn = 7;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '1' && Second[sn] == 'λ')
                    {
                        First[fn] = '1'; Second[sn] = '-';
                        qn = 7;
                        Algorithm5(); break;
                    }
                    else if (First[fn] == '*' && Second[sn] == 'λ')
                    {
                        First[fn] = '*'; Second[sn] = '-';
                        qn = 7;
                        Algorithm5(); break;
                    }
                    break;
                case 'z':
                    return;
            }
        }

        //Перебор слов
        void Bust()
        {
            button1.Invoke(new Action(BlockButtons));
            label1.Invoke(new Action<string>(WriteLabel), "Идет перебор... Ждите...");

            First.Clear();//слово
            tnmax = 0;//максимальное количество тактов для слов определенной длины
            int N = Convert.ToInt32(textBox2.Text); //Макс. длина слова

            //Формировка данных для вывода прогресса
            Words_all = 0;
            Words_count = 0;
            progressBar1.Invoke(new Action<int>(ProgressBarSetValue), 0);
            for (int curr = 0; curr <= N; ++curr)
                Words_all += Convert.ToInt32(Math.Pow(3, curr));
            progressBar1.Invoke(new Action(ProgressBarSetMax));

            File.WriteAllText("Log_Combinations.txt", " ");//Стираем данные с файла

            First.Add(' ');//Первое слово- пустое
            AlgorithmForManyWords();//Запуск подготовки на алгоритм для текущего слова

            //Отрисовка данных графика
            chart1.Invoke(new Action<int, long>(BuildChart), First.Count - 1, tnmax);

            bool flag = false;//Показатель того, что перебор для этой длины окончен
            int i = 0; //текущая позиция в массиве

            //Пока не равно указанной длине длина слова
            while (First.Count != N + 1)
            {
                if (flag == true)
                {
                    
                    for (int j = 0; j <= First.Count - 1; j++)
                        First[j] = '0';
                    First.Add('0');

                    //Отрисовка данных
                    chart1.Invoke(new Action<int, long>(BuildChart), First.Count - 1, tnmax);

                    //Если еще нужно сделать для этого слова алгоритм, запуск на старт алгоритма
                    if (First.Count != N + 1)
                    {
                        tnmax = 0;
                        AlgorithmForManyWords();
                    }

                    i = First.Count - 1;
                    flag = false;
                    continue;
                }           
                if (First[i] == '*')
                {
                    if (i == 0)
                    {
                        flag = true;
                        continue;
                    }                    
                    First[i] = '0';
                    i--;
                }               
                if (First[i] == ' ')
                {
                    First[i] = '0';
                    AlgorithmForManyWords();
                    i = First.Count - 1;
                    continue;
                }             
                else if (First[i] == '0')
                {
                    First[i] = '1';
                    AlgorithmForManyWords();
                    i = First.Count - 1;
                    continue;
                }
                else if (First[i] == '1')
                {
                    First[i] = '*';
                    AlgorithmForManyWords();
                    i = First.Count - 1;
                    continue;
                }
            }

            //Звуковое оповещение
            if (File.Exists("C:\\ready.wav"))
            {
                System.Media.SoundPlayer SoundPlayer = new System.Media.SoundPlayer("C:\\ready.wav");
                SoundPlayer.Play();
            }
            //Текстовое оповещение
            label1.Invoke(new Action<string>(WriteLabel), "Перебор завершен");
            button1.Invoke(new Action(BlockButtons));//Обратно активируем кнопки
        }

        //Запуск алгоритма для перебора одного из множества слов
        void AlgorithmForManyWords()
        {
            //Заменить пустой символ лямбдой в векторе
            if (First.Count == 1 && First[0] == ' ')
                First[0] = 'λ';

            //Добавление лямбда в начало и конец
            First.Insert(0, 'λ');
            First.Add('λ');

            //Очистить счетчик количества тактов и второй вектор
            tn = -1;
            Second.Clear();

            //Добавить данные во второй вектор
            for (int i = 0; i < First.Count; i++)
                Second.Add('λ');

            //Очистка индексов для старта алгоритма
            qn = 0;
            fn = 1;
            sn = 1;

              Algorithm5();
           // Algorithm(); Algorithm2();

            //Если количество тактов текущего слова больше, чем максимальное количество тактов для всех слов этой длины, то присваиваем это значение
            if (tn > tnmax)
                tnmax = tn;

            //Убираем лямбду с конца и начала
            First.RemoveAt(0);
            First.RemoveAt(First.Count - 1);

            //Запись в файл комбинации и ее число тактов
            LogBig();

            //Заменить лямбду на пустой символ для возврата в перебор
            if (First.Count == 1 && First[0] == 'λ')
                First[0] = ' ';

            Words_count++;
            Check = false;
            progressBar1.Invoke(new Action<int>(ProgressBarSetValue), Words_count);
        }


        void LogBig()
        {
            try
            {
                string Text = "";
                for (int i = 0; i < First.Count; i++)
                    Text += First[i];
                Text += " " + tn;
                if (Check == true)
                    Text += " + ";
                else
                    Text += " - ";
                Text += "\r\n";
                File.AppendAllText("C:\\Log_Combinations.txt", Text);
            }
            catch (Exception ex)
            {
                label1.Invoke(new Action<string>(WriteLabel), ex.Message);
            }
        }

        //Запись листинга алгоритма в режиме 1 ленты
        void Log()
        {
            try
            {
                string Text = "--------------------------------------\r\n";
                Text = Text + "Первая:    ";
                for (int i = 0; i < First.Count; i++)
                    Text = Text + First[i] + " ";
                Text = Text + "    Позиция: " + (fn + 1) + "\r\n";
                Text = Text + "Состояние: q" + Q[qn];
                Text = Text + "\r\n";
                Text = Text + "Такт:" + (tn + 1);
                Text = Text + "\r\n";
                File.AppendAllText("C:\\Log_one.txt", Text);
            }
            catch (Exception ex)
            {
                label1.Invoke(new Action<string>(WriteLabel), ex.Message);
            }
        }
        void Log2()
        {
            try
            {
                string Text = "--------------------------------------\r\n";
                Text = Text + "Первая:    ";
                for (int i = 0; i < First.Count; i++)
                    Text = Text + First[i] + " ";
                Text = Text + "    Позиция: " + (fn + 1) + "\r\n";
                Text = Text + "Состояние: q1" + Q[qn];
                Text = Text + "\r\n";
                Text = Text + "Такт:" + (tn + 1);
                Text = Text + "\r\n";
                File.AppendAllText("C:\\Log_one.txt", Text);
            }
            catch (Exception ex)
            {
                label1.Invoke(new Action<string>(WriteLabel), ex.Message);
            }
        }
        //Запись листинга алгоритма в режиме 3 лент
        void Log3()
        {
            try
            {
                string Text = "--------------------------------------\r\n";
                Text = Text + "Первая:    ";
                for (int i = 0; i < First.Count; i++)
                    Text = Text + First[i] + " ";
                Text = Text + "    Позиция: " + (fn + 1) + "\r\n";
                Text = Text + "Вторая:    ";
                for (int i = 0; i < Second.Count; i++)
                    Text = Text + Second[i] + " ";
                Text = Text + "    Позиция: " + (sn + 1) + "\r\n";              
                Text = Text + "Состояние: q" + Q[qn];
                Text = Text + "\r\n";
                Text = Text + "Такт:" + (tn + 1);
                Text = Text + "\r\n";
                File.AppendAllText("C:\\Log_two.txt", Text);
            }
            catch (Exception ex)
            {
                label1.Invoke(new Action<string>(WriteLabel), ex.Message);
            }
        }
        void Log4()
        {
            try
            {
                string Text = "--------------------------------------\r\n";
                Text = Text + "Первая:    ";
                for (int i = 0; i < First.Count; i++)
                    Text = Text + First[i] + " ";
                Text = Text + "    Позиция: " + (fn + 1) + "\r\n";
                Text = Text + "Вторая:    ";
                for (int i = 0; i < Second.Count; i++)
                    Text = Text + Second[i] + " ";
                Text = Text + "    Позиция: " + (sn + 1) + "\r\n";
                Text = Text + "Состояние: q1" + Q[qn];
                Text = Text + "\r\n";
                Text = Text + "Такт:" + (tn + 1);
                Text = Text + "\r\n";
                File.AppendAllText("C:\\Log_two.txt", Text);
            }
            catch (Exception ex)
            {
                label1.Invoke(new Action<string>(WriteLabel), ex.Message);
            }
        }
        //Очистка
        private void button5_Click(object sender, EventArgs e)
        {
            //Очистка лент
            dataGridView1.ColumnCount = 10;
            dataGridView2.ColumnCount = 10;

            //Очистка лент
            ClearTape();

            //Очистка информационных полей
            chart1.Series[0].Points.Clear();
            textBox1.Clear();
            textBox2.Clear();
            textBox3.Clear();
            progressBar1.Value = 0;
            label1.Text = "Все данные в программе очищены!";
        }
    }//конец класса
}//конец нэймспейса 