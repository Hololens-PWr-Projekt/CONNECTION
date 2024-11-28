using System;
using System.Collections.Generic;
using Manager.Json;
using Manager.Networking;
using Model.Packet;
using Model.Vertex;
using UnityEngine;
using Utils.Constants;
using Utils.Packets;

public class Startup : MonoBehaviour
{
    private NetworkManager networkManager;

    void Awake()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        JsonManager.SetSerializerSettings();
    }

    // TODO dont load file into memory at once use JsonTextReader
    public void OnTestButtonClicked()
    {
        Dictionary<string, object> fileData = JsonManager.LoadFromFile("mesh_0");

        if (DataCanBeSent(fileData))
        {
            ProcessAndSendPackets<List<Vertex>>(fileData, Constants.VERTICES);
            ProcessAndSendPackets<List<int>>(fileData, Constants.TRIANGLES);
        }
        else
        {
            Debug.LogError("Failed to load JSON data or NetworkManager not initialized!");
        }
    }

    private void ProcessAndSendPackets<T>(Dictionary<string, object> fileData, string type)
    {
        if (fileData.ContainsKey(type))
        {
            string dataJson = JsonManager.Serialize(fileData[type]);
            var data = JsonManager.Deserialize<T>(dataJson);

            SendPackets(data, type);
        }
        else
        {
            Debug.LogWarning($"No {type} data found in the input JSON.");
        }
    }

    private void SendPackets<T>(T data, string type)
    {
        string serializedValue = JsonManager.Serialize(data);
        string packetId = Guid.NewGuid().ToString();

        List<Packet> packets = PacketUtils.Split(packetId, type, serializedValue);

        networkManager.SendPackets(
            packets,
            onPacketSent: (Packet packet) =>
                Debug.Log(
                    $"Packet {packet.Chunk.SequenceNumber}/{packet.Chunk.TotalChunks} sent successfully."
                ),
            onAllPacketsSent: (string completedPacketId) =>
                Debug.Log($"All packets sent for packetId: {completedPacketId}")
        );
    }

    private bool DataCanBeSent(object data)
    {
        return data != null && networkManager != null;
    }
}
