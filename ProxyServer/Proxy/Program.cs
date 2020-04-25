using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxy
{
    class Program
    {

        static void Main(string[] args)
        {
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8001);
            listener.Start();
            while (true) 
            {
                var client = listener.AcceptTcpClient();
                Thread thread = new Thread(() => ProcessRequest(client));
                thread.Start();
            }
        }

        public static void ProcessRequest(TcpClient client) 
        {
            NetworkStream browserStream = client.GetStream();
            string mainHostRequest;
            byte[] buf = new byte[client.ReceiveBufferSize];
            while (true)
            {
                try
                {
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = browserStream.Read(buf, 0, buf.Length);
                        builder.Append(Encoding.ASCII.GetString(buf, 0, bytes));
                    }
                    while (browserStream.DataAvailable);
                    mainHostRequest = builder.ToString();
                    ForwardRequest(browserStream, client, mainHostRequest);
                }
                catch
                {
                    return;
                }
            }
        }

        public static void ForwardRequest(NetworkStream browserStream, TcpClient client, string request)
        {
            try
            {
                string[] temp = request.Trim().Split(new char[] { '\r', '\n' });
                
                string hostLine = temp.FirstOrDefault(x => x.Contains("Host"));
                hostLine = hostLine.Substring(hostLine.IndexOf(":") + 2);
                string[] endPoint = hostLine.Trim().Split(new char[] { ':' }); //доменное имя + порт
                string[] requestLine = temp[0].Trim().Split(' ');
                string newRequestLine = requestLine[1];
                if (requestLine[1].Contains(endPoint[0]))
                {
                    newRequestLine = requestLine[1].Remove(0, requestLine[1].IndexOf(endPoint[0]));
                    newRequestLine = newRequestLine.Remove(0, newRequestLine.IndexOf('/'));
                    //Console.WriteLine(newRequestLine);
                }

                request = request.Substring(0, request.IndexOf(requestLine[1])) + newRequestLine + " " + request.Substring(request.IndexOf("HTTP/1.1"));

                //Console.WriteLine(request);

                TcpClient forwardingClient;
                if (endPoint.Length == 2)
                {
                    forwardingClient = new TcpClient(endPoint[0], int.Parse(endPoint[1]));
                }
                else
                {
                    forwardingClient = new TcpClient(endPoint[0], 80);
                }

                NetworkStream servStream = forwardingClient.GetStream(); 
                byte[] data = Encoding.ASCII.GetBytes(request); //пересылка пакета с использованием относительного пути
                servStream.Write(data, 0, data.Length); 
                var respBuf = new byte[forwardingClient.ReceiveBufferSize];

                
                
                servStream.Read(respBuf, 0, forwardingClient.ReceiveBufferSize);

                browserStream.Write(respBuf, 0, respBuf.Length);
                
               
                string[] head = Encoding.ASCII.GetString(respBuf).Split(new char[] { '\r', '\n' }); 
         
                string ResponseCode = head[0].Substring(head[0].IndexOf(" ") + 1);
                Console.WriteLine($"Request to {hostLine}\nResponse:\n");
                Console.WriteLine($"{hostLine} {ResponseCode}\n");
                servStream.CopyTo(browserStream);

            }
            catch
            {
                return;
            }
            finally
            {
                client.Dispose();
            }

        }

    }

}

