using Common.Buffers.Extensions;
using System.Buffers;

namespace Ledger.Contract
{
    public class LedgerRequest
    {
        public const int DATA_LIMIT = 255;

        public byte InstructionClass { get; set; }
        public byte Instruction { get; set; }
        public byte Param1 { get; set; }
        public byte Param2 { get; set; }
        public ReadOnlyMemory<byte> Data { get; set; }

        public void WriteTo(IBufferWriter<byte> writer)
        {
            var dataLength = (byte)Data.Length;

            if (dataLength > DATA_LIMIT)
            {
                throw new FormatException();
            }

            writer.Write(InstructionClass);
            writer.Write(Instruction);
            writer.Write(Param1);
            writer.Write(Param2);
            writer.Write(dataLength);
            writer.Write(Data.Span);
        }
    }
}