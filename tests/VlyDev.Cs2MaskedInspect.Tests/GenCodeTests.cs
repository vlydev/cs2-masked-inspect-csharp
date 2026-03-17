using VlyDev.Cs2MaskedInspect;
using Xunit;

namespace VlyDev.Cs2MaskedInspect.Tests;

/// <summary>Tests for GenCode utilities.</summary>
public class GenCodeTests
{
    // -------------------------------------------------------------------------
    // ToGenCode — basic
    // -------------------------------------------------------------------------

    [Fact]
    public void ToGenCode_Minimal_StartsWithPrefix()
    {
        var item = new ItemPreviewData { DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f };
        var code = GenCode.ToGenCode(item);
        Assert.StartsWith("!gen ", code);
    }

    [Fact]
    public void ToGenCode_Minimal_ContainsBaseFields()
    {
        var item = new ItemPreviewData { DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f };
        var code = GenCode.ToGenCode(item);
        Assert.Contains("7 474 306 0.22540508", code);
    }

    [Fact]
    public void ToGenCode_CustomPrefix()
    {
        var item = new ItemPreviewData { DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f };
        var code = GenCode.ToGenCode(item, "!g");
        Assert.StartsWith("!g ", code);
    }

    [Fact]
    public void ToGenCode_EmptyPrefix_NoLeadingSpace()
    {
        var item = new ItemPreviewData { DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f };
        var code = GenCode.ToGenCode(item, "");
        Assert.StartsWith("7 ", code);
    }

    [Fact]
    public void ToGenCode_NullWear_BecomesZero()
    {
        var item = new ItemPreviewData { DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = null };
        var code = GenCode.ToGenCode(item);
        var tokens = code.Split(' ');
        // tokens[0]=!gen [1]=7 [2]=474 [3]=306 [4]=0 (wear)
        Assert.Equal("0", tokens[4]);
    }

    // -------------------------------------------------------------------------
    // ToGenCode — float formatting
    // -------------------------------------------------------------------------

    [Fact]
    public void ToGenCode_Float_StripsTrailingZeros()
    {
        var item = new ItemPreviewData { DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.5f };
        var tokens = GenCode.ToGenCode(item).Split(' ');
        Assert.Equal("0.5", tokens[4]);
    }

    [Fact]
    public void ToGenCode_FloatZero_BecomesSingleZero()
    {
        var item = new ItemPreviewData { DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.0f };
        var tokens = GenCode.ToGenCode(item).Split(' ');
        Assert.Equal("0", tokens[4]);
    }

    [Fact]
    public void ToGenCode_Float_EightDecimalPlaces()
    {
        var item = new ItemPreviewData { DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f };
        var tokens = GenCode.ToGenCode(item).Split(' ');
        Assert.Equal("0.22540508", tokens[4]);
    }

    // -------------------------------------------------------------------------
    // ToGenCode — sticker padding
    // -------------------------------------------------------------------------

    [Fact]
    public void ToGenCode_AlwaysPadsStickerTo5Slots()
    {
        var item = new ItemPreviewData
        {
            DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f,
            Stickers = [new Sticker { Slot = 2, StickerId = 7203 }],
        };
        var tokens = GenCode.ToGenCode(item).Split(' ');
        // !gen + 4 base + 10 sticker = 15 tokens
        Assert.Equal(15, tokens.Length);
        // slot 0: 0 0, slot 1: 0 0, slot 2: 7203 0, slot 3: 0 0, slot 4: 0 0
        Assert.Equal("0",    tokens[5]);  // slot0 id
        Assert.Equal("0",    tokens[6]);  // slot0 wear
        Assert.Equal("0",    tokens[7]);  // slot1 id
        Assert.Equal("0",    tokens[8]);  // slot1 wear
        Assert.Equal("7203", tokens[9]);  // slot2 id
        Assert.Equal("0",    tokens[10]); // slot2 wear
    }

    [Fact]
    public void ToGenCode_NoStickers_Has5EmptySlots()
    {
        var item = new ItemPreviewData { DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.5f };
        var tokens = GenCode.ToGenCode(item).Split(' ');
        // !gen + 4 + 10 = 15
        Assert.Equal(15, tokens.Length);
        for (int i = 5; i <= 14; i++)
            Assert.Equal("0", tokens[i]);
    }

    [Fact]
    public void ToGenCode_StickerWear_Formatted()
    {
        var item = new ItemPreviewData
        {
            DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f,
            Stickers = [new Sticker { Slot = 0, StickerId = 7203, Wear = 0.5f }],
        };
        var tokens = GenCode.ToGenCode(item).Split(' ');
        Assert.Equal("7203", tokens[5]);
        Assert.Equal("0.5",  tokens[6]);
    }

    // -------------------------------------------------------------------------
    // ToGenCode — keychains (no padding)
    // -------------------------------------------------------------------------

    [Fact]
    public void ToGenCode_Keychain_AppendedAfterStickers()
    {
        var item = new ItemPreviewData
        {
            DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f,
            Keychains = [new Sticker { Slot = 0, StickerId = 36 }],
        };
        var tokens = GenCode.ToGenCode(item).Split(' ');
        // !gen + 4 + 10 sticker + 2 keychain = 17
        Assert.Equal(17, tokens.Length);
        Assert.Equal("36", tokens[15]);
        Assert.Equal("0",  tokens[16]);
    }

    [Fact]
    public void ToGenCode_Keychain_NotPadded()
    {
        var item = new ItemPreviewData
        {
            DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f,
            Keychains = [new Sticker { Slot = 2, StickerId = 36 }],
        };
        var tokens = GenCode.ToGenCode(item).Split(' ');
        // Only 1 keychain pair appended, no padding to slot 2
        Assert.Equal(17, tokens.Length);
    }

    // -------------------------------------------------------------------------
    // ParseGenCode — basic
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseGenCode_Minimal_WithPrefix()
    {
        var item = GenCode.ParseGenCode("!gen 7 474 306 0.22540508");
        Assert.Equal(7u, item.DefIndex);
        Assert.Equal(474u, item.PaintIndex);
        Assert.Equal(306u, item.PaintSeed);
        Assert.Equal(0.22540508f, item.PaintWear!.Value, precision: 7);
    }

    [Fact]
    public void ParseGenCode_Minimal_WithoutPrefix()
    {
        var item = GenCode.ParseGenCode("7 474 306 0.22540508");
        Assert.Equal(7u, item.DefIndex);
        Assert.Equal(474u, item.PaintIndex);
    }

    [Fact]
    public void ParseGenCode_CustomPrefix()
    {
        var item = GenCode.ParseGenCode("!g 7 474 306 0.22540508");
        Assert.Equal(7u, item.DefIndex);
    }

    [Fact]
    public void ParseGenCode_TooFewTokens_Throws()
        => Assert.Throws<ArgumentException>(() => GenCode.ParseGenCode("!gen 7 474 306"));

    [Fact]
    public void ParseGenCode_TooFewTokensNoPrefix_Throws()
        => Assert.Throws<ArgumentException>(() => GenCode.ParseGenCode("7 474 306"));

    // -------------------------------------------------------------------------
    // ParseGenCode — stickers
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseGenCode_Stickers_FromPaddedSlots()
    {
        // slot2 = 7203, others empty
        var item = GenCode.ParseGenCode("!gen 7 474 306 0.22540508 0 0 0 0 7203 0 0 0 0 0");
        Assert.Single(item.Stickers);
        Assert.Equal(7203u, item.Stickers[0].StickerId);
        Assert.Equal(2u, item.Stickers[0].Slot);
    }

    [Fact]
    public void ParseGenCode_MultipleStickers()
    {
        var item = GenCode.ParseGenCode("!gen 7 474 306 0.22540508 7436 0 5144 0 0 0 0 0 0 0");
        Assert.Equal(2, item.Stickers.Count);
        var ids = item.Stickers.Select(s => s.StickerId).ToHashSet();
        Assert.Contains(7436u, ids);
        Assert.Contains(5144u, ids);
    }

    [Fact]
    public void ParseGenCode_StickerWear()
    {
        var item = GenCode.ParseGenCode("!gen 7 474 306 0.5 7203 0.25 0 0 0 0 0 0 0 0");
        Assert.Single(item.Stickers);
        Assert.NotNull(item.Stickers[0].Wear);
        Assert.Equal(0.25f, item.Stickers[0].Wear!.Value, precision: 6);
    }

    [Fact]
    public void ParseGenCode_NoStickersWhenLessThan10Tokens()
    {
        var item = GenCode.ParseGenCode("7 474 306 0.5");
        Assert.Empty(item.Stickers);
    }

    // -------------------------------------------------------------------------
    // ParseGenCode — keychains
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseGenCode_Keychain_AfterStickers()
    {
        var item = GenCode.ParseGenCode("7 941 2 0.22540508 0 0 0 0 0 0 0 0 0 0 36 0");
        Assert.Single(item.Keychains);
        Assert.Equal(36u, item.Keychains[0].StickerId);
    }

    // -------------------------------------------------------------------------
    // Round-trip: ToGenCode → ParseGenCode
    // -------------------------------------------------------------------------

    [Fact]
    public void Roundtrip_Minimal()
    {
        var original = new ItemPreviewData { DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f };
        var parsed = GenCode.ParseGenCode(GenCode.ToGenCode(original));
        Assert.Equal(original.DefIndex, parsed.DefIndex);
        Assert.Equal(original.PaintIndex, parsed.PaintIndex);
        Assert.Equal(original.PaintSeed, parsed.PaintSeed);
        Assert.Equal(original.PaintWear!.Value, parsed.PaintWear!.Value, precision: 6);
    }

    [Fact]
    public void Roundtrip_WithStickers()
    {
        var original = new ItemPreviewData
        {
            DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f,
            Stickers =
            [
                new Sticker { Slot = 0, StickerId = 7436 },
                new Sticker { Slot = 3, StickerId = 5144, Wear = 0.25f },
            ],
        };
        var parsed = GenCode.ParseGenCode(GenCode.ToGenCode(original));
        Assert.Equal(2, parsed.Stickers.Count);
        var bySlot = parsed.Stickers.ToDictionary(s => s.Slot);
        Assert.Equal(7436u, bySlot[0].StickerId);
        Assert.Equal(5144u, bySlot[3].StickerId);
        Assert.Equal(0.25f, bySlot[3].Wear!.Value, precision: 6);
    }

    [Fact]
    public void Roundtrip_WithKeychain()
    {
        var original = new ItemPreviewData
        {
            DefIndex = 7, PaintIndex = 474, PaintSeed = 306, PaintWear = 0.22540508f,
            Keychains = [new Sticker { Slot = 0, StickerId = 36 }],
        };
        var parsed = GenCode.ParseGenCode(GenCode.ToGenCode(original));
        Assert.Single(parsed.Keychains);
        Assert.Equal(36u, parsed.Keychains[0].StickerId);
    }

    // -------------------------------------------------------------------------
    // GenCodeFromLink
    // -------------------------------------------------------------------------

    [Fact]
    public void GenCodeFromLink_FromHex_StartsWithPrefix()
    {
        var url = GenCode.Generate(7, 474, 306, 0.22540508f);
        var hex = url[GenCode.InspectBase.Length..];
        var code = GenCode.GenCodeFromLink(hex);
        Assert.StartsWith("!gen 7 474 306", code);
    }

    [Fact]
    public void GenCodeFromLink_FromFullUrl_StartsWithPrefix()
    {
        var url = GenCode.Generate(7, 474, 306, 0.22540508f);
        var code = GenCode.GenCodeFromLink(url);
        Assert.StartsWith("!gen 7 474 306", code);
    }

    // -------------------------------------------------------------------------
    // Generate
    // -------------------------------------------------------------------------

    [Fact]
    public void Generate_ReturnsSteamUrl()
    {
        var url = GenCode.Generate(7, 474, 306, 0.22540508f);
        Assert.StartsWith("steam://rungame/730/76561202255233023/+csgo_econ_action_preview%20", url);
    }

    [Fact]
    public void Generate_UrlContainsUppercaseHex()
    {
        var url = GenCode.Generate(7, 474, 306, 0.22540508f);
        var hex = url[GenCode.InspectBase.Length..];
        Assert.Matches("^[0-9A-F]+$", hex);
    }

    [Fact]
    public void Generate_RoundtripViaDeserialize()
    {
        var url = GenCode.Generate(7, 474, 306, 0.22540508f, rarity: 6);
        var item = InspectLink.Deserialize(url);
        Assert.Equal(7u, item.DefIndex);
        Assert.Equal(474u, item.PaintIndex);
        Assert.Equal(306u, item.PaintSeed);
        Assert.Equal(6u, item.Rarity);
        Assert.Equal(0.22540508f, item.PaintWear!.Value, precision: 6);
    }

    [Fact]
    public void Generate_WithStickers_RoundtripViaDeserialize()
    {
        var url = GenCode.Generate(
            defIndex: 7, paintIndex: 474, paintSeed: 306, paintWear: 0.22540508f,
            stickers: [new Sticker { Slot = 0, StickerId = 7436 }]);
        var item = InspectLink.Deserialize(url);
        Assert.Single(item.Stickers);
        Assert.Equal(7436u, item.Stickers[0].StickerId);
    }

    [Fact]
    public void Generate_WithKeychain_RoundtripViaDeserialize()
    {
        var url = GenCode.Generate(
            defIndex: 7, paintIndex: 474, paintSeed: 306, paintWear: 0.22540508f,
            keychains: [new Sticker { Slot = 0, StickerId = 36 }]);
        var item = InspectLink.Deserialize(url);
        Assert.Single(item.Keychains);
        Assert.Equal(36u, item.Keychains[0].StickerId);
    }

    [Fact]
    public void Generate_CsfloatVectorRoundtrip()
    {
        // CSFloat vector A: defindex=7, paintindex=474, paintseed=306, rarity=6, paintwear≈0.6337
        const string knownHex = "00180720DA03280638FBEE88F90340B2026BC03C96";
        var original = InspectLink.Deserialize(knownHex);
        var url = GenCode.Generate(
            (int)original.DefIndex, (int)original.PaintIndex,
            (int)original.PaintSeed, original.PaintWear!.Value,
            (int)original.Rarity, (int)original.Quality);
        var decoded = InspectLink.Deserialize(url);
        Assert.Equal(original.DefIndex, decoded.DefIndex);
        Assert.Equal(original.PaintIndex, decoded.PaintIndex);
        Assert.Equal(original.PaintSeed, decoded.PaintSeed);
        Assert.Equal(original.PaintWear!.Value, decoded.PaintWear!.Value, precision: 6);
    }
}
