using VlyDev.Cs2MaskedInspect;
using Xunit;

namespace VlyDev.Cs2MaskedInspect.Tests;

/// <summary>Tests for CS2 inspect link serialization/deserialization.</summary>
public class InspectLinkTests
{
    // -------------------------------------------------------------------------
    // Known test vectors
    // -------------------------------------------------------------------------

    /// <summary>A real CS2 item encoded with XOR key 0xE3.</summary>
    private const string NativeHex =
        "E3F3367440334DE2FBE4C345E0CBE0D3E7DB6943400AE0A379E481ECEBE2F36FD9DE2BDB515EA6E30D74D981" +
        "ECEBE3F37BCBDE640D475DA6E35EFCD881ECEBE3F359D5DE37E9D75DA6436DD3DD81ECEBE3F366DCDE3F8F9B" +
        "DDA69B43B6DE81ECEBE3F33BC8DEBB1CA3DFA623F7DDDF8B71E293EBFD43382B";

    /// <summary>A tool-generated link with key 0x00.</summary>
    private const string ToolHex = "00183C20B803280538E9A3C5DD0340E102C246A0D1";

    // -------------------------------------------------------------------------
    // Deserialize — NativeHex (XOR key 0xE3)
    // -------------------------------------------------------------------------

    [Fact] public void NativeXorKey_ItemId()
        => Assert.Equal(46876117973UL, InspectLink.Deserialize(NativeHex).ItemId);

    [Fact] public void NativeXorKey_Defindex()
        => Assert.Equal(7u, InspectLink.Deserialize(NativeHex).DefIndex); // AK-47

    [Fact] public void NativeXorKey_Paintindex()
        => Assert.Equal(422u, InspectLink.Deserialize(NativeHex).PaintIndex);

    [Fact] public void NativeXorKey_Paintseed()
        => Assert.Equal(922u, InspectLink.Deserialize(NativeHex).PaintSeed);

    [Fact] public void NativeXorKey_Paintwear()
        => Assert.Equal(0.04121f, InspectLink.Deserialize(NativeHex).PaintWear!.Value, precision: 3);

    [Fact] public void NativeXorKey_Rarity()
        => Assert.Equal(3u, InspectLink.Deserialize(NativeHex).Rarity);

    [Fact] public void NativeXorKey_Quality()
        => Assert.Equal(4u, InspectLink.Deserialize(NativeHex).Quality);

    [Fact] public void Native_StickerCount()
        => Assert.Equal(5, InspectLink.Deserialize(NativeHex).Stickers.Count);

    [Fact]
    public void Native_StickerIds()
    {
        var ids = InspectLink.Deserialize(NativeHex).Stickers.Select(s => s.StickerId).ToArray();
        Assert.Equal(new uint[] { 7436, 5144, 6970, 8069, 5592 }, ids);
    }

    // -------------------------------------------------------------------------
    // Deserialize — ToolHex (key 0x00)
    // -------------------------------------------------------------------------

    [Fact] public void ToolHex_Defindex()
        => Assert.Equal(60u, InspectLink.Deserialize(ToolHex).DefIndex);

    [Fact] public void ToolHex_Paintindex()
        => Assert.Equal(440u, InspectLink.Deserialize(ToolHex).PaintIndex);

    [Fact] public void ToolHex_Paintseed()
        => Assert.Equal(353u, InspectLink.Deserialize(ToolHex).PaintSeed);

    [Fact] public void ToolHex_Paintwear()
        => Assert.Equal(0.005411375779658556f, InspectLink.Deserialize(ToolHex).PaintWear!.Value,
            precision: 7);

    [Fact] public void ToolHex_Rarity()
        => Assert.Equal(5u, InspectLink.Deserialize(ToolHex).Rarity);

    [Fact] public void LowercaseHex_Accepted()
        => Assert.Equal(60u, InspectLink.Deserialize(ToolHex.ToLower()).DefIndex);

    [Fact]
    public void AcceptsSteamUrl()
    {
        var url = "steam://rungame/730/76561202255233023/+csgo_econ_action_preview%20A" + ToolHex;
        Assert.Equal(60u, InspectLink.Deserialize(url).DefIndex);
    }

    [Fact]
    public void AcceptsCsgoStyleUrl()
    {
        var url = "csgo://rungame/730/76561202255233023/+csgo_econ_action_preview A" + ToolHex;
        Assert.Equal(60u, InspectLink.Deserialize(url).DefIndex);
    }

    [Fact]
    public void PayloadTooShort_Throws()
        => Assert.Throws<ArgumentException>(() => InspectLink.Deserialize("0000"));

    // -------------------------------------------------------------------------
    // Serialize
    // -------------------------------------------------------------------------

    [Fact]
    public void KnownHexOutput()
    {
        var data = new ItemPreviewData
        {
            DefIndex = 60,
            PaintIndex = 440,
            PaintSeed = 353,
            PaintWear = 0.005411375779658556f,
            Rarity = 5,
        };
        Assert.Equal(ToolHex, InspectLink.Serialize(data));
    }

    [Fact]
    public void SerializeReturnsUppercase()
    {
        var result = InspectLink.Serialize(new ItemPreviewData { DefIndex = 1 });
        Assert.Equal(result.ToUpper(), result);
    }

    [Fact]
    public void SerializeStartsWithDoubleZero()
        => Assert.StartsWith("00", InspectLink.Serialize(new ItemPreviewData { DefIndex = 1 }));

    // -------------------------------------------------------------------------
    // Round-trip
    // -------------------------------------------------------------------------

    private static ItemPreviewData Roundtrip(ItemPreviewData data)
        => InspectLink.Deserialize(InspectLink.Serialize(data));

    [Fact] public void Roundtrip_Defindex()
        => Assert.Equal(7u, Roundtrip(new ItemPreviewData { DefIndex = 7 }).DefIndex);

    [Fact] public void Roundtrip_Paintindex()
        => Assert.Equal(422u, Roundtrip(new ItemPreviewData { PaintIndex = 422 }).PaintIndex);

    [Fact] public void Roundtrip_Paintseed()
        => Assert.Equal(999u, Roundtrip(new ItemPreviewData { PaintSeed = 999 }).PaintSeed);

    [Fact]
    public void Roundtrip_Paintwear()
    {
        const float original = 0.123456789f;
        var expected = BitConverter.UInt32BitsToSingle(BitConverter.SingleToUInt32Bits(original));
        var result = Roundtrip(new ItemPreviewData { PaintWear = original });
        Assert.Equal(expected, result.PaintWear!.Value, precision: 7);
    }

    [Fact] public void Roundtrip_ItemidLarge()
        => Assert.Equal(46876117973UL,
            Roundtrip(new ItemPreviewData { ItemId = 46876117973UL }).ItemId);

    [Fact]
    public void Roundtrip_Stickers()
    {
        var data = new ItemPreviewData
        {
            DefIndex = 7,
            Stickers =
            [
                new Sticker { Slot = 0, StickerId = 7436 },
                new Sticker { Slot = 1, StickerId = 5144 },
            ],
        };
        var result = Roundtrip(data);
        Assert.Equal(2, result.Stickers.Count);
        Assert.Equal(7436u, result.Stickers[0].StickerId);
        Assert.Equal(5144u, result.Stickers[1].StickerId);
    }

    [Fact]
    public void Roundtrip_StickerSlots()
    {
        var data = new ItemPreviewData
        {
            Stickers = [new Sticker { Slot = 3, StickerId = 123 }],
        };
        Assert.Equal(3u, Roundtrip(data).Stickers[0].Slot);
    }

    [Fact]
    public void Roundtrip_StickerWear()
    {
        var data = new ItemPreviewData
        {
            Stickers = [new Sticker { StickerId = 1, Wear = 0.5f }],
        };
        var result = Roundtrip(data);
        Assert.NotNull(result.Stickers[0].Wear);
        Assert.Equal(0.5f, result.Stickers[0].Wear!.Value, precision: 6);
    }

    [Fact]
    public void Roundtrip_Keychains()
    {
        var data = new ItemPreviewData
        {
            Keychains = [new Sticker { Slot = 0, StickerId = 999, Pattern = 42 }],
        };
        var result = Roundtrip(data);
        Assert.Single(result.Keychains);
        Assert.Equal(999u, result.Keychains[0].StickerId);
        Assert.Equal(42u, result.Keychains[0].Pattern);
    }

    [Fact]
    public void Roundtrip_Customname()
    {
        var data = new ItemPreviewData { DefIndex = 7, CustomName = "My Knife" };
        Assert.Equal("My Knife", Roundtrip(data).CustomName);
    }

    [Fact]
    public void Roundtrip_RarityQuality()
    {
        var data = new ItemPreviewData { Rarity = 6, Quality = 9 };
        var result = Roundtrip(data);
        Assert.Equal(6u, result.Rarity);
        Assert.Equal(9u, result.Quality);
    }

    [Fact]
    public void Roundtrip_FullItem()
    {
        var data = new ItemPreviewData
        {
            ItemId = 46876117973UL,
            DefIndex = 7,
            PaintIndex = 422,
            Rarity = 3,
            Quality = 4,
            PaintWear = 0.04121f,
            PaintSeed = 922,
            Stickers =
            [
                new Sticker { Slot = 0, StickerId = 7436 },
                new Sticker { Slot = 1, StickerId = 5144 },
                new Sticker { Slot = 2, StickerId = 6970 },
                new Sticker { Slot = 3, StickerId = 8069 },
                new Sticker { Slot = 4, StickerId = 5592 },
            ],
        };
        var result = Roundtrip(data);
        Assert.Equal(7u, result.DefIndex);
        Assert.Equal(422u, result.PaintIndex);
        Assert.Equal(922u, result.PaintSeed);
        Assert.Equal(5, result.Stickers.Count);
        Assert.Equal(new uint[] { 7436, 5144, 6970, 8069, 5592 },
            result.Stickers.Select(s => s.StickerId).ToArray());
    }

    // -------------------------------------------------------------------------
    // Hybrid URL format
    // -------------------------------------------------------------------------

    private const string HybridUrl =
        "steam://rungame/730/76561202255233023/+csgo_econ_action_preview%20S76561199323320483A50075495125D1101C4C4FCD4AB10092D31B8143914211829A1FAE3FD125119591141117308191301EA550C1111912E3C111151D12C413E6BAC54D1D29BAD731E191501B92C2C9B6BF92F5411C25B2A731E191501B92C2CEA2B182E5411F7212A731E191501B92C2C4F89C12F549164592A799713611956F4339F";

    private const string ClassicUrl =
        "steam://rungame/730/76561202255233023/+csgo_econ_action_preview%20S76561199842063946A49749521570D2751293026650298712";

    [Fact]
    public void IsMasked_PureHexPayload()
    {
        var url = "steam://run/730//+csgo_econ_action_preview%20" + ToolHex;
        Assert.True(InspectLink.IsMasked(url));
    }

    [Fact]
    public void IsMasked_FullMaskedUrl()
    {
        var url = "steam://rungame/730/76561202255233023/+csgo_econ_action_preview%20" + NativeHex;
        Assert.True(InspectLink.IsMasked(url));
    }

    [Fact] public void IsMasked_HybridUrl()
        => Assert.True(InspectLink.IsMasked(HybridUrl));

    [Fact] public void IsMasked_ClassicUrl_ReturnsFalse()
        => Assert.False(InspectLink.IsMasked(ClassicUrl));

    [Fact] public void IsClassic_ClassicUrl()
        => Assert.True(InspectLink.IsClassic(ClassicUrl));

    [Fact]
    public void IsClassic_MaskedUrl_ReturnsFalse()
    {
        var url = "steam://run/730//+csgo_econ_action_preview%20" + ToolHex;
        Assert.False(InspectLink.IsClassic(url));
    }

    [Fact] public void IsClassic_HybridUrl_ReturnsFalse()
        => Assert.False(InspectLink.IsClassic(HybridUrl));

    [Fact]
    public void Deserialize_HybridUrl_CorrectItemid()
        => Assert.Equal(50075495125UL, InspectLink.Deserialize(HybridUrl).ItemId);

    // -------------------------------------------------------------------------
    // Regression: hex payload starting with 'A' (key byte = 0xAx)
    //
    //   A_START_1  key=0xA6, longer payload → defindex=34
    //   A_START_2  key=0xA4, shorter payload → defindex=4676
    //   A_START_3  key=0xA6, shorter payload → defindex=1377
    //
    // Previously, ExtractHex() wrongly stripped the leading 'A' (treating it
    // as the classic asset-ID prefix marker), producing an odd-length hex string
    // which caused Convert.FromHexString to throw.
    // Fix: even-length guard on the captured group.
    // -------------------------------------------------------------------------

    public static TheoryData<string, uint> AStartingPayloads => new()
    {
        { "A6B6710C51510DA7BE848628A18EA396A29E1C181D56A5E682CEE8D6AEE7BC380F", 34 },
        { "A4B4725C7B1EE6BC608084A48CA294A0CC9CD4AD3EDF347E",                   4676 },
        { "A6B617190F659DBE47AC86A68EA096A2CEB4D6AFBFFA9FD2",                   1377 },
    };

    [Theory, MemberData(nameof(AStartingPayloads))]
    public void AStarting_IsMasked(string hex, uint _)
    {
        var url = "steam://run/730//+csgo_econ_action_preview%20" + hex;
        Assert.True(InspectLink.IsMasked(url));
    }

    [Theory, MemberData(nameof(AStartingPayloads))]
    public void AStarting_BareHex_Defindex(string hex, uint expectedDefIndex)
        => Assert.Equal(expectedDefIndex, InspectLink.Deserialize(hex).DefIndex);

    [Theory, MemberData(nameof(AStartingPayloads))]
    public void AStarting_SteamRunUrl_Defindex(string hex, uint expectedDefIndex)
    {
        var url = "steam://run/730//+csgo_econ_action_preview%20" + hex;
        Assert.Equal(expectedDefIndex, InspectLink.Deserialize(url).DefIndex);
    }

    [Theory, MemberData(nameof(AStartingPayloads))]
    public void AStarting_SteamRungameUrl_Defindex(string hex, uint expectedDefIndex)
    {
        var url = "steam://rungame/730/76561202255233023/+csgo_econ_action_preview%20" + hex;
        Assert.Equal(expectedDefIndex, InspectLink.Deserialize(url).DefIndex);
    }

    // -------------------------------------------------------------------------
    // Checksum correctness
    // -------------------------------------------------------------------------

    [Fact]
    public void KnownHexChecksumMatches()
    {
        var data = new ItemPreviewData
        {
            DefIndex = 60,
            PaintIndex = 440,
            PaintSeed = 353,
            PaintWear = 0.005411375779658556f,
            Rarity = 5,
        };
        Assert.Equal(ToolHex, InspectLink.Serialize(data));
    }

    // -------------------------------------------------------------------------
    // Validation: payload length, proto field count, value ranges
    // -------------------------------------------------------------------------

    [Fact]
    public void PayloadTooLong_Throws()
        => Assert.Throws<ArgumentException>(() => InspectLink.Deserialize(new string('A', 4097) + "B")); // 4098 chars

    [Fact]
    public void Serialize_PaintwearAboveOne_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            InspectLink.Serialize(new ItemPreviewData { PaintWear = 1.5f }));

    [Fact]
    public void Serialize_PaintwearBelowZero_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            InspectLink.Serialize(new ItemPreviewData { PaintWear = -0.1f }));

    [Fact]
    public void Serialize_PaintwearBoundary_Valid()
    {
        // 0.0 and 1.0 are valid
        Assert.StartsWith("00", InspectLink.Serialize(new ItemPreviewData { PaintWear = 0f }));
        Assert.StartsWith("00", InspectLink.Serialize(new ItemPreviewData { PaintWear = 1f }));
    }

    [Fact]
    public void Serialize_CustomnameTooLong_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            InspectLink.Serialize(new ItemPreviewData { CustomName = new string('A', 101) }));

    [Fact]
    public void Serialize_CustomnameMaxLength_Valid()
        => Assert.StartsWith("00", InspectLink.Serialize(new ItemPreviewData { CustomName = new string('A', 100) }));

    // -------------------------------------------------------------------------
    // CSFloat / gen.test.ts test vectors
    // -------------------------------------------------------------------------

    private const string CsfloatA = "00180720DA03280638FBEE88F90340B2026BC03C96";
    private const string CsfloatB = "00180720C80A280638A4E1F5FB03409A0562040800104C62040801104C62040802104C62040803104C6D4F5E30";
    private const string CsfloatC = "A2B2A2BA69A882A28AA192AECAA2D2B700A3A5AAA2B286FA7BA0D684BE72";

    [Fact] public void CsfloatA_Defindex()
        => Assert.Equal(7u, InspectLink.Deserialize(CsfloatA).DefIndex);

    [Fact] public void CsfloatA_Paintindex()
        => Assert.Equal(474u, InspectLink.Deserialize(CsfloatA).PaintIndex);

    [Fact] public void CsfloatA_Paintseed()
        => Assert.Equal(306u, InspectLink.Deserialize(CsfloatA).PaintSeed);

    [Fact] public void CsfloatA_Rarity()
        => Assert.Equal(6u, InspectLink.Deserialize(CsfloatA).Rarity);

    [Fact] public void CsfloatA_Paintwear_NotNull()
        => Assert.NotNull(InspectLink.Deserialize(CsfloatA).PaintWear);

    [Fact] public void CsfloatA_Paintwear_Approx()
        => Assert.Equal(0.6337f, InspectLink.Deserialize(CsfloatA).PaintWear!.Value, precision: 3);

    [Fact] public void CsfloatB_StickerCount()
        => Assert.Equal(4, InspectLink.Deserialize(CsfloatB).Stickers.Count);

    [Fact] public void CsfloatB_StickerIds()
    {
        foreach (var s in InspectLink.Deserialize(CsfloatB).Stickers)
            Assert.Equal(76u, s.StickerId);
    }

    [Fact] public void CsfloatB_Paintindex()
        => Assert.Equal(1352u, InspectLink.Deserialize(CsfloatB).PaintIndex);

    [Fact] public void CsfloatB_Paintwear_Approx()
        => Assert.Equal(0.99f, InspectLink.Deserialize(CsfloatB).PaintWear!.Value, precision: 2);

    [Fact] public void CsfloatC_Defindex()
        => Assert.Equal(1355u, InspectLink.Deserialize(CsfloatC).DefIndex);

    [Fact] public void CsfloatC_Quality()
        => Assert.Equal(12u, InspectLink.Deserialize(CsfloatC).Quality);

    [Fact] public void CsfloatC_KeychainCount()
        => Assert.Equal(1, InspectLink.Deserialize(CsfloatC).Keychains.Count);

    [Fact] public void CsfloatC_KeychainHighlightReel()
        => Assert.Equal(345u, InspectLink.Deserialize(CsfloatC).Keychains[0].HighlightReel);

    [Fact] public void CsfloatC_NoPaintwear()
        => Assert.Null(InspectLink.Deserialize(CsfloatC).PaintWear);

    [Fact]
    public void Roundtrip_HighlightReel()
    {
        var data = new ItemPreviewData
        {
            DefIndex = 7,
            Keychains = [new Sticker { Slot = 0, StickerId = 36, HighlightReel = 345 }],
        };
        var result = InspectLink.Deserialize(InspectLink.Serialize(data));
        Assert.Equal(345u, result.Keychains[0].HighlightReel);
    }

    [Fact]
    public void Roundtrip_NullPaintwear()
    {
        var data = new ItemPreviewData { DefIndex = 7, PaintWear = null };
        var result = InspectLink.Deserialize(InspectLink.Serialize(data));
        Assert.Null(result.PaintWear);
    }
}
