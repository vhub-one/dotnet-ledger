using System.Net.Sockets;

namespace Ledger.Transport.Speculos
{
    public class LedgerSpeculosDevice : ILedgerDevice
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _id;

        public LedgerSpeculosDevice(string host, int port, string id)
        {
            _host = host;
            _port = port;
            _id = id;
        }

        public string Id
        {
            get { return _id; }
        }

        public async ValueTask<ILedgerDeviceChannel> OpenChannelAsync(CancellationToken token)
        {
            var client = new TcpClient();

            try
            {
                // Try to connect
                await client.ConnectAsync(
                    _host,
                    _port
                );
            }
            catch (Exception ex)
            {
                if (client != null)
                {
                    client.Dispose();
                }

                throw new LedgerDeviceNotAvailableException(ex);
            }

            return new LedgerSpeculosDeviceChannel(client);
        }
    }
}