using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Server.Manager;
using Server.Src.Manager;
using Server.Src.Model;
using Server.Src.Utils;

namespace Server.Src
{
    class Program
    {
        private static readonly ChannelManager _channelManager = new();

        static async Task Main(string[] args)
        {
            HttpListener httpListener = new();

            CancellationTokenSource cts = new();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("Shutting down server...");
                cts.Cancel();
            };

            // Define prefixes
            var prefixes = new[] { "http://localhost:8080/mesh/", "http://localhost:8080/hands/" };

            // Start a listener for each prefix
            var listenerTasks = prefixes
                .Select(prefix => StartHttpListenerForPrefix(httpListener, prefix, cts.Token))
                .ToArray();

            Console.WriteLine("Server started. Press Ctrl+C to shut down.");
            await Task.WhenAll(listenerTasks);
        }

        private static async Task StartHttpListenerForPrefix(
            HttpListener httpListener,
            string prefix,
            CancellationToken token
        )
        {
            httpListener.Prefixes.Add(prefix);
            httpListener.Start();
            Console.WriteLine($"Listening on {prefix}");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var context = await httpListener.GetContextAsync();
                    _ = Task.Run(
                        async () =>
                        {
                            try
                            {
                                if (context.Request.IsWebSocketRequest)
                                {
                                    var webSocketContext = await context.AcceptWebSocketAsync(null);
                                    var manager = new WebSocketManager(webSocketContext.WebSocket);

                                    string channelName = GetChannelNameFromPrefix(prefix);
                                    _channelManager.AddChannel(channelName, manager);

                                    Console.WriteLine(
                                        $"Client connected to channel: {channelName}"
                                    );
                                    await HandleClientPacketsAsync(channelName, manager, token);
                                }
                                else
                                {
                                    context.Response.StatusCode = 400; // Bad Request
                                    context.Response.Close();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(
                                    $"Error handling connection for {prefix}: {ex.Message}"
                                );
                            }
                        },
                        token
                    );
                }
            }
            catch (HttpListenerException) when (token.IsCancellationRequested)
            {
                Console.WriteLine($"Listener for {prefix} shutting down.");
            }
            finally
            {
                httpListener.Stop();
                Console.WriteLine($"Listener for {prefix} stopped.");
            }
        }

        private static string GetChannelNameFromPrefix(string prefix)
        {
            string[] segments = prefix.TrimEnd('/').Split('/');
            return segments[^1];
        }

        private static async Task HandleClientPacketsAsync(
            string channelName,
            WebSocketManager manager,
            CancellationToken cancellationToken
        )
        {
            try
            {
                PacketTimeTracker timeTracker = new();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var packet = await manager.ReceivePacketAsync();

                    if (packet != null)
                    {
                        _channelManager.EnqueuePacket(channelName, packet);
                        timeTracker.PrintPacketTransferTime(packet);
                        if (packet.Chunk.SequenceNumber + 1 == packet.Chunk.TotalChunks)
                        {
                            timeTracker.PrintAveragePacketTransferTime();
                        }
                    }
                    else
                    {
                        await Task.Delay(50, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in channel {channelName}: {ex.Message}");
            }
            finally
            {
                _channelManager.RemoveChannel(channelName);
                Console.WriteLine($"Channel {channelName} disconnected and removed.");
            }
        }
    }
}
