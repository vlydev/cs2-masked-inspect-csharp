using System;
using System.Collections.Generic;
using System.Globalization;

namespace VlyDev.Cs2MaskedInspect;

/// <summary>
/// Gen code utilities for CS2 inspect links.
///
/// Gen codes are space-separated command strings used on community servers:
///   !gen {defindex} {paintindex} {paintseed} {paintwear}
///   !gen ... {s0_id} {s0_wear} {s1_id} {s1_wear} ... {s4_id} {s4_wear} [{kc_id} {kc_wear} ...]
///
/// Stickers are always padded to 5 slot pairs. Keychains follow without padding.
/// </summary>
public static class GenCode
{
    /// <summary>Base URL prefix for Steam inspect links.</summary>
    public const string InspectBase = "steam://rungame/730/76561202255233023/+csgo_econ_action_preview%20";

    /// <summary>Format a float value, stripping trailing zeros (max 8 decimal places).</summary>
    private static string FormatFloat(float value)
    {
        string s = value.ToString("F8", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        return string.IsNullOrEmpty(s) ? "0" : s;
    }

    private static List<string> SerializeStickerPairs(IReadOnlyList<Sticker> stickers, int? padTo)
    {
        var result = new List<string>();
        var filtered = new List<Sticker>();
        foreach (var s in stickers)
        {
            if (s.StickerId != 0) filtered.Add(s);
        }

        if (padTo.HasValue)
        {
            var slotMap = new Dictionary<uint, Sticker>();
            foreach (var s in filtered) slotMap[s.Slot] = s;

            for (int slot = 0; slot < padTo.Value; slot++)
            {
                if (slotMap.TryGetValue((uint)slot, out var s))
                {
                    result.Add(s.StickerId.ToString());
                    result.Add(FormatFloat(s.Wear ?? 0f));
                }
                else
                {
                    result.Add("0");
                    result.Add("0");
                }
            }
        }
        else
        {
            filtered.Sort((a, b) => a.Slot.CompareTo(b.Slot));
            foreach (var s in filtered)
            {
                result.Add(s.StickerId.ToString());
                result.Add(FormatFloat(s.Wear ?? 0f));
                if (s.PaintKit.HasValue)
                    result.Add(s.PaintKit.Value.ToString());
            }
        }

        return result;
    }

    /// <summary>
    /// Convert an <see cref="ItemPreviewData"/> to a gen code string.
    /// </summary>
    /// <param name="item">The item to convert.</param>
    /// <param name="prefix">The command prefix, e.g. "!gen" or "!g".</param>
    /// <returns>A gen code like <c>"!gen 7 474 306 0.22540508"</c>.</returns>
    public static string ToGenCode(ItemPreviewData item, string prefix = "!gen")
    {
        string wearStr = item.PaintWear.HasValue ? FormatFloat(item.PaintWear.Value) : "0";
        var parts = new List<string>
        {
            item.DefIndex.ToString(),
            item.PaintIndex.ToString(),
            item.PaintSeed.ToString(),
            wearStr,
        };

        parts.AddRange(SerializeStickerPairs(item.Stickers, 5));
        parts.AddRange(SerializeStickerPairs(item.Keychains, null));

        string payload = string.Join(" ", parts);
        return string.IsNullOrEmpty(prefix) ? payload : $"{prefix} {payload}";
    }

    /// <summary>
    /// Generate a full Steam inspect URL from item parameters.
    /// </summary>
    /// <param name="defIndex">Weapon definition ID (e.g. 7 = AK-47).</param>
    /// <param name="paintIndex">Skin/paint ID.</param>
    /// <param name="paintSeed">Pattern index (0-1000).</param>
    /// <param name="paintWear">Float value (0.0-1.0).</param>
    /// <param name="rarity">Item rarity tier (default 0).</param>
    /// <param name="quality">Item quality, e.g. 9 = StatTrak (default 0).</param>
    /// <param name="stickers">Stickers applied to the item.</param>
    /// <param name="keychains">Keychains applied to the item.</param>
    /// <returns>Full <c>steam://rungame/...</c> inspect URL.</returns>
    public static string Generate(
        int defIndex,
        int paintIndex,
        int paintSeed,
        float paintWear,
        int rarity = 0,
        int quality = 0,
        IReadOnlyList<Sticker>? stickers = null,
        IReadOnlyList<Sticker>? keychains = null)
    {
        var data = new ItemPreviewData
        {
            DefIndex   = (uint)defIndex,
            PaintIndex = (uint)paintIndex,
            PaintSeed  = (uint)paintSeed,
            PaintWear  = paintWear,
            Rarity     = (uint)rarity,
            Quality    = (uint)quality,
            Stickers   = stickers != null ? new List<Sticker>(stickers) : new List<Sticker>(),
            Keychains  = keychains != null ? new List<Sticker>(keychains) : new List<Sticker>(),
        };
        string hex = InspectLink.Serialize(data);
        return $"{InspectBase}{hex}";
    }

    /// <summary>
    /// Generate a gen code string from an existing CS2 inspect link.
    /// </summary>
    /// <param name="hexOrUrl">A hex payload or full steam:// inspect URL.</param>
    /// <param name="prefix">The command prefix (default "!gen").</param>
    /// <returns>Gen code string like "!gen 7 474 306 0.22540508".</returns>
    public static string GenCodeFromLink(string hexOrUrl, string prefix = "!gen")
    {
        var item = InspectLink.Deserialize(hexOrUrl);
        return ToGenCode(item, prefix);
    }

    /// <summary>
    /// Parse a gen code string into an <see cref="ItemPreviewData"/>.
    /// </summary>
    /// <param name="genCode">A gen code like <c>"!gen 7 474 306 0.22540508"</c>.</param>
    /// <returns>Parsed <see cref="ItemPreviewData"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the code has fewer than 4 tokens.</exception>
    public static ItemPreviewData ParseGenCode(string genCode)
    {
        var tokens = new List<string>(
            genCode.Trim().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));

        if (tokens.Count > 0 && tokens[0].StartsWith("!"))
            tokens.RemoveAt(0);

        if (tokens.Count < 4)
            throw new ArgumentException($"Gen code must have at least 4 tokens, got: \"{genCode}\"");

        uint defIndex   = uint.Parse(tokens[0]);
        uint paintIndex = uint.Parse(tokens[1]);
        uint paintSeed  = uint.Parse(tokens[2]);
        float paintWear = float.Parse(tokens[3], CultureInfo.InvariantCulture);
        var rest = tokens.GetRange(4, tokens.Count - 4);

        var stickers  = new List<Sticker>();
        var keychains = new List<Sticker>();

        if (rest.Count >= 10)
        {
            for (int slot = 0; slot < 5; slot++)
            {
                uint sid   = uint.Parse(rest[slot * 2]);
                float wear = float.Parse(rest[slot * 2 + 1], CultureInfo.InvariantCulture);
                if (sid != 0)
                    stickers.Add(new Sticker { Slot = (uint)slot, StickerId = sid, Wear = wear });
            }
            rest = rest.GetRange(10, rest.Count - 10);
        }

        for (int i = 0; i + 1 < rest.Count; i += 2)
        {
            uint sid   = uint.Parse(rest[i]);
            float wear = float.Parse(rest[i + 1], CultureInfo.InvariantCulture);
            if (sid != 0)
                keychains.Add(new Sticker { Slot = (uint)(i / 2), StickerId = sid, Wear = wear });
        }

        return new ItemPreviewData
        {
            DefIndex   = defIndex,
            PaintIndex = paintIndex,
            PaintSeed  = paintSeed,
            PaintWear  = paintWear,
            Stickers   = stickers,
            Keychains  = keychains,
        };
    }
}
