using System.Text;
using System.Text.RegularExpressions;
using VlyDev.Cs2MaskedInspect.Internal;

namespace VlyDev.Cs2MaskedInspect;

/// <summary>
/// Encodes and decodes CS2 masked inspect links.
///
/// Binary format:
///   [key_byte] [proto_bytes XOR'd with key] [4-byte checksum XOR'd with key]
///
/// For tool-generated links key_byte = 0x00 (no XOR needed).
/// For native CS2 links key_byte != 0x00 — every byte must be XOR'd before parsing.
///
/// Checksum:
///   buffer   = 0x00 + proto_bytes
///   crc      = crc32(buffer)
///   xored    = (crc &amp; 0xffff) ^ (len(proto_bytes) * crc)  [unsigned 32-bit]
///   checksum = big-endian uint32 of (xored &amp; 0xFFFFFFFF)
/// </summary>
public static class InspectLink
{
    // --------------------------------------------------------------------------
    // Regex patterns — same logic as PHP / Python / JS implementations
    // --------------------------------------------------------------------------

    private static readonly Regex InspectUrlRe =
        new(@"(?:%20|\s|\+)A([0-9A-Fa-f]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HybridUrlRe =
        new(@"S\d+A\d+D([0-9A-Fa-f]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ClassicUrlRe =
        new(@"csgo_econ_action_preview(?:%20|\s)[SM]\d+A\d+D\d+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MaskedUrlRe =
        new(@"csgo_econ_action_preview(?:%20|\s)%?[0-9A-Fa-f]{10,}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PureMaskedRe =
        new(@"csgo_econ_action_preview(?:%20|\s|\+)%?([0-9A-Fa-f]{10,})$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HexLetterRe =
        new(@"[A-Fa-f]", RegexOptions.Compiled);

    private static readonly Regex WhitespaceRe =
        new(@"\s+", RegexOptions.Compiled);

    // --------------------------------------------------------------------------
    // Public API
    // --------------------------------------------------------------------------

    /// <summary>
    /// Encode an <see cref="ItemPreviewData"/> to an uppercase hex inspect-link payload.
    /// The key_byte is always 0x00 (no XOR applied).
    /// </summary>
    public static string Serialize(ItemPreviewData data)
    {
        if (data.PaintWear is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(data), $"PaintWear must be in [0.0, 1.0], got {data.PaintWear}");
        if (data.CustomName is { Length: > 100 })
            throw new ArgumentOutOfRangeException(nameof(data), $"CustomName must not exceed 100 characters, got {data.CustomName.Length}");
        var protoBytes = EncodeItem(data);

        var buffer = new byte[1 + protoBytes.Length];
        buffer[0] = 0x00;
        protoBytes.CopyTo(buffer, 1);

        var checksum = ComputeChecksum(buffer, protoBytes.Length);

        var result = new byte[buffer.Length + 4];
        buffer.CopyTo(result, 0);
        checksum.CopyTo(result, buffer.Length);

        return Convert.ToHexString(result); // uppercase in .NET
    }

    /// <summary>
    /// Decode an inspect-link hex payload (or full URL) into an <see cref="ItemPreviewData"/>.
    /// Accepts raw hex strings, steam://rungame/... and csgo://rungame/... URLs.
    /// </summary>
    /// <exception cref="ArgumentException">When the payload is too short or invalid hex.</exception>
    public static ItemPreviewData Deserialize(string input)
    {
        var hex = ExtractHex(input);

        if (hex.Length > 4096)
            throw new ArgumentException($"Payload too long (max 4096 hex chars): \"{input[..Math.Min(64, input.Length)]}...\"");

        byte[] raw;
        try
        {
            raw = Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            throw new ArgumentException($"Payload too short or invalid hex: \"{input}\"");
        }

        if (raw.Length < 6)
            throw new ArgumentException($"Payload too short or invalid hex: \"{input}\"");

        byte key = raw[0];
        byte[] decrypted;

        if (key == 0)
        {
            decrypted = raw;
        }
        else
        {
            decrypted = new byte[raw.Length];
            for (int i = 0; i < raw.Length; i++)
                decrypted[i] = (byte)(raw[i] ^ key);
        }

        // Layout: [key_byte] [proto_bytes...] [4-byte checksum]
        var protoBytes = new byte[decrypted.Length - 5];
        Buffer.BlockCopy(decrypted, 1, protoBytes, 0, protoBytes.Length);

        return DecodeItem(protoBytes);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the link contains a decodable protobuf payload
    /// that can be decoded offline.
    /// </summary>
    public static bool IsMasked(string link)
    {
        var s = link.Trim();
        if (MaskedUrlRe.IsMatch(s)) return true;
        var m = HybridUrlRe.Match(s);
        return m.Success && HexLetterRe.IsMatch(m.Groups[1].Value);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the link is a classic S/A/D inspect URL
    /// with a decimal inspect-ID.
    /// </summary>
    public static bool IsClassic(string link) => ClassicUrlRe.IsMatch(link.Trim());

    // --------------------------------------------------------------------------
    // URL extraction
    // --------------------------------------------------------------------------

    private static string ExtractHex(string input)
    {
        var stripped = input.Trim();

        // Hybrid format: S\d+A\d+D<hexproto>
        var mh = HybridUrlRe.Match(stripped);
        if (mh.Success && HexLetterRe.IsMatch(mh.Groups[1].Value))
            return mh.Groups[1].Value;

        // Classic/market URL: A<hex> preceded by %20, space, or +.
        // Even-length guard: if stripping 'A' yields odd-length hex, 'A' is the first
        // byte of the payload (XOR key = 0xAx), not the classic asset-ID prefix marker.
        var m = InspectUrlRe.Match(stripped);
        if (m.Success && m.Groups[1].Value.Length % 2 == 0)
            return m.Groups[1].Value;

        // Pure masked format — also handles payloads whose first hex char is 'A'.
        var mm = PureMaskedRe.Match(stripped);
        if (mm.Success) return mm.Groups[1].Value;

        // Bare hex — strip whitespace
        return WhitespaceRe.Replace(stripped, "");
    }

    // --------------------------------------------------------------------------
    // Checksum
    // --------------------------------------------------------------------------

    private static byte[] ComputeChecksum(byte[] buffer, int protoLen)
    {
        var crc = Crc32Helper.Compute(buffer);
        var xored = ((crc & 0xFFFF) ^ ((uint)protoLen * crc)) & 0xFFFFFFFF;
        return
        [
            (byte)((xored >> 24) & 0xFF),
            (byte)((xored >> 16) & 0xFF),
            (byte)((xored >> 8) & 0xFF),
            (byte)(xored & 0xFF),
        ];
    }

    // --------------------------------------------------------------------------
    // Sticker encode / decode
    // --------------------------------------------------------------------------

    private static byte[] EncodeSticker(Sticker s)
    {
        var w = new ProtoWriter();
        w.WriteUInt32(1, s.Slot);
        w.WriteUInt32(2, s.StickerId);
        if (s.Wear.HasValue) w.WriteFloat32Fixed(3, s.Wear.Value);
        if (s.Scale.HasValue) w.WriteFloat32Fixed(4, s.Scale.Value);
        if (s.Rotation.HasValue) w.WriteFloat32Fixed(5, s.Rotation.Value);
        w.WriteUInt32(6, s.TintId);
        if (s.OffsetX.HasValue) w.WriteFloat32Fixed(7, s.OffsetX.Value);
        if (s.OffsetY.HasValue) w.WriteFloat32Fixed(8, s.OffsetY.Value);
        if (s.OffsetZ.HasValue) w.WriteFloat32Fixed(9, s.OffsetZ.Value);
        w.WriteUInt32(10, s.Pattern);
        if (s.HighlightReel.HasValue) w.WriteUInt32(11, s.HighlightReel.Value);
        if (s.PaintKit.HasValue) w.WriteUInt32(12, s.PaintKit.Value);
        return w.ToBytes();
    }

    private static Sticker DecodeSticker(byte[] data)
    {
        var reader = new ProtoReader(data);
        var s = new Sticker();
        foreach (var f in reader.ReadAllFields())
        {
            switch (f.FieldNumber)
            {
                case 1: s.Slot = (uint)f.VarIntValue; break;
                case 2: s.StickerId = (uint)f.VarIntValue; break;
                case 3: s.Wear = BitConverter.ToSingle(f.BytesValue!, 0); break;
                case 4: s.Scale = BitConverter.ToSingle(f.BytesValue!, 0); break;
                case 5: s.Rotation = BitConverter.ToSingle(f.BytesValue!, 0); break;
                case 6: s.TintId = (uint)f.VarIntValue; break;
                case 7: s.OffsetX = BitConverter.ToSingle(f.BytesValue!, 0); break;
                case 8: s.OffsetY = BitConverter.ToSingle(f.BytesValue!, 0); break;
                case 9: s.OffsetZ = BitConverter.ToSingle(f.BytesValue!, 0); break;
                case 10: s.Pattern = (uint)f.VarIntValue; break;
                case 11: s.HighlightReel = (uint)f.VarIntValue; break;
                case 12: s.PaintKit = (uint)f.VarIntValue; break;
            }
        }
        return s;
    }

    // --------------------------------------------------------------------------
    // ItemPreviewData encode / decode
    // --------------------------------------------------------------------------

    private static byte[] EncodeItem(ItemPreviewData item)
    {
        var w = new ProtoWriter();
        w.WriteUInt32(1, item.AccountId);
        w.WriteUInt64(2, item.ItemId);
        w.WriteUInt32(3, item.DefIndex);
        w.WriteUInt32(4, item.PaintIndex);
        w.WriteUInt32(5, item.Rarity);
        w.WriteUInt32(6, item.Quality);

        // PaintWear: float32 reinterpreted as uint32 varint
        if (item.PaintWear.HasValue)
            w.WriteUInt32(7, BitConverter.SingleToUInt32Bits(item.PaintWear.Value));

        w.WriteUInt32(8, item.PaintSeed);
        w.WriteUInt32(9, item.KillEaterScoreType);
        w.WriteUInt32(10, item.KillEaterValue);
        w.WriteString(11, item.CustomName);

        foreach (var sticker in item.Stickers)
            w.WriteRawBytes(12, EncodeSticker(sticker));

        w.WriteUInt32(13, item.Inventory);
        w.WriteUInt32(14, item.Origin);
        w.WriteUInt32(15, item.QuestId);
        w.WriteUInt32(16, item.DropReason);
        w.WriteUInt32(17, item.MusicIndex);
        w.WriteInt32(18, item.EntIndex);
        w.WriteUInt32(19, item.PetIndex);

        foreach (var kc in item.Keychains)
            w.WriteRawBytes(20, EncodeSticker(kc));

        return w.ToBytes();
    }

    private static ItemPreviewData DecodeItem(byte[] data)
    {
        var reader = new ProtoReader(data);
        var item = new ItemPreviewData();
        foreach (var f in reader.ReadAllFields())
        {
            switch (f.FieldNumber)
            {
                case 1: item.AccountId = (uint)f.VarIntValue; break;
                case 2: item.ItemId = f.VarIntValue; break;
                case 3: item.DefIndex = (uint)f.VarIntValue; break;
                case 4: item.PaintIndex = (uint)f.VarIntValue; break;
                case 5: item.Rarity = (uint)f.VarIntValue; break;
                case 6: item.Quality = (uint)f.VarIntValue; break;
                case 7: item.PaintWear = BitConverter.UInt32BitsToSingle((uint)f.VarIntValue); break;
                case 8: item.PaintSeed = (uint)f.VarIntValue; break;
                case 9: item.KillEaterScoreType = (uint)f.VarIntValue; break;
                case 10: item.KillEaterValue = (uint)f.VarIntValue; break;
                case 11: item.CustomName = Encoding.UTF8.GetString(f.BytesValue!); break;
                case 12: item.Stickers.Add(DecodeSticker(f.BytesValue!)); break;
                case 13: item.Inventory = (uint)f.VarIntValue; break;
                case 14: item.Origin = (uint)f.VarIntValue; break;
                case 15: item.QuestId = (uint)f.VarIntValue; break;
                case 16: item.DropReason = (uint)f.VarIntValue; break;
                case 17: item.MusicIndex = (uint)f.VarIntValue; break;
                case 18: item.EntIndex = (int)(uint)f.VarIntValue; break;
                case 19: item.PetIndex = (uint)f.VarIntValue; break;
                case 20: item.Keychains.Add(DecodeSticker(f.BytesValue!)); break;
            }
        }
        return item;
    }
}
