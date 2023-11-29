
namespace Ledger.Transport.WinBle
{
    internal static class LedgerBleDeviceChannelPool
    {
        private static readonly Dictionary<string, RegistryEntry> _devices = new();
        private static readonly object _devicesSync = new();

        public static LedgerBleDeviceChannel GetOrCreateChannel(string deviceId, LedgerBleDeviceSpec deviceSpec = null)
        {
            RegistryEntry entry;

            lock (_devicesSync)
            {
                var entryExists = _devices.TryGetValue(deviceId, out entry);

                if (entryExists)
                {
                    entry.Count += 1;
                }
                else
                {
                    if (deviceSpec == null)
                    {
                        return null;
                    }

                    entry = new RegistryEntry
                    {
                        Channel = new LedgerBleDeviceChannel(deviceId, deviceSpec),
                        Count = 1
                    };

                    // Store it
                    _devices[deviceId] = entry;
                }
            }

            return entry.Channel;
        }

        public static bool TryReleaseChannel(string deviceId)
        {
            lock (_devicesSync)
            {
                var entryExists = _devices.TryGetValue(deviceId, out var entry);

                if (entryExists)
                {
                    if (entry.Count > 0)
                    {
                        entry.Count -= 1;
                    }

                    return entry.Count == 0;
                }
            }

            return true;
        }

        public static bool TryRemoveChannel(string deviceId)
        {
            lock (_devicesSync)
            {
                var entryExists = _devices.TryGetValue(deviceId, out var entry);

                if (entryExists)
                {
                    if (entry.Count == 0)
                    {
                        _devices.Remove(deviceId);

                        return true;
                    }
                }
            }

            return false;
        }

        private class RegistryEntry
        {
            public LedgerBleDeviceChannel Channel { get; set; }
            public int Count { get; set; }
        }
    }
}