using Microsoft.Extensions.Options;

namespace Ledger.Transport.Speculos
{
    public class LedgerSpeculosDeviceEnumerator : ILedgerDeviceEnumerator
    {
        private readonly IOptions<LedgerSpeculosOptions> _optionsAccessor;

        public LedgerSpeculosDeviceEnumerator(IOptions<LedgerSpeculosOptions> optionsAccessor)
        {
            _optionsAccessor = optionsAccessor;
        }

        public IAsyncEnumerable<ILedgerDevice> GetDevicesAsync(CancellationToken token = default)
        {
            var devices = new List<ILedgerDevice>();

            var options = _optionsAccessor.Value;

            if (options != null && options.Enable)
            {
                var device = new LedgerSpeculosDevice(
                    options.Host, 
                    options.Port, 
                    options.DeviceId
                );

                devices.Add(device);
            }

            return devices.ToAsyncEnumerable();
        }
    }
}