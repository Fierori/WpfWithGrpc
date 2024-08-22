using Grpc.Core;

namespace GrpcGreeterConsoleServer
{
    using GreetModel;

    class Program
    {
        public static void Main(string[] args)
        {
            const string Host = "127.0.0.1";
            const int    Port = 5001;

            var server = 
                new Server
                {
                    Services = { GrpcGreeter.BindService(new GrpcGreeterImpl())         },
                    Ports    = { new ServerPort(Host, Port, ServerCredentials.Insecure) },
                };

            server.Start();
            Console.WriteLine($"Greeter server listening on port {Port}");

            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.KillAsync().Wait();

            return;
        }
    }
}
