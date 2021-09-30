/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Design {
    using Opc.Ua.Design.Schema;
    using Opc.Ua.Extensions;
    using Opc.Ua.Models;
    using Opc.Ua.Nodeset;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Generates files used to describe data types.
    /// </summary>
    internal sealed class ModelDesignFile : INodeResolver, IModelDesign {

        /// <inheritdoc/>
        public string Namespace => _model.TargetNamespace;

        /// <inheritdoc/>
        public string Version => _model.TargetVersion;

        /// <inheritdoc/>
        public string Name => _model.TargetNamespaceInfo.Prefix;

        /// <inheritdoc/>
        public DateTime? PublicationDate => _model.TargetPublicationDateOrNull;

        /// <summary>
        /// Create model design
        /// </summary>
        /// <param name="model"></param>
        /// <param name="assigner"></param>
        /// <param name="resolver"></param>
        /// <param name="context"></param>
        internal ModelDesignFile(ModelDesign model, INodeIdAssigner assigner,
            INodeResolver resolver, IServiceMessageContext context = null) {

            _resolver = resolver ?? assigner as INodeResolver;
            _assigner = assigner ?? resolver as INodeIdAssigner;
            _context = context ?? ServiceMessageContext.GlobalContext;
            _model = model ?? throw new ArgumentNullException(nameof(model));

            // Import model design
            foreach (var node in model.Items) {
                AddNode(node, null);
            }

            // Validate and expand
            ValidateNodes(_model);

            _model.Items = _nodes.Values.ToArray();

            // Build instance hierarchy
            foreach (var node in _model.Items) {
                node.Hierarchy = CreateInstanceHierarchy(node);
            }

            // Assign ids
            AssignIds(_model);

            // Create nodes and references
            var namespaceUris = new NamespaceTable();
            if (_model.Namespaces != null) {
                foreach (var ns in _model.Namespaces) {
                    if (ns.Value != Namespaces.OpcUa) {
                        namespaceUris.Append(ns.Value);
                    }
                }
            }
            foreach (var node in _model.Items) {
                CreateNode(node, namespaceUris);
            }
        }

        /// <inheritdoc/>
        public void Save(Stream stream) {
            var settings = new XmlWriterSettings {
                Encoding = Encoding.UTF8,
                Indent = true
            };

            var document = new XmlDocument();
            var attribute1 = document.CreateAttribute("tns:ns", _model.TargetNamespace);
            attribute1.Value = _model.TargetNamespace;
            var attribute2 = document.CreateAttribute("uax:ns", Namespaces.OpcUaXsd);
            attribute2.Value = Namespaces.OpcUaXsd;

            _model.AnyAttr = new XmlAttribute[] { attribute1, attribute2 };
            _model.Items = _nodes.Values.ToArray();
            using (var writer = XmlWriter.Create(stream, settings)) {
                var serializer = new XmlSerializer(typeof(ModelDesign));
                serializer.Serialize(writer, _model);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<BaseNodeModel> GetNodes(ISystemContext context) {
            if (_model.Namespaces != null) {
                // Add namespaces
                var namespaces = _model.Namespaces
                    .Select(ns => ns.Value)
                    .Where(ns => ns != Namespaces.OpcUa);
                context.NamespaceUris.Update(Namespaces.OpcUa.YieldReturn().Concat(namespaces));
            }
            // collect the nodes to write.
            var collection = new List<BaseNodeModel>();
            foreach (var node in _model.Items) {
                var isInAddressSpace = !node.NotInAddressSpace;
                if (node is InstanceDesign instance) {
                    if (instance.TypeDefinition != null &&
                        instance.TypeDefinition.Name == "DataTypeEncodingType") {
                        isInAddressSpace = instance.Parent == null || !instance.Parent.NotInAddressSpace;
                    }
                }
                if (node is MethodDesign methodDesign) {
                    if (methodDesign.SymbolicName.Name.EndsWith("MethodType", StringComparison.Ordinal)) {
                        continue;
                    }
                }
                var nodeModel = node.Model;
                if (nodeModel != null) {
                    var children = nodeModel.GetChildren(context);
                    if (isInAddressSpace) {
                        collection.Add(nodeModel);
                    }
                    if (nodeModel is VariableNodeModel variable &&
                        variable.TypeDefinitionId == VariableTypeIds.DataTypeDictionaryType) {
                        var references = variable.GetMatchingReferences(ReferenceTypeIds.HasComponent, true, context);

                        // // Fill type data type dictionaries - TODO: Generate on the fly
                        // string file = null;
                        // if (references.Count > 0 && references[0].TargetId == ObjectIds.XmlSchema_TypeSystem) {
                        //     file = string.Format(@"{0}\{1}.Types.xsd", filePath, _model.TargetNamespaceInfo.Prefix);
                        // }
                        // if (references.Count > 0 && references[0].TargetId == ObjectIds.OPCBinarySchema_TypeSystem) {
                        //     file = string.Format(@"{0}\{1}.Types.bsd", filePath, _model.TargetNamespaceInfo.Prefix);
                        // }
                        // if (file != null) {
                        //     try {
                        //         variable.Value = File.ReadAllBytes(file);
                        //     }
                        //     catch {
                        //         variable.Value = null;
                        //     }
                        // }
                    }
                }
            }
            return collection;
        }

        /// <inheritdoc/>
        public NodeDesign TryResolve(Namespace ns, XmlQualifiedName symbolicId) {
            if (ns == null) {
                throw new ArgumentNullException(nameof(ns));
            }
            if (symbolicId.IsNullOrEmpty()) {
                throw new ArgumentNullException(nameof(symbolicId));
            }
            //
            // We can only try and resolve if we own the namespace.
            // Otherwise we try looking through our dependencies.
            //
            if (_model.TargetNamespace == ns.Value &&
               (_model.TargetVersion == null ||
                _model.TargetVersion == ns.Version)) {

                // This will also look into our dependencies for the id
                return FindNode(symbolicId);
            }
            return null;
        }

        /// <summary>
        /// Add local node
        /// </summary>
        /// <param name="node"></param>
        private void AddNode(NodeDesign node) {
            System.Diagnostics.Debug.Assert(node.SymbolicId.Namespace == Namespace);
            if (_nodes.TryGetValue(node.SymbolicId, out var existing)) {
                _nodes[node.SymbolicId] = node;
            }
            else {
                _nodes.Add(node.SymbolicId, node);
            }
        }

        /// <summary>
        /// Finds the node by first looking in the imported node designs
        /// then in the not yet imported parts and finally using the resolver.
        /// </summary>
        /// <param name="symbolicId"></param>
        /// <param name="requiredType"></param>
        /// <param name="sourceName"></param>
        /// <returns></returns>
        private NodeDesign FindNode(XmlQualifiedName symbolicId,
            Type requiredType, string sourceName) {
            if (symbolicId.IsNullOrEmpty()) {
                throw new ArgumentNullException(nameof(symbolicId));
            }
            var target = FindNode(symbolicId);
            if (target == null) {
                throw new FormatException(
                    $"Node {symbolicId.Name} was not found for {sourceName}.");
            }
            if (!requiredType.IsInstanceOfType(target)) {
                throw new FormatException(
                    $"The node {sourceName} is not a {requiredType.Name}.");
            }
            return target;
        }

        /// <summary>
        /// Returns true is the type is a subtype of the specified type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="superType"></param>
        /// <returns></returns>
        private bool IsTypeOf(TypeDesign type, XmlQualifiedName superType) {
            if (type.SymbolicId == superType) {
                return true;
            }
            if (type.BaseType.IsNullOrEmpty()) {
                return false;
            }
            var node = FindNode(type.BaseType);
            if (node == null) {
                return false;
            }
            return IsTypeOf(node as TypeDesign, superType);
        }

        /// <summary>
        /// Find the node in the processed nodes.  If not found, find it
        /// using the resolver.
        /// </summary>
        /// <param name="symbolicId"></param>
        /// <returns></returns>
        private NodeDesign FindNode(XmlQualifiedName symbolicId) {
            if (_nodes.TryGetValue(symbolicId, out var target)) {
                return target;
            }
            if (_model.TargetNamespace == symbolicId.Namespace) {
                return _model.Items
                    .FirstOrDefault(n => n.SymbolicId == symbolicId);
            }
            // Find concrete namespace in our dependencies
            var ns = _model.Namespaces
                .FirstOrDefault(n => n.Value == symbolicId.Namespace);
            if (ns != null) {
                return _resolver?.TryResolve(ns, symbolicId);
            }
            if (_resolver != null) {
                foreach (var ns2 in _model.Namespaces) {
                    // Try to resolve in each namespace recursively
                    target = _resolver.TryResolve(ns, symbolicId);
                    if (target != null) {
                        return target;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Assign new identifier
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private object AssignIdToNode(NodeDesign node) {
            // assign identifier if one has not already been assigned.
            var id = _assigner?.TryAssignId(_model.TargetNamespaceInfo, node.SymbolicId);
            if (id == null) {
                id = node.SymbolicId.Name;
            }
            node.SetIdentifier(id);
            return id;
        }

        /// <summary>
        /// Add identifiers to the nodes
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private void AssignIds(ModelDesign model) {
            // assign identifiers.
            foreach (var node in model.Items) {
                // assign identifier if one has not already been assigned.
                var id = AssignIdToNode(node);
                if (node.Hierarchy == null) {
                    continue;
                }
                foreach (var current in node.Hierarchy.NodeList) {
                    if (string.IsNullOrEmpty(current.RelativePath)) {
                        current.Identifier = id;
                        current.Instance.SetIdentifier(id);
                        continue;
                    }
                    current.Identifier = AssignIdToNode(current.Instance);
                }
            }
        }

        // Import node design into the model

        /// <summary>
        /// Imports a node.
        /// </summary>
        private void AddNode(NodeDesign node, NodeDesign parent) {
            FixupNode(node, parent);

            // assign default values for various subtypes.
            switch (node) {
                case TypeDesign typeDesign:
                    ImportType(typeDesign);
                    break;
                case InstanceDesign instance:
                    ImportInstance(instance);
                    break;
            }

            // Mark as imported
            AddNode(node);

            // import children.
            if (node.Children != null && node.Children.Items != null) {
                node.HasChildren = true;
                var children = new List<InstanceDesign>();
                foreach (var child in node.Children.Items) {
                    // filter any children with unhandled modelling rules.
                    if (child.ModellingRuleSpecified) {
                        if (child.ModellingRule != ModellingRule.None &&
                            child.ModellingRule != ModellingRule.Mandatory &&
                            child.ModellingRule != ModellingRule.MandatoryShared &&
                            child.ModellingRule != ModellingRule.Optional &&
                            child.ModellingRule != ModellingRule.MandatoryPlaceholder &&
                            child.ModellingRule != ModellingRule.OptionalPlaceholder &&
                            child.ModellingRule != ModellingRule.ExposesItsArray) {
                            continue;
                        }
                    }
                    children.Add(child);
                    AddNode(child, node);
                }
                node.Children.Items = children.ToArray();
            }

            // import references
            if (node.References != null) {
                node.HasReferences = true;
                foreach (var reference in node.References) {
                    if (reference.TargetId.IsNullOrEmpty()) {
                        throw new FormatException(
                            $"The TargetId for a reference is not valid: {node.SymbolicId.Name}.");
                    }
                    reference.SourceNode = node;
                }
            }
        }

        /// <summary>
        /// Ensures all names and ids are set and parent / child are linked.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parent"></param>
        private void FixupNode(NodeDesign node, NodeDesign parent) {
            if (node == null) {
                return;
            }
            if (node is InstanceDesign instance) {
                // copy any symbolic name and browse name from its declaration if specified.
                if (!instance.Declaration.IsNullOrEmpty()) {
                    // Find declaration
                    var declaration = (InstanceDesign)FindNode(instance.Declaration,
                        typeof(InstanceDesign), instance.Declaration.Name);
                    if (declaration == null) {
                        // TODO: Try find in the current model but not yet imported
                    }
                    if (declaration != null) {
                        instance.SymbolicName = declaration.SymbolicName;
                        instance.BrowseName = declaration.BrowseName;
                        instance.TypeDefinition = declaration.TypeDefinition;
                    }
                }
            }

            // check for missing name or id.
            if (node.SymbolicId.IsNullOrEmpty() &&
                node.SymbolicName.IsNullOrEmpty() &&
                string.IsNullOrEmpty(node.BrowseName)) {
                throw new FormatException("The Node does not have SymbolicId, Name or a BrowseName. Parent=" +
                    (parent?.SymbolicId.Name ?? "No Parent"));
            }

            // use the browse name to assign a name.
            if (node.SymbolicName.IsNullOrEmpty()) {
                if (string.IsNullOrEmpty(node.BrowseName)) {
                    throw new FormatException($"A Node does not have SymbolicId, " +
                        $"SymbolicName or a BrowseName: {node.SymbolicId.Name}.");
                }
                // remove any non-symbol characters.
                var name = new StringBuilder();
                foreach (var c in node.BrowseName) {
                    if (char.IsWhiteSpace(c)) {
                        name.Append(NodeDesign.kPathChar);
                        continue;
                    }
                    if (char.IsLetterOrDigit(c) || c == NodeDesign.kPathChar[0]) {
                        name.Append(c);
                        continue;
                    }
                }
                var ns = _model.TargetNamespace;
                if (!node.SymbolicId.IsNullOrEmpty()) {
                    ns = node.SymbolicId.Namespace;
                }
                // create the symbolic name.
                node.SymbolicName = new XmlQualifiedName(name.ToString(), ns);
            }

            // Assign ids and names
            ValidateAndAssignBrowseName(node);
            ValidateAndAssignSymbolicId(node, parent);
            ValidateAssignedNumericId(node);

            // add a display name.
            if (node.DisplayName == null) {
                node.DisplayName = new LocalizedText {
                    Value = node.BrowseName,
                    Key = $"{node.SymbolicId.Name}_DisplayName",
                    IsAutogenerated = true
                };
            }
            else {
                if (node.DisplayName.Value != null) {
                    node.DisplayName.Value = node.DisplayName.Value.Trim();
                }
                if (string.IsNullOrEmpty(node.DisplayName.Key)) {
                    node.DisplayName.Key = $"{node.SymbolicId.Name}_DisplayName";
                }
            }

            // Fixup decription.
            if (node.Description != null) {
                if (node.Description.Value != null) {
                    node.Description.Value = node.Description.Value.Trim();
                }
                if (string.IsNullOrEmpty(node.Description.Key)) {
                    node.Description.Key = $"{node.SymbolicId.Name}_Description";
                }
            }

            // save the relationship to the parent.
            node.Parent = parent;
        }

        /// <summary>
        /// Import type design
        /// </summary>
        /// <param name="type"></param>
        private void ImportType(TypeDesign type) {
            // assign a class name.
            if (string.IsNullOrEmpty(type.ClassName)) {
                type.ClassName = type.SymbolicName.Name;

                if (type.ClassName.EndsWith("Type", StringComparison.Ordinal)) {
                    type.ClassName = type.ClassName.Substring(0, type.ClassName.Length - 4);
                }
            }
            // assign missing fields for object types.
            switch (type) {
                case ObjectTypeDesign objectType:
                    if (objectType.SymbolicId == Constants.BaseObjectType) {
                        objectType.ClassName = "ObjectSource";
                    }
                    else if (type.BaseType == null) {
                        type.BaseType = Constants.BaseObjectType;
                    }
                    if (objectType.SymbolicName != Constants.BaseObjectType) {
                        objectType.BaseTypeNode = (TypeDesign)FindNode(objectType.BaseType,
                            typeof(ObjectTypeDesign), objectType.SymbolicId.Name);
                    }
                    if (!objectType.SupportsEvents) {
                        objectType.SupportsEvents = false;
                    }
                    break;
                case VariableTypeDesign variableType:
                    if (variableType.SymbolicId == Constants.BaseDataVariableType) {
                        variableType.ClassName = "DataVariable";
                    }
                    else if (type.BaseType == null) {
                        if (type.SymbolicId != Constants.BaseVariableType) {
                            type.BaseType = Constants.BaseDataVariableType;
                        }
                    }
                    if (variableType.SymbolicName != Constants.BaseVariableType) {
                        variableType.BaseTypeNode = (TypeDesign)FindNode(variableType.BaseType,
                            typeof(VariableTypeDesign), variableType.SymbolicId.Name);

                        if (variableType.BaseTypeNode != null &&
                            (variableType.DataType == null ||
                             variableType.DataType == Constants.BaseDataType)) {
                            var baseType = (VariableTypeDesign)variableType.BaseTypeNode;
                            variableType.DataType = baseType.DataType;
                            if (!variableType.ValueRankSpecified &&
                                baseType.ValueRank != ValueRank.ScalarOrArray) {
                                variableType.ValueRank = baseType.ValueRank;
                                variableType.ValueRankSpecified = true;
                            }
                        }
                    }
                    if (variableType.DataType == null) {
                        variableType.DataType = Constants.BaseDataType;
                    }
                    if (!variableType.ValueRankSpecified) {
                        variableType.ValueRank = ValueRank.Scalar;
                    }
                    if (!variableType.AccessLevelSpecified) {
                        variableType.AccessLevel = AccessLevel.Read;
                    }
                    if (!variableType.HistorizingSpecified) {
                        variableType.Historizing = false;
                    }
                    if (!variableType.MinimumSamplingIntervalSpecified) {
                        variableType.MinimumSamplingInterval = 0;
                    }
                    break;
                case DataTypeDesign dataType:
                    if (dataType.SymbolicId == Constants.Structure) {
                        dataType.ClassName = "IEncodeable";
                    }
                    else if (type.BaseType == null) {
                        if (dataType.SymbolicId != Constants.BaseDataType) {
                            type.BaseType = Constants.BaseDataType;
                        }
                    }
                    if (dataType.SymbolicName != Constants.BaseDataType) {
                        dataType.BaseTypeNode = (TypeDesign)FindNode(dataType.BaseType,
                            typeof(DataTypeDesign), dataType.SymbolicId.Name);
                    }
                    dataType.IsStructure = dataType.BaseType == Constants.Structure;
                    dataType.IsEnumeration = dataType.BaseType ==
                        Constants.Enumeration || dataType.IsOptionSet;
                    dataType.HasFields = ImportParameters(dataType, dataType.Fields, "Field");
                    dataType.HasEncodings = ImportEncodings(dataType);
                    break;
                case ReferenceTypeDesign referenceType:
                    if (referenceType.BaseType == null) {
                        if (referenceType.SymbolicId != Constants.References) {
                            referenceType.BaseType = Constants.References;
                        }
                    }
                    // add an inverse name.
                    if (referenceType.InverseName == null) {
                        referenceType.InverseName = new LocalizedText {
                            Value = referenceType.DisplayName.Value,
                            IsAutogenerated = true
                        };
                    }
                    if (string.IsNullOrEmpty(referenceType.InverseName.Key)) {
                        referenceType.InverseName.Key = $"{referenceType.SymbolicId.Name}_InverseName";
                    }
                    if (referenceType.SymbolicName != Constants.References) {
                        referenceType.BaseTypeNode = (TypeDesign)FindNode(referenceType.BaseType,
                            typeof(ReferenceTypeDesign), referenceType.SymbolicId.Name);
                    }
                    break;
            }
        }

        /// <summary>
        /// Imports the encodings.
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        private bool ImportEncodings(DataTypeDesign dataType) {
            if (dataType.Encodings == null || dataType.Encodings.Length == 0) {
                return false;
            }
            foreach (var encoding in dataType.Encodings) {
                if (encoding.SymbolicName.IsNullOrEmpty()) {
                    throw new FormatException(
                        $"Encoding node does not have a name: {dataType.SymbolicId.Name}.");
                }
                if (encoding.Children != null && encoding.Children.Items.Length > 0) {
                    throw new FormatException(
                        $"Encoding nodes cannot have childen");
                }
                encoding.SymbolicId = new XmlQualifiedName(
                    $"{dataType.SymbolicId.Name}_Encoding_{encoding.SymbolicName.Name}",
                    dataType.SymbolicId.Namespace);
                encoding.BrowseName = encoding.SymbolicName.Name;

                // add a display name.
                if (encoding.DisplayName == null || string.IsNullOrEmpty(encoding.DisplayName.Value)) {
                    encoding.DisplayName = new LocalizedText {
                        Value = encoding.BrowseName,
                        IsAutogenerated = true
                    };
                }
                // add a description name.
                if (string.IsNullOrEmpty(encoding.Description.Value)) {
                    encoding.Description = new LocalizedText {
                        Value = $"The {encoding.SymbolicName.Name} Encoding " +
                            $"for the {dataType.SymbolicName.Name} data type.",
                        IsAutogenerated = true
                    };
                }

                ValidateAssignedNumericId(encoding);
                // add to table.
                AddNode(encoding);
            }

            return true;
        }

        /// <summary>
        /// Imports an InstanceDesign
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        private void ImportInstance(InstanceDesign instance) {
            // set the reference type.
            if (instance.ReferenceType == null) {
                if (instance is PropertyDesign) {
                    instance.ReferenceType = Constants.HasProperty;
                }
                else {
                    instance.ReferenceType = Constants.HasComponent;
                }
            }

            // set the type definition.
            if (instance.TypeDefinition == null) {
                if (instance is PropertyDesign) {
                    instance.TypeDefinition = Constants.PropertyType;
                }
                else if (instance is VariableDesign) {
                    instance.TypeDefinition = Constants.BaseDataVariableType;
                }
                else if (instance is ObjectDesign) {
                    instance.TypeDefinition = Constants.BaseObjectType;
                }
            }
            if (!instance.ModellingRuleSpecified) {
                instance.ModellingRule = ModellingRule.Mandatory;
            }

            // assign missing fields for objects.
            switch (instance) {
                case ObjectDesign objectd:
                    if (!objectd.SupportsEventsSpecified) {
                        objectd.SupportsEvents = false;
                    }

                    break;
                case VariableDesign variable:
                    if (variable.DataType == null) {
                        variable.DataType = Constants.BaseDataType;
                    }
                    if (!variable.ValueRankSpecified) {
                        variable.ValueRank = ValueRank.Scalar;
                    }
                    if (!variable.AccessLevelSpecified) {
                        variable.AccessLevel = AccessLevel.Read;
                    }
                    if (!variable.MinimumSamplingIntervalSpecified) {
                        variable.MinimumSamplingInterval = 0;
                    }
                    if (!variable.HistorizingSpecified) {
                        variable.Historizing = false;
                    }
                    break;
                case MethodDesign method:
                    method.HasArguments = false;
                    if (!method.NonExecutableSpecified) {
                        method.NonExecutableSpecified = false;
                    }
                    if (ImportParameters(method, method.InputArguments, "InputArgument")) {
                        method.HasArguments = true;
                    }
                    if (ImportParameters(method, method.OutputArguments, "OutputArgument")) {
                        method.HasArguments = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// Creates a property placeholder for the arguments of a method.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private PropertyDesign CreateArgumentProperty(MethodDesign method, string type) {
            var property = new PropertyDesign {
                Parent = method,
                ReferenceType = Constants.HasProperty,
                TypeDefinition = Constants.PropertyType,
                SymbolicId = method.GetSymbolicIdForChild(type),
                SymbolicName = new XmlQualifiedName(type, Namespaces.OpcUa)
            };

            // use the name to assign a browse name.
            ValidateAndAssignBrowseName(property);

            property.AccessLevel = AccessLevel.Read;
            property.ValueRank = ValueRank.Array;
            property.DataType = Constants.Argument;
            property.DecodedValue = null;
            property.DefaultValue = null;
            property.Description = new LocalizedText {
                Value = string.Format("The {1} for the {0} method.", method.SymbolicName.Name, type),
                Key = string.Format("{0}_{1}_Description", property.SymbolicId.Name, type),
                IsAutogenerated = true
            };
            property.DisplayName = new LocalizedText {
                Value = property.BrowseName,
                Key = string.Format("{0}_{1}_DisplayName", property.SymbolicId.Name, type),
                IsAutogenerated = true
            };
            property.Historizing = false;
            property.MinimumSamplingInterval = 0;
            property.ModellingRule = ModellingRule.MandatoryShared;
            property.WriteAccess = 0;
            property.DataTypeNode = (DataTypeDesign)FindNode(property.DataType,
                typeof(DataTypeDesign), method.SymbolicId.Name);
            property.TypeDefinitionNode = (VariableTypeDesign)FindNode(property.TypeDefinition,
                typeof(VariableTypeDesign), method.SymbolicId.Name);

            AddNode(property);
            return property;
        }

        /// <summary>
        /// Imports a list of parameters.
        /// </summary>
        private bool ImportParameters(NodeDesign node, Parameter[] parameters, string parameterType) {
            if (parameters == null || parameters.Length == 0) {
                return false;
            }

            var id = 0;
            foreach (var parameter in parameters) {
                parameter.Parent = node;

                // check name.
                if (string.IsNullOrEmpty(parameter.Name)) {
                    throw new FormatException(
                        $"The node has a parameter without a name: {node.SymbolicId.Name}.");
                }

                var name = parameter.Name;
                // assign an id.
                if (parameter.IdentifierSpecified) {
                    id = parameter.Identifier;
                }
                else if (!string.IsNullOrEmpty(parameter.BitMask)) {

                    if (ulong.TryParse(parameter.BitMask, NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture, out var mask)) {
                        var bytes = BitConverter.GetBytes(mask);
                        parameter.Identifier = BitConverter.ToInt32(bytes, 0);
                        parameter.IdentifierSpecified = true;
                    }
                }

                if (!parameter.IdentifierSpecified) {
                    parameter.Identifier = ++id;
                    parameter.IdentifierSpecified = true;
                }

                // update id if specified in name.
                var index = name.LastIndexOf(NodeDesign.kPathChar[0]);

                if (index != -1) {
                    foreach (var c in name) {
                        if (!char.IsDigit(c)) {
                            index = -1;
                            break;
                        }
                    }
                    if (index != -1) {
                        id = parameter.Identifier = Convert.ToInt32(name.Substring(index + 1));
                        parameter.IdentifierInName = true;
                    }
                }

                // add a description.
                if (parameter.Description == null) {
                    parameter.Description = new LocalizedText {
                        Value = string.Format("A description for the {0} {1}.",
                        parameter.Name, parameterType.ToLower()),
                        IsAutogenerated = true
                    };
                }

                if (string.IsNullOrEmpty(parameter.Description.Key)) {
                    parameter.Description.Key = string.Format("{0}_{1}_{2}_Description",
                        node.SymbolicId.Name, parameterType, parameter.Name);
                }

                // add a datatype.
                if (parameter.DataType.IsNullOrEmpty()) {
                    parameter.DataType = Constants.BaseDataType;
                }
            }
            return true;
        }

        // 2nd pass - validate and expand node design

        /// <summary>
        /// Creates instance nodes out of standard components such as
        /// encodings or method args.
        /// </summary>
        /// <param name="model"></param>
        private void ValidateNodes(ModelDesign model) {
            var hasDataTypesDefined = false;
            var hasMethodsDefined = false;

            // Validate each node in the model
            foreach (var node in model.Items) {
                if (node is DataTypeDesign) {
                    hasDataTypesDefined = true;
                }
                if (node is MethodDesign) {
                    hasMethodsDefined = true;
                }
                Validate(node);
            }

            // Add method argument property children
            if (hasMethodsDefined) {
                foreach (var node in model.Items) {
                    if (node is MethodDesign methodDesign) {
                        AddMethodArguments(methodDesign);
                    }
                }
            }

            // Add additional nodes to the model
            var nodes = new List<NodeDesign>(model.Items);
            if (hasDataTypesDefined) {

                AddDataTypeDictionaryNodes(model, model.TargetNamespaceInfo,
                    EncodingType.Binary, nodes);
                AddDataTypeDictionaryNodes(model, model.TargetNamespaceInfo,
                    EncodingType.Xml, nodes);
                AddDataTypeDictionaryNodes(model, model.TargetNamespaceInfo,
                    EncodingType.Json, nodes);

                foreach (var node in model.Items) {
                    if (node is DataTypeDesign dataTypeDesign) {
                        AddEnumStrings(dataTypeDesign);
                    }
                }
            }
            AddTypesFolder(model, model.TargetNamespaceInfo, nodes);
        }

        /// <summary>
        /// Validates a node.
        /// </summary>
        private void Validate(NodeDesign node) {
            if (node.IsDeclaration()) {
                return;
            }
            switch (node) {
                case TypeDesign typeDesign:
                    ValidateType(typeDesign);
                    break;
                case InstanceDesign instanceDesign:
                    ValidateInstance(instanceDesign);
                    break;
            }
            if (node.HasChildren) {
                foreach (NodeDesign child in node.Children.Items) {
                    Validate(child);
                }
            }
            if (node.HasReferences) {
                foreach (var reference in node.References) {
                    ValidateReference(reference);
                }
            }
        }

        /// <summary>
        /// Validates the type.
        /// </summary>
        private void ValidateType(TypeDesign type) {
            switch (type) {
                case VariableTypeDesign variableType:
                    variableType.DataTypeNode = (DataTypeDesign)FindNode(variableType.DataType,
                        typeof(DataTypeDesign), type.SymbolicId.Name);

                    // TODO: Is this DecodeValue=
                    if (variableType.DefaultValue != null) {
                        var decoder = new XmlDecoder(variableType.DefaultValue, _context);
                        variableType.DecodedValue = decoder.ReadVariantContents(out var typeInfo);
                        if (typeInfo != null) {
                            variableType.ValueRank = (typeInfo.ValueRank == ValueRanks.Scalar) ?
                                ValueRank.Scalar : ValueRank.Array;
                            variableType.ValueRankSpecified = true;
                        }
                        decoder.Close();
                    }

                    if (variableType.BaseTypeNode != null) {
                        var baseType = variableType.BaseTypeNode as VariableTypeDesign;
                        if (baseType.DataType != Constants.BaseDataType) {
                            if (variableType.DataType == Constants.BaseDataType) {
                                variableType.DataType = baseType.DataType;
                                variableType.DataTypeNode = baseType.DataTypeNode;
                            }
                            if (baseType.DataType != variableType.DataType) {
                                throw new FormatException(
                                    $"Subtype cannot redefine the datatype. {type.SymbolicId.Name}");
                            }
                        }
                    }
                    break;
                case DataTypeDesign dataType:
                    ValidateParameters(dataType, dataType.Fields);
                    dataType.IsStructure =
                        IsTypeOf(dataType, Constants.Structure);
                    dataType.IsUnion =
                        IsTypeOf(dataType, Constants.Union);
                    dataType.IsEnumeration =
                        IsTypeOf(dataType, Constants.Enumeration) || dataType.IsOptionSet;
                    dataType.BasicDataType =
                        GetBasicDataType(dataType);
                    if (!dataType.IsStructure) {
                        if (dataType.HasEncodings) {
                            throw new FormatException(
                                $"Encodings but not a structure: {type.SymbolicId.Name}");
                        }
                        if (dataType.IsEnumeration) {
                            if (!dataType.HasFields && !dataType.IsAbstract) {
                                throw new FormatException(
                                    $"Enumeration type with no fields: {type.SymbolicId.Name}");
                            }
                        }
                        else {
                            if (dataType.HasFields && !dataType.IsOptionSet) {
                                throw new FormatException(
                                    $"Simple type with fields defined: {type.SymbolicId.Name}");
                            }
                        }
                    }
                    else {
                        // add encodings.
                        if (!dataType.HasEncodings) {
                            dataType.Encodings = new EncodingDesign[] {
                                CreateEncoding(dataType, Constants.DefaultXml),
                                CreateEncoding(dataType, Constants.DefaultBinary),
                                CreateEncoding(dataType, Constants.DefaultJson)
                            };
                            dataType.HasEncodings = true;
                        }
                        else {
                            // check for duplicates.
                            var encodings = new Dictionary<XmlQualifiedName, EncodingDesign>();
                            foreach (var encoding in dataType.Encodings) {
                                if (encodings.ContainsKey(encoding.SymbolicName)) {
                                    throw new FormatException("The datatype has a duplicate encoding " +
                                        $"defined: {dataType.SymbolicId.Name} {encoding.SymbolicName.Name}");
                                }
                                encodings.Add(encoding.SymbolicName, encoding);
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Determines the basic type for a datatype.
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        private static BasicDataType GetBasicDataType(DataTypeDesign dataType) {
            if (dataType == null) {
                return BasicDataType.BaseDataType;
            }
            // check if it is a built in data type.
            if (dataType.SymbolicName.Namespace == Namespaces.OpcUa) {
                foreach (var name in Enum.GetNames(typeof(BasicDataType))) {
                    if (name == dataType.SymbolicName.Name) {
                        return (BasicDataType)Enum.Parse(typeof(BasicDataType),
                            dataType.SymbolicName.Name);
                    }
                }
            }
            if (dataType.IsOptionSet) {
                return BasicDataType.Enumeration;
            }
            // recursively search hierarchy if conversion to enum fails.
            var basicType = GetBasicDataType(dataType.BaseTypeNode as DataTypeDesign);
            // data type is user defined if a sub-type of structure.
            if (basicType == BasicDataType.Structure) {
                return BasicDataType.UserDefined;
            }
            return basicType;
        }

        /// <summary>
        /// Create the encoding for the data type
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="encodingName"></param>
        /// <returns></returns>
        private EncodingDesign CreateEncoding(DataTypeDesign dataType, XmlQualifiedName encodingName) {
            var symbolicId = new XmlQualifiedName(
                $"{dataType.SymbolicId.Name}_Encoding_{encodingName.Name}",
                dataType.SymbolicId.Namespace);

            var encoding = new EncodingDesign {
                SymbolicName = encodingName,
                SymbolicId = symbolicId,
                ReleaseStatus = dataType.ReleaseStatus,
                Purpose = dataType.Purpose
            };

            if (_nodes.TryGetValue(symbolicId, out var target)) {
                encoding.NumericId = target.NumericId;
                encoding.NumericIdSpecified = target.NumericIdSpecified;
                _nodes.Remove(symbolicId);
            }

            // use the name to assign a browse name.
            ValidateAndAssignBrowseName(encoding);

            encoding.TypeDefinition = Constants.DataTypeEncodingType;
            encoding.Parent = dataType;
            encoding.TypeDefinitionNode = (ObjectTypeDesign)FindNode(encoding.TypeDefinition,
                typeof(ObjectTypeDesign), encoding.SymbolicId.Name);

            // add a display name.
            if (encoding.DisplayName == null || string.IsNullOrEmpty(encoding.DisplayName.Value)) {
                encoding.DisplayName = new LocalizedText {
                    Value = encoding.BrowseName,
                    IsAutogenerated = true
                };
            }
            // add a description name.
            if (encoding.Description == null || string.IsNullOrEmpty(encoding.Description.Value)) {
                encoding.Description = new LocalizedText {
                    Value = $"The {encoding.SymbolicName.Name} Encoding for " +
                        $"the {dataType.SymbolicName.Name} data type.",
                    IsAutogenerated = true
                };
            }
            AddNode(encoding);
            return encoding;
        }

        /// <summary>
        /// Imports an InstanceDesign
        /// </summary>
        private void ValidateInstance(InstanceDesign instance) {
            // set the reference type. /// TODO
            if (instance.ReferenceType == null) {
                if (instance.Parent != null) {
                    var referenceType = (ReferenceTypeDesign)FindNode(instance.ReferenceType,
                        typeof(ReferenceTypeDesign), instance.SymbolicId.Name);
                }
            }

            // assign missing fields for object.
            switch (instance) {
                case ObjectDesign objectd:
                    objectd.TypeDefinitionNode = (TypeDesign)FindNode(instance.TypeDefinition,
                        typeof(ObjectTypeDesign), instance.SymbolicId.Name);
                    break;
                case VariableDesign variable:
                    if (variable is PropertyDesign && variable.HasChildren) {
                        throw new FormatException(
                            $"The Property ({variable.SymbolicId.Name}) has children defined.");
                    }

                    variable.TypeDefinitionNode = (TypeDesign)FindNode(instance.TypeDefinition,
                        typeof(VariableTypeDesign), instance.SymbolicId.Name);
                    if (variable.TypeDefinitionNode != null) {
                        if (variable.DataType == null ||
                            variable.DataType == Constants.BaseDataType) {
                            variable.DataType = ((VariableTypeDesign)variable.TypeDefinitionNode).DataType;

                            var valueRank = ((VariableTypeDesign)variable.TypeDefinitionNode).ValueRank;
                            if (!variable.ValueRankSpecified && valueRank != ValueRank.ScalarOrArray) {
                                variable.ValueRank = valueRank;
                                variable.ValueRankSpecified = true;
                            }
                        }
                    }
                    variable.DataTypeNode = (DataTypeDesign)FindNode(variable.DataType,
                        typeof(DataTypeDesign), instance.SymbolicId.Name);
                    if (variable.DefaultValue != null) {
                        var decoder = new XmlDecoder(variable.DefaultValue, _context);
                        variable.DecodedValue = decoder.ReadVariantContents(out var typeInfo);
                        if (typeInfo != null) {
                            variable.ValueRank = (typeInfo.ValueRank == ValueRanks.Scalar) ?
                                ValueRank.Scalar : ValueRank.Array;
                            variable.ValueRankSpecified = true;
                        }
                        decoder.Close();
                    }
                    break;
                case MethodDesign method:
                    if (instance.TypeDefinition != null) {
                        method.MethodType = (MethodDesign)FindNode(instance.TypeDefinition,
                            typeof(MethodDesign), instance.SymbolicId.Name);
                        method.Description = method.MethodType.Description;
                        method.InputArguments = method.MethodType.InputArguments;
                        method.OutputArguments = method.MethodType.OutputArguments;
                        method.HasArguments =
                            (method.InputArguments != null && method.InputArguments.Length > 0) ||
                            (method.OutputArguments != null && method.OutputArguments.Length > 0);
                    }

                    ValidateParameters(method, method.InputArguments);
                    ValidateParameters(method, method.OutputArguments);
                    if (method.Parent == null) {
                        break; // Global method
                    }
                    var children = new List<InstanceDesign>();
                    if (method.Children != null && method.Children.Items != null) {
                        children.AddRange(method.Children.Items);
                    }
                    if (method.InputArguments != null) {
                        children.Add(CreateArgumentProperty(method, "InputArguments"));
                    }
                    if (method.OutputArguments != null) {
                        children.Add(CreateArgumentProperty(method, "OutputArguments"));
                    }
                    if (children.Count > 0) {
                        method.Children = new ListOfChildren {
                            Items = children.ToArray()
                        };
                        method.HasChildren = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// Validates a list of parameters
        /// </summary>
        private void ValidateParameters(NodeDesign node, Parameter[] parameters) {
            if (parameters == null) {
                return;
            }
            foreach (var parameter in parameters) {
                parameter.DataTypeNode = (DataTypeDesign)FindNode(parameter.DataType,
                    typeof(DataTypeDesign), node.SymbolicId.Name);
            }
        }

        /// <summary>
        /// Validates an Reference.
        /// </summary>
        private void ValidateReference(Reference reference) {
            var referenceType = (ReferenceTypeDesign)FindNode(reference.ReferenceType,
                typeof(ReferenceTypeDesign), reference.SourceNode.SymbolicId.Name);
            reference.TargetNode = FindNode(reference.TargetId, typeof(NodeDesign),
                reference.SourceNode.SymbolicId.Name);
        }

        /// <summary>
        /// Adds the method arguments as children.
        /// </summary>
        /// <param name="method"></param>
        private void AddMethodArguments(MethodDesign method) {
            var children = new List<InstanceDesign>();
            if (method.Children != null && method.Children.Items != null) {
                children.AddRange(method.Children.Items);
            }

            // Add input
            if (method.InputArguments != null && method.InputArguments.Length > 0) {
                var arguments = new List<Argument>();
                foreach (var parameter in method.InputArguments) {
                    var (valueRank, dimensions) = parameter.ValueRank.ToStackValue(
                        parameter.ArrayDimensions);
                    var argument = new Argument {
                        Name = parameter.Name,
                        DataType = new NodeId(parameter.DataType.ToString()),
                        ValueRank = valueRank,
                        ArrayDimensions = dimensions,
                        Description = null
                    };
                    if (!parameter.Description.IsAutogenerated) {
                        argument.Description = new Ua.LocalizedText(parameter.Description.Key,
                            string.Empty, parameter.Description.Value);
                    }
                    arguments.Add(argument);
                }
                AddProperty(method, Constants.InputArguments, Constants.Argument, ValueRank.Array,
                    arguments.ToArray(), children);
            }

            // Add output
            if (method.OutputArguments != null && method.OutputArguments.Length > 0) {
                var arguments = new List<Argument>();
                foreach (var parameter in method.OutputArguments) {
                    var (valueRank, dimensions) = parameter.ValueRank.ToStackValue(
                        parameter.ArrayDimensions);
                    var argument = new Argument {
                        Name = parameter.Name,
                        DataType = new NodeId(parameter.DataType.ToString()),
                        ValueRank = valueRank,
                        ArrayDimensions = dimensions,
                        Description = null
                    };
                    if (!parameter.Description.IsAutogenerated) {
                        argument.Description = new Ua.LocalizedText(parameter.Description.Key,
                            string.Empty, parameter.Description.Value);
                    }
                    arguments.Add(argument);
                }
                AddProperty(method, Constants.OutputArguments, Constants.Argument, ValueRank.Array,
                    arguments.ToArray(), children);
            }
            method.Children = new ListOfChildren {
                Items = children.ToArray()
            };
        }

        /// <summary>
        /// Add enumeration strings
        /// </summary>
        /// <param name="dataType"></param>
        private void AddEnumStrings(DataTypeDesign dataType) {
            var children = new List<InstanceDesign>();

            if (dataType.Children != null && dataType.Children.Items != null) {
                children.AddRange(dataType.Children.Items);
            }
            if (!dataType.IsEnumeration || dataType.Fields == null || dataType.Fields.Length == 0) {
                return;
            }
            if (dataType.IsOptionSet) {
                var values = new List<Ua.LocalizedText>();
                var last = 0;
                for (var index = 0; index < 32; index++) {
                    var hit = 1 << index;
                    foreach (var parameter in dataType.Fields) {
                        if (parameter.Identifier == hit) {
                            while (last++ < index) {
                                values.Add(new Ua.LocalizedText(string.Empty, "Reserved"));
                            }
                            values.Add(new Ua.LocalizedText(string.Empty, parameter.Name));
                            last = index + 1;
                            break;
                        }
                    }
                }
                AddProperty(dataType, Constants.OptionSetValues, Constants.LocalizedText, ValueRank.Array,
                    values.ToArray(), children);
            }
            else {
                var index = 0;
                var sequential = true;
                foreach (var parameter in dataType.Fields) {
                    if (parameter.Identifier != index) {
                        sequential = false;
                        break;
                    }
                    index++;
                }
                if (sequential) {
                    var values = new List<Ua.LocalizedText>();
                    foreach (var parameter in dataType.Fields) {
                        values.Add(new Ua.LocalizedText(string.Empty, parameter.Name));
                    }
                    AddProperty(dataType, Constants.EnumStrings,
                        Constants.LocalizedText, ValueRank.Array,
                        values.ToArray(), children);
                }
                else {
                    var values = new List<EnumValueType>();
                    foreach (var parameter in dataType.Fields) {
                        var value = new EnumValueType {
                            DisplayName = new Ua.LocalizedText(string.Empty, parameter.Name),
                            Value = parameter.Identifier
                        };
                        if (!parameter.Description.IsAutogenerated) {
                            value.Description = new Ua.LocalizedText(parameter.Description.Key,
                                string.Empty, parameter.Description.Value);
                        }
                        values.Add(value);
                    }
                    AddProperty(dataType, Constants.EnumValues, Constants.EnumValueType, ValueRank.Array,
                        values.ToArray(), children);
                }
            }
            dataType.Children = new ListOfChildren {
                Items = children.ToArray()
            };
        }

        /// <summary>
        /// Adds the folders to organize the types used in the namespace.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="ns"></param>
        /// <param name="nodesToAdd"></param>
        private void AddTypesFolder(ModelDesign nodes, Namespace ns, IList<NodeDesign> nodesToAdd) {
            foreach (var node in nodes.Items) {
                ObjectDesign folder = null;
                if (node is ObjectTypeDesign) {
                    folder = GetOrAddTypesFolder(ns, node, NodeClass.ObjectType, nodesToAdd);
                }
                if (node is VariableTypeDesign) {
                    folder = GetOrAddTypesFolder(ns, node, NodeClass.VariableType, nodesToAdd);
                }
                if (node is DataTypeDesign) {
                    folder = GetOrAddTypesFolder(ns, node, NodeClass.DataType, nodesToAdd);
                }
                if (node is ReferenceTypeDesign) {
                    folder = GetOrAddTypesFolder(ns, node, NodeClass.ReferenceType, nodesToAdd);
                }
                if (folder != null) {
                    var references = new List<Reference>();
                    if (node.References != null) {
                        references.AddRange(node.References);
                    }
                    var reference = new Reference {
                        ReferenceType = Constants.Organizes,
                        IsInverse = true,
                        IsOneWay = false,
                        TargetId = folder.SymbolicId
                    };
                    references.Add(reference);
                    node.References = references.ToArray();
                    node.HasReferences = true;
                }
            }
        }

        /// <summary>
        /// Find type folder and if not exist add it.
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="node"></param>
        /// <param name="nodeClass"></param>
        /// <param name="nodesToAdd"></param>
        /// <returns></returns>
        private ObjectDesign GetOrAddTypesFolder(Namespace ns, NodeDesign node,
            NodeClass nodeClass, IList<NodeDesign> nodesToAdd) {
            var folderName = ns.Prefix;

            var isEvent = false;
            if (node is ObjectTypeDesign objectType) {
                while (objectType != null) {
                    if (objectType.SymbolicId == Constants.BaseEventType) {
                        isEvent = true;
                        break;
                    }
                    objectType = objectType.BaseTypeNode as ObjectTypeDesign;
                }
            }
            var name = isEvent ? "EventTypes" : nodeClass.ToString() + "s";
            var folderId = new XmlQualifiedName(
                NodeDesignEx.CreateSymbolicId(name, folderName), ns.Value);
            var target = FindNode(folderId);
            if (target != null) {
                // Folder already exists
                return target as ObjectDesign;
            }

            var reference = new Reference {
                ReferenceType = Constants.Organizes,
                IsInverse = true,
                IsOneWay = false
            };
            if (isEvent) {
                reference.TargetId = Constants.EventTypesFolder;
            }
            else {
                reference.TargetId = new XmlQualifiedName(
                    nodeClass.ToString() + "sFolder", Namespaces.OpcUa);
            }
            var folder = new ObjectDesign {
                SymbolicId = folderId,
                SymbolicName = folderId,
                BrowseName = folderName,
                DisplayName = new LocalizedText {
                    IsAutogenerated = false,
                    Value = ns.Prefix
                },
                Description = new LocalizedText {
                    IsAutogenerated = true,
                    Value = folderName
                },
                WriteAccess = 0,
                TypeDefinition = Constants.FolderType,
                ModellingRule = ModellingRule.None,
                ModellingRuleSpecified = true,
                References = new Reference[] { reference },
                HasReferences = true,
                TypeDefinitionNode = (ObjectTypeDesign)FindNode(Constants.FolderType,
                    typeof(ObjectTypeDesign), folderId.Name)
            };
            AddNode(folder);
            nodesToAdd.Add(folder);
            return folder;
        }

        /// <summary>
        /// Add data type dictionary
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="ns"></param>
        /// <param name="encodingType"></param>
        /// <param name="nodesToAdd"></param>
        private void AddDataTypeDictionaryNodes(ModelDesign nodes, Namespace ns,
            EncodingType encodingType, IList<NodeDesign> nodesToAdd) {
            DictionaryDesign dictionary = null;
            var descriptions = new List<InstanceDesign>();
            if (encodingType != EncodingType.Json) {
                var namespaceUri = ns.Value;
                var isXml = encodingType == EncodingType.Xml;
                if (isXml && !string.IsNullOrEmpty(ns.XmlNamespace)) {
                    namespaceUri = ns.XmlNamespace;
                }
                var id = new XmlQualifiedName(NodeDesignEx.CreateSymbolicId(ns.Name,
                    isXml ? "XmlSchema" : "BinarySchema"), ns.Value);
                var typeDefinition = Constants.DataTypeDictionaryType;
                var reference = new Reference {
                    ReferenceType = Constants.HasComponent,
                    IsInverse = true,
                    IsOneWay = false,
                    TargetId = new XmlQualifiedName(isXml ? "XmlSchema_TypeSystem" :
                        "OPCBinarySchema_TypeSystem", Namespaces.OpcUa)
                };
                dictionary = new DictionaryDesign {
                    SymbolicId = id,
                    SymbolicName = id,
                    BrowseName = ns.Prefix,
                    DisplayName = new LocalizedText {
                        IsAutogenerated = true,
                        Value = ns.Prefix
                    },
                    Description = new LocalizedText {
                        IsAutogenerated = true,
                        Value = ns.Prefix
                    },
                    WriteAccess = 0,
                    TypeDefinition = typeDefinition,
                    TypeDefinitionNode = (VariableTypeDesign)FindNode(
                        typeDefinition, typeof(VariableTypeDesign), id.Name),
                    DataType = Constants.ByteString,
                    DataTypeNode = (DataTypeDesign)FindNode(
                        Constants.ByteString, typeof(DataTypeDesign), id.Name),
                    ValueRank = ValueRank.Scalar,
                    ValueRankSpecified = true,
                    ArrayDimensions = null,
                    AccessLevel = AccessLevel.Read,
                    AccessLevelSpecified = true,
                    MinimumSamplingInterval = 0,
                    MinimumSamplingIntervalSpecified = true,
                    Historizing = false,
                    HistorizingSpecified = true,
                    References = new Reference[] { reference }
                };
                AddProperty(dictionary, Constants.NamespaceUri, Constants.String, ValueRank.Scalar,
                    namespaceUri, descriptions);
                AddProperty(dictionary, Constants.Deprecated, Constants.Boolean, ValueRank.Scalar,
                    true, descriptions);
            }
            foreach (var node in nodes.Items) {
                if (!(node is DataTypeDesign dataType)) {
                    continue;
                }
                if (dataType.BasicDataType == BasicDataType.UserDefined) {
                    AddDataTypeDescription(dataType, dictionary, descriptions,
                        encodingType, nodesToAdd);
                    continue;
                }
            }
            if (dictionary != null) {
                dictionary.Children = new ListOfChildren {
                    Items = descriptions.ToArray()
                };
                AddNode(dictionary);
                nodesToAdd.Add(dictionary);
            }
        }

        /// <summary>
        /// Add property
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="propertyName"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        /// <param name="value"></param>
        /// <param name="children"></param>
        private void AddProperty(NodeDesign parent, XmlQualifiedName propertyName,
            XmlQualifiedName dataType, ValueRank valueRank, object value,
            IList<InstanceDesign> children) {
            var id = parent.GetSymbolicIdForChild(propertyName.Name);
            var typeDefinition = Constants.PropertyType;
            var property = new PropertyDesign {
                Parent = parent,
                ReferenceType = Constants.HasProperty,
                ModellingRule = ModellingRule.Mandatory,
                ModellingRuleSpecified = true,
                SymbolicId = id,
                SymbolicName = propertyName,
                BrowseName = propertyName.Name,
                DisplayName = new LocalizedText {
                    IsAutogenerated = true,
                    Value = propertyName.Name
                },
                Description = new LocalizedText {
                    IsAutogenerated = true,
                    Value = propertyName.Name
                },
                WriteAccess = 0,
                TypeDefinition = typeDefinition,
                TypeDefinitionNode = (VariableTypeDesign)FindNode(
                    typeDefinition, typeof(VariableTypeDesign), id.Name),
                DataType = dataType,
                DataTypeNode = (DataTypeDesign)FindNode(
                    dataType, typeof(DataTypeDesign), id.Name),
                ValueRank = valueRank,
                ValueRankSpecified = true,
                ArrayDimensions = null,
                AccessLevel = AccessLevel.Read,
                AccessLevelSpecified = true,
                MinimumSamplingInterval = 0,
                MinimumSamplingIntervalSpecified = true,
                Historizing = false,
                HistorizingSpecified = true,
                DecodedValue = value
            };
            children.Add(property);
            AddNode(property);
        }

        /// <summary>
        /// Add data type description to node
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="dictionary"></param>
        /// <param name="descriptions"></param>
        /// <param name="encodingType"></param>
        /// <param name="nodesToAdd"></param>
        private void AddDataTypeDescription(DataTypeDesign dataType, DictionaryDesign dictionary,
            IList<InstanceDesign> descriptions, EncodingType encodingType,
            IList<NodeDesign> nodesToAdd) {
            VariableDesign description = null;
            if (encodingType != EncodingType.Json && !dataType.NotInAddressSpace) {

                var id = dictionary.GetSymbolicIdForChild(dataType.SymbolicId.Name);
                var typeDefinition = Constants.DataTypeDescriptionType;
                var type = Constants.String;
                description = new VariableDesign {
                    Parent = dictionary,
                    ReferenceType = Constants.HasComponent,
                    ModellingRule = ModellingRule.Mandatory,
                    ModellingRuleSpecified = true,
                    SymbolicId = id,
                    SymbolicName = new XmlQualifiedName(
                        dataType.SymbolicId.Name, dictionary.SymbolicId.Namespace),
                    BrowseName = dataType.BrowseName,
                    DisplayName = new LocalizedText {
                        IsAutogenerated = true,
                        Value = dataType.BrowseName
                    },
                    Description = new LocalizedText {
                        IsAutogenerated = true,
                        Value = dataType.BrowseName
                    },
                    WriteAccess = 0,
                    TypeDefinition = typeDefinition,
                    TypeDefinitionNode = (VariableTypeDesign)FindNode(
                        typeDefinition, typeof(VariableTypeDesign), id.Name),
                    DataType = type,
                    DataTypeNode = (DataTypeDesign)FindNode(
                        type, typeof(DataTypeDesign), id.Name),
                    ValueRank = ValueRank.Scalar,
                    ValueRankSpecified = true,
                    ArrayDimensions = null,
                    AccessLevel = AccessLevel.Read,
                    AccessLevelSpecified = true,
                    MinimumSamplingInterval = 0,
                    MinimumSamplingIntervalSpecified = true,
                    Historizing = false,
                    HistorizingSpecified = true,
                    PartNo = dataType.PartNo,
                    NotInAddressSpace = dataType.NotInAddressSpace,
                    ReleaseStatus = dataType.ReleaseStatus,
                    Purpose = dataType.Purpose,
                    DecodedValue = encodingType == EncodingType.Xml ?
                        $"//xs:element[@name='{dataType.SymbolicName.Name}']" :
                        dataType.SymbolicName.Name
                };
                descriptions.Add(description);
                AddNode(description);
            }
            if (dataType.BasicDataType == BasicDataType.UserDefined) {
                AddDataTypeEncoding(dataType, description, encodingType, nodesToAdd);
            }
        }

        /// <summary>
        /// Add data type encoding
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="description"></param>
        /// <param name="encodingType"></param>
        /// <param name="nodesToAdd"></param>
        private void AddDataTypeEncoding(DataTypeDesign dataType, VariableDesign description,
            EncodingType encodingType, IList<NodeDesign> nodesToAdd) {
            var encoding = new ObjectDesign {
                Parent = null,
                ReferenceType = null
            };
            switch (encodingType) {
                case EncodingType.Xml:
                    encoding.SymbolicId = new XmlQualifiedName(dataType.SymbolicId.Name +
                        "_Encoding_DefaultXml", dataType.SymbolicId.Namespace);
                    encoding.SymbolicName = Constants.DefaultXml;
                    encoding.BrowseName = "Default XML";
                    break;
                case EncodingType.Json:
                    encoding.SymbolicId = new XmlQualifiedName(dataType.SymbolicId.Name +
                        "_Encoding_DefaultJson", dataType.SymbolicId.Namespace);
                    encoding.SymbolicName = Constants.DefaultJson;
                    encoding.BrowseName = "Default JSON";
                    break;
                default:
                    encoding.SymbolicId = new XmlQualifiedName(dataType.SymbolicId.Name +
                        "_Encoding_DefaultBinary", dataType.SymbolicId.Namespace);
                    encoding.SymbolicName = Constants.DefaultBinary;
                    encoding.BrowseName = "Default Binary";
                    break;
            }
            encoding.DisplayName = new LocalizedText {
                IsAutogenerated = true,
                Value = encoding.BrowseName
            };
            encoding.Description = new LocalizedText {
                IsAutogenerated = true,
                Value = encoding.BrowseName
            };
            encoding.WriteAccess = 0;
            encoding.TypeDefinition =
                Constants.DataTypeEncodingType;
            encoding.TypeDefinitionNode = (ObjectTypeDesign)FindNode(
                encoding.TypeDefinition, typeof(ObjectTypeDesign), encoding.SymbolicId.Name);
            encoding.SupportsEvents = false;
            encoding.SupportsEventsSpecified = true;
            encoding.PartNo = dataType.PartNo;
            encoding.NotInAddressSpace = dataType.NotInAddressSpace;
            encoding.Category = dataType.Category;
            encoding.ReleaseStatus = dataType.ReleaseStatus;
            encoding.Purpose = dataType.Purpose;
            encoding.Parent = dataType;
            var hasEncoding = new Reference {
                ReferenceType = Constants.HasEncoding,
                IsInverse = true,
                IsOneWay = false,
                TargetId = dataType.SymbolicId,
                TargetNode = dataType
            };
            if (description != null && !dataType.NotInAddressSpace) {
                var hasDescription = new Reference {
                    ReferenceType = Constants.HasDescription,
                    IsInverse = false,
                    IsOneWay = false,
                    TargetId = description.SymbolicId,
                    TargetNode = description
                };
                encoding.References = new Reference[] { hasEncoding, hasDescription };
            }
            else {
                encoding.References = new Reference[] { hasEncoding };
            }
            AddNode(encoding);
            nodesToAdd.Add(encoding);
        }


        // 3rd pass - create instance hierarchy

        private Hierarchy CreateInstanceHierarchy(NodeDesign root) {
            SetOverriddenNodes(root);

            // collect all of the nodes that define the hierachy.
            var nodes = new List<HierarchyNode>();
            var references = new List<HierarchyReference>();
            BuildInstanceHierarchy(root, nodes, references);

            var hierarchy = new Hierarchy {
                References = references
            };

            var rootId = root.SymbolicId;

            // add root node.
            var instance = root as InstanceDesign;

            if (instance == null) {
                rootId = new XmlQualifiedName(root.SymbolicId.Name + "Instance",
                    root.SymbolicId.Namespace);
            }

            var rootNode = new HierarchyNode {
                RelativePath = string.Empty
            };

            if (instance == null || instance.TypeDefinitionNode == null) {
                rootNode.Instance = CreateMergedInstance(root, rootId, string.Empty);
            }
            else {
                rootNode.Instance = CreateMergedInstance(instance.TypeDefinitionNode, rootId,
                    string.Empty);

                if (root.SymbolicName == rootNode.Instance.SymbolicName) {
                    rootNode.Instance.BrowseName = root.BrowseName;
                    rootNode.Instance.DisplayName = root.DisplayName;
                }
                ((InstanceDesign)rootNode.Instance).MergeIn(root);
            }

            rootNode.ExplicitlyDefined = false;

            hierarchy.Nodes.Add(string.Empty, rootNode);
            hierarchy.NodeList.Add(rootNode);

            // build instance hierachy.
            foreach (var node in nodes) {
                var explicitlyDefined = false;
                for (var parent = node.Instance; parent != null; parent = parent.Parent) {
                    if (parent.SymbolicId == root.SymbolicId) {
                        explicitlyDefined = true;
                        break;
                    }
                }
                if (!hierarchy.Nodes.TryGetValue(node.RelativePath, out var mergedNode)) {
                    mergedNode = new HierarchyNode {
                        RelativePath = node.RelativePath,
                        Instance = CreateMergedInstance(node.Instance,
                            root.SymbolicId, node.RelativePath),
                        ExplicitlyDefined = false,
                        Inherited = node.Inherited
                    };

                    hierarchy.Nodes.Add(node.RelativePath, mergedNode);
                    hierarchy.NodeList.Add(mergedNode);
                }
                else {
                    ((InstanceDesign)mergedNode.Instance).MergeIn(node.Instance);
                }

                if (mergedNode.OverriddenNodes == null) {
                    mergedNode.OverriddenNodes = new List<NodeDesign>();
                }
                mergedNode.OverriddenNodes.Add(node.Instance);

                if (explicitlyDefined) {
                    mergedNode.ExplicitlyDefined = true;
                }
            }

            return hierarchy;
        }

        /// <summary>
        /// Create node id
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="namespaceUris"></param>
        /// <returns></returns>
        private NodeId CreateNodeId(XmlQualifiedName nodeId, NamespaceTable namespaceUris) {
            if (nodeId == null) {
                return NodeId.Null;
            }
            var node = FindNode(nodeId);
            if (node != null) {
                var id = node.GetNodeId(namespaceUris);
                if (id != NodeId.Null) {
                    return id;
                }
                return new NodeId(nodeId.Name, (ushort)namespaceUris.GetIndex(nodeId.Namespace));
            }
            // TODO
            return new NodeId(Guid.NewGuid(), (ushort)namespaceUris.GetIndex(nodeId.Namespace));
        }

        /// <summary>
        /// Create merged instance
        /// </summary>
        /// <param name="source"></param>
        /// <param name="rootId"></param>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        private NodeDesign CreateMergedInstance(NodeDesign source,
            XmlQualifiedName rootId, string relativePath) {
            if (source is ReferenceTypeDesign || source is DataTypeDesign) {
                return source;
            }
            var mergedInstance = CreateInstance(source, rootId);
            if (mergedInstance == null) {
                return null;
            }

            var instanceId = rootId.Name;
            if (!string.IsNullOrEmpty(relativePath)) {
                instanceId = NodeDesignEx.CreateSymbolicId(instanceId, relativePath);
            }

            mergedInstance.SymbolicId =
                new XmlQualifiedName(instanceId, rootId.Namespace);
            mergedInstance.References = null;
            mergedInstance.IdentifierRequired = true;
            mergedInstance.InstanceDeclarationNode = null;
            mergedInstance.Instance = null;
            mergedInstance.OveriddenNode = null;
            mergedInstance.Parent = null;
            mergedInstance.Category = source.Category;
            mergedInstance.Purpose = source.Purpose;
            mergedInstance.ReleaseStatus = source.ReleaseStatus;
            return mergedInstance;
        }

        /// <summary>
        /// Create instance
        /// </summary>
        /// <param name="node"></param>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        private static InstanceDesign CreateInstance(NodeDesign node,
            XmlQualifiedName instanceId) {
            switch (node) {
                case InstanceDesign instance:
                    // Create instance as a copy of the merged instance design
                    var copy = instance.Copy();
                    if (instance is MethodDesign method) {
                        ((MethodDesign)copy).MethodDeclarationNode = method;
                    }
                    return copy;
                case TypeDesign source:
                    // Create instance from the merged type design
                    InstanceDesign typeInstance = null;
                    var type = source.GetMergedType();
                    switch (type) {
                        case VariableTypeDesign variableType:
                            typeInstance = new VariableDesign {
                                Parent = null,
                                ReferenceType = null,
                                ModellingRule = ModellingRule.Mandatory,
                                ModellingRuleSpecified = true,
                                DisplayName = new LocalizedText(),
                                WriteAccess = 0,
                                DecodedValue = variableType.DecodedValue,
                                DefaultValue = variableType.DefaultValue,
                                DataType = variableType.DataType,
                                DataTypeNode = variableType.DataTypeNode,
                                ValueRank = variableType.ValueRank,
                                ValueRankSpecified = variableType.ValueRankSpecified,
                                ArrayDimensions = variableType.ArrayDimensions,
                                AccessLevel = variableType.AccessLevel,
                                AccessLevelSpecified = variableType.AccessLevelSpecified,
                                MinimumSamplingInterval = variableType.MinimumSamplingInterval,
                                MinimumSamplingIntervalSpecified =
                                    variableType.MinimumSamplingIntervalSpecified,
                                Historizing = variableType.Historizing,
                                HistorizingSpecified = variableType.HistorizingSpecified,
                                Category = variableType.Category,
                                Purpose = variableType.Purpose,
                                ReleaseStatus = variableType.ReleaseStatus
                            };
                            break;
                        case ObjectTypeDesign objectType:
                            typeInstance = new ObjectDesign {
                                Parent = null,
                                ReferenceType = null,
                                ModellingRule = ModellingRule.Mandatory,
                                ModellingRuleSpecified = true,
                                DisplayName = new LocalizedText(),
                                WriteAccess = 0,
                                SupportsEvents = objectType.SupportsEvents,
                                SupportsEventsSpecified = true,
                                Category = objectType.Category,
                                Purpose = objectType.Purpose,
                                ReleaseStatus = objectType.ReleaseStatus
                            };
                            break;
                    }
                    if (type.Description != null && !type.Description.IsAutogenerated) {
                        typeInstance.Description = type.Description;
                    }
                    typeInstance.SymbolicName = instanceId;
                    typeInstance.NumericId = source.NumericId;
                    typeInstance.NumericIdSpecified = source.NumericIdSpecified;
                    typeInstance.StringId = source.StringId;
                    typeInstance.BrowseName = instanceId.Name;
                    typeInstance.DisplayName.Value = instanceId.Name;
                    typeInstance.DisplayName.IsAutogenerated = true;
                    typeInstance.TypeDefinition = source.SymbolicId;
                    typeInstance.TypeDefinitionNode = source;
                    return typeInstance;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Set overridden nodes
        /// </summary>
        /// <param name="node"></param>
        private void SetOverriddenNodes(NodeDesign node) {
            if (node is ReferenceTypeDesign || node is DataTypeDesign) {
                return;
            }
            var nodes = new Dictionary<string, InstanceDesign>();
            switch (node) {
                case TypeDesign type:
                    SetOverriddenNodes(type, string.Empty, nodes);
                    return;
                case InstanceDesign instance:
                    SetOverriddenNodes(instance, string.Empty, nodes);
                    return;
            }
        }

        /// <summary>
        /// Set overridden nodes for types
        /// </summary>
        /// <param name="type"></param>
        /// <param name="basePath"></param>
        /// <param name="nodes"></param>
        private void SetOverriddenNodes(TypeDesign type, string basePath,
            Dictionary<string, InstanceDesign> nodes) {
            if (type.BaseTypeNode != null) {
                SetOverriddenNodes(type.BaseTypeNode, basePath, nodes);
            }
            if (type.Children != null && type.Children.Items != null) {
                foreach (var instance in type.Children.Items) {
                    if (instance.ModellingRule == ModellingRule.ExposesItsArray ||
                        instance.ModellingRule == ModellingRule.MandatoryPlaceholder ||
                        instance.ModellingRule == ModellingRule.OptionalPlaceholder) {
                        continue;
                    }
                    var browsePath = instance.GetBrowsePath(basePath);
                    SetOverriddenNodes(instance, browsePath, nodes);

                    if (nodes.TryGetValue(browsePath, out var overriddenInstance)) {
                        var inPath = false;
                        for (var current = overriddenInstance; current != null;
                            current = current.OveriddenNode) {
                            if (current.SymbolicId == instance.SymbolicId) {
                                inPath = true;
                                break;
                            }
                        }
                        if (!inPath) {
                            instance.OveriddenNode = overriddenInstance;
                        }
                    }
                    // special handling for built-in properties.
                    var propertyName = Constants.EnumStrings;
                    if (instance is PropertyDesign && instance.SymbolicName == propertyName) {
                        instance.OveriddenNode = (VariableDesign)FindNode(propertyName,
                            typeof(VariableDesign), instance.SymbolicId.Name);
                    }
                    nodes[browsePath] = instance;
                }
            }
        }

        /// <summary>
        /// Set overridden nodes for instance
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="basePath"></param>
        /// <param name="nodes"></param>
        private void SetOverriddenNodes(InstanceDesign parent, string basePath,
            Dictionary<string, InstanceDesign> nodes) {
            if (parent.TypeDefinitionNode != null) {
                SetOverriddenNodes(parent.TypeDefinitionNode, basePath, nodes);
            }
            if (parent.Children != null && parent.Children.Items != null) {
                foreach (var instance in parent.Children.Items) {
                    var browsePath = instance.GetBrowsePath(basePath);
                    SetOverriddenNodes(instance, browsePath, nodes);
                    if (nodes.TryGetValue(browsePath, out var overriddenInstance)) {
                        var inPath = false;
                        for (var current = overriddenInstance; current != null;
                            current = current.OveriddenNode) {
                            if (current.SymbolicId == instance.SymbolicId) {
                                inPath = true;
                                break;
                            }
                        }
                        if (!inPath) {
                            instance.OveriddenNode = overriddenInstance;
                        }
                    }
                    nodes[browsePath] = instance;
                }
            }
        }

        /// <summary>
        /// Collects all of children for a type.
        /// </summary>
        private void BuildInstanceHierarchy(TypeDesign type, string basePath,
            List<HierarchyNode> nodes, List<HierarchyReference> references,
            bool suppressInverseHierarchicalAtTypeLevel, bool inherited) {
            if (type.BaseTypeNode != null) {
                if (type is VariableTypeDesign || type is ObjectTypeDesign) {
                    BuildInstanceHierarchy(type.BaseTypeNode, basePath, nodes, references,
                        true, true);
                }
            }
            TranslateReferences(basePath, type, references,
                suppressInverseHierarchicalAtTypeLevel, inherited);
            if (type.Children != null && type.Children.Items != null) {
                foreach (var instance in type.Children.Items) {
                    if (!string.IsNullOrEmpty(basePath) &&
                        (instance.ModellingRule == ModellingRule.None ||
                         instance.ModellingRule == ModellingRule.ExposesItsArray ||
                         instance.ModellingRule == ModellingRule.MandatoryPlaceholder ||
                         instance.ModellingRule == ModellingRule.OptionalPlaceholder)) {
                        continue;
                    }

                    var browsePath = instance.GetBrowsePath(basePath);
                    var child = new HierarchyNode {
                        RelativePath = browsePath,
                        Instance = instance,
                        Inherited = inherited
                    };
                    nodes.Add(child);
                    BuildInstanceHierarchy(instance, browsePath, nodes,
                        references, inherited);
                }
            }
        }

        /// <summary>
        /// Collects all of children for an instance.
        /// </summary>
        private void BuildInstanceHierarchy(InstanceDesign parent, string basePath,
            List<HierarchyNode> nodes, List<HierarchyReference> references, bool inherited) {
            if (parent.TypeDefinitionNode != null) {
                BuildInstanceHierarchy(parent.TypeDefinitionNode, basePath,
                    nodes, references, true, inherited);
            }

            if (parent.TypeDefinition != null && parent is MethodDesign) {
                var methodType = (MethodDesign)FindNode(parent.TypeDefinition,
                    typeof(MethodDesign), parent.SymbolicId.Name);
                if (methodType != null) {
                    BuildInstanceHierarchy(methodType, basePath, nodes,
                        references, inherited);
                }
            }

            TranslateReferences(basePath, parent, references, false, false);
            if (parent.Children != null && parent.Children.Items != null) {
                foreach (var instance in parent.Children.Items) {
                    var browsePath = instance.GetBrowsePath(basePath);
                    var child = new HierarchyNode {
                        RelativePath = browsePath,
                        Instance = instance,
                        Inherited = inherited
                    };
                    nodes.Add(child);
                    BuildInstanceHierarchy(instance, browsePath, nodes,
                        references, inherited);
                }
            }
        }

        /// <summary>
        /// Translate references
        /// </summary>
        /// <param name="currentPath"></param>
        /// <param name="source"></param>
        /// <param name="references"></param>
        /// <param name="suppressInverseHierarchicalAtTypeLevel"></param>
        /// <param name="inherited"></param>
        private void TranslateReferences(string currentPath, NodeDesign source,
            List<HierarchyReference> references, bool suppressInverseHierarchicalAtTypeLevel,
            bool inherited) {
            if (source.References == null || source.References.Length == 0) {
                return;
            }

            foreach (var reference in source.References) {
                if (reference.ReferenceType == Constants.HasModelParent) {
                    continue;
                }
                // suppress inhierited non-hierarchial references.
                if (inherited) {
                    var target = FindNode(reference.ReferenceType);
                    if (target != null) {
                        var found = false;
                        var referenceType = target as ReferenceTypeDesign;
                        while (referenceType != null) {
                            if (referenceType.SymbolicName == Constants.NonHierarchicalReferences) {
                                found = true;
                                break;
                            }
                            referenceType = referenceType.BaseTypeNode as ReferenceTypeDesign;
                        }
                        if (found) {
                            continue;
                        }
                    }
                }

                if (suppressInverseHierarchicalAtTypeLevel &&
                    reference.IsInverse &&
                    reference.ReferenceType == Constants.Organizes) {
                    continue;
                }
                var hierarchyReference = TranslateReference(currentPath, source.SymbolicId, reference);
                references.Add(hierarchyReference);
            }
        }

        /// <summary>
        /// Translate reference
        /// </summary>
        /// <param name="currentPath"></param>
        /// <param name="sourceId"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        private HierarchyReference TranslateReference(string currentPath, XmlQualifiedName sourceId,
            Reference reference) {
            if (currentPath == null) {
                currentPath = string.Empty;
            }

            var mergedReference = new HierarchyReference {
                SourcePath = currentPath,
                ReferenceType = reference.ReferenceType,
                IsInverse = reference.IsInverse,
                TargetId = reference.TargetId
            };

            if (reference.TargetId == null || sourceId.Namespace != reference.TargetId.Namespace) {
                return mergedReference;
            }

            var currentPathParts = currentPath.Split(new char[] { NodeDesign.kPathChar[0] }, StringSplitOptions.RemoveEmptyEntries);
            var sourceIdParts = sourceId.Name.Split(new char[] { NodeDesign.kPathChar[0] }, StringSplitOptions.RemoveEmptyEntries);
            var targetIdParts = reference.TargetId.Name.Split(new char[] { NodeDesign.kPathChar[0] }, StringSplitOptions.RemoveEmptyEntries);

            // find the common root in the type declaration.
            string[] targetPath = null;
            string[] sourcePath = null;

            if (sourceIdParts.Length == 0 || targetIdParts.Length == 0 || targetIdParts[0] != sourceIdParts[0]) {
                return mergedReference;
            }

            for (var index = 0; index < sourceIdParts.Length; index++) {
                if (index >= targetIdParts.Length) {
                    sourcePath = new string[sourceIdParts.Length - index];
                    Array.Copy(sourceIdParts, index, sourcePath, 0, sourcePath.Length);
                    targetPath = new string[0];
                    break;
                }

                if (targetIdParts[index] != sourceIdParts[index]) {
                    sourcePath = new string[sourceIdParts.Length - index];
                    Array.Copy(sourceIdParts, index, sourcePath, 0, sourcePath.Length);
                    targetPath = new string[targetIdParts.Length - index];
                    Array.Copy(targetIdParts, index, targetPath, 0, targetPath.Length);
                    break;
                }
            }

            // no common root.
            if (sourcePath == null) {
                sourcePath = new string[0];
                targetPath = new string[targetIdParts.Length - sourceIdParts.Length];
                Array.Copy(targetIdParts, sourceIdParts.Length, targetPath, 0, targetPath.Length);
            }

            // find the new root.
            string[] targetRoot = null;

            for (var index = 1; index < sourcePath.Length; index++) {
                if (index > currentPathParts.Length) {
                    return mergedReference;
                }

                if (currentPathParts[currentPathParts.Length - index] != sourcePath[sourcePath.Length - index]) {
                    targetRoot = new string[currentPathParts.Length - index];
                    Array.Copy(currentPathParts, 0, targetRoot, 0, targetRoot.Length);
                    break;
                }
            }

            // no common root.
            if (targetRoot == null && currentPathParts.Length > sourcePath.Length) {
                targetRoot = new string[currentPathParts.Length - sourcePath.Length];
                Array.Copy(currentPathParts, 0, targetRoot, 0, targetRoot.Length);
            }

            var builder = new StringBuilder();
            if (targetRoot != null) {
                foreach (var item in targetRoot) {
                    if (builder.Length > 0) {
                        builder.Append(NodeDesign.kPathChar);
                    }
                    builder.Append(item);
                }
            }
            if (targetPath != null) {
                foreach (var item in targetPath) {
                    if (builder.Length > 0) {
                        builder.Append(NodeDesign.kPathChar);
                    }
                    builder.Append(item);
                }
            }

            mergedReference.TargetId = null;
            mergedReference.TargetPath = builder.ToString();

            return mergedReference;
        }


        /// <summary>
        /// Build instance hierarchy
        /// </summary>
        /// <param name="node"></param>
        /// <param name="nodes"></param>
        /// <param name="references"></param>
        private void BuildInstanceHierarchy(NodeDesign node, List<HierarchyNode> nodes,
            List<HierarchyReference> references) {
            switch (node) {
                case TypeDesign type:
                    BuildInstanceHierarchy(type, string.Empty, nodes, references, false, false);
                    break;
                case InstanceDesign instance:
                    BuildInstanceHierarchy(instance, string.Empty, nodes, references, false);
                    break;
            }
        }

        /// <summary>
        /// Create node model
        /// </summary>
        /// <param name="root"></param>
        /// <param name="namespaceUris"></param>
        private void CreateNode(NodeDesign root, NamespaceTable namespaceUris) {
            if (root is InstanceDesign) {
                root.Model = CreateNode(root.Hierarchy.NodeList[0].Instance, null, string.Empty,
                    root.Hierarchy, false, false, namespaceUris);
                if (root.Model is InstanceNodeModel instance) {
                    instance.ClearModellingRules(_context.ToSystemContext());
                }
            }
            else {
                root.Model = CreateNode(root, null, string.Empty,
                    root.Hierarchy, true, true, namespaceUris);
            }

            if (root.Hierarchy != null && root is TypeDesign) {
                if (root.Hierarchy.Nodes.TryGetValue(string.Empty, out var hierarchyNode)) {
                    if (hierarchyNode.Identifier != null) {
                        if (hierarchyNode.Identifier is uint) {
                            hierarchyNode.Instance.NumericId = (uint)hierarchyNode.Identifier;
                            hierarchyNode.Instance.NumericIdSpecified = false;
                        }
                        else if (hierarchyNode.Identifier is string) {
                            hierarchyNode.Instance.StringId = (string)hierarchyNode.Identifier;
                        }
                    }
                    root.Instance = hierarchyNode.Instance.Model = CreateNode(
                        hierarchyNode.Instance, null, string.Empty, root.Hierarchy,
                        false, false, namespaceUris);
                    if (hierarchyNode.Instance.Model is InstanceNodeModel instance) {
                        instance.ClearModellingRules(_context.ToSystemContext());
                    }
                }
            }
        }

        /// <summary>
        /// Create node model
        /// </summary>
        /// <param name="nodeDesign"></param>
        /// <param name="parent"></param>
        /// <param name="basePath"></param>
        /// <param name="hierarchy"></param>
        /// <param name="explicitOnly"></param>
        /// <param name="isTypeDefinition"></param>
        /// <param name="namespaceUris"></param>
        /// <returns></returns>
        private BaseNodeModel CreateNode(NodeDesign nodeDesign, BaseNodeModel parent, string basePath,
            Hierarchy hierarchy, bool explicitOnly, bool isTypeDefinition, NamespaceTable namespaceUris) {

            // Create node model
            var nodeModel = CreateNode(nodeDesign, parent, namespaceUris);
            nodeModel.SymbolicName = nodeDesign.SymbolicName.Name;
            nodeModel.NodeId = nodeDesign.GetNodeId(namespaceUris);
            nodeModel.BrowseName = new QualifiedName(nodeDesign.BrowseName,
                (ushort)namespaceUris.GetIndex(nodeDesign.SymbolicName.Namespace));
            nodeModel.DisplayName = new Ua.LocalizedText(nodeDesign.DisplayName.Key,
                string.Empty, nodeDesign.DisplayName.Value);
            if (nodeDesign.Description != null && !nodeDesign.Description.IsAutogenerated) {
                nodeModel.Description = new Ua.LocalizedText(nodeDesign.Description.Key,
                    string.Empty, nodeDesign.Description.Value);
            }
            nodeModel.WriteMask = AttributeWriteMask.None;
            nodeModel.UserWriteMask = AttributeWriteMask.None;

            if (nodeModel is MethodNodeModel method) {
                var design = (MethodDesign)nodeDesign;
                if (design.MethodDeclarationNode != null) {
                    method.TypeDefinitionId = design.MethodDeclarationNode.GetNodeId(namespaceUris);
                }
            }
            if (hierarchy == null) {
                return nodeModel;
            }

            // Build references for node
            foreach (var reference in hierarchy.References) {
                if (reference.SourcePath != basePath &&
                    reference.TargetPath != basePath) {
                    continue;
                }
                var referenceTypeId = CreateNodeId(reference.ReferenceType, namespaceUris);
                var isInverse = reference.IsInverse;
                if (reference.TargetId != null) {
                    var targetId = CreateNodeId(reference.TargetId, namespaceUris);
                    nodeModel.AddReference(referenceTypeId, isInverse, targetId);
                    continue;
                }
                if (string.IsNullOrEmpty(reference.TargetPath)) {
                    if (parent != null) {
                        nodeModel.AddReference(referenceTypeId, isInverse, parent.NodeId);
                        continue;
                    }
                }
                if (reference.SourcePath == basePath) {
                    if (!hierarchy.Nodes.TryGetValue(reference.TargetPath, out var target)) {
                        continue;
                    }
                    if (!target.ExplicitlyDefined && isTypeDefinition) {
                        continue;
                    }
                    var targetId = target.Instance.GetNodeId(namespaceUris);
                    nodeModel.AddReference(referenceTypeId, isInverse, targetId);
                    continue;
                }

                if (!hierarchy.Nodes.TryGetValue(reference.SourcePath, out var source)) {
                    continue;
                }
                if (!source.ExplicitlyDefined && isTypeDefinition) {
                    continue;
                }

                var sourceId = source.Instance.GetNodeId(namespaceUris);
                nodeModel.AddReference(referenceTypeId, !isInverse, sourceId);
            }

            // Add child nodes from hierarchy
            foreach (var node in hierarchy.NodeList) {
                if (explicitOnly) {
                    if (!node.ExplicitlyDefined) {
                        continue;
                    }
                }
                var childPath = node.RelativePath;
                // only looking for nodes in the current tree (part of the base path).
                if (!childPath.StartsWith(basePath, StringComparison.Ordinal)) {
                    continue;
                }
                // ignore reference to the current base node.
                if (childPath == basePath) {
                    continue;
                }
                // relative should always end in the name of the current instance.
                if (!childPath.EndsWith(node.Instance.SymbolicName.Name, StringComparison.Ordinal)) {
                    continue;
                }
                // get the parent path which is without our name at the end.
                if (childPath.Length > node.Instance.SymbolicName.Name.Length) {
                    var parentPath = node.RelativePath
                        .Substring(0, childPath.Length - node.Instance.SymbolicName.Name.Length - 1);
                    if (parentPath != basePath) {
                        continue;
                    }
                }
                else {
                    if (!string.IsNullOrEmpty(basePath)) {
                        // Error?
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(basePath)) {
                    childPath = childPath.Substring(basePath.Length + 1);
                    childPath = string.Format("{0}{1}{2}", basePath, NodeDesign.kPathChar, childPath);
                }

                // Follow the modelling rules
                if (!explicitOnly) {
                    if (node.Instance is InstanceDesign instanceDesign) {
                        if (!node.ExplicitlyDefined &&
                            instanceDesign.ModellingRule != ModellingRule.Mandatory) {
                            if ((instanceDesign.ModellingRule != ModellingRule.None &&
                                 instanceDesign.ModellingRule != ModellingRule.ExposesItsArray &&
                                 instanceDesign.ModellingRule != ModellingRule.OptionalPlaceholder &&
                                 instanceDesign.ModellingRule != ModellingRule.MandatoryPlaceholder) ||
                                 !isTypeDefinition) {
                                continue;
                            }
                        }
                    }
                }

                // Create child node model
                node.Instance.Model = CreateNode(node.Instance, nodeModel, childPath, hierarchy,
                    false, isTypeDefinition, namespaceUris);

                // Add nodes based on modelling rule modelling rules
                if (node.Instance.Model is InstanceNodeModel instance) {
                    if (explicitOnly) {
                        if (node.ExplicitlyDefined) {
                            nodeModel.AddChild(instance);
                        }
                    }
                    else if (isTypeDefinition) {
                        if (instance.ModellingRuleId == ObjectIds.ModellingRule_Mandatory) {
                            nodeModel.AddChild(instance);
                        }
                        else if (node.ExplicitlyDefined &&
                            instance.ModellingRuleId == ObjectIds.ModellingRule_Optional) {
                            nodeModel.AddChild(instance);
                        }
                        else if (node.ExplicitlyDefined && (
                            instance.ModellingRuleId == ObjectIds.ModellingRule_ExposesItsArray ||
                            instance.ModellingRuleId == ObjectIds.ModellingRule_OptionalPlaceholder ||
                            instance.ModellingRuleId == ObjectIds.ModellingRule_MandatoryPlaceholder)) {
                            nodeModel.AddChild(instance);
                        }
                    }
                    else {
                        if (instance.ModellingRuleId == ObjectIds.ModellingRule_Mandatory) {
                            nodeModel.AddChild(instance);
                        }
                        else if (node.ExplicitlyDefined &&
                            instance.ModellingRuleId == ObjectIds.ModellingRule_Optional) {
                            nodeModel.AddChild(instance);
                        }
                    }
                }
            }
            return nodeModel;
        }

        /// <summary>
        /// Convert single node design to node model
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceUris"></param>
        /// <returns></returns>
        private BaseNodeModel CreateNode(NodeDesign node, BaseNodeModel parent, NamespaceTable namespaceUris) {
            switch (node) {
                case ObjectTypeDesign objectTypeDesign:
                    return new ObjectTypeNodeModel {
                        Handle = objectTypeDesign,
                        IsAbstract = objectTypeDesign.IsAbstract,
                        SuperTypeId = objectTypeDesign.BaseTypeNode?.GetNodeId(namespaceUris)
                    };
                case VariableTypeDesign variableTypeDesign:
                    var variableNode = new DataVariableTypeNodeModel {
                        Handle = variableTypeDesign,
                        IsAbstract = variableTypeDesign.IsAbstract,
                        SuperTypeId = variableTypeDesign.BaseTypeNode?.GetNodeId(namespaceUris)
                    };
                    if (variableTypeDesign.Hierarchy != null &&
                        variableTypeDesign.Hierarchy.Nodes.TryGetValue(string.Empty, out var instance) &&
                        instance.Instance is VariableDesign mergedInstance) {
                        var (valueRank, dimensions) = mergedInstance.ValueRank.ToStackValue(
                            mergedInstance.ArrayDimensions);
                        variableNode.Value = mergedInstance.DecodedValue == null ? (Variant?)null :
                            new Variant(mergedInstance.DecodedValue);
                        variableNode.DataType = mergedInstance.DataTypeNode.GetNodeId(namespaceUris);
                        variableNode.ValueRank = valueRank;
                        variableNode.ArrayDimensions = dimensions?.ToArray();
                    }
                    else {
                        var (valueRank, dimensions) = variableTypeDesign.ValueRank.ToStackValue(
                            variableTypeDesign.ArrayDimensions);
                        variableNode.Value = variableTypeDesign.DecodedValue == null ? (Variant?)null :
                            new Variant(variableTypeDesign.DecodedValue);
                        variableNode.DataType = variableTypeDesign.DataTypeNode.GetNodeId(namespaceUris);
                        variableNode.ValueRank = valueRank;
                        variableNode.ArrayDimensions = dimensions?.ToArray();
                    }
                    return variableNode;
                case ReferenceTypeDesign referenceTypeDesign:
                    return new ReferenceTypeNodeModel {
                        Handle = referenceTypeDesign,
                        IsAbstract = referenceTypeDesign.IsAbstract,
                        Symmetric = referenceTypeDesign.Symmetric,
                        InverseName = referenceTypeDesign.Symmetric ? Ua.LocalizedText.Null :
                            new Ua.LocalizedText(referenceTypeDesign.InverseName.Key, string.Empty,
                            referenceTypeDesign.InverseName.Value),
                        SuperTypeId = referenceTypeDesign.BaseTypeNode?.GetNodeId(namespaceUris)
                    };
                case ObjectDesign objectDesign:
                    return new ObjectNodeModel(parent) {
                        Handle = node,
                        TypeDefinitionId = objectDesign.TypeDefinitionNode.GetNodeId(namespaceUris),
                        ReferenceTypeId = CreateNodeId(
                            objectDesign.ReferenceType, namespaceUris),
                        ModellingRuleId = objectDesign.ModellingRule.ToNodeId(),
                        EventNotifier = objectDesign.SupportsEvents ?
                            EventNotifiers.SubscribeToEvents : EventNotifiers.None,
                        NumericId = objectDesign.NumericIdSpecified ? objectDesign.NumericId : 0u
                    };
                case PropertyDesign propertyDesign:
                    var pacl = propertyDesign.AccessLevel.ToStackValue();
                    var (pvalueRank, pdimensions) = propertyDesign.ValueRank.ToStackValue(
                        propertyDesign.ArrayDimensions);
                    return new PropertyNodeModel(parent) {
                        Handle = node,
                        TypeDefinitionId = propertyDesign.TypeDefinitionNode.GetNodeId(namespaceUris),
                        ReferenceTypeId = CreateNodeId(
                            propertyDesign.ReferenceType, namespaceUris),
                        ModellingRuleId = propertyDesign.ModellingRule.ToNodeId(),
                        NumericId = propertyDesign.NumericIdSpecified ? propertyDesign.NumericId : 0u,
                        DataType = propertyDesign.DataTypeNode.GetNodeId(namespaceUris),
                        ValueRank = pvalueRank,
                        ArrayDimensions = pdimensions?.ToArray(),
                        AccessLevel = pacl,
                        UserAccessLevel = pacl,
                        MinimumSamplingInterval = propertyDesign.MinimumSamplingInterval,
                        Historizing = propertyDesign.Historizing,
                        Value = DecodeValue(propertyDesign, namespaceUris)
                    };
                case VariableDesign variableDesign:
                    var vacl = variableDesign.AccessLevel.ToStackValue();
                    var (vvalueRank, vdimensions) = variableDesign.ValueRank.ToStackValue(
                        variableDesign.ArrayDimensions);
                    return new DataVariableNodeModel(parent) {
                        Handle = node,
                        TypeDefinitionId = variableDesign.TypeDefinitionNode.GetNodeId(namespaceUris),
                        ReferenceTypeId = CreateNodeId(
                            variableDesign.ReferenceType, namespaceUris),
                        ModellingRuleId = variableDesign.ModellingRule.ToNodeId(),
                        NumericId = variableDesign.NumericIdSpecified ?
                            variableDesign.NumericId : 0u,
                        DataType = variableDesign.DataTypeNode.GetNodeId(namespaceUris),
                        ValueRank = vvalueRank,
                        ArrayDimensions = vdimensions?.ToArray(),
                        AccessLevel = vacl,
                        UserAccessLevel = vacl,
                        MinimumSamplingInterval = variableDesign.MinimumSamplingInterval,
                        Historizing = variableDesign.Historizing,
                        Value = DecodeValue(variableDesign, namespaceUris)
                    };
                case DataTypeDesign dataTypeDesign:
                    return new DataTypeNodeModel {
                        Handle = dataTypeDesign,
                        IsAbstract = dataTypeDesign.IsAbstract,
                        Purpose = (Nodeset.Schema.DataTypePurpose)(int)dataTypeDesign.Purpose,
                        Definition = DecodeDataTypeDefinition(dataTypeDesign, namespaceUris),
                        SuperTypeId = dataTypeDesign.BaseTypeNode?.GetNodeId(namespaceUris)
                    };
                case MethodDesign methodDesign:
                    return new MethodNodeModel(parent) {
                        Handle = methodDesign,
                        TypeDefinitionId = null,
                        ReferenceTypeId = CreateNodeId(methodDesign.ReferenceType, namespaceUris),
                        ModellingRuleId = methodDesign.ModellingRule.ToNodeId(),
                        Executable = !methodDesign.NonExecutable,
                        UserExecutable = !methodDesign.NonExecutable,
                        NumericId = methodDesign.NumericIdSpecified ? methodDesign.NumericId : 0u
                    };
                case ViewDesign viewDesign:
                    return new ViewNodeModel {
                        Handle = node,
                        EventNotifier = viewDesign.SupportsEvents ?
                            EventNotifiers.SubscribeToEvents : EventNotifiers.None,
                        ContainsNoLoops = viewDesign.ContainsNoLoops
                    };
            }
            return null;
        }

        /// <summary>
        /// Decode value from variable design
        /// </summary>
        /// <param name="variableDesign"></param>
        /// <param name="namespaceUris"></param>
        /// <returns></returns>
        private Variant? DecodeValue(VariableDesign variableDesign, NamespaceTable namespaceUris) {
            var value = variableDesign.DecodedValue;
            switch (value) {
                case ExtensionObject extensionObject:
                    SetExtensionObjectTypeId(extensionObject, namespaceUris);
                    break;
                case ExtensionObject[] listOfExtensionObjects:
                    foreach (var item in listOfExtensionObjects) {
                        SetExtensionObjectTypeId(item, namespaceUris);
                    }
                    break;
                case IList<Argument> arguments:
                    foreach (var argument in arguments) {
                        var namespaceUri = Namespaces.OpcUa;
                        if (!(argument.DataType.Identifier is string name)) {
                            continue;
                        }
                        var index = name.LastIndexOf(':');
                        if (index != -1) {
                            namespaceUri = name.Substring(0, index);
                            name = name.Substring(index + 1);
                        }
                        argument.DataType = CreateNodeId(
                            new XmlQualifiedName(name, namespaceUri), namespaceUris);
                    }
                    break;
            }
            if (value == null) {
                return null;
            }
            return new Variant(value);
        }

        /// <summary>
        /// Decode data type definition
        /// </summary>
        /// <param name="dataTypeDesign"></param>
        /// <param name="namespaceUris"></param>
        /// <returns></returns>
        private DataTypeDefinition DecodeDataTypeDefinition(DataTypeDesign dataTypeDesign,
            NamespaceTable namespaceUris) {
            if (!dataTypeDesign.HasFields) {
                return null;
            }

            if (dataTypeDesign.IsEnumeration) {
                var fields = new List<EnumField2>();
                EnumField2 exportedField;
                foreach (var field in dataTypeDesign.Fields) {
                    if (dataTypeDesign.IsOptionSet) {
                        long bit = 1;
                        var value = 0;
                        while (field.Identifier > 0 && bit <= uint.MaxValue) {
                            if ((bit & field.Identifier) != 0) {
                                break;
                            }
                            bit <<= 1;
                            value++;
                        }
                        exportedField = new EnumField2 {
                            Name = field.Name,
                            Value = value,
                            DisplayName = new Ua.LocalizedText(field.Name),
                            SymbolicName = field.Name
                        };
                    }
                    else {
                        exportedField = new EnumField2 {
                            Name = field.Name,
                            Value = field.Identifier,
                            DisplayName = new Ua.LocalizedText(field.Name),
                            SymbolicName = field.Name
                        };
                    }
                    if (field.Description != null && !field.Description.IsAutogenerated) {
                        exportedField.Description = new Ua.LocalizedText(field.Description.Value);
                    }
                    fields.Add(exportedField);
                }
                var definition = new EnumDefinition2 {
                    Fields = new EnumFieldCollection(fields),
                    Name = dataTypeDesign.BrowseName,
                    IsOptionSet = dataTypeDesign.IsOptionSet
                };
                if (dataTypeDesign.Description != null && !dataTypeDesign.Description.IsAutogenerated) {
                    definition.Description = new Ua.LocalizedText(dataTypeDesign.Description.Value);
                }
                if (definition.Name != dataTypeDesign.SymbolicName.Name) {
                    definition.SymbolicName = dataTypeDesign.SymbolicName.Name;
                }
                if (dataTypeDesign.BaseTypeNode is DataTypeDesign baseType) {
                    // Set base type
                    definition.BaseType = new QualifiedName(baseType.SymbolicId.Name,
                        (ushort)namespaceUris.GetIndex(baseType.SymbolicId.Namespace)).ToString();
                }
                return definition;
            }

            if (dataTypeDesign.IsStructure) {
                var fields = new List<StructureField2>();
                var optionalFields = false;
                foreach (var field in dataTypeDesign.Fields) {
                    var (valueRank, dimensions) = field.ValueRank.ToStackValue(field.ArrayDimensions);
                    var exportedField = new StructureField2 {
                        Name = field.Name,
                        DataType = field.DataTypeNode.GetNodeId(namespaceUris),
                        ValueRank = valueRank,
                        ArrayDimensions = dimensions,
                        IsOptional = field.IsOptional,
                        MaxStringLength = 0, // TODO
                        DisplayName = new Ua.LocalizedText(field.Name),
                        SymbolicName = field.Name,
                        Value = -1
                    };
                    if (field.Description != null && !field.Description.IsAutogenerated) {
                        exportedField.Description = new Ua.LocalizedText(field.Description.Value);
                    }
                    if (field.IsOptional) {
                        optionalFields = true;
                    }
                    fields.Add(exportedField);
                }
                var definition = new StructureDefinition2 {
                    IsOptionSet = dataTypeDesign.IsOptionSet,
                    StructureType =
                        dataTypeDesign.IsUnion ? StructureType.Union :
                        optionalFields ? StructureType.StructureWithOptionalFields :
                            StructureType.Structure,
                    Fields = new StructureFieldCollection(fields),
                    Name = dataTypeDesign.BrowseName
                };
                if (dataTypeDesign.Description != null && !dataTypeDesign.Description.IsAutogenerated) {
                    definition.Description = new Ua.LocalizedText(dataTypeDesign.Description.Value);
                }
                if (definition.Name != dataTypeDesign.SymbolicName.Name) {
                    definition.SymbolicName = dataTypeDesign.SymbolicName.Name;
                }
                if (dataTypeDesign.BaseTypeNode is DataTypeDesign baseType) {
                    // Set base type
                    definition.BaseType = new QualifiedName(dataTypeDesign.SymbolicId.Name,
                        (ushort)namespaceUris.GetIndex(baseType.SymbolicId.Namespace)).ToString();
                    definition.BaseDataType = CreateNodeId(baseType.SymbolicId, namespaceUris);
                }
                return definition;
            }

            // TODO Log
            return null;
        }

        /// <summary>
        /// Set type id of extension object object
        /// </summary>
        /// <param name="extensionObject"></param>
        /// <param name="namespaceUris"></param>
        private void SetExtensionObjectTypeId(ExtensionObject extensionObject,
            NamespaceTable namespaceUris) {
            XmlQualifiedName qname = null;
            if (extensionObject.Body is XmlElement element) {
                // determine the data type of the element.
                qname = new XmlQualifiedName(element.LocalName, element.NamespaceURI);
                var prefix = element.GetPrefixOfNamespace(Namespaces.XmlSchemaInstance);
                var xsitype = element.GetAttribute(prefix + ":type");
                if (!string.IsNullOrEmpty(xsitype)) {
                    var index = xsitype.IndexOf(':');
                    if (index > 0) {
                        qname = new XmlQualifiedName(xsitype.Substring(index + 1),
                            element.GetNamespaceOfPrefix(xsitype.Substring(0, index)));
                    }
                    else {
                        qname = new XmlQualifiedName(xsitype.Substring(index + 1),
                            element.NamespaceURI);
                    }
                }
            }
            else {
                if (extensionObject.Body is IEncodeable encodeable) {
                    qname = EncodeableFactory.GetXmlName(encodeable.GetType());
                }
            }
            if (FindNode(qname) is DataTypeDesign dataTypeNode) {
                var numericId = dataTypeNode.NumericId;
                var namespaceIndex = namespaceUris.GetIndex(qname.Namespace);
                // look up XML encoding id.
                if (dataTypeNode.HasEncodings) {
                    foreach (var encoding in dataTypeNode.Encodings) {
                        var encodingNode = (ObjectDesign)FindNode(encoding.SymbolicId,
                            typeof(ObjectDesign), encoding.SymbolicId.Name);
                        if (encodingNode != null && encodingNode.SymbolicName.Name == "DefaultXml") {
                            numericId = encodingNode.NumericId;
                            namespaceIndex = namespaceUris.GetIndex(encodingNode.SymbolicId.Namespace);
                            break;
                        }
                    }
                }
                if (namespaceIndex >= 0) {
                    extensionObject.TypeId = new NodeId(numericId, (ushort)namespaceIndex);
                }
            }
        }

        /// <summary>
        /// Validate and assign numeric id
        /// </summary>
        /// <param name="node"></param>
        private void ValidateAssignedNumericId(NodeDesign node) {
            // check numeric id was set in model.
            if (node.NumericIdSpecified) {
                if (_identifiers.ContainsKey(node.NumericId)) {
                    // throw new FormatException(
                    //     $"The NumericId is already used by another node: {node.NumericId}.");
                    node.NumericIdSpecified = false;
                    node.NumericId = 0;
                    return;
                }
                _identifiers.Add(node.NumericId, node);
            }
        }

        private readonly Dictionary<uint, NodeDesign> _identifiers =
            new Dictionary<uint, NodeDesign>();

        /// <summary>
        /// Validate and assign browse names
        /// </summary>
        /// <param name="node"></param>
        private void ValidateAndAssignBrowseName(NodeDesign node) {
            // use the name to assign a browse name.
            if (string.IsNullOrEmpty(node.BrowseName)) {
                node.BrowseName = _browseNames.GetOrAdd(node.SymbolicName,
                    node.SymbolicName.Name);
                return;
            }
            var browseName = _browseNames.GetOrAdd(node.SymbolicName, node.BrowseName);
            if (browseName != node.BrowseName) {
                throw new FormatException(
                    $"The SymbolicName {node.SymbolicName.Name} has a " +
                    $"BrowseName {browseName} but expected {node.BrowseName}.");
            }
        }
        private readonly Dictionary<XmlQualifiedName, string> _browseNames =
            new Dictionary<XmlQualifiedName, string>();

        /// <summary>
        /// Assign a symbolic id and validate it does not yet exist
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parent"></param>
        private void ValidateAndAssignSymbolicId(NodeDesign node, NodeDesign parent) {
            // use the name to assign a symbolic id.
            if (node.SymbolicId.IsNullOrEmpty()) {
                node.SymbolicId = parent.CreateSymbolicId(node.SymbolicName);
            }
            // check for duplicates.
            if (_nodes.ContainsKey(node.SymbolicId)) {
                throw new FormatException(
                    $"The SymbolicId is already used by another node: {node.SymbolicId.Name}.");
            }
        }
        private readonly Dictionary<XmlQualifiedName, NodeDesign> _nodes =
            new Dictionary<XmlQualifiedName, NodeDesign>();

        private readonly INodeResolver _resolver;
        private readonly INodeIdAssigner _assigner;
        private readonly ModelDesign _model;
        private readonly IServiceMessageContext _context;
    }
}
