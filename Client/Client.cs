using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Client
    {
        IPHostEntry? host;
        IPAddress? ipAddr;
        IPEndPoint? endPoint;

        Socket? s_Client;

        public Client(string ip, int port)
        {
            try
            {
                host = Dns.GetHostEntry(ip);
                ipAddr = host.AddressList[0];
                endPoint = new IPEndPoint(ipAddr, port);

                s_Client = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating client: " + ex.Message);
            }
        }

    public void Start()
    {
        try
        {
            if (s_Client!= null && endPoint!= null)
            {
                Console.Clear();
                s_Client.Connect(endPoint);
                Console.WriteLine("Connected to server");

                // Send name to server
                Console.Write("Enter your name: ");
                string? name = Console.ReadLine();
                Send(name!);

                // Receive list of online clients
                byte[] buffer = new byte[1024];
                s_Client.Receive(buffer);
                string clientList = Encoding.ASCII.GetString(buffer);
                Console.WriteLine(clientList);

                while (true)
                {
                    Console.Write("Enter message (or 'exit' to quit): ");
                    string? message = Console.ReadLine();
                    if (message!.ToLower() == "exit")
                    {
                        Send("exit");
                        Environment.Exit(0);
                        break;
                    }
                    Send(message);
                }
            }
            else
            {
                Console.WriteLine("Error connecting to server");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Error connecting to server: " + ex.Message);
        }
    }
        public void Send(string msg)
        {
            try
            {
                if (s_Client != null)
                {
                    byte[] byteMsg = Encoding.ASCII.GetBytes(msg);
                    s_Client.Send(byteMsg);
                    Console.WriteLine("You: " + msg);
                }
                else
                {
                    Console.WriteLine("Error sending message: socket is not connected");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending message: " + ex.Message);
            }
        }
    }
}