
namespace Ledger.Transport.WinBle
{
    public class LedgerBleDevice : ILedgerDevice
    {
        private string _deviceId;
        private readonly LedgerBleDeviceSpec _deviceSpec;

        public LedgerBleDevice(string deviceId, LedgerBleDeviceSpec deviceSpec)
        {
            _deviceId = deviceId;
            _deviceSpec = deviceSpec;
        }

        public string Id
        {
            get { return _deviceId; }
        }

        public async ValueTask<ILedgerDeviceChannel> OpenChannelAsync(CancellationToken token)
        {
            var channel = LedgerBleDeviceChannelPool.GetOrCreateChannel(_deviceId, _deviceSpec);

            if (channel != null)
            {
                try
                {
                    // Make sure channel is open
                    await channel.OpenAsync(token);
                }
                catch
                {
                    // We didn't manage to open channel => dispose it
                    await channel.DisposeAsync();

                    throw;
                }
            }

            return channel;
        }
    }
}