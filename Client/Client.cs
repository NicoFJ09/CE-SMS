using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    class Client
    {
        IPHostEntry? host;
        IPAddress? ipAddr;
        IPEndPoint? endPoint;

        Socket? s_Client;
        private string previousClientList = string.Empty;
        private Thread? clientThread;
        private volatile bool isRunning = true; // Flag to control the thread execution
        private string name = string.Empty; // Default initialization
        private const int historyLines = 20; // Number of lines to keep in history
        private LinkedList<string> messageHistory = new LinkedList<string>(); // Message history

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
                if (s_Client != null && endPoint != null)
                {
                    Console.Clear();
                    s_Client.Connect(endPoint);
                    Console.WriteLine("Connected to server");

                    // Send name to server
                    Console.Write("Enter your name: ");
                    name = Console.ReadLine() ?? "Unknown"; // Use a default name if none provided
                    Send(name, false); // Indicate this is not a regular message

                    // Start a thread to handle incoming messages and client list updates
                    clientThread = new Thread(ReceiveMessages);
                    clientThread.Start();

                    while (isRunning)
                    {
                        // Display the prompt at the bottom
                        Console.SetCursorPosition(0, Console.WindowHeight - 1); // Move cursor to the prompt line
                        Console.Write("Enter message (or 'exit' to quit): ");

                        // Read user input
                        string? message = Console.ReadLine();

                        if (message != null)
                        {
                            if (message.ToLower() == "exit")
                            {
                                Send("exit", false);
                                isRunning = false; // Signal the thread to stop
                                Console.Clear();
                                Environment.Exit(0);
                                break; // Exit the loop
                            }
                            Send(message, true);

                            // Update message history
                            AddToHistory($"{name}: {message}");
                        }
                    }

                    // Ensure the client thread is completed before closing the socket
                    clientThread?.Join();
                    s_Client.Close();
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

        public void Send(string msg, bool isMessage = true)
        {
            try
            {
                if (s_Client != null)
                {
                    byte[] byteMsg = Encoding.ASCII.GetBytes(msg);
                    s_Client.Send(byteMsg);
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
        private void ReceiveMessages()
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (isRunning)
                {
                    if (s_Client == null) break;

                    if (s_Client.Available > 0)
                    {
                        int bytesRead = s_Client.Receive(buffer);
                        if (bytesRead == 0) break;

                        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                        // Check if the message starts with the client list prefix
                        if (message.StartsWith("Online clients:"))
                        {
                            // This is a client list update
                            if (message != previousClientList)
                            {
                                previousClientList = message;

                                // Clear the console and display the client list
                                Console.Clear();
                                Console.WriteLine(message);

                                // Print message history
                                Console.WriteLine("\nMessage History:");
                                foreach (var history in messageHistory)
                                {
                                    Console.WriteLine(history);
                                }

                                // Ensure the prompt is always visible at the bottom
                                Console.SetCursorPosition(0, Console.WindowHeight - 1);
                                Console.Write("Enter message (or 'exit' to quit): ");
                            }
                        }
                        else
                        {
                            // Print server message
                            Console.SetCursorPosition(0, Console.CursorTop);
                            Console.WriteLine($"Server: {message}");

                            // Print message history
                            Console.SetCursorPosition(0, Console.WindowHeight - 1);
                            Console.Write("Enter message (or 'exit' to quit): ");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error receiving message: " + ex.Message);
            }
            finally
            {
                s_Client?.Close();
            }
        }



        private void AddToHistory(string message)
        {
            if (messageHistory.Count >= historyLines)
            {
                messageHistory.RemoveFirst(); // Remove the oldest message if at history limit
            }
            messageHistory.AddLast(message);
        }
    }
}
