using System.Net.Sockets;

namespace WordsSorter;

public class Client
{
    private TcpClient client;
    private Stream s;
    private StreamReader sr;
    private StreamWriter sw;
    
    public enum RequestStatus
    {
        Ok,
        Cancel
    }
    
    public enum LogSource
    {
        Server,
        Client
    }

    private void LogConsole(LogSource source, String? message)
    {
        String messagePrefix = (source == LogSource.Client) ? "[CLIENT]: " : "[SERVER]: ";
        Console.WriteLine(messagePrefix + message);
    }
    
    public void Start()
    {
        //Порт нашего сервера
        Int32 port = 13000;
        string serverAddr = "127.0.0.1";

        client = new TcpClient(serverAddr, port);
        LogConsole(LogSource.Client, "Client connected with server");

        try
        {
            s = client.GetStream();
            sr = new StreamReader(s);
            sw = new StreamWriter(s);
            sw.AutoFlush = true;

            // LogConsole(LogSource.Server, sr.ReadLine());

            RequestStatus requestStatus = RequestStatus.Ok;
            while (requestStatus == RequestStatus.Ok)
            {
                requestStatus = SendRequest();
            }
        }
        finally
        {
            client.Close();
        }
    }
    
    public RequestStatus SendRequest()
    {
        // LogConsole(LogSource.Client, "Enter the name: ");
        LogConsole(LogSource.Server, sr.ReadLine());
        
        string? inputText = Console.ReadLine();
        sw.WriteLine(inputText);
        if (string.IsNullOrEmpty(inputText)) return RequestStatus.Cancel;

        // Получение результата от сервера
        string? resultText = sr.ReadLine();
        
        LogConsole(LogSource.Server, "Sorted words: ");
        string[] words = resultText.Split(", ", StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            Console.WriteLine(word);
        }
        
        return RequestStatus.Ok;
    }
}