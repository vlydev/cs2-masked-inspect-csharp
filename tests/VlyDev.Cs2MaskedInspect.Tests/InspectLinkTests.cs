using System.Collections.Generic;
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
        => Assert.Throws<MalformedInspectLinkException>(() => InspectLink.Deserialize("0000"));

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
        => Assert.Throws<MalformedInspectLinkException>(() => InspectLink.Deserialize(new string('A', 4097) + "B")); // 4098 chars

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

    // -------------------------------------------------------------------------
    // Sticker Slab test vectors
    //
    // Sticker Slabs: defIndex=1355, quality=8, keychains[0].stickerId=37 (placeholder)
    // keychains[0].paintKit = actual slab variant ID
    //
    // URL A: rarity=5, paintKit=7256
    // URL B: rarity=3, paintKit=275
    // -------------------------------------------------------------------------

    private const string StickerSlabA =
        "steam://run/730//+csgo_econ_action_preview%20918191895A9BB191B994A199F991E191339096999181B4F149A98D5C0889";

    private const string StickerSlabB =
        "steam://run/730//+csgo_econ_action_preview%20CBDBCBD300C1EBCBE3C8FBC3A3CBBBCB69CACCC3CBDBEEAB58C9B8B67C83";

    [Fact] public void StickerSlabA_DefIndex()
        => Assert.Equal(1355u, InspectLink.Deserialize(StickerSlabA).DefIndex);

    [Fact] public void StickerSlabA_Quality()
        => Assert.Equal(8u, InspectLink.Deserialize(StickerSlabA).Quality);

    [Fact] public void StickerSlabA_Rarity()
        => Assert.Equal(5u, InspectLink.Deserialize(StickerSlabA).Rarity);

    [Fact] public void StickerSlabA_KeychainCount()
        => Assert.Single(InspectLink.Deserialize(StickerSlabA).Keychains);

    [Fact] public void StickerSlabA_KeychainStickerId()
        => Assert.Equal(37u, InspectLink.Deserialize(StickerSlabA).Keychains[0].StickerId);

    [Fact] public void StickerSlabA_KeychainPaintKit()
        => Assert.Equal(7256u, InspectLink.Deserialize(StickerSlabA).Keychains[0].PaintKit);

    [Fact] public void StickerSlabB_DefIndex()
        => Assert.Equal(1355u, InspectLink.Deserialize(StickerSlabB).DefIndex);

    [Fact] public void StickerSlabB_Quality()
        => Assert.Equal(8u, InspectLink.Deserialize(StickerSlabB).Quality);

    [Fact] public void StickerSlabB_Rarity()
        => Assert.Equal(3u, InspectLink.Deserialize(StickerSlabB).Rarity);

    [Fact] public void StickerSlabB_KeychainCount()
        => Assert.Single(InspectLink.Deserialize(StickerSlabB).Keychains);

    [Fact] public void StickerSlabB_KeychainStickerId()
        => Assert.Equal(37u, InspectLink.Deserialize(StickerSlabB).Keychains[0].StickerId);

    [Fact] public void StickerSlabB_KeychainPaintKit()
        => Assert.Equal(275u, InspectLink.Deserialize(StickerSlabB).Keychains[0].PaintKit);

    [Fact]
    public void Roundtrip_PaintKit()
    {
        var data = new ItemPreviewData
        {
            DefIndex = 1355,
            Quality = 8,
            Rarity = 5,
            Keychains = [new Sticker { Slot = 0, StickerId = 37, PaintKit = 7256 }],
        };
        var result = InspectLink.Deserialize(InspectLink.Serialize(data));
        Assert.Single(result.Keychains);
        Assert.Equal(37u, result.Keychains[0].StickerId);
        Assert.Equal(7256u, result.Keychains[0].PaintKit);
    }

    // -----------------------------------------------------------------------
    // Malformed URLs (regression: must reject cleanly with MalformedInspectLinkException)
    // -----------------------------------------------------------------------

    public static IEnumerable<object[]> MalformedUrls => new[]
    {
        new object[] { "steam://run/730//+csgo_econ_action_preview%20ADBD1050393912ACB5AC8D45AC85A99DA9956A116D5FAEED21ACCFB4A5AFBD348EB0ADAD2D9280ADADDD6F90EDA37510E84D8BEE11CFB4A5ACBD348EB0ADAD2D9280ADAD5D6F906F2B4C13E84D93D591CFB9A5ADBD419EB0ADAD2D9290ADF22010E8B72FB213CFB4A5ADBD549EB0ADAD2D9280ADADED6C90CFD43F10E892DFE513CFB4A5ADBD549EB0ADAD2D9280ADAD85EE902F952210E82EB8A613C52E2D2D2DA1DDA90FACBBA5ADBD89902923AAECE83" },
        new object[] { "steam://run/730//+csgo_econ_action_preview%20EEFE3144332550EFF6E7CE28E8C6EADEEAD642323218EDAE4DEA8CFAE6ECFE35A7F302BFD6D1D3004FCB50ABAE5CF8528CF7E6EEFE0DA7F3394DDED1C3EEEE7EAFD39E25B3D3AB9EAE70D28CFAE6ECFE32A7F3EEEE6ED1D3595B17D3AB9E8E65538CFAE6ECFE64ADF3EEEE6ED1D3E597F3D3AB2EEA1AD58CF7E6EDFE0BCAF302BFD6D1C3EEEEEF2DD3AEF5F552ABEE31A855866D6E6E6EE29EE64CEFF8E6EEFED8D3B6CBCBACABFD70EED1A31F96E7AFBE5" },
        new object[] { "steam://run/730//+csgo_econ_action_preview%204A5A8EFCB1B9F44B524B6AA24B624E7A4E72BACFF6B8490AD449285E42485AAB75576316457577CA2422760F4A413E712853424A5AA679574A4ACA75674A4A8A8A770A7F85760FD04246F4285342495AB279574A4ACA75674A4A8A0A7799714A750F0A5140F7285342495AAD7277EB4547F40F0A00EB7122C9CACACA463A4EE84B5D424A5A4C776C02A10A0F34A5C17407F0145C0A1AA" },
        new object[] { "steam://run/730//+csgo_econ_action_preview%20FAEA5766387F45FBE2FDDA71F2D2FECAF3C2142C0C0EF9BA7CFFB2FAAAFA98EEF2F8EA3BFBD7FAFA3ABBC7EA2FB7C7BF9ACC47C698E3F2F9EA03C9E7FAFA7AC5D7FAFABA3AC780CA89C4BFFAAAD9C198E3F2F9EA03C9E7FAFA7AC5D7FAFACEB9C7B60177C4BFAAB82AC698EEF2F9EA03C9E7FAFA7AC5C7C11558C4BFFA43DBC198F5F2FBEA13DEC7759A1147BF9A16F64692797A7A7AF68AF258FBEFF2FAEADEC7DEAC32BBBF0BF9BAC4B760382CC5A2D24" },
        new object[] { "steam://run/730//+csgo_econ_action_preview%209F8F4F504C7C219E87B7BF629EB79CAF9BA73F1D53419CDF0699FD8B979F8F5CCF82050686A0A2F4038821DA17F3FD22FD8B979F8F49D4825C6AB7A0A25F50B224DA7F0CF222FD8B979D8F5DCF82F9F9B9A0A2B7B25422DA6F6F1422FD8B979D8F5DCF822781DAA0A247B731A2DA7F92EF23FD86979F8F5DCF827EE5CBA0B29F9FDFDFA285AD4322DA9FFD8AA3F71C1F1F1F93EF873D9E88979F8FDDA243C05EDFDA4CFB44A0D202B77EDFCF3F339C3DF89" },
        new object[] { "steam://run/730//+csgo_econ_action_preview%20FAEA5B24060844FBE2C6DA10F2D2FECAF3C2631A3308F9BA47F8B2FAAAFA98EEF2FEEA29B2E781EED4C5C7776B78C4BFFAFA21CD98EEF2FAEA09BCE7F02DD9C5C7FE6C57C7BF7A58ED4698EEF2FEEA6DBDE79C9CDCC5C7929F2F47BFFA461BC398E3F2FBEA15BDE7E57FD1C5D7FAFA8AB8C7C2CEDC46BF12973EC798EEF2FEEA24B8E781EED4C5C75696FCC5BF2AEBF2C792797A7A7AF68AF258FBEDF2FAEAD2C7B3683FBBBF065F8CC5B763A382BAAA0C5" },
        new object[] { "steam://run/730//+csgo_econ_action_preview%204D5D9DF8C7D2F34C556E6DDC4C654E7D4975B2D2AEBB4E0DAB4B2F59454E5D9A70604D4DBD8C7002A356F308CD4603F62F5445495DEB745011C20F72604D4D798F70797F9FF3089D49ABF12F5945495DEB74502B2BAB737045B3FAF308FD4BE8F12F5945495DF868508081417270BFB9D2F308ADD283F12F5945495DF86850AC375972702FA3CBF3083D2EBFF125CECDCDCD413D5AEF4C5A454D5D567084E4F80C08254C547200CE3E1D0D1DF9D24C63938" },
        new object[] { "steam://run/730//+csgo_econ_action_preview%20CFDF6258412F71CED7C8EF5CC6E7C9FFCBF7465B3B38CC8F7ACEADD6C7CEDF3BF2D2F2C5D8F0E2CFCFE40CF27F72E1728AF786F5F2ADC0C7CCDF3BF2F241D88EF18A0F03F6F2ADDBC7CCDF3CF2E2CFCF0F8DF27B3FB7F18A6F5ACDF2ADD6C7CCDF3CF2D2C518ECF0E2CFCF8F0EF2D5C29AF18ACFCFD8F5ADDBC7CFDF3CF2E2CFCF6D8DF24DB0DA718A2FA6DBF3A74C4F4F4FC3BFC76DCED8C7CFDF87F27B950C8E8A37D0A3F182A2B8A48F9F4332CD2C2B6" },
        new object[] { "steam://run/730//+csgo_econ_action_preview%20CEDE51082D1C70CFD6CFEE54C6E6CAFEC7F631274538CD8E2DC886CE9ECEACDAC6CDDE0C8DD3CECE4EF1F382996B738B4E5E8D75ACD7C6CEDE0C8DD3CECE4EF1E3CECE8E0FF3A56603708BECDBE170ACD7C6CEDE0C8DD3CECE4EF1E3CECEDE0FF37682A5708B0650FB70ACD7C6CEDE0C8DD3CECE4EF1E3CECEDE0FF34E3CEC738BDAC6F870ACD7C6CDDE798AD3CECE4EF1E3CECE0E0EF3333BC5F18BAE8E9FF2A64D4E4E4EC2BECA6CCFD9C6CEDECFF37C6" },
        new object[] { "ABC" },
        new object[] { "" },
        new object[] { "ZZZZZZZZZZZZ" },
    };

    [Theory]
    [MemberData(nameof(MalformedUrls))]
    public void Malformed_Url_Throws_MalformedInspectLinkException(string url)
        => Assert.Throws<MalformedInspectLinkException>(() => InspectLink.Deserialize(url));

    [Fact]
    public void MalformedInspectLinkException_Inherits_ArgumentException()
        => Assert.True(typeof(System.ArgumentException).IsAssignableFrom(typeof(MalformedInspectLinkException)));

    [Fact]
    public void Malformed_OddHex_Message_Mentions_Length()
    {
        var ex = Assert.Throws<MalformedInspectLinkException>(() => InspectLink.Deserialize("ABC"));
        Assert.Contains("Malformed", ex.Message);
        Assert.Matches("(?i)length|even|hex", ex.Message);
    }
}
