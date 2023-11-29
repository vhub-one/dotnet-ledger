
namespace Ledger.Transport.WinBle
{
    public abstract class LedgerBleException : Exception
    {
        protected LedgerBleException(string message, Exception ex)
            : base(message, ex)
        {

        }
    }

    public class LedgerBleTransportException : LedgerBleException
    {
        public LedgerBleTransportException(Exception ex = null)
            : base("Device is not available", ex)
        {
        }
    }

    public class LedgerBleProtocolException : LedgerBleException
    {
        public LedgerBleProtocolException()
            : base("Invalid data received", null)
        {
        }
    }
}