using System.Buffers.Binary;
using System.Net.Sockets;

namespace Ledger.Transport.Speculos
{
    public class LedgerSpeculosDeviceChannel : ILedgerDeviceChannel
    {
        private readonly TcpClient _client;
        private readonly Stream _clientStream;

        public LedgerSpeculosDeviceChannel(TcpClient client)
        {
            _client = client;
            _clientStream = client.GetStream();
        }

        public async ValueTask<ReadOnlyMemory<byte>> ExchangeAsync(ReadOnlyMemory<byte> command, CancellationToken token)
        {
            await SendAsync(command, token);
            return await ReceiveAsync(token);
        }

        private async ValueTask SendAsync(ReadOnlyMemory<byte> command, CancellationToken token)
        {
            var commandLengthBuffer = new byte[4];

            BinaryPrimitives.WriteUInt32BigEndian(commandLengthBuffer, (uint)command.Length);

            // Write message length
            await _clientStream.WriteAsync(commandLengthBuffer, token);
            // Write message
            await _clientStream.WriteAsync(command, token);
        }

        private async ValueTask<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken token)
        {
            var commandLengthBuffer = new byte[4];

            // Read message length
            await _clientStream.ReadExactlyAsync(commandLengthBuffer, token);

            // For some reason command result length is 2 byte less (doesn't inclue SW word)
            var commandResultBufferLength = BinaryPrimitives.ReadUInt32BigEndian(commandLengthBuffer);
            var commandResultBuffer = new byte[commandResultBufferLength + 2];

            // Read message bytes
            await _clientStream.ReadExactlyAsync(commandResultBuffer, token);

            // Return read mesage as is
            return commandResultBuffer;
        }

        public ValueTask DisposeAsync()
        {
            if (_clientStream != null)
            {
                _clientStream.Dispose();
            }

            if (_client != null)
            {
                _client.Dispose();
            }

            return ValueTask.CompletedTask;
        }
    }
}