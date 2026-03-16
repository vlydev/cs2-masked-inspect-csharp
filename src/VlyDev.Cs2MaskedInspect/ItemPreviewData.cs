namespace VlyDev.Cs2MaskedInspect;

/// <summary>Decoded data from a CS2 masked inspect link.</summary>
public sealed class ItemPreviewData
{
    public uint AccountId { get; set; }
    public ulong ItemId { get; set; }
    public uint DefIndex { get; set; }
    public uint PaintIndex { get; set; }
    public uint Rarity { get; set; }
    public uint Quality { get; set; }
    public float? PaintWear { get; set; }
    public uint PaintSeed { get; set; }
    public uint KillEaterScoreType { get; set; }
    public uint KillEaterValue { get; set; }
    public string? CustomName { get; set; }
    public List<Sticker> Stickers { get; set; } = new();
    public uint Inventory { get; set; }
    public uint Origin { get; set; }
    public uint QuestId { get; set; }
    public uint DropReason { get; set; }
    public uint MusicIndex { get; set; }
    public int EntIndex { get; set; }
    public uint PetIndex { get; set; }
    public List<Sticker> Keychains { get; set; } = new();
}
