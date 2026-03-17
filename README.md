# cs2-masked-inspect (C#)

Pure C# library for encoding and decoding CS2 masked inspect links — no runtime dependencies, targets .NET 8+ and .NET 10+.

## Installation

```bash
dotnet add package VlyDev.Cs2MaskedInspect
```

## Usage

### Deserialize a CS2 inspect link

```csharp
using VlyDev.Cs2MaskedInspect;

// Accepts a full steam:// URL or a raw hex string
var item = InspectLink.Deserialize(
    "steam://run/730//+csgo_econ_action_preview%20E3F3367440334DE2FBE4C345E0CBE0D3..."
);

Console.WriteLine(item.DefIndex);   // 7  (AK-47)
Console.WriteLine(item.PaintIndex); // 422
Console.WriteLine(item.PaintSeed);  // 922
Console.WriteLine(item.PaintWear);  // ~0.04121
Console.WriteLine(item.ItemId);     // 46876117973

foreach (var s in item.Stickers)
    Console.WriteLine(s.StickerId); // 7436, 5144, 6970, 8069, 5592
```

### Serialize an item to a hex payload

```csharp
using VlyDev.Cs2MaskedInspect;

var data = new ItemPreviewData
{
    DefIndex   = 60,
    PaintIndex = 440,
    PaintSeed  = 353,
    PaintWear  = 0.005411375779658556f,
    Rarity     = 5,
};

string hex = InspectLink.Serialize(data);
// 00183C20B803280538E9A3C5DD0340E102C246A0D1

string url = $"steam://run/730//+csgo_econ_action_preview%20{hex}";
```

### Item with stickers and keychains

```csharp
using VlyDev.Cs2MaskedInspect;

var data = new ItemPreviewData
{
    DefIndex   = 7,
    PaintIndex = 422,
    PaintSeed  = 922,
    PaintWear  = 0.04121f,
    Rarity     = 3,
    Quality    = 4,
    Stickers   =
    [
        new Sticker { Slot = 0, StickerId = 7436 },
        new Sticker { Slot = 1, StickerId = 5144, Wear = 0.1f },
    ],
};

string hex = InspectLink.Serialize(data);
var decoded = InspectLink.Deserialize(hex); // round-trip
```

---

## Gen codes

Gen codes are space-separated command strings used on CS2 community servers to spawn items.

Format:
```
!gen {defindex} {paintindex} {paintseed} {paintwear} [{s0_id} {s0_wear} ... {s4_id} {s4_wear}] [{kc_id} {kc_wear} ...]
```

- Stickers are always represented as 5 slot pairs (padded with `0 0` for empty slots)
- Keychains are appended without padding, only for present slots
- Float values have trailing zeros stripped (max 8 decimal places); `0.0` becomes `"0"`

### Generate a Steam inspect URL from parameters

```csharp
using VlyDev.Cs2MaskedInspect;

string url = GenCode.Generate(
    defIndex:   7,
    paintIndex: 474,
    paintSeed:  306,
    paintWear:  0.22540508f,
    rarity:     6);
// steam://rungame/730/76561202255233023/+csgo_econ_action_preview%200018...
```

### Convert an ItemPreviewData to a gen code

```csharp
using VlyDev.Cs2MaskedInspect;

var item = new ItemPreviewData
{
    DefIndex   = 7,
    PaintIndex = 474,
    PaintSeed  = 306,
    PaintWear  = 0.22540508f,
    Stickers   = [new Sticker { Slot = 2, StickerId = 7203 }],
};

string code = GenCode.ToGenCode(item);
// "!gen 7 474 306 0.22540508 0 0 0 0 7203 0 0 0 0 0"

string code2 = GenCode.ToGenCode(item, "!g"); // custom prefix
```

### Parse a gen code string

```csharp
using VlyDev.Cs2MaskedInspect;

var item = GenCode.ParseGenCode("!gen 7 474 306 0.22540508 0 0 0 0 7203 0 0 0 0 0");
Console.WriteLine(item.DefIndex);    // 7
Console.WriteLine(item.PaintIndex);  // 474
Console.WriteLine(item.PaintSeed);   // 306
Console.WriteLine(item.PaintWear);   // 0.22540508

var item2 = GenCode.ParseGenCode("7 474 306 0.22540508"); // prefix is optional
```

### Convert an existing inspect link directly to a gen code

```csharp
string code = GenCode.GenCodeFromLink("steam://rungame/730/76561202255233023/+csgo_econ_action_preview%20001A...");
// "!gen 7 474 306 0.22540508"
```

---

## Validation

Use `IsMasked()` and `IsClassic()` to detect the link type without decoding it.

```csharp
using VlyDev.Cs2MaskedInspect;

// New masked format (pure hex blob) — can be decoded offline
var maskedUrl = "steam://run/730//+csgo_econ_action_preview%20E3F3...";
InspectLink.IsMasked(maskedUrl);   // true
InspectLink.IsClassic(maskedUrl);  // false

// Hybrid format (S/A/D prefix with hex proto after D) — also decodable offline
var hybridUrl = "steam://rungame/730/.../+csgo_econ_action_preview%20S76561199323320483A50075495125D1101C4C4FCD4AB10...";
InspectLink.IsMasked(hybridUrl);   // true
InspectLink.IsClassic(hybridUrl);  // false

// Classic format — requires Steam Game Coordinator to fetch item info
var classicUrl = "steam://rungame/730/.../+csgo_econ_action_preview%20S76561199842063946A49749521570D2751293026650298712";
InspectLink.IsMasked(classicUrl);  // false
InspectLink.IsClassic(classicUrl); // true
```

---

## Validation rules

`Deserialize()` enforces:

| Rule | Limit | Exception |
|------|-------|-----------|
| Hex payload length | max 4,096 characters | `ArgumentException` |
| Protobuf field count | max 100 per message | `InvalidOperationException` |

`Serialize()` enforces:

| Field | Constraint | Exception |
|-------|-----------|-----------|
| `PaintWear` | `[0.0, 1.0]` | `ArgumentOutOfRangeException` |
| `CustomName` | max 100 characters | `ArgumentOutOfRangeException` |

---

## How the format works

Three URL formats are handled:

1. **New masked format** — pure hex blob after `csgo_econ_action_preview`:
   ```
   steam://run/730//+csgo_econ_action_preview%20<hexbytes>
   ```

2. **Hybrid format** — old-style `S/A/D` prefix, but with a hex proto appended after `D` (instead of a decimal did):
   ```
   steam://rungame/730/.../+csgo_econ_action_preview%20S<steamid>A<assetid>D<hexproto>
   ```

3. **Classic format** — old-style `S/A/D` with a decimal did; requires Steam GC to resolve item details.

For formats 1 and 2 the library decodes the item offline. For format 3 only URL parsing is possible.

The hex blob (formats 1 and 2) has the following binary layout:

```
[key_byte] [proto_bytes XOR'd with key] [4-byte checksum XOR'd with key]
```

| Section | Size | Description |
|---------|------|-------------|
| `key_byte` | 1 byte | XOR key. `0x00` = no obfuscation (tool links). Other values = native CS2 links. |
| `proto_bytes` | variable | `CEconItemPreviewDataBlock` protobuf, each byte XOR'd with `key_byte`. |
| `checksum` | 4 bytes | Big-endian uint32, XOR'd with `key_byte`. |

### Checksum algorithm

```csharp
var buffer   = new byte[] { 0x00 }.Concat(protoBytes).ToArray();
uint crc     = Crc32(buffer);
uint xored   = ((crc & 0xFFFF) ^ ((uint)protoBytes.Length * crc)) & 0xFFFFFFFF;
// stored as big-endian uint32
```

### `PaintWear` encoding

`PaintWear` is stored as a `uint32` varint whose bit pattern is the IEEE 754 representation
of a `float`. The library handles this transparently — callers always work with `float` values.

---

## Proto field reference

### CEconItemPreviewDataBlock

| Field | Number | Type | Description |
|-------|--------|------|-------------|
| `AccountId` | 1 | uint32 | Steam account ID (often 0) |
| `ItemId` | 2 | uint64 | Item ID in the owner's inventory |
| `DefIndex` | 3 | uint32 | Item definition index (weapon type) |
| `PaintIndex` | 4 | uint32 | Skin paint index |
| `Rarity` | 5 | uint32 | Item rarity |
| `Quality` | 6 | uint32 | Item quality |
| `PaintWear` | 7 | uint32* | float reinterpreted as uint32 |
| `PaintSeed` | 8 | uint32 | Pattern seed (0–1000) |
| `KillEaterScoreType` | 9 | uint32 | StatTrak counter type |
| `KillEaterValue` | 10 | uint32 | StatTrak value |
| `CustomName` | 11 | string | Name tag |
| `Stickers` | 12 | repeated Sticker | Applied stickers |
| `Inventory` | 13 | uint32 | Inventory flags |
| `Origin` | 14 | uint32 | Origin |
| `QuestId` | 15 | uint32 | Quest ID |
| `DropReason` | 16 | uint32 | Drop reason |
| `MusicIndex` | 17 | uint32 | Music kit index |
| `EntIndex` | 18 | int32 | Entity index |
| `PetIndex` | 19 | uint32 | Pet index |
| `Keychains` | 20 | repeated Sticker | Applied keychains |

### Sticker

| Field | Number | Type | Description |
|-------|--------|------|-------------|
| `Slot` | 1 | uint32 | Slot position |
| `StickerId` | 2 | uint32 | Sticker definition ID |
| `Wear` | 3 | float? | Wear (fixed32) |
| `Scale` | 4 | float? | Scale (fixed32) |
| `Rotation` | 5 | float? | Rotation (fixed32) |
| `TintId` | 6 | uint32 | Tint |
| `OffsetX` | 7 | float? | X offset (fixed32) |
| `OffsetY` | 8 | float? | Y offset (fixed32) |
| `OffsetZ` | 9 | float? | Z offset (fixed32) |
| `Pattern` | 10 | uint32 | Pattern (keychains) |

---

## Known test vectors

### Vector 1 — Native CS2 link (XOR key 0xE3)

```
E3F3367440334DE2FBE4C345E0CBE0D3E7DB6943400AE0A379E481ECEBE2F36F
D9DE2BDB515EA6E30D74D981ECEBE3F37BCBDE640D475DA6E35EFCD881ECEBE3
F359D5DE37E9D75DA6436DD3DD81ECEBE3F366DCDE3F8F9BDDA69B43B6DE81EC
EBE3F33BC8DEBB1CA3DFA623F7DDDF8B71E293EBFD43382B
```

| Field | Value |
|-------|-------|
| `ItemId` | `46876117973` |
| `DefIndex` | `7` (AK-47) |
| `PaintIndex` | `422` |
| `PaintSeed` | `922` |
| `PaintWear` | `≈ 0.04121` |
| `Rarity` | `3` |
| `Quality` | `4` |
| Sticker IDs | `[7436, 5144, 6970, 8069, 5592]` |

### Vector 2 — Tool-generated link (key 0x00)

```csharp
new ItemPreviewData { DefIndex = 60, PaintIndex = 440, PaintSeed = 353,
                      PaintWear = 0.005411375779658556f, Rarity = 5 }
```

Expected hex:

```
00183C20B803280538E9A3C5DD0340E102C246A0D1
```

---

## Running tests

```bash
dotnet test
```

---

## Contributing

Bug reports and pull requests are welcome on [GitHub](https://github.com/vlydev/cs2-masked-inspect-csharp).

1. Fork the repository
2. Create a branch: `git checkout -b my-fix`
3. Make your changes and add tests
4. Ensure all tests pass: `dotnet test`
5. Open a Pull Request

All PRs require the CI checks to pass before merging.

---

## Author

[VlyDev](https://github.com/vlydev) — vladdnepr1989@gmail.com

---

## License

MIT © [VlyDev](https://github.com/vlydev)
