using LedgerWallet.Transports;
using System.Runtime.CompilerServices;

namespace Ledger.Transport.Usb
{
    public class LedgerUsbDeviceEnumerator : ILedgerDeviceEnumerator
    {
        public async IAsyncEnumerable<ILedgerDevice> GetDevicesAsync([EnumeratorCancellation] CancellationToken token)
        {
            var transportList = await HIDLedgerTransport.GetHIDTransportsAsync(cancellation: token);

            foreach (var transport in transportList)
            {
                yield return new LedgerUsbDevice(transport);
            }
        }
    }
}