using System.Net;
using System.Net.Sockets;

namespace SocketServer;

class EmployeeTCPServer{
    
    const int LIMIT = 5; //можем обработать сразу 5 клиентов одновременно
    TcpListener? listener;
    
    Dictionary<string, string> employees =
        new Dictionary<string, string>()
        {
            {"john", "manager"},
            {"jane", "steno"},
            {"jim", "clerk"},
            {"jack", "salesman"}
        };

    public static void showIPs(string[] args)
    {
        string name = (args.Length < 1) ? Dns.GetHostName() : args[0];
        try{
            IPAddress[] addrs = Dns.Resolve(name).AddressList;
            foreach(IPAddress addr in addrs)
                Console.WriteLine("{0}/{1}",name,addr);
        }catch(Exception e){
            Console.WriteLine(e.Message);
        }
    }


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

                sw.WriteLine("{0} Employees available",
                    employees.Count);
                
                while (true)
                {
                    sw.WriteLine("Enter the employee name -> ");
                    // Console.WriteLine("Enter the employee name -> ");
                    string? name = sr.ReadLine();
                    if (string.IsNullOrEmpty(name))
                    {
                        sw.Write("Connection will be closed...");
                        break;
                    }
                    string response =
                        employees.GetValueOrDefault(name, "NotExistingName");
                    if (response == "NotExistingName") response = $"No such employee with name {name}";
                    sw.WriteLine($"Employee {name} has job: {response}");
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