// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Furly.Extensions.Serializers;
    using Opc.Ua;
    using Opc.Ua.Client.ComplexTypes;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal partial class DataSetResolver
    {
        internal abstract partial class Field
        {
            /// <summary>
            /// Extension field
            /// </summary>
            private class Extension : Field
            {
                /// <inheritdoc/>
                public override string? NodeId { get; set; }

                /// <inheritdoc/>
                public override IReadOnlyList<string>? BrowsePath { get; set; }

                /// <inheritdoc/>
                public override string? ResolvedName { get; set; }

                /// <inheritdoc/>
                public override PublishingQueueSettingsModel? Publishing { get; set; }

                /// <inheritdoc/>
                public override ServiceResultModel? ErrorInfo { get; set; }

                /// <inheritdoc/>
                public override PublishedNodeExpansion Flags { get; set; }

                /// <inheritdoc/>
                public override bool MetaDataNeedsRefresh
                {
                    get => base.MetaDataNeedsRefresh || _extension.MetaData == null;
                    set => base.MetaDataNeedsRefresh = value;
                }

                /// <inheritdoc/>
                public override string? Id
                    => _extension.Id;

                /// <inheritdoc/>
                public override bool NeedsUpdate => MetaDataNeedsRefresh;

                /// <inheritdoc/>
                public Extension(DataSetResolver resolver, DataSetWriterModel writer,
                    ExtensionFieldModel extension) : base(resolver, writer)
                {
                    _extension = extension;

                    if (_extension.Id == null)
                    {
                        var sb = new StringBuilder()
                            .Append(nameof(Extension))
                            .Append(_extension.DataSetFieldName)
                            .Append(_extension.DataSetClassFieldId)
                            // .Append(_extension.Triggering)
                            ;
                        _extension.Id = sb
                            .ToString()
                            .ToSha1Hash()
                            ;
                    }
                }

                /// <inheritdoc/>
                public override bool Merge(Field existing)
                {
                    if (!base.Merge(existing) || existing is not Extension other)
                    {
                        return false;
                    }

                    _extension.MetaData = other._extension.MetaData;
                    return true;
                }

                /// <inheritdoc/>
                public override async ValueTask ResolveMetaDataAsync(IOpcUaSession session,
                    ComplexTypeSystem? typeSystem, CancellationToken ct)
                {
                    if (_extension.MetaData != null)
                    {
                        return;
                    }
                    _extension.MetaData = await ResolveMetaDataAsync(session, typeSystem,
                        new VariableNode
                        {
                            DataType = GetBuiltInType(_extension.Value),
                            ValueRank = _extension.Value.IsArray ?
                                ValueRanks.OneDimension : ValueRanks.Scalar
                        }, 0u, ct).ConfigureAwait(false);

                    static NodeId GetBuiltInType(VariantValue value)
                    {
                        return new NodeId((uint)(value.GetTypeCode() switch
                        {
                            TypeCode.Boolean => BuiltInType.Boolean,
                            TypeCode.SByte => BuiltInType.SByte,
                            TypeCode.Byte => BuiltInType.Byte,
                            TypeCode.Int16 => BuiltInType.Int16,
                            TypeCode.UInt16 => BuiltInType.UInt16,
                            TypeCode.Int32 => BuiltInType.Int32,
                            TypeCode.UInt32 => BuiltInType.UInt32,
                            TypeCode.Int64 => BuiltInType.Int64,
                            TypeCode.UInt64 => BuiltInType.UInt64,
                            TypeCode.Double => BuiltInType.Double,
                            TypeCode.String => BuiltInType.String,
                            TypeCode.DateTime => BuiltInType.DateTime,
                            TypeCode.Empty => BuiltInType.Null,
                            TypeCode.Object => BuiltInType.Variant, // Structure
                            TypeCode.Char => BuiltInType.String,
                            TypeCode.Single => BuiltInType.Float,
                            TypeCode.Decimal => BuiltInType.Variant, // Number
                            _ => BuiltInType.Variant
                        }));
                    }
                }

                private readonly ExtensionFieldModel _extension;
            }
        }
    }
}
