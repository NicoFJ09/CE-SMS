using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Start the server
            Server server = new Server("localhost", 12345);
            Task serverTask = Task.Run(() => server.Start());

            // Define the path to the Client directory
            string currentPath = Directory.GetCurrentDirectory();
            string clientDirectoryPath = Path.Combine(Directory.GetParent(currentPath)?.FullName ?? string.Empty, "Client");

            // Ensure the path is not null or empty and the directory exists
            if (string.IsNullOrEmpty(clientDirectoryPath) || !Directory.Exists(clientDirectoryPath))
            {
                Console.WriteLine($"Failed to locate the Client directory. Path: {clientDirectoryPath}");
                return;
            }

            // Open a new CMD window, navigate to the Client directory, and run dotnet run
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k \"cd /d \"{clientDirectoryPath}\" && dotnet run\"",
                RedirectStandardOutput = false,
                UseShellExecute = true,
                CreateNoWindow = false
            });

            // Wait for the server to finish (if desired)
            await serverTask;
        }
    }
}
