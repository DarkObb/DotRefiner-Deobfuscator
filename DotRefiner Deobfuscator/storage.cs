using System.IO;
using System.Text;

namespace DotRefiner_Deobfuscator {
    internal class storage {
        private readonly BinaryReader storingShit;
        public storage(Stream arr) => storingShit = new BinaryReader(arr, Encoding.Unicode);
        public storage(byte[] arr) : this(new MemoryStream(arr)) { }
        public string GetString() => storingShit.ReadString();
        public sbyte GetSByte() => storingShit.ReadSByte();
        public int GetInt() => storingShit.ReadInt32();
        public long GetLong() => storingShit.ReadInt64();
        public float GetFloat() => storingShit.ReadSingle();
        public double GetDouble() => storingShit.ReadDouble();      
    }
}

/* Made by DarkObb */