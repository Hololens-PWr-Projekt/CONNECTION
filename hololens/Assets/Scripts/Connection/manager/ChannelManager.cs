using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Hololens.Assets.Scripts.Connection.Model;
using Hololens.Assets.Scripts.Connection.Utils;
using UnityEngine;

namespace Hololens.Assets.Scripts.Connection.Manager
{
    public class ChannelManager : MonoBehaviour
    {
        private readonly ConcurrentDictionary<string, ChannelState> _channels = new();

        public IEnumerator AddChannel(ChannelConfig channelConfig)
        {
            string channelName = channelConfig.Name;
            string endpoint = channelConfig.Endpoint;

            if (_channels.ContainsKey(channelName))
            {
                Debug.Log($"Channel {channelName} has already exists.");
                yield break;
            }

            WebSocketManager manager = new(endpoint);
            yield return StartCoroutine(manager.ConnectAsync());

            ChannelState state = new(manager);
            _channels[channelName] = state;

            Action<Packet> onPacketReceived = (packet) =>
            {
                Debug.Log($"Received packet on channel {channelName}: {packet.PacketId}");
                ProcessPacket(channelName, packet);
            };

            StartCoroutine(
                ListenForPackets(manager, onPacketReceived, state.CancellationTokenSource.Token)
            );

            StartCoroutine(ProcessQueue(channelName, state.CancellationTokenSource.Token));

            Debug.Log($"Channel {channelName} added and connected.");
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
                    FileProcessor.ReassembleFile(channelName, state.PacketsReceived);
                    state.PacketsReceived.Clear();
                }
            }
            else
            {
                Debug.LogError($"Channel {channelName} is not registered.");
            }
        }

        private IEnumerator ListenForPackets(
            WebSocketManager manager,
            Action<Packet> onPacketReceived,
            CancellationToken cancellationToken
        )
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                yield return StartCoroutine(manager.ReceiveAsync(onPacketReceived));
            }
        }

        public void RemoveChannel(string channelName)
        {
            if (_channels.TryRemove(channelName, out var state))
            {
                state.CancellationTokenSource?.Cancel();
                StartCoroutine(state.Manager.CloseAsync());
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

        private IEnumerator ProcessQueue(string channelName, CancellationToken cancellationToken)
        {
            if (!_channels.TryGetValue(channelName, out var state))
            {
                Debug.LogError($"Channel {channelName} is not registered.");
                yield break;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (state.PacketQueue.TryDequeue(out var packet))
                {
                    yield return StartCoroutine(state.Manager.SendAsync(packet));
                    // Debug.Log(
                    //     $"Packet {packet.Chunk.SequenceNumber + 1}/{packet.Chunk.TotalChunks} send on channel {channelName}"
                    // );
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
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

            Debug.Log($"File transmission completed for channel {channelName}");
        }

        public IEnumerator SendSignal(string channelName, bool start)
        {
            string signal = start ? "start" : "stop";

            if (_channels.TryGetValue(channelName, out var state))
            {
                Packet signalPacket = new(
                    signal,
                    (Channel)Enum.Parse(typeof(Channel), channelName, true),
                    new Chunk(1, 1, new byte[1])
                );
                yield return StartCoroutine(state.Manager.SendAsync(signalPacket));
                Debug.Log($"{signal} signal sent for channel {channelName}");
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
            Debug.Log("ChannelManager is being destroyed and is cleaning resources...");

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
#nullable enable
        public CancellationTokenSource? CancellationTokenSource { get; }
        public List<Packet> PacketsReceived { get; }

#nullable disable
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
