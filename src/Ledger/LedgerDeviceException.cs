
namespace Ledger
{
    public class LedgerDeviceException : Exception
    {
        public LedgerDeviceException(string message, Exception ex = null)
            : base(message, ex)
        {
        }
    }

    public class LedgerDeviceNotAvailableException : LedgerDeviceException
    {
        public LedgerDeviceNotAvailableException(Exception ex = null)
            : base("Device is not available", ex)
        {
        }
    }
}