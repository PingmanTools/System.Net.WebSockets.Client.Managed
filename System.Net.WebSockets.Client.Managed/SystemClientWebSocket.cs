using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
   public static class SystemClientWebSocket
   {
      /// <summary>
      /// False if System.Net.WebSockets.ClientWebSocket is available on this platform, true if System.Net.WebSockets.Managed.ClientWebSocket is required.
      /// </summary>
      public static bool ManagedWebSocketRequired => _managedWebSocketRequired.Value;

      static Lazy<bool> _managedWebSocketRequired => new Lazy<bool>(CheckManagedWebSocketRequired);

      static bool CheckManagedWebSocketRequired()
      {
         try
         {
            using (var clientWebSocket = new ClientWebSocket())
            {
               return false;
            }
         }
         catch (PlatformNotSupportedException)
         {
            return true;
         }
      }

      /// <summary>
      /// Creates a ClientWebSocket that works for this platform. Uses System.Net.WebSockets.ClientWebSocket if supported or System.Net.WebSockets.Managed.ClientWebSocket if not.
      /// </summary>
      public static WebSocket CreateClientWebSocket()
      {
         if (ManagedWebSocketRequired)
         {
            return new Managed.ClientWebSocket();
         }
         else
         {
            return new ClientWebSocket();
         }
      }

      /// <summary>
      /// Creates and connects a ClientWebSocket that works for this platform. Uses System.Net.WebSockets.ClientWebSocket if supported or System.Net.WebSockets.Managed.ClientWebSocket if not.
      /// </summary>
      public static async Task<WebSocket> ConnectAsync(Uri uri, CancellationToken cancellationToken)
      {
         var clientWebSocket = CreateClientWebSocket();
         await clientWebSocket.ConnectAsync(uri, cancellationToken);
         return clientWebSocket;
      }

      public static Task ConnectAsync(this WebSocket clientWebSocket, Uri uri, CancellationToken cancellationToken)
      {
         if (clientWebSocket is ClientWebSocket)
         {
            return (clientWebSocket as ClientWebSocket).ConnectAsync(uri, cancellationToken);
         }
         else if (clientWebSocket is Managed.ClientWebSocket)
         {
            return (clientWebSocket as Managed.ClientWebSocket).ConnectAsync(uri, cancellationToken);
         }

         throw new ArgumentException("WebSocket must be an instance of System.Net.WebSockets.ClientWebSocket or System.Net.WebSockets.Managed.ClientWebSocket", nameof(clientWebSocket));
      }

   }
}
