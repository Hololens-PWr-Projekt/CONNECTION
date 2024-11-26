using System;
using System.ComponentModel;
using System.Reflection;

namespace Server.Model
{
  [Serializable]
  public class Packet
  {
    private string packetId;
    private string packetType;
    private Chunk chunk;
    private Metadata metadata;

    public Packet(string packetId, PacketType type, Chunk chunk, Metadata metadata)
    {
      this.PacketId = packetId;
      this.PacketType = PacketTypeExtensions.GetDescription(type);
      this.Chunk = chunk;
      this.Metadata = metadata;
    }

    public string PacketId { get => packetId; set => packetId = value; }
    public string PacketType { get => packetType; set => packetType = value; }
    public Chunk Chunk { get => chunk; set => chunk = value; }
    public Metadata Metadata { get => metadata; set => metadata = value; }
    public bool IsLast()
    {
      return chunk.SequenceNumber == chunk.TotalChunks;
    }
  }

  [Serializable]
  public class Chunk
  {
    private int sequenceNumber;
    private int totalChunks;
    private string data;

    public Chunk(int sequenceNumber, int totalChunks, string data)
    {
      this.SequenceNumber = sequenceNumber;
      this.TotalChunks = totalChunks;
      this.Data = data;
    }

    public int SequenceNumber { get => sequenceNumber; set => sequenceNumber = value; }
    public int TotalChunks { get => totalChunks; set => totalChunks = value; }
    public string Data { get => data; set => data = value; }
  }

  [Serializable]
  public class Metadata
  {
    private int chunkSizeBytes;

    public Metadata(int chunkSizeBytes)
    {
      this.ChunkSizeBytes = chunkSizeBytes;
    }

    public int ChunkSizeBytes { get => chunkSizeBytes; set => chunkSizeBytes = value; }
  }

  // In future, get PacketTypes from server on startup
  public enum PacketType
  {
    [Description("vertices")]
    Vertices,
    [Description("triangles")]
    Triangles
  }

  // Helper class for getting enum's string description
  public static class PacketTypeExtensions
  {
    public static string GetDescription(this Enum value)
    {
      Type type = value.GetType();
      string name = Enum.GetName(type, value);
      if (name != null)
      {
        FieldInfo field = type.GetField(name);
        if (field != null)
        {
          if (Attribute.GetCustomAttribute(field,
                   typeof(DescriptionAttribute)) is DescriptionAttribute attr)
          {
            return attr.Description;
          }
        }
      }
      return null;
    }
  }
}