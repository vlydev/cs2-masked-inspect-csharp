namespace VlyDev.Cs2MaskedInspect.Internal;

internal static class Crc32Helper
{
    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var c = i;
            for (int j = 0; j < 8; j++)
                c = (c & 1) != 0 ? (0xEDB88320u ^ (c >> 1)) : (c >> 1);
            table[i] = c;
        }
        return table;
    }

    internal static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
            crc = (crc >> 8) ^ Table[(crc ^ b) & 0xFF];
        return crc ^ 0xFFFFFFFF;
    }
}
