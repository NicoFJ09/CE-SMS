using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    class Server
    {
        IPHostEntry host;
        IPAddress ipAddr;
        IPEndPoint endPoint;
        
        Socket s_Server;
        Socket? s_Client;

        public Server(string ip, int port)
        {
            host = Dns.GetHostEntry(ip);
            ipAddr = host.AddressList[0];
            endPoint = new IPEndPoint(ipAddr, port);

            s_Server = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            s_Server.Bind(endPoint);
            s_Server.Listen(10);
        }

        public void Start()
        {   
            byte[] buffer = new byte[1024];
            string message;
            s_Client = s_Server.Accept();

            s_Client.Receive(buffer);
            message = Encoding.ASCII.GetString(buffer);
            Console.WriteLine("Se recibio el mensaje: " + message);
        }
    }
}