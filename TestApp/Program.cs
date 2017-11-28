using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net.WebSockets.Managed;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        const string WS_TEST_SERVER = "ws://echo.websocket.org";
        const string WSS_TEST_SERVER = "wss://echo.websocket.org";

        static void Main(string[] args)
        {
            TestConnection(WS_TEST_SERVER).GetAwaiter().GetResult();
            TestConnection(WSS_TEST_SERVER).GetAwaiter().GetResult();
        }

        static async Task TestConnection(string server)
        {
            using (var ws = new System.Net.WebSockets.Managed.ClientWebSocket())
            {
                await ws.ConnectAsync(new Uri(server), CancellationToken.None);

                var buffer = new ArraySegment<byte>(new byte[1024]);
                var readTask = ws.ReceiveAsync(buffer, CancellationToken.None);

                const string msg = "hello";
                var testMsg = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
                await ws.SendAsync(testMsg, WebSocketMessageType.Text, true, CancellationToken.None);

                var read = await readTask;
                var reply = Encoding.UTF8.GetString(buffer.Array, 0, read.Count);

                if (reply != msg)
                {
                    throw new Exception($"Expected to read back '{msg}' but got '{reply}' for server {server}");
                }
                Console.WriteLine("Success connecting to server " + server);
            }
        }
    }
}
