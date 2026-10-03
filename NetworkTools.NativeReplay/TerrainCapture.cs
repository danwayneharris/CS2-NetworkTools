using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using NativeReplay;
using Unity.Mathematics;

static class TerrainCapture {
    static ushort[] Samples(JsonElement data) {
        int count = data.GetProperty("length").GetInt32();
        if (!data.GetProperty("created").GetBoolean()) {
            if (count != 0) throw new ArgumentException("Uncreated terrain array has data");
            return Array.Empty<ushort>();
        }
        if (count < 0 || count > 67108864 || data.GetProperty("encoding").GetString() != "uint16-little-endian")
            throw new ArgumentException("Terrain array contract changed");
        byte[] bytes = File.ReadAllBytes(data.GetProperty("path").GetString()!);
        if (bytes.Length != count * 2 || Convert.ToHexString(SHA256.HashData(bytes)) != data.GetProperty("sha256").GetString())
            throw new ArgumentException("Terrain archive checksum/length mismatch");
        var values = new ushort[count];
        for (int i = 0; i < count; i++) values[i] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(i * 2, 2));
        return values;
    }
    static float3 V(JsonElement p) => new(p.GetProperty("x").GetSingle(), p.GetProperty("y").GetSingle(), p.GetProperty("z").GetSingle());
    static int3 I(JsonElement p) => new(p.GetProperty("x").GetInt32(), p.GetProperty("y").GetInt32(), p.GetProperty("z").GetInt32());
    public static ReplayTerrainData Load(JsonElement data) {
        if (data.GetProperty("storage").GetString() != "TerrainHeightData-ushort-arrays-v1")
            throw new ArgumentException("Unknown terrain capture contract");
        var result = new ReplayTerrainData {
            heights = Samples(data.GetProperty("heights")), downscaledHeights = Samples(data.GetProperty("downscaledHeights")),
            resolution = I(data.GetProperty("resolution")), downScaledResolution = I(data.GetProperty("downScaledResolution")),
            scale = V(data.GetProperty("scale")), offset = V(data.GetProperty("offset")),
            hasBackdrop = data.GetProperty("hasBackdrop").GetBoolean(), captured = true
        };
        if (!math.all(math.isfinite(result.scale)) || !math.all(math.isfinite(result.offset)) || math.any(result.scale <= 0)
            || result.resolution.x <= 0 || result.resolution.z <= 0
            || (long)result.resolution.x * result.resolution.z != result.heights.Length)
            throw new ArgumentException("Invalid terrain transform/resolution");
        if (result.downscaledHeights.Length != 0 && (long)result.downScaledResolution.x * result.downScaledResolution.z != result.downscaledHeights.Length)
            throw new ArgumentException("Backdrop array/resolution mismatch");
        return result;
    }
}
