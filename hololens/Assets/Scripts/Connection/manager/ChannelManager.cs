using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hololens.Assets.Scripts.Connection.Model;
using Hololens.Assets.Scripts.Connection.Utils;
using Unity.VisualScripting;
using UnityEngine;

namespace Hololens.Assets.Scripts.Connection.Manager
{
    public class ChannelManager : MonoBehaviour
    {
        private readonly ConcurrentDictionary<string, ChannelState> _channels = new();

        public async void AddChannel(ChannelConfig channelConfig)
        {
            string channelName = channelConfig.Name;
            string endpoint = channelConfig.Endpoint;

            if (_channels.ContainsKey(channelName))
            {
                Debug.Log($"Channel {channelName} already exists.");
                return;
            }

            WebSocketManager manager = new(endpoint);
            await manager.ConnectAsync();

            ChannelState state = new(manager);
            _channels[channelName] = state;

            Action<Packet> onPacketReceived = (packet) =>
            {
                Debug.Log($"Received packet on channel {channelName}: {packet.PacketId}");
                ProcessPacket(channelName, packet);
            };

            ProcessQueue(channelName, state.CancellationTokenSource.Token);

            Debug.Log($"Channel {channelName} added and connected.");
        }

        private async void ProcessPacket(string channelName, Packet packet)
        {
            if (_channels.TryGetValue(channelName, out var state))
            {
                state.PacketsReceived.Add(packet);

                if (
                    packet.Chunk.SequenceNumber == packet.Chunk.TotalChunks - 1
                    && state.PacketsReceived.Count == packet.Chunk.TotalChunks
                )
                {
                    await FileProcessor.ReassembleFileAsync(channelName, state.PacketsReceived);
                    state.PacketsReceived.Clear();
                }
            }
            else
            {
                Debug.LogError($"Channel {channelName} is not registered.");
            }
        }

        public async void RemoveChannel(string channelName)
        {
            if (_channels.TryRemove(channelName, out var state))
            {
                state.CancellationTokenSource?.Cancel();
                await state.Manager.CloseAsync();
                Debug.Log($"Channel {channelName} removed.");
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
                Debug.LogError($"Channel {channelName} is not registered.");
            }
        }

        private async void ProcessQueue(string channelName, CancellationToken cancellationToken)
        {
            if (!_channels.TryGetValue(channelName, out var state))
            {
                Debug.LogError($"Channel {channelName} is not registered.");
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
                    await Task.Delay(600);
                }
            }

            Debug.Log($"Stopped processing queue for channel {channelName} due to cancellation.");
        }

        public IEnumerator TransmitFile(string channelName, byte[] data)
        {
            if (!_channels.TryGetValue(channelName, out var _))
            {
                Debug.LogError($"Channel {channelName} is not registered.");
                yield break;
            }

            var packets = FileProcessor.SplitFile(channelName, data);

            foreach (var packet in packets)
            {
                EnqueuePacket(channelName, packet);
                yield return null;
            }

            Debug.Log($"File transmission completed for channel {channelName}.");
        }

        public async void SendSignal(string channelName, bool start)
        {
            string signal = start ? "start" : "stop";

            if (_channels.TryGetValue(channelName, out var state))
            {
                Packet signalPacket = new(
                    signal,
                    (Channel)Enum.Parse(typeof(Channel), channelName, true),
                    new Chunk(1, 1, new byte[1])
                );
                await state.Manager.SendAsync(signalPacket);
                Debug.Log($"{signal} signal sent for channel {channelName}.");
            }
            else
            {
                Debug.LogError($"Channel {channelName} is not registered.");
            }
        }

        public bool IsChannelOpen(string channelName)
        {
            if (_channels.TryGetValue(channelName, out var state))
            {
                return state.Manager.IsWebSocketOpen();
            }

            return false;
        }

        private void OnDestroy()
        {
            Debug.Log("ChannelManager is being destroyed and cleaning resources...");

            foreach (var channelName in _channels.Keys)
            {
                RemoveChannel(channelName);
            }

            Debug.Log("ChannelManager cleanup complete.");
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
