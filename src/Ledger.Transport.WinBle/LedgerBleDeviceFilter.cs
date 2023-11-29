using Common.Resources;
using System.Text;

namespace Ledger.Transport.WinBle
{
    public static class LedgerBleDeviceFilter
    {
        private const string FILTER_RESOURCE = "Ledger.Transport.WinBle.Query.LedgerDevices.aqs";

        public static string ReadAqsFilter()
        {
            var devicesFilter = ResourceUtils.ReadForCallingAssembly(FILTER_RESOURCE);
            var devicseFilterParts = devicesFilter.Split('\r', '\n', ' ');

            var devicesFilterBuilder = new StringBuilder();

            foreach (var devicseFilterPart in devicseFilterParts)
            {
                if (devicseFilterPart.Length == 0)
                {
                    continue;
                }
                if (devicesFilterBuilder.Length > 0)
                {
                    var isCloseToBracket = devicesFilterBuilder[^1] == '(' || devicseFilterPart[0] == ')';

                    if (isCloseToBracket == false)
                    {
                        devicesFilterBuilder.Append(' ');
                    }
                }

                devicesFilterBuilder.Append(devicseFilterPart);
            }

            return devicesFilterBuilder.ToString();
        }
    }
}
