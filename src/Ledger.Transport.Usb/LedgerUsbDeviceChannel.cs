using LedgerWallet;
using LedgerWallet.Transports;

namespace Ledger.Transport.Usb
{
    public class LedgerUsbDeviceChannel : ILedgerDeviceChannel
    {
        private readonly HIDLedgerTransport _transport;

        public LedgerUsbDeviceChannel(HIDLedgerTransport transport)
        {
            _transport = transport;
        }

        public async ValueTask<ReadOnlyMemory<byte>> ExchangeAsync(ReadOnlyMemory<byte> command, CancellationToken token)
        {
            // Serialize
            var apdu = command.ToArray();
            var apduList = new[] { apdu };
            
            byte[][] response;

            try
            {
                // Exchange
                response = await _transport.Exchange(apduList, token);
            }
            catch (LedgerWalletException ex)
            {
                throw new LedgerDeviceException("Unable to receive result", ex);
            }

            // Parse result
            if (response != null && 
                response.Length == 1)
            {
                var commandResponse = response[0];

                if (commandResponse != null)
                {
                    return commandResponse;
                }
            }

            throw new LedgerDeviceException("Transport returned empty result");
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}