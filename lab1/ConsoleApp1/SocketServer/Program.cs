// See https://aka.ms/new-console-template for more information

using System.Net.Sockets;

using System;
using System.Net;
using SocketServer;

EmployeeTCPServer server = new EmployeeTCPServer();
// EmployeeUDPServer server = new EmployeeUDPServer();

server.Start();
