using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
   static class SocketExtensions
   {
      public static async Task<SocketAsyncEventArgs> ConnectAsync(this Socket socket, IPAddress address, int port)
      {
         var tcs = new TaskCompletionSource<SocketAsyncEventArgs>();

         var eventArgs = new SocketAsyncEventArgs();
         eventArgs.RemoteEndPoint = new IPEndPoint(address, port);
         eventArgs.Completed += (sender, args) =>
         {
            if (args.SocketError != SocketError.Success)
            {
               tcs.TrySetException(new SocketException((int)args.SocketError));
            }
            else
            {
               tcs.TrySetResult(args);
            }
         };

         var connectResult = socket.ConnectAsync(eventArgs);
         if (!connectResult)
         {
            tcs.TrySetResult(null);
         }

         return await tcs.Task;
      }
   }

   static class UriExtensions
   {
      public static string GetIdnHost(this Uri uri)
      {
         return new Globalization.IdnMapping().GetAscii(uri.Host);
      }
   }

   static class TaskUtil
   {
      public static Task CompletedTask
      {
         get
         {
            var t = new TaskCompletionSource<bool>();
            t.SetResult(true);
            return t.Task;
         }
      }

      public static Task<T> FromCanceled<T>(CancellationToken cancellationToken)
      {
         if (!cancellationToken.IsCancellationRequested)
         {
            throw new ArgumentOutOfRangeException("Cancellation has not been requested for cancellationToken; its IsCancellationRequested property is false.");
         }
         return new Task<T>(() => default(T), cancellationToken, TaskCreationOptions.None);
      }

      public static Task FromCanceled(CancellationToken cancellationToken)
      {
         return FromCanceled<bool>(cancellationToken);
      }

      public static Task<T> FromException<T>(Exception exception)
      {
         if (exception == null)
         {
            throw new ArgumentNullException(nameof(exception));
         }
         var tcs = new TaskCompletionSource<T>();
         tcs.TrySetException(exception);
         return tcs.Task;
      }

      public static Task FromException(Exception exception)
      {
         return FromException<bool>(exception);
      }

   }
}

namespace System.Net.WebSockets.Managed
{
   public static class NetEventSource
   {
      public const bool IsEnabled = false;
      public static void Enter(params object[] p) { }
      public static void Exit(params object[] p) { }
      public static void Error(params object[] p) { }
   }
}