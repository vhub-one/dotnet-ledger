using Common.Objects;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Ledger.Transport.WinBle
{
    public class LedgerBleDeviceChannel : ILedgerDeviceChannel
    {
        private readonly string _deviceId;
        private readonly LedgerBleDeviceSpec _deviceSpec;

        private readonly SemaphoreSlim _deviceSemaphore = new(1, 1);

        private BluetoothLEDevice _device;
        private GattDeviceService _deviceService;

        private LedgerBleTransmissionController _transmissionManager;

        public LedgerBleDeviceChannel(string deviceId, LedgerBleDeviceSpec deviceSpec)
        {
            _deviceId = deviceId;
            _deviceSpec = deviceSpec;
        }

        internal async ValueTask OpenAsync(CancellationToken token)
        {
            await _deviceSemaphore.WaitAsync(token);

            try
            {
                var device = _device;

                if (device == null)
                {
                    device = await BluetoothLEDevice.FromIdAsync(_deviceId);

                    // Check if device exists
                    if (device == null)
                    {
                        throw new LedgerDeviceNotAvailableException();
                    }

                    _device = device;
                    _device.ConnectionStatusChanged += HandleDisconnectEventAsync;
                }

                var deviceService = _deviceService;

                if (deviceService == null)
                {
                    var deviceServiceResult = await device.GetGattServicesForUuidAsync(_deviceSpec.Service, BluetoothCacheMode.Uncached);

                    if (deviceServiceResult.Status == GattCommunicationStatus.Success)
                    {
                        deviceService = deviceServiceResult.Services[0];
                    }

                    if (deviceService == null)
                    {
                        // We can't establish connection
                        throw new LedgerDeviceNotAvailableException();
                    }

                    var serviceOpenStatus = await deviceService.OpenAsync(GattSharingMode.Exclusive);

                    if (serviceOpenStatus != GattOpenStatus.Success)
                    {
                        // We can't establish exclusive connection
                        throw new LedgerDeviceNotAvailableException();
                    }

                    _deviceService = deviceService;
                }

                // Try to force connection to open
                var characteristicsResult = await deviceService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);

                if (characteristicsResult.Status != GattCommunicationStatus.Success)
                {
                    // We can't establish exclusive connection
                    throw new LedgerDeviceNotAvailableException();
                }

                if (_transmissionManager == null)
                {
                    var characteristicN = default(GattCharacteristic);
                    var characteristicW = default(GattCharacteristic);

                    foreach (var characteristic in characteristicsResult.Characteristics)
                    {
                        if (characteristic.Uuid == _deviceSpec.CharacteristicNotify)
                        {
                            characteristicN = characteristic;
                        }
                        if (characteristic.Uuid == _deviceSpec.CharacteristicWrite)
                        {
                            characteristicW = characteristic;
                        }
                    }

                    if (characteristicN == null ||
                        characteristicW == null)
                    {
                        // We can't establish connection
                        throw new LedgerDeviceNotAvailableException();
                    }

                    _transmissionManager = new LedgerBleTransmissionController(characteristicN, characteristicW);
                }
            }
            finally
            {
                _deviceSemaphore.Release();
            }
        }

        public async ValueTask<ReadOnlyMemory<byte>> ExchangeAsync(ReadOnlyMemory<byte> command, CancellationToken token)
        {
            await _deviceSemaphore.WaitAsync(token);

            try
            {
                // Send apdu command
                return await _transmissionManager.ExchangeApduAsync(command, token);
            }
            finally
            {
                _deviceSemaphore.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            var channelReleased = LedgerBleDeviceChannelPool.TryReleaseChannel(_deviceId);

            if (channelReleased)
            {
                await TryDisposeChannelAsync();
            }
        }

        private async void HandleDisconnectEventAsync(BluetoothLEDevice device, object args)
        {
            await TryDisposeChannelAsync();
        }

        private async ValueTask TryDisposeChannelAsync()
        {
            await _deviceSemaphore.WaitAsync();

            try
            {
                if (_device != null &&
                    _device.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
                    return;
                }

                var channelRemoved = LedgerBleDeviceChannelPool.TryRemoveChannel(_deviceId);

                if (channelRemoved == false)
                {
                    return;
                }

                var transmissionManager = ObjectUtils.Swap(ref _transmissionManager, null);

                if (transmissionManager != null)
                {
                    await transmissionManager.DisposeAsync();
                }

                var deviceService = ObjectUtils.Swap(ref _deviceService, null);

                if (deviceService != null)
                {
                    deviceService.Dispose();
                }

                var device = ObjectUtils.Swap(ref _device, null);

                if (device != null)
                {
                    device.ConnectionStatusChanged -= HandleDisconnectEventAsync;
                    device.Dispose();
                }
            }
            finally
            {
                _deviceSemaphore.Release();
            }
        }
    }
}