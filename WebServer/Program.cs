using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebServer
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
                    Console.WriteLine($"New client just connected from: {client.Client.RemoteEndPoint}");

                    Thread clientThread = new Thread(() => HandleClientConnection(client));
                    clientThread.Name = $"ClientHandler-{client.Client.RemoteEndPoint}";
                    clientThread.Start();
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine($"Network socket problem occurred: {se.Message}");
                if (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    Console.WriteLine($"Port {Port} is already in use.");
                }
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred in Main: {ex.Message}");
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
            }
            finally
            {
                listener?.Stop();
                Console.WriteLine("Server is shutting down.");
            }
        }

        private static void HandleClientConnection(TcpClient client)
        {
            string threadName = Thread.CurrentThread.Name ?? "UnnamedClientThread";
            IPEndPoint clientEndPoint = null;
            NetworkStream stream = null;

            try
            {
                clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                Console.WriteLine($"[{threadName}] Now handling the connection from {clientEndPoint}.");

                stream = client.GetStream();
                
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
                using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                string requestLine = reader.ReadLine();
                if (string.IsNullOrEmpty(requestLine))
                {
                    Console.WriteLine($"[{threadName}] Received empty or null request from {clientEndPoint}");
                    return; 
                }

                Console.WriteLine($"[{threadName}] Received request: {requestLine}");

                string[] requestParts = requestLine.Split(' ');
                if (requestParts.Length < 3)
                {
                    Console.WriteLine($"[{threadName}] Malformed request line: {requestLine}");
                    string badRequestBody = "<html><head><title>400 Bad Request</title></head><body><h1>400 Bad Request</h1><p>Your browser sent a request that this server could not understand.</p></body></html>";
                    string badRequestResponse = $"HTTP/1.1 400 Bad Request\r\n" +
                                                $"Content-Type: text/html\r\n" +
                                                $"Content-Length: {Encoding.UTF8.GetByteCount(badRequestBody)}\r\n" +
                                                $"Connection: close\r\n\r\n" +
                                                badRequestBody;
                    writer.Write(badRequestResponse);
                    return;
                }

                string httpMethod = requestParts[0];
                string requestedUrl = requestParts[1];

                if (httpMethod.ToUpper() != "GET")
                {
                    Console.WriteLine($"[{threadName}] Method '{httpMethod}' not allowed");
                    string responseBody = "<html><head><title>405 Method Not Allowed</title></head><body><h1>Error 405: Method Not Allowed</h1></body></html>";
                    string response = $"HTTP/1.1 405 Method Not Allowed\r\n" +
                                      $"Content-Type: text/html\r\n" +
                                      $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n" +
                                      $"Connection: close\r\n\r\n" +
                                      responseBody;
                    writer.Write(response);
                    Console.WriteLine($"[{threadName}] Sent 405 response to {clientEndPoint}.");
                    return;
                }
                
                string notImplementedBody = $"<html><head><title>501 Not Implemented</title></head><body><h1>501 Not Implemented</h1><p>GET request for {WebUtility.HtmlEncode(requestedUrl)} is valid but not yet fully implemented.</p></body></html>";
                string notImplementedResponse = $"HTTP/1.1 501 Not Implemented\r\n" +
                                     $"Content-Type: text/html\r\n" +
                                     $"Content-Length: {Encoding.UTF8.GetByteCount(notImplementedBody)}\r\n" +
                                     $"Connection: close\r\n\r\n" +
                                     notImplementedBody;
                writer.Write(notImplementedResponse);
                Console.WriteLine($"[{threadName}] Sent 501 Not Implemented for GET {requestedUrl} as a placeholder.");

            }
            catch (IOException ioe)
            {
                if (clientEndPoint != null)
                {
                    Console.WriteLine($"[{threadName}] IO Exception while handling client {clientEndPoint}: {ioe.Message}");
                }
                else
                {
                    Console.WriteLine($"[{threadName}] IO Exception: {ioe.Message}");
                }
            }
            catch (Exception ex)
            {
                if (clientEndPoint != null)
                {
                    Console.WriteLine($"[{threadName}] error occurred while handling the client {clientEndPoint}: {ex.Message}");
                }
                else
                {
                    Console.WriteLine($"[{threadName}] error occurred while handling a client: {ex.Message}");
                }
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
                    Console.WriteLine($"[{threadName}] Client connection was closed.");
                }
            }
        }
    }
}