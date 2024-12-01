using System;
using System.Collections.Generic;
using System.Text;
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
            ProcessAndSendPackets(fileData, Constants.VERTICES);
            ProcessAndSendPackets(fileData, Constants.TRIANGLES);
        }
        else
        {
            Debug.LogError("Failed to load JSON data or NetworkManager not initialized!");
        }
    }

    private void ProcessAndSendPackets(
        Dictionary<string, object> fileData,
        string type,
        bool forceSplitPacket = true
    )
    {
        if (fileData.ContainsKey(type))
        {
            string dataJson = JsonManager.Serialize(fileData[type]);
            bool isMaxChunkBytesExceeded =
                Encoding.UTF8.GetByteCount(dataJson) > Constants.MAX_CHUNK_BYTES;

            SendPackets(dataJson, type, forceSplitPacket && isMaxChunkBytesExceeded);
        }
        else
        {
            Debug.LogWarning($"No {type} data found in the input JSON.");
        }
    }

    private void SendPackets(string dataJson, string type, bool splitPacket)
    {
        string packetId = Guid.NewGuid().ToString();
        List<Packet> packets = splitPacket
            ? PacketUtils.Split(packetId, type, dataJson)
            : PacketUtils.CreatePacket(packetId, type, dataJson);

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
