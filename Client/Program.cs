using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client("localhost", 12345);
            client.Start();

            while (true)
            {
                string? message = Console.ReadLine();
                if ((message ?? string.Empty).ToLower() == "/exit")
                {
                    break;
                }
                client.Send(message ?? string.Empty); 
            }
        }
    }
}