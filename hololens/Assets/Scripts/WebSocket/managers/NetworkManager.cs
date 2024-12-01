using System.Collections.Generic;
using NativeWebSocket;
using UnityEngine;
using Manager.Json;
using Model.Packet;
using System.Collections;
using Utils.Constants;
using System;
using System.Text;

namespace Manager.Networking
{
  public class NetworkManager : MonoBehaviour
  {
    private WebSocket webSocket;
    private Dictionary<string, List<Packet>> pendingPackets;

    void Awake()
    {
      pendingPackets = new Dictionary<string, List<Packet>>();
      ConnectToServer();
      // Try to reconnect to a server every 5s
      StartCoroutine(CheckConnectionStatus());
    }

    async void ConnectToServer()
    {
      webSocket = new WebSocket(Constants.WEBSOCKET_URL);

      webSocket.OnOpen += () => Debug.Log("Connection open!");
      webSocket.OnError += (e) => Debug.Log("Error! " + e);
      webSocket.OnClose += (_) => Debug.Log("Connection closed!");

      // Receive data from the server
      webSocket.OnMessage += (bytes) =>
      {
        var message = Encoding.UTF8.GetString(bytes);
        Debug.Log("Received OnMessage! (" + bytes.Length + " bytes) " + message);
      };

      await webSocket.Connect();
    }

    public async void SendPackets(List<Packet> packets, Action<Packet> onPacketSent = null, Action<string> onAllPacketsSent = null)
    {
      if (ArePacketsEmpty(packets))
      {
        Debug.LogWarning("No packed to send.");
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

        if (IsWebSocketClosed())
        {
          Debug.LogError("WebSocket is not connected! Cannot send packets!");
          break;
        }

        await webSocket.SendText(packetJson);
        onPacketSent?.Invoke(packet);
        pendingPackets[packetId].Remove(packet);

        if (IsLastPacket(packet))
        {
          pendingPackets.Remove(packetId);
          onAllPacketsSent?.Invoke(packetId);
        }
      }
    }

    private IEnumerator CheckConnectionStatus()
    {
      while (true)
      {
        yield return new WaitForSeconds(Constants.RECONNECTION_INTERVAL);

        if (IsWebSocketClosed())
        {
          Debug.Log("WebSocket not connected. Attempting to reconnect...");
          ConnectToServer();
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
      if (!IsWebSocketClosed())
      {
        await webSocket.Close();
      }
    }

    private bool IsWebSocketClosed() => webSocket.State == WebSocketState.Closed;

    private bool ArePacketsEmpty(List<Packet> packets) => packets.Count == 0;

    private bool IsLastPacket(Packet packet) => packet.Chunk.SequenceNumber == packet.Chunk.TotalChunks && pendingPackets[packet.PacketId].Count == 0;
  }
}