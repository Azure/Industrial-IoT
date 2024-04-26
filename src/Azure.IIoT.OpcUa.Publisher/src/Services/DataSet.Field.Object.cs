// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Opc.Ua.Client.ComplexTypes;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal partial class DataSetResolver
    {
        internal abstract partial class Field
        {
            /// <summary>
            /// Object item
            /// </summary>
            public sealed class Object : Field
            {
                /// <inheritdoc/>
                public override string? NodeId
                {
                    get => _object.PublishedNodeId;
                    set
                    {
                        if (_object.PublishedNodeId != value)
                        {
                            _object.PublishedNodeId = value;
                        }
                    }
                }

                /// <inheritdoc/>
                public override IReadOnlyList<string>? BrowsePath
                {
                    get => _object.BrowsePath;
                    set => _object.BrowsePath = value;
                }

                /// <inheritdoc/>
                public override string? ResolvedName
                {
                    get => _object.Name;
                    set
                    {
                        if (_object.Name != value)
                        {
                            _object.Name = value;
                        }
                    }
                }

                /// <inheritdoc/>
                public override PublishedNodeExpansion Flags
                {
                    get => _object.Flags;
                    set => _object.Flags = value;
                }

                /// <inheritdoc/>
                public override void Expand(DataSetResolver resolver,
                    PublishedDataItemsModel value)
                {
                    if (value != null)
                    {
                        _object.PublishedVariables = value;

                        foreach (var item in value.PublishedData)
                        {
                            resolver._items.Add(new ObjectVariable(resolver, DataSetWriter, item));
                        }
                        Flags &= ~PublishedNodeExpansion.Expand;
                    }
                }

                /// <inheritdoc/>
                public override PublishingQueueSettingsModel? Publishing { get; set; }

                /// <inheritdoc/>
                public override ServiceResultModel? ErrorInfo
                {
                    get => _object.State;
                    set => _object.State = ErrorInfo;
                }
                /// <inheritdoc/>
                public override string? Id => _object.Id;

                /// <inheritdoc/>
                public override bool NeedsQueueNameResolving => false;

                /// <inheritdoc/>
                public Object(DataSetResolver resolver,
                    DataSetWriterModel writer, PublishedObjectModel obj)
                    : base(resolver, writer)
                {
                    _object = obj;

                    if ((_object.PublishedVariables?.PublishedData.Count ?? 0) > 0 &&
                        ErrorInfo == null)
                    {
                        _object.Flags &= ~PublishedNodeExpansion.Expand;
                    }
                    else
                    {
                        _object.Flags |= PublishedNodeExpansion.Expand;
                    }

                    if (_object.Id == null)
                    {
                        var sb = new StringBuilder()
                            .Append(nameof(Object))
                        //    .Append(_object.DisplayName)
                            .Append(_object.PublishedNodeId)
                            // .Append(_object.DataSetClassId)
                            // .Append(_variable.Triggering)
                            .Append(_object.Template.Publishing?.QueueName ?? string.Empty)
                            ;
                        _object.BrowsePath?.ForEach(b => sb.Append(b));

                        _object.Id = sb
                            .ToString()
                            .ToSha1Hash()
                            ;
                    }
                }

                /// <inheritdoc/>
                public override bool Merge(Field existing)
                {
                    if (!base.Merge(existing) || existing is not Object other)
                    {
                        return false;
                    }

                    // Merge state
                    _object.PublishedVariables = other._object.PublishedVariables;
                    return true;
                }

                /// <inheritdoc/>
                public override PublishedDataSetVariableModel CreateVariable(
                    string nodeId, string name)
                {
                    return _object.Template with
                    {
                        Attribute = NodeAttribute.Value,
                        DataSetFieldName = name,
                        BrowsePath = null,
                        PublishedVariableNodeId = nodeId,
                        Publishing = Publishing
                    };
                }

                /// <inheritdoc/>
                public static IEnumerable<DataSetWriterModel> Split(DataSetWriterModel writer,
                    IEnumerable<Field> items, int maxItemsPerWriter)
                {
                    // Each object data set generates a writer
                    foreach (var item in items.OfType<Object>())
                    {
                        if (item._object.PublishedVariables == null)
                        {
                            continue;
                        }
                        foreach (var set in item._object.PublishedVariables.PublishedData
                            .Batch(maxItemsPerWriter))
                        {
                            var copy = Copy(writer);
                            Debug.Assert(copy.DataSet?.DataSetSource != null);

                            var oSet = item._object with
                            {
                                // No need to clone more members
                                PublishedVariables = new PublishedDataItemsModel
                                {
                                    PublishedData = set
                                        .Select((item, i) => item with { FieldIndex = i })
                                        .ToList()
                                }
                            };

                            copy.DataSet.DataSetSource.PublishedObjects =
                                new PublishedObjectItemsModel
                                {
                                    PublishedData = new[] { oSet }
                                };
                            copy.DataSet.Name = item._object.Name;
                            copy.DataSet.ExtensionFields = copy.DataSet.ExtensionFields?
                                // No need to clone more members of the field
                                .Select((f, i) => f with
                                {
                                    FieldIndex = i + oSet.PublishedVariables.PublishedData.Count
                                })
                                .ToList();
                            yield return copy;
                        }
                    }
                }

                /// <inheritdoc/>
                public override ValueTask ResolveMetaDataAsync(IOpcUaSession session,
                    ComplexTypeSystem? typeSystem, CancellationToken ct)
                {
                    return ValueTask.CompletedTask;
                }
                private readonly PublishedObjectModel _object;
            }

            /// <summary>
            /// Object Variable item
            /// </summary>
            public sealed class ObjectVariable : Variable
            {
                public ObjectVariable(DataSetResolver resolver,
                    DataSetWriterModel writer, PublishedDataSetVariableModel variable)
                    : base(resolver, writer, variable)
                {
                }
            }
        }
    }
}
