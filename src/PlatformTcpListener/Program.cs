using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PlatformTcpListener
{
    // This is the most hardcoded thing possible. It doesn't even really do HTTP.
    public class Program
    {
        const string ResponseText = @"
HTTP/1.1 200 OK
Server: TcpListener
Date: Thu, 26 Sep 2019 15:53:58 GMT
Content-Type: application/json
Content-Length: 503

[{""date"":""2019-09-27T08:53:59.4733011-07:00"",""temperatureC"":35,""temperatureF"":94,""summary"":""Hot""},{""date"":""2019-09-28T08:53:59.4733167-07:00"",""temperatureC"":30,""temperatureF"":85,""summary"":""Sweltering""},{""date"":""2019-09-29T08:53:59.4733242-07:00"",""temperatureC"":-5,""temperatureF"":24,""summary"":""Hot""},{""date"":""2019-09-30T08:53:59.4733299-07:00"",""temperatureC"":10,""temperatureF"":49,""summary"":""Bracing""},{""date"":""2019-10-01T08:53:59.4733355-07:00"",""temperatureC"":-14,""temperatureF"":7,""summary"":""Scorching""}]";

        static byte[] ResponseBytes;

        public static async Task Main(string[] args)
        {
            ResponseBytes = Encoding.ASCII.GetBytes(ResponseText);

            var listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            Console.WriteLine("Application Started.");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => ProcessClient(client));
            }
        }

        private static async Task ProcessClient(TcpClient client)
        {
            var stream = client.GetStream();

            var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
            _ = await stream.ReadAsync(buffer);
            ArrayPool<byte>.Shared.Return(buffer);

            await stream.WriteAsync(ResponseBytes);

            client.Close();
        }
    }
}
