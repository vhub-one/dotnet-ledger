using System.Runtime.CompilerServices;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;

namespace Ledger.Transport.WinBle
{
    public class LedgerBleDeviceEnumerator : ILedgerDeviceEnumerator
    {
        private const string SERVICE_ID_PROP = "System.DeviceInterface.Bluetooth.ServiceGuid";

        private static readonly LedgerBleDeviceSpec NanoX = new()
        {
            Service = Guid.Parse("13D63400-2C97-0004-0000-4C6564676572"),
            CharacteristicWrite = Guid.Parse("13D63400-2C97-0004-0002-4C6564676572"),
            CharacteristicNotify = Guid.Parse("13D63400-2C97-0004-0001-4C6564676572")
        };

        private static readonly LedgerBleDeviceSpec Stax = new()
        {
            Service = Guid.Parse("13D63400-2C97-6004-0000-4C6564676572"),
            CharacteristicWrite = Guid.Parse("13D63400-2C97-6004-0002-4C6564676572"),
            CharacteristicNotify = Guid.Parse("13D63400-2C97-6004-0001-4C6564676572")
        };

        public async IAsyncEnumerable<ILedgerDevice> GetDevicesAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            var adapter = await BluetoothAdapter.GetDefaultAsync();

            if (adapter != null &&
                adapter.IsLowEnergySupported)
            {
                var adapterRadio = await adapter.GetRadioAsync();

                if (adapterRadio.State == RadioState.On)
                {
                    var devicesSelector = LedgerBleDeviceFilter.ReadAqsFilter();
                    var devices = await DeviceInformation.FindAllAsync(devicesSelector, new[] { SERVICE_ID_PROP });

                    foreach (var deviceInformation in devices)
                    {
                        var serviceIdExists = deviceInformation.Properties.TryGetValue(SERVICE_ID_PROP, out var serviceId);

                        if (serviceIdExists &&
                            serviceId is Guid service)
                        {
                            if (service == NanoX.Service)
                            {
                                yield return new LedgerBleDevice(deviceInformation.Id, NanoX);
                            }
                            if (service == Stax.Service)
                            {
                                yield return new LedgerBleDevice(deviceInformation.Id, Stax);
                            }
                        }
                    }
                }
            }
        }
    }
}
