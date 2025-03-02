using System.Net.Sockets;

namespace SocketClient;

class EmployeeTCPClient
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

            LogConsole(LogSource.Server, sr.ReadLine());

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
        string? name = Console.ReadLine();
        
        sw.WriteLine(name);
        if (name == "") return RequestStatus.Cancel;
        LogConsole(LogSource.Server, sr.ReadLine());
        return RequestStatus.Ok;
    }
}