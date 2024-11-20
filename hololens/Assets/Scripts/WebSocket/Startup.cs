using System.Collections.Generic;
using UnityEngine;
using Manager.Json;
using Manager.Networking;
using Model.Packet;
using Model.Vector3D;
using Utils.Packets;


public class Startup : MonoBehaviour
{
    private NetworkManager networkManager;

    void Awake()
    {
        networkManager = FindObjectOfType<NetworkManager>();
    }

    void Start()
    {
        var data = JsonManager.LoadFromFile("mesh_0");


        if (data != null && networkManager != null)
        {
            ProcessAndSendPackets<Vector3D>(data, PacketType.Vertices);
            ProcessAndSendPackets<int>(data, PacketType.Triangles);
        }
        else
        {
            Debug.LogError($"Failed to load JSON data or NetworkManager not initialized!");
        }
    }

    private void ProcessAndSendPackets<T>(Dictionary<string, object> data, PacketType type)
    {
        string key = PacketTypeExtensions.GetDescription(type);

        if (data.ContainsKey(key))
        {
            var vertexDataJson = JsonManager.Serialize(data[key]);
            var vertexData = JsonManager.Deserialize<List<T>>(vertexDataJson);

            SendPackets(vertexData, type);
        }
        else
        {
            Debug.LogWarning($"No {key} data found in the input JSON.");
        }
    }

    private void SendPackets<T>(List<T> data, PacketType type)
    {
        string serializedValue = JsonManager.Serialize(data);

        // Split data into packets
        int counter = 0;
        string packetId = "data_" + counter;
        List<Packet> packets = PacketUtils.SplitData(packetId, type, serializedValue);
        // Send packets using the network manager
        networkManager.SendPackets(
            packets,
            onPacketSent: (Packet packet) =>
            {
                Debug.Log($"Packet {packet.Index} sent successfully for type: {packet.Type}");
            },
            onAllPacketsSent: (string packetId) =>
            {
                Debug.Log($"All packets sent for packetId: {packetId}");
                counter++;
            }
        );
    }
}
