using System.Runtime.CompilerServices;

namespace Ledger
{
    public class LedgerAggregatedDeviceEnumerator : ILedgerDeviceEnumerator
    {
        private readonly IEnumerable<ILedgerDeviceEnumerator> _deviceEnumerators;

        public LedgerAggregatedDeviceEnumerator(params ILedgerDeviceEnumerator[] deviceEnumerators)
        {
            _deviceEnumerators = deviceEnumerators;
        }

        public async IAsyncEnumerable<ILedgerDevice> GetDevicesAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            foreach (var deviceEnumerator in _deviceEnumerators)
            {
                var devicesStream = deviceEnumerator.GetDevicesAsync(token);

                await foreach (var device in devicesStream)
                {
                    yield return device;
                }
            }
        }
    }
}