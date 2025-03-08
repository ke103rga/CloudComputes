using System.Net;
using System.Net.Sockets;

namespace WordsSorter;

public class Server
{
    private const String FILEPATH = "messages.txt";
    const int LIMIT = 5; //можем обработать сразу 5 клиентов одновременно
    TcpListener? listener;
    
    public void Start()
    {
        //Порт нашего сервера
        Int32 port = 13000;
        //IP-адрес нашего сервера – локальная машина
        IPAddress localAddr = IPAddress.Parse("127.0.0.1");

        listener = new TcpListener(localAddr, port);
        listener.Start();
        
        //We would launch our server in a separate thread
        for (int i = 0; i < LIMIT; i++)
        {
            Thread t = new Thread(new ThreadStart(ServiceSocket));
            t.Start();
        }
    }
    
    private void ServiceSocket()
    {
        while (true)
        {
            Socket soc = listener.AcceptSocket(); // block

            try
            {
                Stream s = new NetworkStream(soc);
                Console.WriteLine("Stream was created");

                StreamReader sr = new StreamReader(s);
                StreamWriter sw = new StreamWriter(s);
                sw.AutoFlush = true; // enable automatic flushing
                
                while (true)
                {
                    sw.WriteLine("Enter command 'LIST' for messages list or enter a message to add it.");
                    
                    // Получение текста от клиента
                    string text = sr.ReadLine();
                    
                    // Проверка на завершение работы
                    if (string.IsNullOrEmpty(text))
                    {
                        sw.Write("Connection will be closed...");
                        break;
                    }

                    // Обработка команд
                    if (text.Equals("LIST", StringComparison.OrdinalIgnoreCase))
                    {
                        // Вывод всех объявлений
                        string messages = GetAllMessages();
                        // messages = messages.ConvertAll(m => m.Trim());
                        sw.WriteLine(messages);
                    }
                    else
                    {
                        // Добавление текста в файл
                        string confirmation = AddMessage(text);
                        sw.WriteLine(confirmation);
                    }
                }

                s.Close();
                soc.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                soc.Close();

            }
        }
    }
    
    // Метод для сохранения сообщения и возврата подтверждения
    private string AddMessage(string message)
    {
        // Записываем сообщение в файл
        using (StreamWriter sw = new StreamWriter(FILEPATH, true)) 
        {
            sw.WriteLine(message);
        }

        return "Message added: \"" + message + "\"";
    }

    // Метод для получения всех сообщений
    private string GetAllMessages()
    {
        if (!File.Exists(FILEPATH)) return "No messages available."; // если файла нет

        List<string> messages = new List<string>(File.ReadAllLines(FILEPATH));
        messages = messages.ConvertAll(m => m.Trim());
        return string.Join($"; ", messages);
    }
    
    private List<string> GetMessagesList()
    {
        if (!File.Exists(FILEPATH)) return new List<string>(); // если файла нет

        List<string> messages = new List<string>(File.ReadAllLines(FILEPATH));
        return messages;
    }
}