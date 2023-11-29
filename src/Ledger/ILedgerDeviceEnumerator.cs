namespace Ledger
{
    public interface ILedgerDeviceEnumerator
    {
        public IAsyncEnumerable<ILedgerDevice> GetDevicesAsync(CancellationToken token = default);
    }
}