using System.Buffers.Binary;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using Unity.Mathematics;

// A bounded interpretation of installed native instructions, not a speculative
// correction to game source. No game DLL is loaded/executed by PEReader.
sealed class BurstFinishLookup {
    public const string BinaryHash="C907D1A8E74368513756FE860853DAF0B032D59DC5E30A12EA236A6469CDC2EB";
    static readonly (string cpu,int first,int second)[] Blocks={
        ("sse2",0x1cf30f,0x1cf3d0),("avx2",0xf9241a,0xf924b0),
        ("avx",0x1ce1f3a,0x1ce1fd0),("sse4",0x2a1efca,0x2a1f080)};
    readonly uint multiplier,startConstant,endConstant;
    readonly int mask;
    public readonly List<object> Queries=new();
    public object Evidence { get; }

    public BurstFinishLookup(string path,int edgeCount,JsonElement capturedMap) {
        if(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(int2).Assembly.Location)))!="F8C037B71D2BB4496DDAB30B79D8840F5A32F0738A1547BA5534E5B43254C9B8")
            throw new ArgumentException("Mathematics binary changed; revalidate producer hashing");
        byte[] binary=File.ReadAllBytes(path);
        if(Convert.ToHexString(SHA256.HashData(binary))!=BinaryHash)throw new ArgumentException("Burst binary changed; revalidate native lookup semantics");
        using var pe=new PEReader(new MemoryStream(binary));
        var decoded=new List<object>();
        foreach(var block in Blocks) {
            var first=pe.GetSectionData(block.first).GetContent(0,14).ToArray();
            var second=pe.GetSectionData(block.second).GetContent(0,6).ToArray();
            // IMUL edx,r12d,imm32; LEA r9d,[rdx+imm32]; OR edx,imm32.
            if(!first.AsSpan(0,3).SequenceEqual(new byte[]{0x41,0x69,0xd4})
                ||!first.AsSpan(7,3).SequenceEqual(new byte[]{0x44,0x8d,0x8a})
                ||!second.AsSpan(0,2).SequenceEqual(new byte[]{0x81,0xca}))throw new ArgumentException("Native lookup opcode changed");
            uint mul=BinaryPrimitives.ReadUInt32LittleEndian(first.AsSpan(3,4));
            uint add=BinaryPrimitives.ReadUInt32LittleEndian(first.AsSpan(10,4));
            uint or=BinaryPrimitives.ReadUInt32LittleEndian(second.AsSpan(2,4));
            if(decoded.Count>0&&(mul!=multiplier||add!=startConstant||or!=endConstant))throw new ArgumentException("CPU lookup variants disagree");
            multiplier=mul;startConstant=add;endConstant=or;
            decoded.Add(new{block.cpu,firstRva=block.first,secondRva=block.second,multiplier=mul,startAdd=add,endOr=or});
        }
        // Hash-pinned AllocateBuffersJob sets Capacity=2*edge count. Native map
        // allocates ceilpow2(2*capacity) buckets. Neither entries nor captured
        // bucket placement supplies computed flatten values or missing decisions.
        int capacity=checked(2*edgeCount),buckets=1;
        if(capacity<=0||capacity>16384)throw new ArgumentException("Unsupported map capacity");
        while(buckets<2*capacity)buckets<<=1;
        mask=buckets-1;
        if(capturedMap.TryGetProperty("capacity",out var c)&&(c.GetInt32()!=capacity||capturedMap.GetProperty("bucketCapacityMask").GetInt32()!=mask))
            throw new ArgumentException("Native map allocation differs from predicted allocation");
        Evidence=new{path=Path.GetFullPath(path),sha256=BinaryHash,
            exportFamily="853da17896382c2aa97461e4792dcf61",decoded,capacity,bucketCapacityMask=mask,
            scope="Native instruction-derived Finish lookup hashing; source-derived producer hashes; well-formed hash table, exact key equality; no recorded map values substituted"};
    }
    public bool CanRetrieve(int2 key) {
        if(key.x<0||key.y<0||key.y>1)throw new ArgumentException("Finish lookup requires nonnegative entity index and endpoint 0/1");
        uint product=unchecked((uint)key.x*multiplier);
        uint hash=key.y==0?unchecked(product+startConstant):product|endConstant;
        int nativeBucket=(int)(hash&(uint)mask),producerBucket=key.GetHashCode()&mask;
        bool reachable=nativeBucket==producerBucket;
        Queries.Add(new{key=new[]{key.x,key.y},producerBucket,nativeBucket,reachable});
        return reachable;
    }
}
