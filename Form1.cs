using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Network_Traffic
{
    public partial class Form1 : Form
    {
        // задаем порт для прослушивания
        int port = 8080;

        // создаем экземпляр TcpListener
        TcpListener listener;
        private TcpListener tcpListener;

        public Form1()
        {
            InitializeComponent();
            // инициализируем экземпляр TcpListener в конструкторе формы
            listener = new TcpListener(IPAddress.Any, port);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // запускаем прослушивание входящего трафика при нажатии кнопки "Старт"
            listener.Start();

            // ожидаем подключения клиентов в отдельном потоке
            Thread t = new Thread(new ThreadStart(ListenForClients));
            t.Start();
        }

        private void ListenForClients()
        {
            try
            {
                tcpListener.Start(); // начинаем прослушивание входящих подключений

                while (true) // бесконечный цикл для ожидания подключений
                {
                    TcpClient client = tcpListener.AcceptTcpClient(); // ждем подключения клиента

                    // создаем новый поток для обработки запросов клиента
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                    clientThread.Start(client);
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show("Ошибка сокета: " + ex.Message);
            }
            finally
            {
                tcpListener.Stop(); // останавливаем прослушивание
            }
        }

        public enum FtpPacketType
        {
            USER,
            PASS,
            CWD,
            QUIT
        }

        //Перечисление содержит числовые коды FTP-ответов и соответствующие им значения
        public enum FtpResponseCode
        {
            Undefined = -1,
            RestartMarker = 110,
            ServiceReadyIn = 120,
            DataConnectionAlreadyOpened = 125,
            FileStatusOK = 150,
            CommandOK = 200,
            CommandNotImplemented = 202,
            SystemStatus = 211,
            DirectoryStatus = 212,
            FileStatus = 213,
            HelpMessage = 214,
            SystemType = 215,
            ServiceReady = 220,
            ClosingConnection = 221,
            DataConnectionOpened = 225,
            ClosingDataConnection = 226,
            PassiveMode = 227,
            LoginAccepted = 230,
            FileActionOK = 250,
            DirectoryCreated = 257,
            NeedPassword = 331,
            NeedAccount = 332,
            FileActionPending = 350,
            ServiceNotAvailable = 421,
            CantOpenDataConnection = 425,
            ConnectionClosed = 426,
            InvalidUsernameOrPassword = 430,
            RequestedHostUnavailable = 434,
            RequestedFileActionNotTaken = 450,
            ActionAborted = 451,
            InsufficientStorage = 452,
            SyntaxError = 500,
            SyntaxErrorInArguments = 501,
            NotLoggedIn = 530,
            NeedAccountForStoringFiles = 532,
            FileUnavailable = 550,
            PageTypeUnknown = 551,
            ExceededStorageAllocation = 552,
            BadFilename = 553
        }



        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    // Читаем данные из потока клиента
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch (Exception ex)
                {
                    // Обработка ошибок чтения из потока клиента
                    string errorMessage = string.Format("Ошибка чтения из потока клиента: {0}", ex.Message);

                    // Выводим сообщение на форму в элемент управления TextBox
                    textBox1.Invoke(new Action(() =>
                    {
                        textBox1.AppendText(errorMessage);
                        textBox1.ScrollToCaret();
                    }));

                    // Закрываем соединение
                    tcpClient.Close();
                    break;
                }

                if (bytesRead == 0)
                {
                    // Клиент отключился
                    // Выводим сообщение на форму в элемент управления TextBox
                    string datta = "Клиент отключился";
                    textBox1.Invoke(new Action(() =>
                    {
                        textBox1.AppendText(datta);
                        textBox1.ScrollToCaret();
                    }));

                    // Закрываем соединение
                    tcpClient.Close();
                    break;
                }

                // Преобразуем полученные данные в строку
                string data = Encoding.ASCII.GetString(message, 0, bytesRead);

                // Чтение входящего трафика
                while ((bytesRead = clientStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Анализ полученных данных
                    data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    // Добавление данных в листбокс на форме
                    listBoxTraffic.Items.Add(data);
                }

                // Выводим данные на форму в элемент управления TextBox
                textBox1.Invoke(new Action(() =>
                {
                    textBox1.AppendText(data);
                    textBox1.ScrollToCaret();
                }));



                // Отправка пакета обратно клиенту
                clientStream.Write(message, 0, bytesRead);
            }

            tcpClient.Close();
        }

        private void HandleHttpRequest(TcpClient client, NetworkStream stream, string request)
        {
            // получаем первую строку запроса
            var firstLine = request.Substring(0, request.IndexOf("\r\n"));

            // разбиваем первую строку на отдельные слова
            var words = firstLine.Split(' ');

            // получаем метод запроса, URL и версию протокола
            var method = words[0];
            var url = words[1];
            var protocolVersion = words[2];

            // выводим информацию о запросе на форму
            this.Invoke(new Action(() =>
            {
                textBox1.AppendText("HTTP request received from " + client.Client.RemoteEndPoint.ToString() + "\r\n");
                textBox1.AppendText("Method: " + method + "\r\n");
                textBox1.AppendText("URL: " + url + "\r\n");
                textBox1.AppendText("Protocol version: " + protocolVersion + "\r\n");
            }));


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TcpClient client = new TcpClient();

            try
            {
                client.Connect("192.168.1.1", 8080);

                // получение потока данных
                NetworkStream stream = client.GetStream();

                // отправка данных на хост
                byte[] data = Encoding.UTF8.GetBytes("Привет, хост!");
                stream.Write(data, 0, data.Length);

                // чтение данных из потока
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                MessageBox.Show("Получен ответ от хоста: " + response);
            }
            catch (SocketException ex)
            {
                // обработка исключения подключения
                MessageBox.Show("Не удалось подключиться к хосту: " + ex.Message);
            }
            finally
            {
                client.Close(); // закрытие соединения с хостом
            }
        }

        private void ListenForTraffic()
        {


            // создаем объект TcpListener
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            // выводим информацию о запуске прослушивания на форму
            this.Invoke(new Action(() =>
            {
                textBox1.AppendText("Listening for traffic on port " + port + "...\r\n");
            }));

            while (true)
            {
                // принимаем входящее соединение
                TcpClient client = listener.AcceptTcpClient();

                // получаем объект NetworkStream для чтения входящего трафика
                NetworkStream stream = client.GetStream();

                // создаем буфер для чтения входящего трафика
                byte[] buffer = new byte[client.ReceiveBufferSize];

                // читаем входящий трафик
                int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);

                // преобразуем входящий трафик в строку
                string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                // обрабатываем HTTP-запрос
                if (request.StartsWith("GET") || request.StartsWith("POST"))
                {
                    HandleHttpRequest(client, stream, request);
                }
                else
                {
                    // TODO: обработка других типов трафика
                }
                // обрабатываем FTP-пакеты
                if (request.StartsWith("USER"))
                {
                    HandleFtpPacket(client, stream, request, FtpPacketType.USER);
                }
                else if (request.StartsWith("PASS"))
                {
                    HandleFtpPacket(client, stream, request, FtpPacketType.PASS);
                }
                else if (request.StartsWith("CWD"))
                {
                    HandleFtpPacket(client, stream, request, FtpPacketType.CWD);
                }
                else if (request.StartsWith("QUIT"))
                {
                    HandleFtpPacket(client, stream, request, FtpPacketType.QUIT);
                }
                else
                {
                    // TODO: обработка других типов трафика
                }
            }
        }

        private void HandleFtpPacket(TcpClient client, NetworkStream stream, string request, FtpPacketType packetType)
        {
            string response;
            switch (packetType)
            {
                //обработка пакета USER
                case FtpPacketType.USER:
                    // получаем имя пользователя из запроса
                    var username = request.Substring(5).Trim();

                    // выводим информацию о запросе на форму
                    this.Invoke(new Action(() =>
                    {
                        textBox1.AppendText("FTP USER request received from " + client.Client.RemoteEndPoint.ToString() + "\r\n");
                        textBox1.AppendText("Username: " + username + "\r\n");
                    }));
                    break;

                // обработка пакета PASS
                case FtpPacketType.PASS:
                    // получаем пароль из запроса
                    var password = request.Substring(5).Trim();

                    // выводим информацию о запросе на форму
                    this.Invoke(new Action(() =>
                    {
                        textBox1.AppendText("FTP packet received from " + client.Client.RemoteEndPoint.ToString() + "\r\n");
                        textBox1.AppendText("Packet type: PASS\r\n");
                        textBox1.AppendText("Password: " + password + "\r\n");
                    }));

                    // отправляем ответ клиенту
                    response = "230 User logged in\r\n";
                    var responseData = Encoding.ASCII.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                    break;

                // обработка пакета CWD
                case FtpPacketType.CWD:
                    // получаем аргумент команды CWD
                    string directory = request.Substring(request.IndexOf(" ") + 1).Trim();

                    // изменяем текущую директорию на заданную
                    if (ChangeWorkingDirectory(directory))
                    {
                        // отправляем успешный ответ клиенту
                        response = "250 Directory changed to " + directory + "\r\n";
                        byte[] buffer = Encoding.ASCII.GetBytes(response);
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        // отправляем ошибку клиенту
                        response = "550 Failed to change directory\r\n";
                        byte[] buffer = Encoding.ASCII.GetBytes(response);
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    break;

                case FtpPacketType.QUIT:
                    // обработка пакета QUIT
                    break;

                default:
                    // обработка неизвестного типа пакета
                    break;
            }
        }

        private string ReadResponse(TcpClient client, NetworkStream stream)
        {
            byte[] buffer = new byte[2048];
            int count = stream.Read(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, count);
        }

        private int GetResponseCode(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                throw new ArgumentException("Response string cannot be null or empty.", nameof(response));
            }

            var responseCode = response.Substring(0, 3);
            if (!int.TryParse(responseCode, out int code))
            {
                throw new InvalidOperationException($"Failed to parse response code from string: {response}");
            }

            return code;
        }


        private void ChangeWorkingDirectory(string path, TcpClient client, NetworkStream stream, FtpResponseCode ftpresponsecode)
        {
            var command = "CWD " + path + "\r\n";
            var commandBytes = Encoding.ASCII.GetBytes(command);
            stream.Write(commandBytes, 0, commandBytes.Length);

            var response = ReadResponse(client, stream);
            var responseCode = GetResponseCode(response);

            if (responseCode != FtpResponseCode.CommandOK)
            {
                // Ошибка, выводим сообщение об ошибке на форму
                this.Invoke(new Action(() =>
                {
                    textBox1.AppendText("Error changing working directory to " + path + "\r\n");
                }));
            }
            else
            {
                // Рабочая директория изменена, выводим сообщение на форму
                this.Invoke(new Action(() =>
                {
                    textBox1.AppendText("Working directory changed to " + path + "\r\n");
                }));
            }
        }

        private void HandleFtpCommand(TcpClient client, NetworkStream stream)
        {
            // читаем данные пакета
            var data = ReadFtpPacketData(stream);

            // выводим информацию о команде на форму
            this.Invoke(new Action(() =>
            {
                textBox1.AppendText("FTP command received from " + client.Client.RemoteEndPoint.ToString() + "\r\n");
                textBox1.AppendText("Command: " + data + "\r\n");
            }));
        }

        private void HandleFtpData(TcpClient client, NetworkStream stream)
        {
            // читаем данные пакета
            var data = ReadFtpPacketData(stream);

            // выводим информацию о данных на форму
            this.Invoke(new Action(() =>
            {
                textBox1.AppendText("FTP data received from " + client.Client.RemoteEndPoint.ToString() + "\r\n");
                textBox1.AppendText("Data: " + data + "\r\n");
            }));
        }

        private string ReadFtpPacketData(NetworkStream stream)
        {
            // читаем длину данных из пакета (3 байта)
            var lengthBuffer = new byte[3];
            stream.Read(lengthBuffer, 0, lengthBuffer.Length);
            var length = BitConverter.ToInt32(lengthBuffer, 0);

            // читаем сами данные пакета
            var dataBuffer = new byte[length];
            stream.Read(dataBuffer, 0, dataBuffer.Length);
            var data = Encoding.UTF8.GetString(dataBuffer);

            return data;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите закрыть приложение?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                // закрываем TcpListener при закрытии формы
                listener.Stop();
            }
        }
    }
}


