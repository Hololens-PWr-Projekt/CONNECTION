using System;
using System.ComponentModel;
using System.Reflection;

namespace Model.Packet
{
  [Serializable]
  public class Packet
  {
    private string packetId;
    private string packetType;
    private int index;
    private bool isLast;
    private string chunk;

    public Packet(string packetId, PacketType type, int index, bool isLast, string chunk)
    {
      this.PacketId = packetId;
      this.PacketType = PacketTypeExtensions.GetDescription(type);
      this.Index = index;
      this.IsLast = isLast;
      this.Chunk = chunk;
    }

    public string PacketId { get => packetId; set => packetId = value; }
    public string PacketType { get => packetType; set => packetType = value; }
    public int Index { get => index; set => index = value; }
    public bool IsLast { get => isLast; set => isLast = value; }
    public string Chunk { get => chunk; set => chunk = value; }
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