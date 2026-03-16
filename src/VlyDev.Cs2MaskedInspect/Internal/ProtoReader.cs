namespace VlyDev.Cs2MaskedInspect.Internal;

internal readonly struct ProtoField
{
    internal int FieldNumber { get; init; }
    internal int WireType { get; init; }
    internal ulong VarIntValue { get; init; }
    internal byte[]? BytesValue { get; init; }
}

internal sealed class ProtoReader
{
    private readonly byte[] _data;
    private int _pos;

    internal ProtoReader(byte[] data)
    {
        _data = data;
        _pos = 0;
    }

    internal IEnumerable<ProtoField> ReadAllFields()
    {
        int fieldCount = 0;
        while (_pos < _data.Length)
        {
            if (++fieldCount > 100)
                throw new InvalidOperationException("Protobuf field count exceeds limit of 100");
            var tag = ReadVarInt();
            var wireType = (int)(tag & 0x7);
            var fieldNumber = (int)(tag >> 3);

            switch (wireType)
            {
                case 0: // varint
                    yield return new ProtoField
                    {
                        FieldNumber = fieldNumber,
                        WireType = 0,
                        VarIntValue = ReadVarInt(),
                    };
                    break;

                case 2: // length-delimited
                    var len = (int)ReadVarInt();
                    var bytes = new byte[len];
                    Buffer.BlockCopy(_data, _pos, bytes, 0, len);
                    _pos += len;
                    yield return new ProtoField
                    {
                        FieldNumber = fieldNumber,
                        WireType = 2,
                        BytesValue = bytes,
                    };
                    break;

                case 5: // 32-bit fixed
                    var fixed32 = new byte[4];
                    Buffer.BlockCopy(_data, _pos, fixed32, 0, 4);
                    _pos += 4;
                    yield return new ProtoField
                    {
                        FieldNumber = fieldNumber,
                        WireType = 5,
                        BytesValue = fixed32,
                    };
                    break;

                default:
                    throw new FormatException(
                        $"Unsupported protobuf wire type {wireType} for field {fieldNumber}.");
            }
        }
    }

    private ulong ReadVarInt()
    {
        ulong result = 0;
        int shift = 0;
        while (true)
        {
            if (_pos >= _data.Length)
                throw new FormatException("Unexpected end of data while reading varint.");
            byte b = _data[_pos++];
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
            if (shift > 63)
                throw new FormatException("Varint is too long.");
        }
        return result;
    }
}
