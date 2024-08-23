// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Asset
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Xml;
    using TestData;

    public sealed class SimulatedAsset : IAsset
    {
        public ServiceResult Read(AssetTag tag, ref object? value)
        {
            return GetTag(tag).Read(ref value);
        }

        public ServiceResult Write(AssetTag tag, ref object value)
        {
            return GetTag(tag).Write(value);
        }

        public void Observe(AssetTag tag, uint id, OnAssetTagChange callback)
        {
            GetTag(tag).Observe(id, callback);
        }

        public void Unobserve(AssetTag tag, uint id)
        {
            ArgumentNullException.ThrowIfNull(tag.Address);
            if (_tags.TryGetValue(tag.Address, out var assetTag))
            {
                assetTag.Unobserve(id);
            }
        }

        public void Dispose()
        {
            foreach (var tag in _tags.Values)
            {
                tag.Dispose();
            }
            _tags.Clear();
        }

        private SimulatedTag GetTag(AssetTag tag)
        {
            if (tag is not AssetTag<SimulatedForm> simTag)
            {
                throw ServiceResultException.Create(StatusCodes.BadInvalidArgument,
                    "Not a simulated tag");
            }
            ArgumentNullException.ThrowIfNull(tag.Address);
            return _tags.GetOrAdd(tag.Address, _ => new SimulatedTag(this, simTag));
        }

        /// <summary>
        /// Simulated tag
        /// </summary>
        internal sealed record class SimulatedTag : IDisposable
        {
            public SimulatedAsset Asset { get; }

            public AssetTag<SimulatedForm> Tag { get; }

            /// <summary>
            /// Create tag
            /// </summary>
            /// <param name="asset"></param>
            /// <param name="tag"></param>
            public SimulatedTag(SimulatedAsset asset, AssetTag<SimulatedForm> tag)
            {
                Asset = asset;
                Tag = tag;
                _timer = new Timer(PollValue);
            }

            public void Dispose()
            {
                _timer.Dispose();
            }

            public void Observe(uint id, OnAssetTagChange callback)
            {
                if (Interlocked.Increment(ref _monitoringCount) == 1)
                {
                    _callback = callback;
                    _timer.Change(Tag.Form.PollingTime, Tag.Form.PollingTime);
                }
            }

            public void Unobserve(uint id)
            {
                if (Interlocked.Decrement(ref _monitoringCount) == 0)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }

            public ServiceResult Write(object value)
            {
                throw new NotImplementedException();
            }

            public ServiceResult Read(ref object? value)
            {
                var dataType = Tag.Form.GetDataTypeId();
                if (NodeId.IsNull(dataType))
                {
                    return ServiceResult.Create(StatusCodes.BadDataTypeIdUnknown, "Bad payload");
                }
                if (Tag.Form.IsArray)
                {
                    value = dataType.Identifier switch
                    {
                        Opc.Ua.DataTypes.Boolean => _generator.GetRandomArray<bool>(),
                        Opc.Ua.DataTypes.SByte => _generator.GetRandomArray<sbyte>(),
                        Opc.Ua.DataTypes.Byte => _generator.GetRandomArray<byte>(),
                        Opc.Ua.DataTypes.Int16 => _generator.GetRandomArray<short>(),
                        Opc.Ua.DataTypes.UInt16 => _generator.GetRandomArray<ushort>(),
                        Opc.Ua.DataTypes.Int32 => _generator.GetRandomArray<int>(),
                        Opc.Ua.DataTypes.UInt32 => _generator.GetRandomArray<uint>(),
                        Opc.Ua.DataTypes.Int64 => _generator.GetRandomArray<long>(),
                        Opc.Ua.DataTypes.UInt64 => _generator.GetRandomArray<ulong>(),
                        Opc.Ua.DataTypes.Float => _generator.GetRandomArray<float>(),
                        Opc.Ua.DataTypes.Double => _generator.GetRandomArray<double>(),
                        Opc.Ua.DataTypes.String => _generator.GetRandomArray<string>(),
                        Opc.Ua.DataTypes.DateTime => _generator.GetRandomArray<DateTime>(),
                        Opc.Ua.DataTypes.Guid => _generator.GetRandomArray<Guid>(),
                        Opc.Ua.DataTypes.ByteString => _generator.GetRandomArray<byte[]>(),
                        Opc.Ua.DataTypes.XmlElement => _generator.GetRandomArray<XmlElement>(),
                        Opc.Ua.DataTypes.NodeId => _generator.GetRandomArray<NodeId>(),
                        Opc.Ua.DataTypes.ExpandedNodeId => _generator.GetRandomArray<ExpandedNodeId>(),
                        Opc.Ua.DataTypes.QualifiedName => _generator.GetRandomArray<QualifiedName>(),
                        Opc.Ua.DataTypes.LocalizedText => _generator.GetRandomArray<LocalizedText>(),
                        Opc.Ua.DataTypes.StatusCode => _generator.GetRandomArray<StatusCode>(),
                        Opc.Ua.DataTypes.BaseDataType => _generator.GetRandomArray<object>(),
                        Opc.Ua.DataTypes.Enumeration => _generator.GetRandomArray<int>(),
                        Opc.Ua.DataTypes.Number => _generator.GetRandomArray(BuiltInType.Number, 100, false),
                        Opc.Ua.DataTypes.Integer => _generator.GetRandomArray(BuiltInType.Integer, 100, false),
                        Opc.Ua.DataTypes.UInteger => _generator.GetRandomArray(BuiltInType.UInteger, 100, false),
                        Opc.Ua.DataTypes.Structure => GetRandomArray(),
                        _ => null
                    };
                    return ServiceResult.Good;
                }

                value = dataType.Identifier switch
                {
                    Opc.Ua.DataTypes.Boolean => _generator.GetRandom<bool>(),
                    Opc.Ua.DataTypes.SByte => _generator.GetRandom<sbyte>(),
                    Opc.Ua.DataTypes.Byte => _generator.GetRandom<byte>(),
                    Opc.Ua.DataTypes.Int16 => _generator.GetRandom<short>(),
                    Opc.Ua.DataTypes.UInt16 => _generator.GetRandom<ushort>(),
                    Opc.Ua.DataTypes.Int32 => _generator.GetRandom<int>(),
                    Opc.Ua.DataTypes.UInt32 => _generator.GetRandom<uint>(),
                    Opc.Ua.DataTypes.Int64 => _generator.GetRandom<long>(),
                    Opc.Ua.DataTypes.UInt64 => _generator.GetRandom<ulong>(),
                    Opc.Ua.DataTypes.Float => _generator.GetRandom<float>(),
                    Opc.Ua.DataTypes.Double => _generator.GetRandom<double>(),
                    Opc.Ua.DataTypes.String => _generator.GetRandom<string>(),
                    Opc.Ua.DataTypes.DateTime => _generator.GetRandom<DateTime>(),
                    Opc.Ua.DataTypes.Guid => _generator.GetRandom<Guid>(),
                    Opc.Ua.DataTypes.ByteString => _generator.GetRandom<byte[]>(),
                    Opc.Ua.DataTypes.XmlElement => _generator.GetRandom<XmlElement>(),
                    Opc.Ua.DataTypes.NodeId => _generator.GetRandom<NodeId>(),
                    Opc.Ua.DataTypes.ExpandedNodeId => _generator.GetRandom<ExpandedNodeId>(),
                    Opc.Ua.DataTypes.QualifiedName => _generator.GetRandom<QualifiedName>(),
                    Opc.Ua.DataTypes.LocalizedText => _generator.GetRandom<LocalizedText>(),
                    Opc.Ua.DataTypes.StatusCode => _generator.GetRandom<StatusCode>(),
                    Opc.Ua.DataTypes.BaseDataType => _generator.GetRandomVariant().Value,
                    Opc.Ua.DataTypes.Structure => GetRandomStructure(),
                    Opc.Ua.DataTypes.Enumeration => _generator.GetRandom<int>(),
                    Opc.Ua.DataTypes.Number => _generator.GetRandom(BuiltInType.Number),
                    Opc.Ua.DataTypes.Integer => _generator.GetRandom(BuiltInType.Integer),
                    Opc.Ua.DataTypes.UInteger => _generator.GetRandom(BuiltInType.UInteger),
                    _ => null
                };

                return ServiceResult.Good;

                ExtensionObject[]? GetRandomArray()
                {
                    var values = _generator.GetRandomArray<ExtensionObject>(10);
                    for (var i = 0; values != null && i < values.Length; i++)
                    {
                        values[i] = GetRandomStructure();
                    }
                    return values;
                }

                ExtensionObject GetRandomStructure()
                {
                    if (_generator.GetRandomBoolean())
                    {
                        var scalar = new ScalarValueDataType
                        {
                            BooleanValue = _generator.GetRandom<bool>(),
                            SByteValue = _generator.GetRandom<sbyte>(),
                            ByteValue = _generator.GetRandom<byte>(),
                            Int16Value = _generator.GetRandom<short>(),
                            UInt16Value = _generator.GetRandom<ushort>(),
                            Int32Value = _generator.GetRandom<int>(),
                            UInt32Value = _generator.GetRandom<uint>(),
                            Int64Value = _generator.GetRandom<long>(),
                            UInt64Value = _generator.GetRandom<ulong>(),
                            FloatValue = _generator.GetRandom<float>(),
                            DoubleValue = _generator.GetRandom<double>(),
                            StringValue = _generator.GetRandom<string>(),
                            DateTimeValue = _generator.GetRandom<DateTime>(),
                            GuidValue = _generator.GetRandom<Uuid>(),
                            ByteStringValue = _generator.GetRandom<byte[]>(),
                            XmlElementValue = _generator.GetRandom<XmlElement>(),
                            NodeIdValue = _generator.GetRandom<NodeId>(),
                            ExpandedNodeIdValue = _generator.GetRandom<ExpandedNodeId>(),
                            QualifiedNameValue = _generator.GetRandom<QualifiedName>(),
                            LocalizedTextValue = _generator.GetRandom<LocalizedText>(),
                            StatusCodeValue = _generator.GetRandom<StatusCode>(),
                            VariantValue = _generator.GetRandomVariant()
                        };
                        return new ExtensionObject(scalar);
                    }
                    var array = new ArrayValueDataType
                    {
                        BooleanValue = _generator.GetRandomArray<bool>(10),
                        SByteValue = _generator.GetRandomArray<sbyte>(10),
                        ByteValue = _generator.GetRandomArray<byte>(10),
                        Int16Value = _generator.GetRandomArray<short>(10),
                        UInt16Value = _generator.GetRandomArray<ushort>(10),
                        Int32Value = _generator.GetRandomArray<int>(10),
                        UInt32Value = _generator.GetRandomArray<uint>(10),
                        Int64Value = _generator.GetRandomArray<long>(10),
                        UInt64Value = _generator.GetRandomArray<ulong>(10),
                        FloatValue = _generator.GetRandomArray<float>(10),
                        DoubleValue = _generator.GetRandomArray<double>(10),
                        StringValue = _generator.GetRandomArray<string>(10),
                        DateTimeValue = _generator.GetRandomArray<DateTime>(10),
                        GuidValue = _generator.GetRandomArray<Uuid>(10),
                        ByteStringValue = _generator.GetRandomArray<byte[]>(10),
                        XmlElementValue = _generator.GetRandomArray<XmlElement>(10),
                        NodeIdValue = _generator.GetRandomArray<NodeId>(10),
                        ExpandedNodeIdValue = _generator.GetRandomArray<ExpandedNodeId>(10),
                        QualifiedNameValue = _generator.GetRandomArray<QualifiedName>(10),
                        LocalizedTextValue = _generator.GetRandomArray<LocalizedText>(10),
                        StatusCodeValue = _generator.GetRandomArray<StatusCode>(10)
                    };

                    var values = _generator.GetRandomArray<object>(10);
                    for (var i = 0; values != null && i < values.Length; i++)
                    {
                        array.VariantValue.Add(new Variant(values[i]));
                    }

                    return new ExtensionObject(array.TypeId, array);
                }
            }

            private void PollValue(object? state)
            {
                object? value = null;
                var result = Read(ref value);
                _callback?.Invoke(Tag, value, result.StatusCode, DateTime.UtcNow);
            }

            private readonly Opc.Ua.Test.TestDataGenerator _generator = new();
            private readonly Timer _timer;
            private int _monitoringCount;
            private OnAssetTagChange? _callback;
        }

        private readonly ConcurrentDictionary<Uri, SimulatedTag> _tags = new();
    }
}
