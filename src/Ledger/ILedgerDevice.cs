
namespace Ledger
{
    public interface ILedgerDevice
    {
        public string Id { get; }
        public ValueTask<ILedgerDeviceChannel> OpenChannelAsync(CancellationToken token);
    }
}