namespace SocketClient;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;


class EmployeeUDPClient
{
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
        string ipadress = "127.0.0.1";
        Int32 port = 13000;
        // if (args.Length > 0)
        //     ipadress = args[0];
        
        UdpClient udpc = new UdpClient(ipadress, port);
        IPEndPoint ep = null;
        while (true)
        {
            // Console.Write("Name: ");
            LogConsole(LogSource.Client, "Enter the name -> ");
            string name = Console.ReadLine();
            if (name == "")
            {
                byte[] stopMessageData = Encoding.ASCII.GetBytes("StopServer");
                udpc.Send(stopMessageData, stopMessageData.Length);
                break;
            }
            byte[] sdata = Encoding.ASCII.GetBytes(name);
            udpc.Send(sdata, sdata.Length);
            byte[] rdata = udpc.Receive(ref ep);
            string response = Encoding.ASCII.GetString(rdata);
            LogConsole(LogSource.Server, response);
            // Console.WriteLine(job);
        }
    }
}