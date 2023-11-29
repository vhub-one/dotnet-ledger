
namespace Ledger
{
    public interface ILedgerDeviceChannel : IAsyncDisposable
    {
        public ValueTask<ReadOnlyMemory<byte>> ExchangeAsync(ReadOnlyMemory<byte> request, CancellationToken token);
    }
}