using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

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
        private volatile bool isRunning = true;
        private string name = string.Empty;
        private LinkedList<string> messageHistory = new LinkedList<string>();
        private int previousWindowWidth = Console.WindowWidth;
        private int previousWindowHeight = Console.WindowHeight;

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
                    name = Console.ReadLine() ?? "Unknown";
                    Send(name, false);

                    // Start a thread to handle incoming messages and client list updates
                    clientThread = new Thread(ReceiveMessages);
                    clientThread.Start();

                    while (isRunning)
                    {
                        // Check if console size has changed
                        if (Console.WindowWidth != previousWindowWidth || Console.WindowHeight != previousWindowHeight)
                        {
                            previousWindowWidth = Console.WindowWidth;
                            previousWindowHeight = Console.WindowHeight;
                            RedrawScreen();
                        }

                        // Ensure the prompt is in the correct position
                        UpdatePromptLine();

                        // Read user input
                        string? message = Console.ReadLine();

                        if (message != null)
                        {
                            if (message.ToLower() == "/exit")
                            {
                                Send("/exit", false);
                                isRunning = false;
                                Console.Clear();
                                Environment.Exit(0);
                                break;
                            }
                            else if (message.ToLower() == "/new")
                            {
                                string currentPath = Directory.GetCurrentDirectory();
                                string clientPath = Path.Combine(Directory.GetParent(currentPath)?.FullName ?? string.Empty, "Client");

                                if (clientPath != null)
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "cmd.exe",
                                        Arguments = $"/k \"cd /d \"{clientPath}\" && dotnet run\"",
                                        RedirectStandardOutput = false,
                                        UseShellExecute = true,
                                        CreateNoWindow = false
                                    });
                                    UpdatePromptLine();
                                }
                                else
                                {
                                    Console.WriteLine("Failed to determine the client directory path.");
                                }
                            }
                            else if (message.StartsWith("/"))
                            {
                                string numberStr = message.Substring(1);
                                if (int.TryParse(numberStr, out int number))
                                {
                                    // Send the command to the server
                                    Send($"IS_NUM {number - 1}", true);
                                }
                                else
                                {
                                    Send(message, false);
                                    AddToHistory($"{name} (you): {message}");
                                }
                            }
                            else
                            {
                                Send(message, false);
                                AddToHistory($"{name} (you): {message}");
                            }
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
                            previousClientList = message;
                            RedrawScreen();
                        }
                        else
                        {
                            // Add the message to history and print it
                            AddToHistory(message);
                            RedrawScreen();
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

        private void RedrawScreen()
        {
            Console.Clear();

            // Print the client list at the top
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(previousClientList);

            // Print message history
            PrintMessageHistory();

            // Print the prompt
            UpdatePromptLine();
        }

        private void AddToHistory(string message)
        {
            // Determine available history lines based on current screen size
            int clientListHeight = previousClientList.Split('\n').Length;
            int availableLines = Console.WindowHeight - 2 - clientListHeight; // 1 for prompt, 1 for spacing

            // Ensure we keep the history within the available space
            while (messageHistory.Count >= availableLines)
            {
                messageHistory.RemoveFirst(); // Remove the oldest message if at history limit
            }
            messageHistory.AddLast(message);

            // Print message history
            PrintMessageHistory();
        }

        private void PrintMessageHistory()
        {
            // Determine available space for history
            int clientListHeight = previousClientList.Split('\n').Length;
            int availableLines = Math.Max(Console.WindowHeight - 2 - clientListHeight, 0); // 1 for prompt, 1 for spacing

            // Move the cursor to the correct line and clear the area for history
            Console.SetCursorPosition(0, clientListHeight + 1); // 1 line for spacing
            Console.Write(new string(' ', Console.WindowWidth * (availableLines - 1))); // Clear area for history
            Console.SetCursorPosition(0, clientListHeight + 1); // Reset cursor position

            foreach (var history in messageHistory)
            {
                Console.WriteLine(history);
            }
        }

        private void UpdatePromptLine()
        {
            // Move the cursor to the prompt line and clear it
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write(new string(' ', Console.WindowWidth)); // Clear the line
            Console.SetCursorPosition(0, Console.WindowHeight - 1); // Reset cursor position

            Console.Write("Enter message (or '/exit' to quit, '/new' to open a new client): ");
        }
    }
}
