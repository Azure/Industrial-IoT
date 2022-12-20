// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Linq;

    /// <summary>
    /// Filter conversion
    /// </summary>
    public static class FilterEncoderEx {

        /// <summary>
        /// Gets a default event filter.
        /// </summary>
        /// <returns></returns>
        private static EventFilter GetDefaultEventFilter() {
            var filter = new EventFilter();
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.EventId);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.EventType);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.SourceNode);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.SourceName);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Time);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.ReceiveTime);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.LocalTime);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Message);
            filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Severity);
            return filter;
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="model"></param>
        /// <param name="noDefaultFilter"></param>
        /// <returns></returns>
        public static EventFilter Decode(this IVariantEncoder encoder, EventFilterModel model,
            bool noDefaultFilter = false) {
            if (model == null || !(model.SelectClauses?.Any() ?? false)) {
                return noDefaultFilter ? null : GetDefaultEventFilter();
            }
            return new EventFilter {
                SelectClauses = new SimpleAttributeOperandCollection(
                    model.SelectClauses == null ? Enumerable.Empty<SimpleAttributeOperand>() :
                    model.SelectClauses.Select(c => c.ToStackModel(encoder.Context))),
                //
                // Per Part 4 only allow simple attribute operands in where clause
                // elements of event filters.
                //
                WhereClause = encoder.Decode(model.WhereClause, true)
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="model"></param>
        /// <param name="onlySimpleAttributeOperands"></param>
        /// <returns></returns>
        public static ContentFilter Decode(this IVariantEncoder encoder, ContentFilterModel model,
            bool onlySimpleAttributeOperands = false) {
            if (model == null) {
                return new ContentFilter();
            }
            return new ContentFilter {
                Elements = new ContentFilterElementCollection(model.Elements == null ?
                    Enumerable.Empty<ContentFilterElement>() : model.Elements
                        .Select(e => encoder.Decode(e, onlySimpleAttributeOperands)))
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ContentFilterModel Encode(this IVariantEncoder encoder, ContentFilter model) {
            if (model == null) {
                return null;
            }
            return new ContentFilterModel {
                Elements = model.Elements?
                    .Select(e => encoder.Encode(e))
                    .ToList()
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="model"></param>
        /// <param name="onlySimpleAttributeOperands"></param>
        /// <returns></returns>
        public static ContentFilterElement Decode(this IVariantEncoder encoder,
            ContentFilterElementModel model, bool onlySimpleAttributeOperands = false) {
            if (model == null) {
                return null;
            }
            return new ContentFilterElement {
                FilterOperands = new ExtensionObjectCollection(model?.FilterOperands == null ?
                    Enumerable.Empty<ExtensionObject>() : model.FilterOperands
                        .Select(e => new ExtensionObject(
                            encoder.Decode(e, onlySimpleAttributeOperands)))),
                FilterOperator = model.FilterOperator.ToStackType()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ContentFilterElementModel Encode(this IVariantEncoder encoder,
            ContentFilterElement model) {
            if (model == null) {
                return null;
            }
            return new ContentFilterElementModel {
                FilterOperands = model.FilterOperands
                    .Select(e => e.Body)
                    .Cast<FilterOperand>()
                    .Select(o => encoder.Encode(o))
                    .ToList(),
                FilterOperator = model.FilterOperator.ToServiceType()
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="model"></param>
        /// <param name="onlySimpleAttributeOperands"></param>
        /// <returns></returns>
        public static FilterOperand Decode(this IVariantEncoder encoder,
            FilterOperandModel model, bool onlySimpleAttributeOperands = false) {
            if (model == null) {
                return null;
            }
            if (model.Index != null) {
                return new ElementOperand {
                    Index = model.Index.Value
                };
            }
            if (model.Value != null) {
                var typeInfo = new TypeInfo(BuiltInType.NodeId, ValueRanks.Scalar);
                try {
                    // assume it's a node and try to parse it into correct namespace index
                    // if it fails, it's ok it will go to the default route
                    var nodeId = encoder.Decode(model.Value, null);
                    var typeDefinitionId = nodeId.ToString().ToNodeId(encoder.Context);
                    if (typeDefinitionId != null) {
                        return new LiteralOperand(TypeInfo.Cast(typeDefinitionId, typeInfo.BuiltInType));
                    }
                }
                catch { };
                return new LiteralOperand(TypeInfo.Cast(encoder.Decode(model.Value, null), typeInfo.BuiltInType));
            }
            if (model.Alias != null && !onlySimpleAttributeOperands) {
                return new AttributeOperand {
                    Alias = model.Alias,
                    NodeId = model.NodeId.ToNodeId(encoder.Context),
                    AttributeId = (uint)(model.AttributeId ?? NodeAttribute.Value),
                    BrowsePath = model.BrowsePath.ToRelativePath(encoder.Context),
                    IndexRange = model.IndexRange
                };
            }
            return new SimpleAttributeOperand {
                TypeDefinitionId = model.NodeId.ToNodeId(encoder.Context),
                AttributeId = (uint)(model.AttributeId ?? NodeAttribute.Value),
                BrowsePath = new QualifiedNameCollection(model.BrowsePath == null ?
                    Enumerable.Empty<QualifiedName>() :
                    model.BrowsePath?.Select(n => n.ToQualifiedName(encoder.Context))),
                IndexRange = model.IndexRange
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static FilterOperandModel Encode(this IVariantEncoder encoder,
            FilterOperand model) {
            if (model == null) {
                return null;
            }
            switch (model) {
                case ElementOperand elem:
                    return new FilterOperandModel {
                        Index = elem.Index
                    };
                case LiteralOperand lit:
                    return new FilterOperandModel {
                        Value = encoder.Encode(lit.Value, out _)
                    };
                case AttributeOperand attr:
                    return new FilterOperandModel {
                        NodeId = attr.NodeId.AsString(encoder.Context),
                        AttributeId = (NodeAttribute)attr.AttributeId,
                        BrowsePath = attr.BrowsePath.AsString(encoder.Context),
                        IndexRange = attr.IndexRange,
                        Alias = attr.Alias
                    };
                case SimpleAttributeOperand sattr:
                    return new FilterOperandModel {
                        NodeId = sattr.TypeDefinitionId.AsString(encoder.Context),
                        AttributeId = (NodeAttribute)sattr.AttributeId,
                        BrowsePath = sattr.BrowsePath?
                            .Select(p => p.AsString(encoder.Context))
                            .ToArray(),
                        IndexRange = sattr.IndexRange
                    };
                default:
                    throw new NotSupportedException("Operand not supported");
            }
        }
    }
}
