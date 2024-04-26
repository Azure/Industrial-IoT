// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal partial class DataSetResolver
    {
        internal abstract partial class Field
        {
            /// <summary>
            /// Variable item base
            /// </summary>
            public abstract class Variable : Field
            {
                /// <inheritdoc/>
                public override string? NodeId
                {
                    get => _variable.PublishedVariableNodeId;
                    set
                    {
                        if (_variable.PublishedVariableNodeId != value)
                        {
                            MetaDataNeedsRefresh = true;
                            _variable.PublishedVariableNodeId = value;
                            if (_variable.ReadDisplayNameFromNode != true &&
                                _variable.DataSetFieldName == null)
                            {
                                ResolvedName = value;
                            }
                        }
                    }
                }

                /// <inheritdoc/>
                public override IReadOnlyList<string>? BrowsePath
                {
                    get => _variable.BrowsePath;
                    set => _variable.BrowsePath = value;
                }

                /// <inheritdoc/>
                public override string? ResolvedName
                {
                    get => _variable.DataSetFieldName;
                    set
                    {
                        if (_variable.DataSetFieldName != value)
                        {
                            MetaDataNeedsRefresh = true;
                            _variable.DataSetFieldName = value;
                        }
                    }
                }

                /// <inheritdoc/>
                public override PublishedNodeExpansion Flags { get; set; }

                /// <inheritdoc/>
                public override PublishingQueueSettingsModel? Publishing
                {
                    get => _variable.Publishing;
                    set => _variable.Publishing = value;
                }

                /// <inheritdoc/>
                public override ServiceResultModel? ErrorInfo
                {
                    get => _variable.State;
                    set => _variable.State = ErrorInfo;
                }
                /// <inheritdoc/>
                public override string? Id => _variable.Id;

                /// <inheritdoc/>
                public override bool MetaDataNeedsRefresh
                {
                    get => base.MetaDataNeedsRefresh || _variable.MetaData == null;
                    set => base.MetaDataNeedsRefresh = value;
                }

                /// <inheritdoc/>
                protected Variable(DataSetResolver resolver,
                    DataSetWriterModel writer, PublishedDataSetVariableModel variable)
                    : base(resolver, writer)
                {
                    _variable = variable;

                    if (_variable.ReadDisplayNameFromNode != true &&
                        _variable.DataSetFieldName == null)
                    {
                        ResolvedName = _variable.Id ?? NodeId;
                    }
                    if (_variable.Id == null)
                    {
                        var sb = new StringBuilder()
                            .Append(nameof(Variable))
                            .Append(_variable.DataSetFieldName)
                            .Append(_variable.PublishedVariableNodeId)
                            .Append(_variable.ReadDisplayNameFromNode == true)
                            .Append(_variable.DataSetClassFieldId)
                            // .Append(_variable.Triggering)
                            .Append(_variable.Publishing?.QueueName ?? string.Empty)
                            ;
                        _variable.BrowsePath?.ForEach(b => sb.Append(b));
                        _variable.Id = sb
                            .ToString()
                            .ToSha1Hash()
                            ;
                    }
                }

                /// <inheritdoc/>
                public override bool Merge(Field existing)
                {
                    if (!base.Merge(existing) || existing is not Variable other)
                    {
                        return false;
                    }

                    // Merge state
                    _variable.MetaData = other._variable.MetaData;
                    return true;
                }

                /// <inheritdoc/>
                public override async ValueTask ResolveMetaDataAsync(IOpcUaSession session,
                    ComplexTypeSystem? typeSystem, CancellationToken ct)
                {
                    Debug.Assert(_variable.Id != null);
                    try
                    {
                        var nodeId = _variable.PublishedVariableNodeId.ToNodeId(session.MessageContext);
                        if (Opc.Ua.NodeId.IsNull(nodeId))
                        {
                            _variable.State = kItemInvalid;
                            return;
                        }
                        var dataTypes = new NodeIdDictionary<DataTypeDescription>();
                        var fields = new FieldMetaDataCollection();
                        var node = await session.NodeCache.FetchNodeAsync(nodeId,
                            ct).ConfigureAwait(false);
                        if (node is VariableNode variable)
                        {
                            _variable.MetaData = await ResolveMetaDataAsync(session,
                                typeSystem, variable, (_variable.MetaData?.MinorVersion ?? 0) + 1,
                                ct).ConfigureAwait(false);
                            MetaDataNeedsRefresh = false;
                        }
                        else
                        {
                            _variable.State = kItemInvalid;
                        }
                    }
                    catch (ServiceResultException sre)
                    {
                        _variable.State = sre.ToServiceResultModel();
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogDebug("{Item}: Failed to get meta data for field {Field} " +
                            "with node {NodeId} with message {Message}.", this, _variable.Id,
                            _variable.PublishedVariableNodeId, ex.Message);

                        _variable.State = ex.ToServiceResultModel();
                    }
                }

                protected readonly PublishedDataSetVariableModel _variable;
            }
        }
    }
}
