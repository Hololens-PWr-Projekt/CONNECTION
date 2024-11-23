using System.Collections.Generic;
using NativeWebSocket;
using UnityEngine;
using Manager.Json;
using Model.Packet;

namespace Manager.Networking
{

  public class NetworkManager : MonoBehaviour
  {
    public delegate void PacketSentCallback(Packet packet);
    public delegate void AllPacketsSentCallback(string packetId);

    private WebSocket webSocket;
    private Dictionary<string, List<Packet>> pendingPackets;

    void Awake()
    {
      pendingPackets = new Dictionary<string, List<Packet>>();
      ConnectToServer();
    }

    async void ConnectToServer()
    {
      webSocket = new WebSocket("ws://localhost:8080/ws/hololens");

      webSocket.OnOpen += () => Debug.Log("NetworkManager - Connection open!");
      webSocket.OnError += (e) => Debug.Log("NetworkManager - Error! " + e);
      webSocket.OnClose += (e) => Debug.Log("NetworkManager - Connection closed!");

      // Receive data from the server
      webSocket.OnMessage += (bytes) =>
      {
        var message = System.Text.Encoding.UTF8.GetString(bytes);
        Debug.Log("Received OnMessage! (" + bytes.Length + " bytes) " + message);
      };

      await webSocket.Connect();
    }

    public async void SendPackets(List<Packet> packets, PacketSentCallback onPacketSent = null, AllPacketsSentCallback onAllPacketsSent = null)
    {
      if (ArePacketsEmpty(packets))
      {
        Debug.LogWarning("NetworkManager - No packed to send.");
        return;
      }

      string packetId = packets[0].PacketId;

      if (!pendingPackets.ContainsKey(packetId))
      {
        pendingPackets[packetId] = new List<Packet>(packets);
      }

      foreach (var packet in packets)
      {
        string packetJson = JsonManager.Serialize(packet);

        if (IsWebSocketOpen())
        {
          await webSocket.SendText(packetJson);
          onPacketSent?.Invoke(packet);
          pendingPackets[packetId].Remove(packet);

          if (IsLastPacket(packet))
          {
            pendingPackets.Remove(packetId);
            onAllPacketsSent?.Invoke(packetId);
          }
        }
        else
        {
          Debug.LogError("NetworkManager - WebSocket is not connected! Cannot send packets!");
          break;
        }
      }
    }

    void Update()
    {
      // Poll the WebSocket on each frame to receive messages
      webSocket?.DispatchMessageQueue();
    }

    private async void OnApplicationQuit()
    {
      if (IsWebSocketOpen())
      {
        await webSocket.Close();
      }
    }

    private bool IsWebSocketOpen()
    {
      return webSocket.State == WebSocketState.Open;
    }

    private bool ArePacketsEmpty(List<Packet> packets)
    {
      return packets.Count == 0;
    }

    private bool IsLastPacket(Packet packet)
    {
      return packet.Chunk.SequenceNumber == packet.Chunk.TotalChunks && pendingPackets[packet.PacketId].Count == 0;
    }
  }
}