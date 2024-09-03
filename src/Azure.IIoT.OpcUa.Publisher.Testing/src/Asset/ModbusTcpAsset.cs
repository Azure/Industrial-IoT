// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Asset
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO.Pipelines;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// See https://w3c.github.io/wot-binding-templates/bindings/protocols/modbus/
    /// </summary>
    sealed class ModbusTcpAsset : IAsset, IAssetFactory
    {
        private ModbusTcpAsset(Uri address, ILogger logger)
        {
            _logger = logger;

            // check if we can reach the Modbus asset
            _unitId = byte.Parse(address.PathAndQuery, CultureInfo.InvariantCulture);

            _address = address;
            Reconnect();
        }

        public static bool TryConnect(Uri tdBase, ILogger logger,
            [NotNullWhen(true)] out IAsset? asset)
        {
            // Create modbus asset
            if (tdBase.Scheme != "modbus+tcp")
            {
                asset = default;
                return false;
            }

            asset = new ModbusTcpAsset(tdBase, logger);
            return true;
        }

        public void Dispose()
        {
            try
            {
                _tcpClient?.Dispose();
                _tcpClient = null;
            }
            finally
            {
                _lock.Dispose();
            }
        }

        public ServiceResult Read(AssetTag tag, ref object? value)
        {
            if (tag is not AssetTag<ModbusForm> modbusTag)
            {
                return ServiceResult.Create(StatusCodes.BadInvalidArgument, "Not a modbus tag");
            }
            // {address}?quantity={?quantity}
            if (!ushort.TryParse(modbusTag.Address.LocalPath, CultureInfo.InvariantCulture,
                out var registerAddress))
            {
                return ServiceResult.Create(StatusCodes.BadInvalidArgument, "Not a register address");
            }
            var form = modbusTag.Form;
            var function = form.Entity switch
            {
                ModbusEntity.Coil => ModbusFunction.ReadCoil,
                ModbusEntity.DiscreteInput => ModbusFunction.ReadDiscreteInput,
                ModbusEntity.HoldingRegister => ModbusFunction.ReadHoldingRegisters,
                ModbusEntity.InputRegister => ModbusFunction.ReadInputRegisters,
                _ => form.Function ?? ModbusFunction.ReadHoldingRegisters
            };
            // Read the amount of registers/coils referenced in this URL
            var queryParts = modbusTag.Address.Query.Split(new char[] { '?', '&', '=' },
                StringSplitOptions.RemoveEmptyEntries);
            ushort quantity = kDefaultQuantity;
            if (queryParts.Length > 0)
            {
                if (queryParts.Length != 2 || queryParts[0] != "quantity" ||
                    !ushort.TryParse(queryParts[1], CultureInfo.InvariantCulture, out quantity))
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                        "Invalid quantity in query.");
                }
            }
            var timeout = form.Timeout ?? kDefaultTimeout;
            try
            {
                var tagBytes = ReadAsync(function, registerAddress, quantity, timeout)
                    .GetAwaiter().GetResult();
                return form.ToObject(tagBytes, ref value);
            }
            catch (ServiceResultException sre)
            {
                return sre.Result;
            }
        }

        public ServiceResult Write(AssetTag tag, ref object value)
        {
            if (tag is not AssetTag<ModbusForm> modbusTag)
            {
                return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                    "Not a modbus tag");
            }
            // {address}?quantity={?quantity}
            if (!ushort.TryParse(modbusTag.Address.LocalPath, CultureInfo.InvariantCulture,
                out var registerAddress))
            {
                return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                    "Not a register address");
            }

            var form = modbusTag.Form;
            // Read the amount of registers/coils referenced in this URL
            var queryParts = modbusTag.Address.Query.Split(new char[] { '?', '&', '=' },
                StringSplitOptions.RemoveEmptyEntries);
            ushort quantity = kDefaultQuantity;
            if (queryParts.Length > 0)
            {
                if (queryParts.Length != 2 || queryParts[0] != "quantity" ||
                    !ushort.TryParse(queryParts[1], CultureInfo.InvariantCulture,
                    out quantity))
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                        "Invalid quantity in query.");
                }
            }
            var function = form.Entity switch
            {
                ModbusEntity.DiscreteInput or ModbusEntity.InputRegister
                    => (ModbusFunction?)null, // Invalid
                ModbusEntity.Coil when quantity == 1
                    => ModbusFunction.WriteSingleCoil,
                ModbusEntity.Coil
                    => ModbusFunction.WriteMultipleCoils,
                ModbusEntity.HoldingRegister when quantity == 1
                    => ModbusFunction.WriteSingleHoldingRegister,
                ModbusEntity.HoldingRegister
                    => ModbusFunction.WriteMultipleHoldingRegisters,
                _ => form.Function ?? ModbusFunction.WriteSingleCoil
            };
            if (!function.HasValue)
            {
                return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                    "Function or entity not supported");
            }
            var timeout = form.Timeout ?? kDefaultTimeout;
            try
            {
                WriteAsync(function.Value, registerAddress, quantity, timeout,
                    form.ToBuffer(value)).GetAwaiter().GetResult();
                return ServiceResult.Good;
            }
            catch (ServiceResultException sre)
            {
                return sre.Result;
            }
        }

        public void Observe(AssetTag tag, uint id, OnAssetTagChange callback)
        {
            if (tag is not AssetTag<ModbusForm> modbusTag)
            {
                throw ServiceResultException.Create(StatusCodes.BadInvalidArgument,
                    "Not a modbus tag");
            }

            // TODO: Implement polling
        }

        public void Unobserve(AssetTag tag, uint id)
        {
            if (tag is not AssetTag<ModbusForm> modbusTag)
            {
                throw ServiceResultException.Create(StatusCodes.BadInvalidArgument,
                    "Not a modbus tag");
            }

            // TODO: Implement polling
        }

        private async Task WriteAsync(ModbusFunction function, ushort address, ushort quantity,
            int timeout, ReadOnlyMemory<byte> values, CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_tcpClient == null, this);
                _tcpClient.SendTimeout = timeout;
                _tcpClient.ReceiveTimeout = timeout;

                Debug.Assert(_writer != null);
                Debug.Assert(_reader != null);

                var mbap = new ModbusProtocol.Mbap(_transactionID++, _unitId);
                _writer.WriteWriteRequest(function, mbap, address, quantity, values.Span);
                // send request to Modbus server
                await _writer.FlushAsync(ct).ConfigureAwait(false);

                // TODO: go full duplex using transaction id
                var error = ServiceResult.Good;
                while (true)
                {
                    var result = await _reader.ReadAsync(ct).ConfigureAwait(false);
                    if (result.IsCanceled)
                    {
                        throw new TimeoutException();
                    }
                    var buffer = result.Buffer;

                    var responseBuffer = new ArrayBufferWriter<byte>();
                    if (!responseBuffer.TryReadWriteResponse(function,
                        ref buffer, address, quantity, values.Span, ref error))
                    {
                        // More must be read to completely parse the response
                        continue;
                    }

                    //
                    // All has been read that is to be read or we would have
                    // thrown here, advance to the end so that next read can
                    // start
                    //
                    _reader.AdvanceTo(buffer.End);

                    // Check error received and throw if not good
                    if (error != ServiceResult.Good)
                    {
                        throw new ServiceResultException(error);
                    }
                    return; // Done
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                Reconnect();
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<ReadOnlyMemory<byte>> ReadAsync(ModbusFunction function,
            ushort address, ushort quantity, int timeout, CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_tcpClient == null, this);
                _tcpClient.SendTimeout = timeout;
                _tcpClient.ReceiveTimeout = timeout;

                Debug.Assert(_writer != null);
                Debug.Assert(_reader != null);

                var mbap = new ModbusProtocol.Mbap(_transactionID++, _unitId);
                _writer.EncodeReadRequest(function, mbap, address, quantity);
                // send request to Modbus server
                await _writer.FlushAsync(ct).ConfigureAwait(false);

                // TODO: go full duplex using transaction id
                var error = ServiceResult.Good;
                while (true)
                {
                    var result = await _reader.ReadAsync(ct).ConfigureAwait(false);
                    if (result.IsCanceled)
                    {
                        throw new TimeoutException();
                    }
                    var buffer = result.Buffer;

                    var responseBuffer = new ArrayBufferWriter<byte>();
                    if (!responseBuffer.TryReadReadResponse(function,
                        ref buffer, ref error))
                    {
                        // More must be read to completely parse the response
                        continue;
                    }

                    //
                    // All has been read that is to be read or we would have
                    // thrown here, advance to the end so that next read can
                    // start
                    //
                    _reader.AdvanceTo(buffer.End);

                    // Check error received and throw if not good
                    if (error != ServiceResult.Good)
                    {
                        throw new ServiceResultException(error);
                    }
                    return responseBuffer.WrittenMemory;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                Reconnect();
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        private void Reconnect()
        {
            Debug.Assert(_lock.CurrentCount == 0,
                "Reconnect should not be called concurrently");
            _tcpClient?.Dispose();
            var port = _address.Port == 0 ? kIanaPort : _address.Port;
            _tcpClient = new TcpClient(_address.IdnHost, port);
            var stream = _tcpClient.GetStream();
            _writer = PipeWriter.Create(stream);
            _reader = PipeReader.Create(stream);
        }

        private const int kDefaultQuantity = 1;
        private const int kIanaPort = 502;
        /// <summary>
        /// private const bool kDefaultZeroBaseAddressing = false;
        /// private const bool kDefaultMostSignificantByte = true;
        /// private const bool kDefaultMostSignificantWord = true;
        /// </summary>
        private const int kDefaultTimeout = Timeout.Infinite;
        private ushort _transactionID;
        private readonly byte _unitId;
        private readonly ILogger _logger;
        private readonly Uri _address;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private TcpClient? _tcpClient;
        private PipeReader? _reader;
        private PipeWriter? _writer;
    }
}
