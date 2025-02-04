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
        IPHostEntry? host;
        IPAddress? ipAddr;
        IPEndPoint? endPoint;

        Socket? s_Server;
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
                Socket s_Client = s_Server!.Accept();
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

            // Ask client for their name
            int bytesRead = clientInfo.Socket.Receive(buffer);
            clientInfo.Name = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
            Console.WriteLine("Client connected: " + clientInfo.Name);

            // Send the updated client list to all clients, including the newly connected one
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
                        // Notify remaining clients about the disconnection
                        BroadcastClientList();
                        break;
                    }

                    // Read the message from the client
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine($"{clientInfo.Name} sent a message: {message}");

                    if (message.StartsWith("IS_NUM"))
                    {
                        string[] parts = message.Split(' ');
                        if (parts.Length > 1)
                        {
                            int clientId = int.Parse(parts[1]);
                            if (clientId >= 0 && clientId < clients.Count)
                            {
                                // Check if the client is trying to write to themselves
                                if (clients[clientId] == clientInfo)
                                {
                                    Console.WriteLine($"Client {clientInfo.Name} tried to write to themselves. Ignoring.");
                                }
                                else
                                {
                                    // Update the selected recipient for the client
                                    clientInfo.SelectedRecipientId = clientId;
                                    // Send updated client list to all clients
                                    BroadcastClientList();
                                    Console.WriteLine($"Updated selected recipient to client {clientId} ({clients[clientId].Name})");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid client ID");
                            }
                        }
                    }
                    else
                    {
                        // Handle message routing to the selected recipient
                        int recipientId = clientInfo.SelectedRecipientId;
                        if (recipientId >= 0 && recipientId < clients.Count)
                        {
                            string routedMessage = $"{clientInfo.Name}: {message}";
                            SendMessageToClient(clients[recipientId], routedMessage);
                            Console.WriteLine($"Message from {clientInfo.Name} routed to {clients[recipientId].Name}");
                        }
                        else
                        {
                            Console.WriteLine("No valid recipient selected for the message.");
                        }
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10054)
                    {
                        Console.WriteLine("Client disconnected: " + clientInfo.Name);
                        clients.Remove(clientInfo);
                        // Notify remaining clients about the disconnection
                        BroadcastClientList();
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

        private void SendMessageToClient(ClientInfo client, string message)
        {
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                client.Socket.Send(buffer);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Error sending message to {client.Name}: {ex.Message}");
            }
        }

        private void SendClientList(string clientList, IEnumerable<ClientInfo> recipients)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(clientList);

            foreach (ClientInfo client in recipients)
            {
                try
                {
                    client.Socket.Send(buffer);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Error sending client list to {client.Name}: {ex.Message}");
                }
            }
        }

        private void BroadcastClientList()
        {
            foreach (var client in clients)
            {
                string clientList = GenerateClientList(client);
                SendClientList(clientList, new List<ClientInfo> { client });
            }
        }

        private string GenerateClientList(ClientInfo currentClient)
        {
            StringBuilder clientListBuilder = new StringBuilder("Online clients:\r\n");
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i] == currentClient)
                {
                    clientListBuilder.AppendLine($"{i + 1}. {clients[i].Name} (you) - Currently texting: {(clients[i].SelectedRecipientId >= 0 && clients[i].SelectedRecipientId < clients.Count ? clients[clients[i].SelectedRecipientId].Name : "None")}");
                }
                else
                {
                    clientListBuilder.AppendLine($"{i + 1}. {clients[i].Name}");
                }
            }
            return clientListBuilder.ToString();
        }
    }

    class ClientInfo
    {
        public Socket Socket { get; set; }
        public string? Name { get; set; }
        public int SelectedRecipientId { get; set; } = -1; // Default: no recipient selected

        public ClientInfo(Socket socket)
        {
            Socket = socket;
        }
    }
}
