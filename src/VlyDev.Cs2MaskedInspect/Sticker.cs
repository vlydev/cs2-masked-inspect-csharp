namespace VlyDev.Cs2MaskedInspect;

/// <summary>Represents a sticker or keychain attached to a CS2 item.</summary>
public sealed class Sticker
{
    public uint Slot { get; set; }
    public uint StickerId { get; set; }
    public float? Wear { get; set; }
    public float? Scale { get; set; }
    public float? Rotation { get; set; }
    public uint TintId { get; set; }
    public float? OffsetX { get; set; }
    public float? OffsetY { get; set; }
    public float? OffsetZ { get; set; }
    public uint Pattern { get; set; }
}
