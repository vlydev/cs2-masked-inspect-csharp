using System.Text;

namespace VlyDev.Cs2MaskedInspect.Internal;

internal sealed class ProtoWriter
{
    private readonly List<byte> _buf = new();

    internal void WriteUInt32(int field, uint value)
    {
        if (value == 0) return;
        WriteTag(field, 0);
        WriteVarInt(value);
    }

    internal void WriteInt32(int field, int value)
    {
        if (value == 0) return;
        WriteTag(field, 0);
        // Negative int32 is sign-extended to 64-bit before varint encoding (protobuf spec).
        WriteVarInt(value < 0 ? (ulong)(long)value : (ulong)value);
    }

    internal void WriteUInt64(int field, ulong value)
    {
        if (value == 0) return;
        WriteTag(field, 0);
        WriteVarInt(value);
    }

    internal void WriteString(int field, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(field, 2);
        WriteVarInt((ulong)bytes.Length);
        _buf.AddRange(bytes);
    }

    internal void WriteFloat32Fixed(int field, float value)
    {
        WriteTag(field, 5);
        var bits = BitConverter.SingleToUInt32Bits(value);
        // Little-endian 32-bit
        _buf.Add((byte)(bits & 0xFF));
        _buf.Add((byte)((bits >> 8) & 0xFF));
        _buf.Add((byte)((bits >> 16) & 0xFF));
        _buf.Add((byte)((bits >> 24) & 0xFF));
    }

    internal void WriteRawBytes(int field, byte[] bytes)
    {
        if (bytes.Length == 0) return;
        WriteTag(field, 2);
        WriteVarInt((ulong)bytes.Length);
        _buf.AddRange(bytes);
    }

    internal byte[] ToBytes() => _buf.ToArray();

    private void WriteTag(int field, int wireType) =>
        WriteVarInt((ulong)((field << 3) | wireType));

    private void WriteVarInt(ulong value)
    {
        while (value > 0x7F)
        {
            _buf.Add((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }
        _buf.Add((byte)(value & 0x7F));
    }
}
