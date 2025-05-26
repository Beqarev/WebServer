using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebServer
{
    class Program
    {
        public const int Port = 8080;
        public static readonly string ResolvedWebRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "webroot"));

        static async Task Main(string[] args)
        {
            Console.WriteLine("Let's get this server running! Here's the setup:");
            Console.WriteLine($"  Listening on port: {Port}");
            Console.WriteLine($"  Serving files from: {ResolvedWebRootPath}");
            Console.WriteLine("--------------------------------------------------");

            if (!Directory.Exists(ResolvedWebRootPath))
            {
                Console.WriteLine($"FATAL ERROR: Web root directory not found at '{ResolvedWebRootPath}'. Please create it and add files.");
                Console.WriteLine("Server cannot start. Press any key to exit.");
                Console.ReadKey();
                return;
            }

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
                    Console.WriteLine($"Port {Port} is already in use");
                }
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
                    Console.WriteLine($"[{threadName}] Received empty or null request from {clientEndPoint}. Closing connection.");
                    return; 
                }

                Console.WriteLine($"[{threadName}] Received request: {requestLine}");

                string[] requestParts = requestLine.Split(' ');
                if (requestParts.Length < 3)
                {
                    Console.WriteLine($"[{threadName}] Malformed request line: {requestLine}. Sending 400 Bad Request.");
                    SendTextResponse(writer, "400 Bad Request", "400 Bad Request", "Your browser sent a request that this server could not understand.");
                    return;
                }

                string httpMethod = requestParts[0];
                string requestedUrl = requestParts[1];
                
                string headerLine;
                while (!string.IsNullOrEmpty(headerLine = reader.ReadLine()))
                {
                    // Optionally process headers here if needed later
                }

                if (httpMethod.ToUpper() != "GET")
                {
                    Console.WriteLine($"[{threadName}] Method '{httpMethod}' not allowed. Sending 405 response.");
                    SendTextResponse(writer, "405 Method Not Allowed", "Error 405: Method Not Allowed", "The requested method is not allowed on this server.");
                    return;
                }
                
                string relativePath = requestedUrl.TrimStart('/');
                if (string.IsNullOrEmpty(relativePath))
                {
                    relativePath = "index.html";
                }
                
                if (relativePath.Contains(".."))
                {
                    Console.WriteLine($"[{threadName}] Path traversal attempt (contains '..'): '{relativePath}'. Sending 403 Forbidden.");
                    SendTextResponse(writer, "403 Forbidden", "Error 403: Forbidden", "Access to this resource is denied due to invalid path components.");
                    return;
                }
                
                string fullFilePath = Path.Combine(ResolvedWebRootPath, relativePath);
                fullFilePath = Path.GetFullPath(fullFilePath);
                
                
                if (!fullFilePath.StartsWith(ResolvedWebRootPath, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[{threadName}] Path traversal attempt (outside web root): '{relativePath}' resolved to '{fullFilePath}'. Sending 403 Forbidden.");
                    SendTextResponse(writer, "403 Forbidden", "Error 403: Forbidden", "Access to this resource is denied.");
                    return;
                }
                
                string fileExtension = Path.GetExtension(fullFilePath).ToLowerInvariant();
                string contentType;

                switch (fileExtension)
                {
                    case ".html":
                        contentType = "text/html; charset=UTF-8";
                        break;
                    case ".css":
                        contentType = "text/css; charset=UTF-8";
                        break;
                    case ".js":
                        contentType = "application/javascript; charset=UTF-8";
                        break;
                    default:
                        Console.WriteLine($"[{threadName}] Unsupported file type requested: '{relativePath}' (extension: '{fileExtension}'). Sending 403 Forbidden.");
                        SendTextResponse(writer, "403 Forbidden", "Error 403: Forbidden", "The requested file type is not supported.");
                        return;
                }
                
                
                if (File.Exists(fullFilePath))
                {
                    try
                    {
                        byte[] fileBytes = File.ReadAllBytes(fullFilePath);
                        
                        writer.Write($"HTTP/1.1 200 OK\r\n");
                        writer.Write($"Content-Type: {contentType}\r\n");
                        writer.Write($"Content-Length: {fileBytes.Length}\r\n");
                        writer.Write($"Connection: close\r\n");
                        writer.Write($"\r\n");
                        writer.Flush();
                        stream.Write(fileBytes, 0, fileBytes.Length);
                        stream.Flush();

                        Console.WriteLine($"[{threadName}] Successfully served '{relativePath}' ({fileBytes.Length} bytes) as {contentType} to {clientEndPoint}.");
                    }
                    catch (IOException ioe)
                    {
                         Console.WriteLine($"[{threadName}] IOException while reading/sending file '{fullFilePath}': {ioe.Message}. Client might have disconnected.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{threadName}] Error reading or sending file '{fullFilePath}': {ex.Message}. Sending 500 Internal Server Error.");
                        SendTextResponse(writer, "500 Internal Server Error", "500 Internal Server Error", "The server encountered an internal error trying to process your request.");
                    }
                }
                else
                {
                    Console.WriteLine($"[{threadName}] File not found: '{relativePath}' (resolved to '{fullFilePath}'). Sending 404 Not Found.");
                    SendTextResponse(writer, "404 Not Found", "Error 404: Page Not Found", "The requested resource could not be found on this server.");
                }
            }
            catch (IOException ioe)
            {
                 if (clientEndPoint != null)
                {
                    Console.WriteLine($"[{threadName}] IO Exception (likely client disconnect) for {clientEndPoint}: {ioe.Message}.");
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
                    Console.WriteLine($"[{threadName}] An error occurred while handling the client {clientEndPoint}: {ex.Message}");
                }
                else
                {
                    Console.WriteLine($"[{threadName}] An error occurred while handling a client: {ex.Message}");
                }
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
                
                try
                {
                    if (stream != null && stream.CanWrite && client.Connected)
                    {
                        using StreamWriter errorWriter = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                        SendTextResponse(errorWriter, "500 Internal Server Error", "500 Internal Server Error", "An unexpected error occurred on the server.");
                    }
                }
                catch (Exception iex)
                {
                    Console.WriteLine($"[{threadName}] Further error trying to send 500 response: {iex.Message}");
                }
            }
            finally
            {
                try
                {
                    stream?.Close();
                    client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{threadName}] Error during cleanup: {ex.Message}");
                }
                
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

        private static void SendTextResponse(StreamWriter writer, string statusCodeAndMessage, string title, string bodyMessage)
        {
            string htmlTitle = WebUtility.HtmlEncode(title);
            string htmlBodyContent = WebUtility.HtmlEncode(bodyMessage);

            string responseBody = $"<html><head><title>{htmlTitle}</title></head><body><h1>{htmlTitle}</h1><p>{htmlBodyContent}</p></body></html>";
            
            try
            {
                writer.Write($"HTTP/1.1 {statusCodeAndMessage}\r\n");
                writer.Write($"Content-Type: text/html; charset=UTF-8\r\n");
                writer.Write($"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n");
                writer.Write($"Connection: close\r\n");
                writer.Write($"\r\n");
                writer.Write(responseBody);
                writer.Flush();
            }
            catch (IOException ioe)
            {
                Console.WriteLine($"Error sending response (client likely disconnected): {ioe.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generic error sending response: {ex.Message}");
            }
        }
    }
}