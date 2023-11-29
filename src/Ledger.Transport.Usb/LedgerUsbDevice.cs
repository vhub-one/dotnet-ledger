using LedgerWallet.Transports;

namespace Ledger.Transport.Usb
{
    public class LedgerUsbDevice : ILedgerDevice
    {
        private readonly HIDLedgerTransport _transport;

        public LedgerUsbDevice(HIDLedgerTransport transport)
        {
            _transport = transport;
        }

        public string Id
        {
            get { return _transport.DevicePath; }
        }

        public ValueTask<ILedgerDeviceChannel> OpenChannelAsync(CancellationToken token)
        {
            var channel = new LedgerUsbDeviceChannel(_transport);

            return ValueTask.FromResult<ILedgerDeviceChannel>(
                channel
            );
        }
    }
}