using Common.Buffers;
using Common.Buffers.Extensions;
using System.Buffers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Channels;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Ledger.Transport.WinBle
{
    public class LedgerBleTransmissionController : IAsyncDisposable
    {
        private const byte HEADER_SIZE = 0x03;
        private const byte MTU_SIZE = 0x0A;

        private const byte VERSION = 0x00;

        private const byte TAG_VERSION = 0x00;
        private const byte TAG_INIT = 0x01;
        private const byte TAG_APDU = 0x05;
        private const byte TAG_MTU = 0x08;

        private int _mtu = MTU_SIZE;

        private bool _transportInit = false;
        private bool _protocolInit = true;
        private bool _mtuInit = false;
        private bool _versionChecked = false;

        private Channel<ReadOnlyMemory<byte>> _channel;

        private GattCharacteristic _characteristicNotify;
        private GattCharacteristic _characteristicWrite;

        public LedgerBleTransmissionController(GattCharacteristic characteristicNotify, GattCharacteristic characteristicWrite)
        {
            _characteristicNotify = characteristicNotify;
            _characteristicWrite = characteristicWrite;
        }

        public async ValueTask<ReadOnlyMemory<byte>> ExchangeApduAsync(ReadOnlyMemory<byte> data, CancellationToken token)
        {
            try
            {
                await EnsureTransportInitializedAsync(token);
                await EnsureProtocolInitializedAsync(token);
                await EnsureMtuInitializedAsync(token);
                await EnsureVersionAsync(token);

                // Try to execute command
                return await TransmitAsync(TAG_APDU, data, token);
            }
            catch (LedgerBleProtocolException)
            {
                // We will need to reinitialize connection
                _protocolInit = false;

                throw;
            }
            catch
            {
                _transportInit = false;
                _protocolInit = false;
                _mtuInit = false;
                _versionChecked = false;

                // Close transport in case of communication error
                await EnsureTransportClosedAsync();

                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await EnsureTransportClosedAsync();
        }

        public async ValueTask EnsureTransportClosedAsync()
        {
            if (_characteristicNotify != null)
            {
                _characteristicNotify.ValueChanged -= HandleNotification;

                // Handle notifications
                await _characteristicNotify.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.None
                );
            }
        }

        private async ValueTask EnsureTransportInitializedAsync(CancellationToken token)
        {
            if (_transportInit == false)
            {
                // Handle incoming messages
                _characteristicNotify.ValueChanged += HandleNotification;

                // Handle notifications
                await _characteristicNotify.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify
                );

                // Mark as initialized
                _transportInit = true;
            }
        }

        private void HandleNotification(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var channel = _channel;

            if (channel != null)
            {
                var chunkSizeOriginal =(int) args.CharacteristicValue.Length;
                var chunkSize = Math.Max(chunkSizeOriginal, 5);

                var chunk = new byte[chunkSize];

                args.CharacteristicValue.CopyTo(0, chunk, 0, chunkSizeOriginal);

                var chunkWritten = channel.Writer.TryWrite(chunk);

                if (chunkWritten == false)
                {
                    // We didn't manage to write chunk => complete channel
                    channel.Writer.TryComplete();
                }
            }
        }

        private async ValueTask EnsureMtuInitializedAsync(CancellationToken token)
        {
            if (_mtuInit == false)
            {
                var mtuData = await TransmitAsync(TAG_MTU, Memory<byte>.Empty, token);
                var mtuDataReader = MemoryBufferReader.Create(mtuData);

                // Mark mtu as initialized
                _mtu = mtuDataReader.Read();
                _mtuInit = true;
            }
        }

        private async ValueTask EnsureProtocolInitializedAsync(CancellationToken token)
        {
            if (_protocolInit == false)
            {
                await TransmitAsync(TAG_INIT, Memory<byte>.Empty, token);

                // Mark connection as initialized
                _protocolInit = true;
            }
        }

        private async ValueTask EnsureVersionAsync(CancellationToken token)
        {
            if (_versionChecked == false)
            {
                var versionData = await TransmitAsync(TAG_VERSION, Memory<byte>.Empty, token);

                if (versionData.Length != 0)
                {
                    var versionDataReader = MemoryBufferReader.Create(versionData);
                    var version = versionDataReader.ReadUInt32BigEndian();

                    if (version != VERSION)
                    {
                        throw new LedgerBleProtocolException();
                    }
                }

                _versionChecked = true;
            }
        }

        private async ValueTask<ReadOnlyMemory<byte>> TransmitAsync(byte tag, ReadOnlyMemory<byte> data, CancellationToken token)
        {
            // Start packets capturing
            _channel = Channel.CreateBounded<ReadOnlyMemory<byte>>(10);

            try
            {
                // Send packet
                await WriteAsync(tag, data, token);

                // Try to read response
                return await ReadAsync(tag, token);
            }
            finally
            {
                // Stop packets capturing
                _channel = null;
            }
        }

        private async ValueTask WriteAsync(byte tag, ReadOnlyMemory<byte> data, CancellationToken token = default)
        {
            var requestWriter = new ArrayBufferWriter<byte>();

            requestWriter.WriteUInt16BigEndian((ushort)data.Length);
            requestWriter.Write(data.Span);

            var chunks = requestWriter.WrittenMemory.Chunk(_mtu - HEADER_SIZE);
            var chunkWriter = new ArrayBufferWriter<byte>();

            foreach (var chunk in chunks)
            {
                chunkWriter.Clear();

                // Write header
                chunkWriter.Write(tag);
                chunkWriter.WriteUInt16BigEndian((ushort)chunk.Index);

                // Write payload
                chunkWriter.Write(chunk.Data.Span);

                // Send chunk
                try
                {
                    await _characteristicWrite.WriteValueAsync(
                        chunkWriter.WrittenSpan.ToArray().AsBuffer()
                    );
                }
                catch (Exception ex)
                {
                    throw new LedgerBleTransportException(ex);
                }
            }
        }

        private async ValueTask<ReadOnlyMemory<byte>> ReadAsync(byte tag, CancellationToken token)
        {
            var packetWriter = new ArrayBufferWriter<byte>();
            var packetWriterSize = 0;

            var index = 0;

            while (true)
            {
                var chunk = await _channel.Reader.ReadAsync(token);
                var chunkReader = MemoryBufferReader.Create(chunk);

                var chunkTag = chunkReader.Read();

                if (chunkTag != tag)
                {
                    throw new LedgerBleProtocolException();
                }

                var chunkIndex = chunkReader.ReadUInt16BigEndian();

                if (chunkIndex != index)
                {
                    throw new LedgerBleProtocolException();
                }

                if (chunkIndex == 0)
                {
                    packetWriterSize = chunkReader.ReadUInt16BigEndian();
                }

                var data = chunkReader.ReadAll();

                if (data.Length > 0)
                {
                    packetWriter.Write(data.Span);
                }

                if (packetWriterSize == packetWriter.WrittenCount)
                {
                    break;
                }
            }

            // Return result
            return packetWriter.WrittenMemory;
        }
    }
}