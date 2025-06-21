// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Parser
{
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Extensions.Serializers;
    using Irony.Parsing;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Builds event filter and query filters from a syntax tree.
    /// We do not bother with AST generation as the output of the
    /// parser is good enough for us to build the models
    /// </summary>
    internal sealed class FilterModelBuilder
    {
        /// <summary>
        /// Create event builder
        /// </summary>
        /// <param name="syntaxTree"></param>
        /// <param name="context"></param>
        /// <param name="serializer"></param>
        private FilterModelBuilder(ParseTree syntaxTree,
            IFilterParserContext context, IJsonSerializer serializer)
        {
            _serializer = serializer;
            _context = context;
            _syntaxTree = syntaxTree;

            var index = 0;
            var selectStmt = syntaxTree.Root
                .GetChild("selectStmt", ref index, _syntaxTree);

            index = 0;
            var prefixList = selectStmt
                .GetChild("prefixList", ref index);

            //
            // Get the prefix to namespace uri lookup table from
            // the prefix declarations. Prefixes are optional and
            // if not specified namespace indexes and node ids
            // are allowed to be used inline.
            //
            _prefixToNamespaceUri = prefixList
                .GetChildren()
                .ToDictionary(
                    n => n.GetChildTokenText(0, _syntaxTree), // prefix
                    n => ExpandNamespaceUri(
                         n.GetChildTokenText(1, _syntaxTree))) // nsuri
                        ?? [];

            var selList = selectStmt
                .GetChild("selList", ref index);
            _fromClauseOpt = selectStmt
                .GetChild("fromClauseOpt", ref index);
            _whereClauseOpt = selectStmt
                .GetChild("whereClauseOpt", ref index);

            //
            // Get the selected source type list. We maintain order
            // to ensure we pick the first declared one in case of
            // ambiguity.
            //
            index = 0;
            _typeToAliasOrdered = _fromClauseOpt
                .GetChild("typeList", ref index)
                .GetChildren("type")
                .Select(id =>
                {
                    var typeName = id
                        .GetChildTokenText(0, _syntaxTree)
                        .TrimMatchingChar('´');
                    var type = ExpandSimpleIdentifier(typeName, id, true);
                    var alias = id.GetChildTokenText(1, string.Empty);
                    return (type, alias);
                })
                .ToList();

            if (_typeToAliasOrdered.Count == 0)
            {
                // from clause is optional, default to BaseEventType.
                _typeToAliasOrdered = new List<(string, string)>
                {
                    (Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                        string.Empty)
                };
            }

            index = 0;
            _fieldItemList = selList
                .GetChild("fieldItemList", ref index)
                .GetChildren()
                .ToList();
        }

        /// <summary>
        /// Build the event filter model using the builder
        /// </summary>
        /// <param name="syntaxTree"></param>
        /// <param name="context"></param>
        /// <param name="serializer"></param>
        /// <param name="ct"></param>
        public static Task<EventFilterModel> BuildEventFilterAsync(
            ParseTree syntaxTree, IFilterParserContext context,
            IJsonSerializer serializer, CancellationToken ct)
        {
            var builder = new FilterModelBuilder(syntaxTree,
                context, serializer);
            return builder.BuildEventFilterAsync(ct);
        }

        /// <summary>
        /// Build event filter model
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<EventFilterModel> BuildEventFilterAsync(
            CancellationToken ct)
        {
            await LoadBuilderContextAsync(ct).ConfigureAwait(false);

            BuildSelectClauses();

            BuildContentFilter();

            var elements = GetContentFilterElements().ToList();
            return new EventFilterModel
            {
                SelectClauses = _selectClauses.ToList(),
                WhereClause = elements.Count != 0 ?
                    new ContentFilterModel
                    {
                        Elements = elements
                    } : null
            };
        }

        /// <summary>
        /// Load the builder context from the parser context
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task LoadBuilderContextAsync(CancellationToken ct)
        {
            // Get valid identifier lookup table
            foreach (var typeId in _typeToAliasOrdered.Select(t => t.type).Distinct())
            {
                var identifiers = await _context.GetIdentifiersAsync(typeId,
                    ct).ConfigureAwait(false);

                var identifierTable = new Dictionary<ImmutableRelativePath, IdentifierMetaData>();
                foreach (var identifier in identifiers)
                {
                    // Ordered from super type to sub type so we override here
                    identifierTable.AddOrUpdate(new ImmutableRelativePath(identifier.BrowsePath), identifier);
                }
                _validIdsForType.AddOrUpdate(typeId, identifierTable);
            }
        }

        /// <summary>
        /// Get content filter elements
        /// </summary>
        private IEnumerable<ContentFilterElementModel> GetContentFilterElements()
        {
            // Adjust all existing filter element indexes
            // Reverse the list
            _contentFilter.Reverse();
            foreach (var filterElement in _contentFilter)
            {
                var element = filterElement.Element;

                if (element.FilterOperands?.Any(operand => operand.Index != null) != true)
                {
                    yield return element;
                }
                else
                {
                    var operands = new List<FilterOperandModel>();
                    foreach (var operand in element.FilterOperands)
                    {
                        if (operand.Index != null)
                        {
                            for (var index = 0; index < _contentFilter.Count; index++)
                            {
                                if (operand.Index == _contentFilter[index].Id)
                                {
                                    operands.Add(operand with
                                    {
                                        Index = (uint)index
                                    });
                                    break;
                                }
                            }
                        }
                        else
                        {
                            operands.Add(operand);
                        }
                    }
                    yield return element with
                    {
                        FilterOperands = operands
                    };
                }
            }
        }

        /// <summary>
        /// Build select clauses
        /// </summary>
        /// <returns></returns>
        private void BuildSelectClauses()
        {
            // Selected item indexed by name or alias
            if (_fieldItemList.Count != 0)
            {
                foreach (var item in _fieldItemList)
                {
                    var index = 0;
                    var fieldSource = item
                        .GetChild("fieldSource", ref index, _syntaxTree)
                        ;
                    var path = ExpandPathIdentifier(fieldSource, out var type,
                        out var displayName, out _, out var attribute);

                    var operand = new SimpleAttributeOperandModel
                    {
                        AttributeId = attribute,
                        BrowsePath = path,
                        DisplayName = displayName,
                        IndexRange = null, // TODO
                        TypeDefinitionId = type
                    };

                    var aliasOpt = item
                        .GetChild("aliasOpt", ref index, _syntaxTree)
                        .FindTokenAndGetText();
                    if (aliasOpt != null)
                    {
                        //
                        // If an alias for later use was declared store it
                        // for later lookup
                        //
                        _aliasForId.Add(aliasOpt, operand);
                    }
                    _selectClauses.Add(operand);
                }
            }
            else
            {
                // Select everything from every item
                foreach (var id in _validIdsForType.SelectMany(kv => kv.Value))
                {
                    _selectClauses.Add(new SimpleAttributeOperandModel
                    {
                        AttributeId = NodeAttribute.Value,
                        BrowsePath = id.Value.BrowsePath,
                        DisplayName = "/" + id.Value.DisplayName.Trim('/')
                            + ".Value",
                        TypeDefinitionId = id.Value.TypeDefinitionId
                    });
                }
            }
        }

        /// <summary>
        /// Build where clauses
        /// </summary>
        /// <returns></returns>
        private void BuildContentFilter()
        {
            // Filter stack
            var expression = _whereClauseOpt.GetChild(0);
            if (expression == null)
            {
                return;
            }

            // Process first expression
            ProcessExpression(expression);
        }

        /// <summary>
        /// Process any filter expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// <exception cref="ParserException"></exception>
        private FilterOperandModel ProcessExpression(ParseTreeNode expression)
        {
            var index = 0;
            var exprName = expression.Term?.Name;
            switch (exprName)
            {
                case "unExpr":
                    var unop = expression.GetChild("unOp", ref index,
                        _syntaxTree);
                    var unexpr = expression.GetChild(index,
                        _syntaxTree);
                    return ProcessExpression(unop, unexpr);
                case "binExpr":
                    var lhs = expression.GetChild(index++, _syntaxTree);
                    var binOp = expression.GetChild("binOp", ref index,
                        _syntaxTree);
                    var rhs = expression.GetChild(index,
                        _syntaxTree);
                    return ProcessExpression(binOp, lhs, rhs);
                case "fnExpr":
                    var obj = expression.GetChild(index++, _syntaxTree);
                    var fnOp = expression.GetChild("fnOp", ref index,
                        _syntaxTree);
                    var args = expression.GetChild("exprList", ref index,
                        _syntaxTree);
                    return ProcessExpression(fnOp, obj
                        .YieldReturn()
                        .Concat(args.GetChildren())
                        .ToArray());
                case "literal":
                    return ProcessLiteralOperand(expression);
                case "id":
                    return ProcessIdentifierOperand(expression);
                case "parSelectStmt":
                    throw ParserException.Create(
                        "Unsupported expression found.",
                        _syntaxTree, expression);
                default:
                    throw ParserException.Create(
                        $"Unsupported expression {exprName} found.",
                        _syntaxTree, expression);
            }
        }

        /// <summary>
        /// Process element expression
        /// </summary>
        /// <param name="op"></param>
        /// <param name="operandExpressions"></param>
        /// <exception cref="ParserException"></exception>
        private FilterOperandModel ProcessExpression(ParseTreeNode op,
            params ParseTreeNode[] operandExpressions)
        {
            var filterOp = GetFilterOperator(op, out var negate,
                out var exprValidator);
            // Push the elements onto the stack

            // Process the children first and push each into the list
            var elements = new List<FilterOperandModel>();
            foreach (var expression in operandExpressions)
            {
                var operand = ProcessExpression(expression);

                if (filterOp == FilterOperatorType.OfType &&
                    operand.AttributeId == NodeAttribute.NodeId)
                {
                    //
                    // Switch simple operand with NodeId attribute
                    // into a literal with node type.
                    //
                    operand = new FilterOperandModel
                    {
                        Value = operand.NodeId,
                        DataType = operand.AttributeId.ToString()
                    };
                }
                elements.Add(operand);
            }

            var error = exprValidator(elements);
            if (!string.IsNullOrEmpty(error))
            {
                throw ParserException.Create(error, _syntaxTree,
                    op.YieldReturn().Concat(operandExpressions).ToArray());
            }

            _contentFilter.Add(new ContentFilterElement2Model(_contentFilter.Count + 1,
                new ContentFilterElementModel
                {
                    FilterOperator = filterOp,
                    FilterOperands = elements
                }));

            if (negate)
            {
                // Shift all indexes and insert another negating element
                _contentFilter.Add(new ContentFilterElement2Model(_contentFilter.Count + 1,
                    new ContentFilterElementModel
                    {
                        FilterOperator = FilterOperatorType.Not,
                        FilterOperands = new[] {
                            new FilterOperandModel
                            {
                                Index = (uint)_contentFilter.Count
                            }
                        }
                    }));
            }

            // Return an element used by others
            return new FilterOperandModel
            {
                Index = (uint)_contentFilter.Count
            };
        }

        /// <summary>
        /// Process identifier expression and push onto the stack
        /// </summary>
        /// <param name="expression"></param>
        private FilterOperandModel ProcessIdentifierOperand(
            ParseTreeNode expression)
        {
            // a browse path identifier
            var path = ExpandPathIdentifier(expression,
                out var type, out _, out var alias, out var attribute);
            return new FilterOperandModel
            {
                AttributeId = attribute,
                BrowsePath = path,
                Alias = string.IsNullOrEmpty(alias) ? null : alias,
                IndexRange = null, // TODO
                NodeId = type
            };
        }

        private FilterOperandModel ProcessLiteralOperand(
            ParseTreeNode expression)
        {
            var index = 0;
            var value = expression
                .GetChild("value", ref index, _syntaxTree)
                .GetChild(0, _syntaxTree);

            var dataType = expression
                .GetChild(index)?
                .FindTokenAndGetText()?
                .TrimMatchingChar('´');

            var str = value.Term?.Name;
            if (value.Term is StringLiteral)
            {
                str = value.FindTokenAndGetText()?.TrimQuotes();

                // Expand any namespaced simple identifiers
                if (dataType != null && str != null)
                {
                    if (dataType.Equals("NodeId",
                            StringComparison.OrdinalIgnoreCase) ||
                        dataType.Equals("ExpandedNodeId",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        str = ExpandSimpleIdentifier(str, value, true);
                    }
                    else if (dataType.Equals("QualifiedName",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        str = ExpandSimpleIdentifier(str, value, false);
                    }
                }
                str = $"\"{str}\"";
            }
            else if (value.Term is NumberLiteral)
            {
                str = value.FindTokenAndGetText();
            }

            if (string.IsNullOrEmpty(str))
            {
                str = "null";
            }

            return new FilterOperandModel
            {
                DataType = dataType,
                Value = _serializer.Parse(str)
            };
        }

        /// <summary>
        /// Convert filter operator
        /// </summary>
        /// <param name="op"></param>
        /// <param name="negate"></param>
        /// <param name="validator"></param>
        /// <returns></returns>
        /// <exception cref="ParserException"></exception>
        private FilterOperatorType GetFilterOperator(ParseTreeNode op, out bool negate,
            out Func<List<FilterOperandModel>, string?> validator)
        {
            negate = false;
            var opStr = op.FindTokenAndGetText();
            FilterOperatorType filterOp;
            switch (opStr.ToUpperInvariant())
            {
                case "NOT":
                case "!":
                    filterOp = FilterOperatorType.Not;
                    validator = l => ValidateExact(1, l);
                    break;
                case "=":
                case "==":
                    filterOp = FilterOperatorType.Equals;
                    validator = l => ValidateExact(2, l);
                    break;
                case ">":
                    filterOp = FilterOperatorType.GreaterThan;
                    validator = l => ValidateExact(2, l);
                    break;
                case "<":
                    filterOp = FilterOperatorType.LessThan;
                    validator = l => ValidateExact(2, l);
                    break;
                case ">=":
                    filterOp = FilterOperatorType.GreaterThan;
                    validator = l => ValidateExact(2, l);
                    break;
                case "<=":
                    filterOp = FilterOperatorType.LessThanOrEqual;
                    validator = l => ValidateExact(2, l);
                    break;
                case "LIKE":
                    filterOp = FilterOperatorType.Like;
                    validator = l => ValidateExact(2, l);
                    break;
                case "<>":
                case "!=":
                    negate = true;
                    filterOp = FilterOperatorType.Equals;
                    validator = l => ValidateExact(2, l);
                    break;
                case "!<":
                    negate = true;
                    filterOp = FilterOperatorType.LessThan;
                    validator = l => ValidateExact(2, l);
                    break;
                case "!>":
                    negate = true;
                    filterOp = FilterOperatorType.GreaterThan;
                    validator = l => ValidateExact(2, l);
                    break;
                case "AND":
                    filterOp = FilterOperatorType.And;
                    validator = l => ValidateExact(2, l, true);
                    break;
                case "OR":
                    filterOp = FilterOperatorType.Or;
                    validator = l => ValidateExact(2, l, true);
                    break;
                case "CAST":
                    filterOp = FilterOperatorType.Cast;
                    validator = l => ValidateExact(2, l);
                    break;
                case "&":
                    filterOp = FilterOperatorType.BitwiseAnd;
                    validator = l => ValidateExact(2, l);
                    break;
                case "|":
                    filterOp = FilterOperatorType.BitwiseOr;
                    validator = l => ValidateExact(2, l);
                    break;
                case "IN":
                    filterOp = FilterOperatorType.InList;
                    validator = l => ValidateMin(1, l);
                    break;
                case "BETWEEN":
                    filterOp = FilterOperatorType.Between;
                    validator = l => ValidateExact(3, l);
                    break;
                case "RELATEDTO":
                    filterOp = FilterOperatorType.RelatedTo;
                    validator = ValidateRelatedTo;
                    break;
                case "ISNULL":
                    filterOp = FilterOperatorType.IsNull;
                    validator = l => ValidateExact(1, l);
                    break;
                case "OFTYPE":
                    filterOp = FilterOperatorType.OfType;
                    validator = l => ValidateExact(1, l);
                    break;
                case "INVIEW":
                    filterOp = FilterOperatorType.InView;
                    validator = l => ValidateExact(1, l);
                    break;
                default:
                    throw ParserException.Create($"Operand {opStr} not supported",
                        _syntaxTree, op);
            }
            return filterOp;

            //
            // Generic expression validation. Validates the exact length of
            // provided operands.
            //
            string? ValidateExact(int exact, List<FilterOperandModel> operands,
                bool reverseOperands = false)
            {
                if (exact != operands.Count)
                {
                    return $"Operator {opStr} ({filterOp}) requires {exact} " +
                        $"operand(s) but only {operands.Count} provided.";
                }
                if (reverseOperands && operands.All(o => o.Index != null))
                {
                    //
                    // Call reverse on the operands to match the specification
                    // examples (validated in our tests)
                    //
                    operands.Reverse();
                }
                return null;
            }

            //
            // Special case related to, which can have 3-6 operands
            //
            string? ValidateRelatedTo(IList<FilterOperandModel> operands)
            {
                if (operands.Count < 3)
                {
                    return $"Operator {opStr} ({filterOp}) requires at least " +
                        $"3 operands, but {operands.Count} provided.";
                }
                if (operands.Count > 6)
                {
                    return $"Operator {opStr} ({filterOp}) requires at most " +
                        $"6 operands, but {operands.Count} provided.";
                }
                return null;
            }

            //
            // Validate minimum operands are provided.
            //
            string? ValidateMin(int min, IList<FilterOperandModel> operands)
            {
                if (operands.Count < min)
                {
                    return $"Operator {opStr} ({filterOp}) requires at least " +
                        $"{min} operand(s), but {operands.Count} provided.";
                }
                return null;
            }
        }

        /// <summary>
        /// Fix up a namespace uri
        /// </summary>
        /// <param name="namespaceUri"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static string ExpandNamespaceUri(string namespaceUri)
        {
            namespaceUri = namespaceUri.Trim();
            if (namespaceUri[0] == '<' && namespaceUri[^1] == '>')
            {
                // Trim prefix quotes
                namespaceUri = namespaceUri[1..^1];
            }
            return namespaceUri.TrimQuotes().TrimEnd('#');
        }

        /// <summary>
        /// Expands a path identifier into the components of a simple or
        /// full operand identifier.
        /// </summary>
        /// <param name="pathIdNode"></param>
        /// <param name="typeDefinitionId"></param>
        /// <param name="displayName"></param>
        /// <param name="alias"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        /// <exception cref="ParserException"></exception>
        private IReadOnlyList<string> ExpandPathIdentifier(ParseTreeNode pathIdNode,
            out string typeDefinitionId, out string displayName, out string alias,
            out NodeAttribute attribute)
        {
            var pathId = pathIdNode.FindTokenAndGetText().TrimMatchingChar('´');
            // Check first if the id is an alias for a selector.
            if (_aliasForId.TryGetValue(pathId, out var operand) &&
                operand.BrowsePath != null &&
                operand.DisplayName != null)
            {
                typeDefinitionId = operand.TypeDefinitionId!;
                displayName = operand.DisplayName;
                attribute = operand.AttributeId ?? NodeAttribute.Value;
                alias = pathId;
                return operand.BrowsePath;
            }

            //
            // A path can start with a type or type alias, if not the
            // default alias is assumed (empty string). If the path starts
            // with a aggregate, hierachical or other relationship, then
            // it is a path starting from an implicit type.
            //
            List<RelativePathElementModel> pathElements;
            string typeOrTypeAlias;
            try
            {
                pathElements = pathId.ToRelativePath(out typeOrTypeAlias)
                    .ToList();
            }
            catch (FormatException formatException)
            {
                throw ParserException.Create(formatException.Message,
                    formatException, _syntaxTree, pathIdNode);
            }

            //
            // Extract attribute. The attribute is appended using a . which
            // also stands for an aggregates reference element.
            //
            var last = pathElements.LastOrDefault();
            NodeAttribute? nodeAttribute = null;
            if (last?.IsAggregatesReference() == true &&
                    last.NoSubtypes != true && last.IsInverse != true &&
                Enum.TryParse<NodeAttribute>(last.TargetName, true, out var attr))
            {
                pathElements.Remove(last);
                nodeAttribute = attr;
                // Not an attribute - leave as is
            }

            // Check whether the alias points to a type
            var aliasedTypes = _typeToAliasOrdered
                .Where(typeAndAlias => typeAndAlias.alias == typeOrTypeAlias)
                ;
            if (!aliasedTypes.Any())
            {
                if (string.IsNullOrEmpty(typeOrTypeAlias))
                {
                    //
                    // Try to resolve the path from all specified types
                    // with precedence of order of declaration.
                    //
                    aliasedTypes = _typeToAliasOrdered;
                }
                else
                {
                    //
                    // If the type alias is actually a valid type, then put
                    // it first (without any alias).
                    //
                    var typeId = ExpandSimpleIdentifier(typeOrTypeAlias,
                        pathIdNode, true);
                    if (_typeToAliasOrdered
                        .Any(typeAndAlias => typeAndAlias.type == typeId))
                    {
                        aliasedTypes = (typeId, string.Empty).YieldReturn();
                    }
                }
            }

            if (pathElements.Count == 0)
            {
                //
                // There was only a type alias here. This is allowed
                // and means we want the type and an empty path after
                // We select node id of the type instead of value.
                //
                var aliasedType = aliasedTypes.FirstOrDefault();
                if (!string.IsNullOrEmpty(aliasedType.type))
                {
                    attribute = nodeAttribute ?? NodeAttribute.NodeId;
                    displayName = "/." + attribute.ToString();
                    typeDefinitionId = aliasedType.type;
                    alias = aliasedType.alias;
                    return Array.Empty<string>();
                }
                throw ParserException.Create("Could not find " +
                    $"type {typeOrTypeAlias} in candidates declared in FROM.",
                    _syntaxTree, pathIdNode, _fromClauseOpt);
            }

            //
            // The browse path is now pathElements.
            // We need to expand it to incorporate the prefixes
            // and resolve any session namespace indexes.
            //

            pathElements = pathElements.ConvertAll(element => element with
            {
                TargetName = ExpandSimpleIdentifier(
                    element.TargetName, pathIdNode, false),
                ReferenceTypeId = ExpandSimpleIdentifier(
                    element.ReferenceTypeId, pathIdNode, true),
            });

            //
            // We now have candidate types in order of declaration
            // Try to resolve a valid identifier on these types.
            //
            // Try to uniquely identify the path in the valid ids
            var browsePath = new ImmutableRelativePath(pathElements.AsString());
            foreach (var aliasedType in aliasedTypes)
            {
                var ids = _validIdsForType[aliasedType.type];
                Debug.Assert(ids != null);

                if (ids.TryGetValue(browsePath, out var metadata))
                {
                    // Found it.
                    attribute = nodeAttribute ?? NodeAttribute.Value;
                    displayName = "/" + metadata.DisplayName.Trim('/')
                        + "." + attribute;
                    typeDefinitionId = metadata.TypeDefinitionId;
                    alias = aliasedType.alias;
                    return browsePath.Path;
                }

                // TODO: if we do not find it we could still use the context
                // to translate the browse path into a node etc. or just
                // return a stand in item.
            }

            var candidates = string.Join(", ", aliasedTypes.Select(a => a.type));
            if (string.IsNullOrEmpty(candidates))
            {
                throw ParserException.Create("Could not find " +
                    $"candidate types that contain {browsePath}. " +
                    "Make sure to declare a candidate type in FROM.", _syntaxTree,
                    pathIdNode, _fromClauseOpt);
            }
            var available = string.Join(", ", aliasedTypes
                .SelectMany(a => _validIdsForType[a.type].Keys)
                .Distinct());
            throw ParserException.Create(
                $"Could not find {browsePath} in one of the candidate types " +
                $"{candidates} provided in FROM.\nAvailable paths: {available}",
                _syntaxTree, pathIdNode, _fromClauseOpt);
        }

        /// <summary>
        /// Expand an identifier into a full string using lookup tables
        /// </summary>
        /// <param name="id"></param>
        /// <param name="idNode"></param>
        /// <param name="isNodeId"></param>
        /// <returns></returns>
        /// <exception cref="ParserException"></exception>
        private string ExpandSimpleIdentifier(string id, ParseTreeNode? idNode,
            bool isNodeId)
        {
            if (id.Length > 1 && id[0] == '[' && id[^1] == ']')
            {
                id = id[1..^1];
            }
            if (Uri.TryCreate(id, UriKind.Absolute, out var uri) &&
                !string.IsNullOrEmpty(uri.Host))
            {
                // Uri - keep as is
                return id;
            }

            var parts = id.Split(':', StringSplitOptions.TrimEntries |
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                if (!isNodeId && parts.Length > 2)
                {
                    // We assume that a node id can contain > 1 colon
                    throw ParserException.Create(
                        $"Malformed identifier {id} contains > 1 colon character.",
                        _syntaxTree, idNode);
                }

                id = id[(parts[0].Length + 1)..];
                if (_prefixToNamespaceUri.TryGetValue(parts[0], out var namespaceUri) ||
                    TryGetNamespace(parts[0], out namespaceUri))
                {
                    if (string.IsNullOrEmpty(namespaceUri))
                    {
                        return TranslateOpcUaIdentifier(id); // Default ua namespace
                    }
                    return namespaceUri + "#" + id;
                }

                if (!isNodeId) // We assume that a node id can contain 1..n colons
                {
                    throw ParserException.Create("Failed to retrieve " +
                        $"namespace for prefix {parts[0]} of identifier {id}.",
                        _syntaxTree, idNode);
                }

                //
                // If this is a node id then let the resolver convert the string
                // later in the context of the session. Otherwise we assume that
                // if this is a path we have thrown earlier in case we could not
                // parse it here.
                //
                return id; // Custom node identifier

                bool TryGetNamespace(string prefix, out string? namespaceUri)
                {
                    if (prefix.StartsWith("ns", StringComparison.InvariantCulture))
                    {
                        prefix = prefix[2..];
                    }
                    if (uint.TryParse(prefix, out var index))
                    {
                        return _context.TryGetNamespaceUri(index, out namespaceUri);
                    }
                    namespaceUri = null;
                    return false;
                }
            }

            return TranslateOpcUaIdentifier(id);

            static string TranslateOpcUaIdentifier(string id)
            {
                // Try convert object type identifiers
                if (TypeMaps.ObjectTypes.Value.TryGetIdentifier(id,
                    out var identifier))
                {
                    return new Opc.Ua.NodeId(identifier).ToString();
                }
                return id;
            }
        }

        /// <summary>
        /// Compare attribute operands. We have to use a comparer because reord
        /// do not compare arrays or read only lists by value.
        /// </summary>
        private sealed class SimpleAttributeOperandComperer :
            IEqualityComparer<SimpleAttributeOperandModel>
        {
            /// <inheritdoc/>
            public bool Equals(SimpleAttributeOperandModel? x, SimpleAttributeOperandModel? y)
            {
                return x.IsSameAs(y);
            }

            /// <inheritdoc/>
            public int GetHashCode([DisallowNull] SimpleAttributeOperandModel obj)
            {
                return HashCode.Combine(obj.IndexRange, obj.AttributeId, obj.DisplayName,
                    obj.DataSetClassFieldId, obj.TypeDefinitionId, obj.BrowsePath == null ? 0 :
                    new ImmutableRelativePath(obj.BrowsePath).GetHashCode());
            }
        }

        private record class ContentFilterElement2Model(int Id, ContentFilterElementModel Element);
        private readonly List<ContentFilterElement2Model> _contentFilter = [];
        private readonly HashSet<SimpleAttributeOperandModel> _selectClauses =
            new(new SimpleAttributeOperandComperer());
        private readonly IJsonSerializer _serializer;
        private readonly IFilterParserContext _context;
        private readonly ParseTree _syntaxTree;
        private readonly List<ParseTreeNode> _fieldItemList;
        private readonly ParseTreeNode? _fromClauseOpt;
        private readonly ParseTreeNode? _whereClauseOpt;

        /// <summary>
        /// Lookup tables
        /// </summary>
        private readonly Dictionary<string,
            string> _prefixToNamespaceUri;
        private readonly Dictionary<string,
            SimpleAttributeOperandModel> _aliasForId = [];
        private readonly Dictionary<string,
            Dictionary<ImmutableRelativePath, IdentifierMetaData>> _validIdsForType = [];
        private readonly IReadOnlyList<(string type, string alias)> _typeToAliasOrdered;
    }
}
