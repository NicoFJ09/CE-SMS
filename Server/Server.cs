using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    class Server
    {
        IPHostEntry host;
        IPAddress ipAddr;
        IPEndPoint endPoint;

        Socket s_Server;
        List<ClientInfo> clients;

        public Server(string ip, int port)
        {
            host = Dns.GetHostEntry(ip);
            ipAddr = host.AddressList[0];
            endPoint = new IPEndPoint(ipAddr, port);

            s_Server = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            s_Server.Bind(endPoint);
            s_Server.Listen(10);

            clients = new List<ClientInfo>();
        }

        public void Start()
        {
            Console.WriteLine("Server started. Waiting for connections...");

            while (true)
            {
                Socket s_Client = s_Server.Accept();
                Console.WriteLine("Client connected");

                ClientInfo clientInfo = new ClientInfo(s_Client);
                clients.Add(clientInfo);

                // Create a new thread to handle the client
                Thread clientThread = new Thread(() => HandleClient(clientInfo));
                clientThread.Start();
            }
        }

        private void HandleClient(ClientInfo clientInfo)
        {
            byte[] buffer = new byte[1024];
            string message;

            // Ask client for their name
            clientInfo.Socket.Send(Encoding.ASCII.GetBytes("Enter your name: "));
            int bytesRead = clientInfo.Socket.Receive(buffer);
            clientInfo.Name = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Client connected: " + clientInfo.Name);

            // Broadcast client list to all clients
            BroadcastClientList();

            while (true)
            {
                try
                {
                    bytesRead = clientInfo.Socket.Receive(buffer);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Client disconnected: " + clientInfo.Name);
                        clients.Remove(clientInfo);
                        break;
                    }
                    message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    if (message.ToLower() == "exit")
                    {
                        Console.WriteLine("Client " + clientInfo.Name + " left");
                        clients.Remove(clientInfo);
                        break;
                    }
                    Console.WriteLine("Received message from " + clientInfo.Name + ": " + message);

                    // Broadcast message to all other clients
                    Broadcast(message, clientInfo);
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10054)
                    {
                        Console.WriteLine("Client disconnected: " + clientInfo.Name);
                        clients.Remove(clientInfo);
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Error receiving data: " + ex.Message);
                        break;
                    }
                }
            }

            clientInfo.Socket.Close();
        }

        private void Broadcast(string message, ClientInfo sender)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(sender.Name + ": " + message);

            foreach (ClientInfo client in clients)
            {
                if (client != sender)
                {
                    client.Socket.Send(buffer);
                }
            }
        }

        private void BroadcastClientList()
        {
            string clientList = "Connected clients: ";
            foreach (ClientInfo client in clients)
            {
                clientList += client.Name + ", ";
            }
            clientList = clientList.TrimEnd(',', ' ');

            byte[] buffer = Encoding.ASCII.GetBytes(clientList);

            foreach (ClientInfo client in clients)
            {
                client.Socket.Send(buffer);
            }
        }
    }

    class ClientInfo
    {
        public Socket Socket { get; set; }
        public string Name { get; set; }

        public ClientInfo(Socket socket)
        {
            Socket = socket;
        }
    }
}