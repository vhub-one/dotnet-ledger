
namespace Ledger.Transport.WinBle
{
    public class LedgerBleDeviceSpec
    {
        public Guid Service { get; set; }
        public Guid CharacteristicWrite { get; set; }
        public Guid CharacteristicNotify { get; set; }
    }
}