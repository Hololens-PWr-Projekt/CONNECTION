using UnityEngine;
using Manager.Json;
using Manager.Networking;
using Model.Packet;
using Model.Vector3D;
using Utils.Packets;
using System.Collections.Generic;


public class Startup : MonoBehaviour
{
    private NetworkManager networkManager;

    void Awake()
    {
        Debug.Log("Startup - Initializing application...");
        networkManager = FindObjectOfType<NetworkManager>();
    }

    void Start()
    {
        var data = JsonManager.LoadFromFile("mesh_3");

        if (data != null && networkManager != null)
        {
            ProcessAndSendPackets(data);
        }
        else
        {
            Debug.LogError($"Failed to load mesh_0.json data.");
        }
    }

    private void ProcessAndSendPackets(Dictionary<string, object> data)
    {
        if (data.ContainsKey("vertices"))
        {
            var vertexDataJson = JsonManager.ToJson(data["vertices"]);
            var vertexData = JsonManager.FromJson<List<Vector3D>>(vertexDataJson);

            SendPackets(vertexData, PacketType.Vertices);
        }
        else
        {
            Debug.LogWarning("No 'vertices' data found in the input JSON.");
        }

        if (data.ContainsKey("triangles"))
        {
            var triangleDataJson = JsonManager.ToJson(data["triangles"]);
            var triangleData = JsonManager.FromJson<List<int>>(triangleDataJson);


            SendPackets(triangleData, PacketType.Triangles);
        }
        else
        {
            Debug.LogWarning("No 'triangles' data found in the input JSON.");
        }
    }

    private void SendPackets<T>(List<T> data, PacketType type)
    {
        string serializedValue = JsonManager.ToJson(data);

        // Split data into packets
        int counter = 0;
        string packetId = "data_" + counter;
        List<Packet> packets = PacketUtils.SplitData(packetId, type, serializedValue);
        // Send packets using the network manager
        networkManager.SendPackets(
            packets,
            onPacketSent: (Packet packet) =>
            {
                Debug.Log($"Packet {packet.Index} sent successfully for dataType: {packet.Type}");
            },
            onAllPacketsSent: (string packetId) =>
            {
                Debug.Log($"All packets sent for packetId: {packetId}");
                counter++;
            }
        );
    }
}
