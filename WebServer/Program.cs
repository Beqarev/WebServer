using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebServer
{
    class Program
    {
        public const int Port = 8080;
        public static readonly string WebRootPath = Path.Combine(AppContext.BaseDirectory, "webroot");

        static async Task Main(string[] args)
        {
            Console.WriteLine("Let's get this server running! Here's the setup:");
            Console.WriteLine($"  Listening on port: {Port}");
            Console.WriteLine($"  Serving files from: {WebRootPath}");
            Console.WriteLine("--------------------------------------------------");

            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Any, Port);
                listener.Start();
                Console.WriteLine($"Web server is up and running! Listening for visitors on port {Port}.");
                Console.WriteLine("To shut down the server, press Ctrl+C.");

                while (true)
                {
                    Console.WriteLine("Ready and waiting for the next connection...");
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine($"Great! A new client just connected from: {client.Client.RemoteEndPoint}");

                    Thread clientThread = new Thread(() => HandleClientConnection(client));
                    clientThread.Name = $"ClientHandler-{client.Client.RemoteEndPoint}";
                    clientThread.Start();
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine($"\nOops! A network socket problem occurred: {se.Message} (Error Code: {se.SocketErrorCode})");
                if (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    Console.WriteLine($"It looks like port {Port} is already busy. Another application might be using it.");
                }
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh dear, an unexpected issue popped up in the server's main loop: {ex.Message}");
                Console.WriteLine("Here are the full details for debugging:");
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
            }
            finally
            {
                listener?.Stop();
                Console.WriteLine("\nServer is shutting down. Goodbye!");
            }
        }

        private static void HandleClientConnection(TcpClient client)
        {
            string threadName = Thread.CurrentThread.Name ?? "UnnamedClientThread";
            IPEndPoint clientEndPoint = null;
            try
            {
                clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                Console.WriteLine($"[{threadName}] Now handling the connection from {clientEndPoint}.");
                Console.WriteLine($"[{threadName}] Working on the request from {clientEndPoint}");
                Thread.Sleep(100);
                Console.WriteLine($"[{threadName}] All done with the request from {clientEndPoint}!");
            }
            catch (Exception ex)
            {
                if (clientEndPoint != null)
                {
                    Console.WriteLine($"[{threadName}] Uh oh, ran into an issue while handling the client {clientEndPoint}: {ex.Message}");
                }
                else
                {
                    Console.WriteLine($"[{threadName}] An error occurred while handling a client: {ex.Message}");
                }
                Console.WriteLine("Full error details for this client thread:");
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
            }
            finally
            {
                client.Close();
                if (clientEndPoint != null)
                {
                    Console.WriteLine($"[{threadName}] Connection with {clientEndPoint} has been closed.");
                }
                else
                {
                    Console.WriteLine($"[{threadName}] A client connection was closed (their address wasn't available when closing).");
                }
            }
        }
    }
}