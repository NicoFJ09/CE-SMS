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
            Client client = new Client("localhost", 8080);
            client.Start();
            client.Send("Hello, server!");
            Console.ReadLine();
        }
    }
}