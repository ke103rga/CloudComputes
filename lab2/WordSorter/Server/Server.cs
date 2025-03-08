using System.Net;
using System.Net.Sockets;

namespace WordsSorter;

public class Server
{
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
                    sw.WriteLine("Enter the sequence of words in one line. Use ', ' as a separator");
                    
                    // Получение текста от клиента
                    string text = sr.ReadLine();
                    
                    // Проверка на завершение работы
                    if (string.IsNullOrEmpty(text))
                    {
                        sw.Write("Connection will be closed...");
                        break;
                    }

                    // Обработка текста
                    string[] words = text.Split(", ", StringSplitOptions.RemoveEmptyEntries);
                    HashSet<string> uniqueWords = new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
                    List<string> sortedWords = new List<string>(uniqueWords);
                    sortedWords.Sort();

                    // Отправка результата обратно клиенту
                    string result = string.Join(", ", sortedWords);
                    sw.WriteLine(result);
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
}