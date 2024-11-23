using System.Collections.Generic;
using System;
using UnityEngine;
using Manager.Json;
using Manager.Networking;
using Model.Packet;
using Model.Vertex;
using Utils.Packets;

public class Startup : MonoBehaviour
{
    private NetworkManager networkManager;

    void Awake()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        JsonManager.SetSerializerSettings();
    }

    public void OnTestButtonClicked()
    {
        Dictionary<string, object> fileData = JsonManager.LoadFromFile("mesh_3");

        if (CanDataBeSent(fileData))
        {
            ProcessAndSendPackets<List<Vertex>>(fileData, PacketType.Vertices);
            ProcessAndSendPackets<List<int>>(fileData, PacketType.Triangles);
        }
        else
        {
            Debug.LogError($"Failed to load JSON data or NetworkManager not initialized!");
        }
    }

    private void ProcessAndSendPackets<T>(Dictionary<string, object> fileData, PacketType type)
    {
        string key = PacketTypeExtensions.GetDescription(type);

        if (fileData.ContainsKey(key))
        {
            string dataJson = JsonManager.Serialize(fileData[key]);
            var data = JsonManager.Deserialize<T>(dataJson);
            SendPackets(data, type);
        }
        else
        {
            Debug.LogWarning($"No {key} data found in the input JSON.");
        }
    }

    private void SendPackets<T>(T data, PacketType type)
    {
        string serializedValue = JsonManager.Serialize(data);
        string packetId = Guid.NewGuid().ToString();

        List<Packet> packets = PacketUtils.Split(packetId, type, serializedValue);

        networkManager.SendPackets(
            packets,
            onPacketSent: (Packet packet) =>
            {
                Debug.Log($"Packet {packet.Chunk.SequenceNumber}/{packet.Chunk.TotalChunks} sent successfully.");
            },
            onAllPacketsSent: (string completedPacketId) =>
            {
                Debug.Log($"All packets sent for packetId: {completedPacketId}");
            }
        );
    }


    private bool CanDataBeSent(object data)
    {
        return data != null && networkManager != null;
    }
}
