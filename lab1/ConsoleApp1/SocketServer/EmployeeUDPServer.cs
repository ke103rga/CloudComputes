using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;

class EmployeeUDPServer
{
    Dictionary<string, string> employees =
        new Dictionary<string, string>()
        {
            {"john", "manager"},
            {"jane", "steno"},
            {"jim", "clerk"},
            {"jack", "salesman"}
        };
    public void Start()
    {
        Int32 port = 13000;
        UdpClient udpc = new UdpClient(port);
        Console.WriteLine("Server started, servicing on port "+port.ToString());
        IPEndPoint ep = null;
        while (true)
        {
            byte[] rdata = udpc.Receive(ref ep);
            string name = Encoding.ASCII.GetString(rdata);
            if (name == "StopServer")
            {
                byte[] messageData = Encoding.ASCII.GetBytes("Connection will be closed...");
                udpc.Send(messageData, messageData.Length, ep);
                break;
            }
            string response = employees.GetValueOrDefault(name, "NotExistingName");
            if (response == "NotExistingName")
            {
                response = $"No such employee with name {name}";
            }
            else
            {
                response = $"Employee {name} has job: {response}";
            }
            
            byte[] sdata = Encoding.ASCII.GetBytes(response);
            udpc.Send(sdata, sdata.Length, ep);
        }
        udpc.Close();
        Console.WriteLine("Server closed");
    }
}