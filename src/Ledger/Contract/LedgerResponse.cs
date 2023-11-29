using Common.Buffers;
using Common.Buffers.Extensions;

namespace Ledger.Contract
{
    public class LedgerResponse
    {
        public ReadOnlyMemory<byte> Data { get; set; }
        public uint SW { get; set; }

        public static LedgerResponse ReadFrom(IBufferReader<byte> reader)
        {
            return new LedgerResponse
            {
                Data = reader.Read(reader.Memory.Length - 2),
                SW = reader.ReadUInt16BigEndian()
            };
        }
    }
}