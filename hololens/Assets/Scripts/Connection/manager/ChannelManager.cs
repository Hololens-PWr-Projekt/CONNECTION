using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hololens.Assets.Scripts.Connection.Model;
using Hololens.Assets.Scripts.Connection.Utils;
using UnityEngine;

namespace Hololens.Assets.Scripts.Connection.Manager
{
    public class ChannelManager : MonoBehaviour
    {
        private readonly ConcurrentDictionary<string, ChannelState> _channels = new();
        private AdvancedLogger _logger;

        private void Start()
        {
            string logDirectoryPath = Path.Combine(
                Application.persistentDataPath,
                "ServerWebRTC_Logs"
            );
            _logger = new AdvancedLogger(logDirectoryPath);
        }

        public async Task AddChannelAsync(ChannelConfig channelConfig)
        {
            string channelName = channelConfig.Name;
            string endpoint = channelConfig.Endpoint;

            if (_channels.ContainsKey(channelName))
            {
                await _logger.LogAsync($"Channel {channelName} already exists.");
                return;
            }

            var manager = new WebSocketManager(endpoint);
            await manager.ConnectAsync();

            var state = new ChannelState(manager);
            _channels[channelName] = state;

            Action<Packet> onPacketReceived = packet =>
            {
                _logger.Log($"Received packet on channel {channelName}: {packet.PacketId}");
                ProcessPacket(channelName, packet);
            };

            _ = ListenForPacketsAsync(
                manager,
                onPacketReceived,
                state.CancellationTokenSource.Token
            );
            _ = ProcessQueueAsync(channelName, state.CancellationTokenSource.Token);

            await _logger.LogAsync($"Channel {channelName} added and connected.");
        }

        private void ProcessPacket(string channelName, Packet packet)
        {
            if (_channels.TryGetValue(channelName, out var state))
            {
                state.PacketsReceived.Add(packet);

                if (
                    packet.Chunk.SequenceNumber == packet.Chunk.TotalChunks - 1
                    && state.PacketsReceived.Count == packet.Chunk.TotalChunks
                )
                {
                    FileProcessor.ReassembleFileAsync(channelName, state.PacketsReceived);
                    state.PacketsReceived.Clear();
                }
            }
            else
            {
                _logger.Log($"Channel {channelName} is not registered.");
            }
        }

        private async Task ListenForPacketsAsync(
            WebSocketManager manager,
            Action<Packet> onPacketReceived,
            CancellationToken cancellationToken
        )
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await manager.ReceiveAsync(onPacketReceived);
                }
                catch (Exception ex)
                {
                    await _logger.LogAsync($"Error listening for packets: {ex.ToString()}");
                    break;
                }
            }
        }

        public async Task RemoveChannelAsync(string channelName)
        {
            if (_channels.TryRemove(channelName, out var state))
            {
                state.CancellationTokenSource?.Cancel();
                await state.Manager.CloseAsync();
                await _logger.LogAsync($"Channel {channelName} removed.");
            }
        }

        public void EnqueuePacket(string channelName, Packet packet)
        {
            if (_channels.TryGetValue(channelName, out var state))
            {
                state.PacketQueue.Enqueue(packet);
            }
            else
            {
                _logger.Log($"Channel {channelName} is not registered.");
            }
        }

        private async Task ProcessQueueAsync(
            string channelName,
            CancellationToken cancellationToken
        )
        {
            if (!_channels.TryGetValue(channelName, out var state))
            {
                await _logger.LogAsync($"Channel {channelName} is not registered.");
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (state.PacketQueue.TryDequeue(out var packet))
                {
                    await state.Manager.SendAsync(packet);
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }

        public async Task TransmitFileAsync(string channelName, byte[] data)
        {
            if (!_channels.TryGetValue(channelName, out var _))
            {
                await _logger.LogAsync($"Channel {channelName} is not registered.");
                return;
            }

            var packets = await FileProcessor.SplitFileAsync(channelName, data);

            foreach (var packet in packets)
            {
                EnqueuePacket(channelName, packet);
                await Task.Yield();
            }

            await _logger.LogAsync($"File transmission completed for channel {channelName}");
        }

        public async Task SendSignalAsync(string channelName, bool start)
        {
            string signal = start ? "start" : "stop";

            if (_channels.TryGetValue(channelName, out var state))
            {
                var signalPacket = new Packet(
                    signal,
                    (Channel)Enum.Parse(typeof(Channel), channelName, true),
                    new Chunk(1, 1, new byte[1])
                );
                await state.Manager.SendAsync(signalPacket);
                await _logger.LogAsync($"{signal} signal sent for channel {channelName}");
            }
            else
            {
                await _logger.LogAsync($"Channel {channelName} is not registered.");
            }
        }

        public bool IsChannelOpen(string channelName)
        {
            return _channels.TryGetValue(channelName, out var state)
                && state.Manager.IsWebSocketOpen();
        }

        private async void OnDestroy()
        {
            await _logger.LogAsync("ChannelManager is being destroyed and cleaning resources...");

            foreach (var channelName in _channels.Keys)
            {
                await RemoveChannelAsync(channelName);
            }

            await _logger.LogAsync(("ChannelManager cleanup complete."));
        }
    }

    internal class ChannelState
    {
        public WebSocketManager Manager { get; }
        public ConcurrentQueue<Packet> PacketQueue { get; }
        public CancellationTokenSource CancellationTokenSource { get; }
        public List<Packet> PacketsReceived { get; }

        public ChannelState(WebSocketManager manager)
        {
            Manager = manager;
            PacketQueue = new ConcurrentQueue<Packet>();
            CancellationTokenSource = new CancellationTokenSource();
            PacketsReceived = new List<Packet>();
        }
    }

    public class ChannelConfig
    {
        public string Name { get; }
        public string Endpoint { get; }

        public ChannelConfig(string name, string endpoint)
        {
            Name = name;
            Endpoint = endpoint;
        }
    }
}
