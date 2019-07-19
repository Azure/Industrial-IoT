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

namespace Opc.Ua.Design.Schema {
    using Opc.Ua.Models;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// Type schema generator
    /// </summary>
    public class SchemaGenerator {
        private const string TemplatePath = "Opc.Ua.ModelCompiler.Templates.";
        private const string DefaultNamespace = "http://opcfoundation.org/UA/";

        private readonly ModelDesign _model;
        private string[] _excludedCategories;

        /// <summary>
        /// Create generator
        /// </summary>
        /// <param name="model"></param>
        public SchemaGenerator(ModelDesign model) { // TODO
            _model = model;
        }

        /// <summary>
        /// Generates a single file containing all of the classes.
        /// </summary>
        public virtual void Generate(string filePath, string[] excludedCategories) {
            _excludedCategories = excludedCategories;
            // write type and object definitions.
            var nodes = GetNodeList();
            WriteTemplate_BinarySchema(filePath, nodes);
        }

        /// <summary>
        /// Creates a class that defines all types in the namespace.
        /// </summary>
        private void WriteTemplate_BinarySchema(string filePath, List<NodeDesign> nodes) {
            var writer = new StreamWriter(string.Format(@"{0}\{1}.Types.bsd", filePath, _model.TargetNamespaceInfo.Prefix), false);

            try {
                var template = new Template(writer, TemplatePath + "BinarySchema.File.xml", Assembly.GetExecutingAssembly());

                template.AddReplacement("_DictionaryUri_", _model.TargetNamespace);
                template.Replacements.Add("_BuildDate_", Utils.Format("{0:yyyy-MM-dd}", DateTime.UtcNow));
                template.Replacements.Add("_Version_", Utils.Format("{0}.{1}", Utils.GetAssemblySoftwareVersion(), Utils.GetAssemblyBuildNumber()));

                AddTemplate(
                    template,
                    "xmlns:s0=\"ListOfNamespaces\"",
                    null,
                    _model.Namespaces,
                    new LoadTemplateEventHandler(LoadTemplate_BinaryNamespaceImports),
                    null);

                AddTemplate(
                    template,
                    "<!-- Imports -->",
                    null,
                    _model.Namespaces,
                    new LoadTemplateEventHandler(LoadTemplate_BinaryNamespaceImports),
                    null);

                AddTemplate(
                    template,
                    "<!-- BuiltInTypes -->",
                    TemplatePath + "BinarySchema.BuiltInTypes.bsd",
                    new ModelDesign[] { _model },
                    new LoadTemplateEventHandler(LoadTemplate_BinaryType),
                    new WriteTemplateEventHandler(WriteTemplate_BinaryType));

                AddTemplate(
                    template,
                    "<!-- ListOfTypes -->",
                    null,
                    nodes,
                    new LoadTemplateEventHandler(LoadTemplate_BinaryType),
                    new WriteTemplateEventHandler(WriteTemplate_BinaryType));

                template.WriteTemplate(null);
            }
            finally {
                writer.Close();
            }
        }

        /// <summary>
        /// Writes the code to defined a identifier for a type.
        /// </summary>
        private string LoadTemplate_BinaryNamespaceImports(Template template, GeneratorContext context) {

            if (!(context.Target is Namespace ns)) {
                return null;
            }

            if (ns.Value == _model.TargetNamespace) {
                return null;
            }

            if (context.Token.Contains("xmlns:s0")) {
                if (ns.Value == DefaultNamespace) {
                    return null;
                }

                template.WriteNextLine(context.Prefix);
                //      template.Write("xmlns:{0}=\"{1}\"", GetXmlNamespacePrefix(ns.Value), ns.Value);
                return null;
            }

            template.WriteNextLine(context.Prefix);
            template.Write("<opc:Import Namespace=\"{0}\" Location=\"{1}.BinarySchema.bsd\"/>", ns.Value, GetNamespacePrefix(ns.Value));

            return null;
        }

        /// <summary>
        /// Writes the code to defined a identifier for a type.
        /// </summary>
        private string LoadTemplate_BinaryType(Template template, GeneratorContext context) {

            if (context.Target is ModelDesign model) {
                if (_model.TargetNamespace == DefaultNamespace) {
                    return context.TemplatePath;
                }

                return null;
            }


            if (!(context.Target is DataTypeDesign dataType)) {
                return null;
            }

            // don't write built-in types.
            if (dataType.NumericId < 256 && dataType.SymbolicId.Namespace == DefaultNamespace) {
                switch (dataType.NumericId) {
                    case DataTypes.PermissionType:
                    case DataTypes.RolePermissionType:
                    case DataTypes.StructureDefinition:
                    case DataTypes.StructureField:
                    case DataTypes.StructureType:
                    case DataTypes.EnumDefinition:
                    case DataTypes.EnumField: {
                            break;
                        }

                    default: {
                            return null;
                        }
                }
            }

            var basicType = dataType.BasicDataType;

            if (basicType == BasicDataType.Enumeration) {
                return TemplatePath + "BinarySchema.EnumeratedType.xml";
            }

            else if (basicType == BasicDataType.UserDefined) {
                return TemplatePath + "BinarySchema.ComplexType.xml";
            }

            return TemplatePath + "BinarySchema.OpaqueType.xml";
        }

        /// <summary>
        /// Writes the code to defined a identifier for a type.
        /// </summary>
        private bool WriteTemplate_BinaryType(Template template, GeneratorContext context) {

            if (context.Target is ModelDesign model) {
                if (_model.TargetNamespace == DefaultNamespace) {
                    template.WriteNextLine(string.Empty);
                    return template.WriteTemplate(context);
                }

                return false;
            }


            if (!(context.Target is DataTypeDesign dataType)) {
                return false;
            }

            if (context.FirstInList) {
                template.WriteNextLine(string.Empty);
            }

            template.AddReplacement("_TypeName_", dataType.SymbolicName.Name);

            if (dataType.BasicDataType == BasicDataType.UserDefined) {
                template.AddReplacement("_BaseType_", GetBinaryDataType(dataType.BaseTypeNode as DataTypeDesign));
            }

            var fields = new List<Parameter>();
            var parents = new Stack<DataTypeDesign>();

            for (var parent = dataType as DataTypeDesign; parent != null; parent = parent.BaseTypeNode as DataTypeDesign) {
                if (parent.Fields != null) {
                    parents.Push(parent);
                }
            }

            while (parents.Count > 0) {
                var parent = parents.Pop();

                foreach (var field in parent.Fields) {
                    if (ReferenceEquals(dataType, parent)) {
                        fields.Add(field);
                        continue;
                    }

                    fields.Add(new Parameter {
                        DataType = field.DataType,
                        DataTypeNode = field.DataTypeNode,
                        Description = field.Description,
                        Identifier = field.Identifier,
                        IdentifierInName = field.IdentifierInName,
                        IdentifierSpecified = field.IdentifierSpecified,
                        IsInherited = true,
                        Name = field.Name,
                        Parent = field.Parent,
                        ValueRank = field.ValueRank
                    });
                }
            }

            if (dataType.BasicDataType == BasicDataType.Enumeration) {
                uint lengthInBits = 32;
                var isOptionSet = false;

                if (dataType.IsOptionSet) {
                    isOptionSet = true;

                    switch (dataType.BaseType.Name) {
                        case "SByte": { lengthInBits = 8; break; }
                        case "Byte": { lengthInBits = 8; break; }
                        case "Int16": { lengthInBits = 16; break; }
                        case "UInt16": { lengthInBits = 16; break; }
                        case "Int32": { lengthInBits = 32; break; }
                        case "UInt32": { lengthInBits = 32; break; }
                        case "Int64": { lengthInBits = 64; break; }
                        case "UInt64": { lengthInBits = 64; break; }
                    }

                    fields.Insert(0, new Parameter {
                        Name = "None",
                        Identifier = 0,
                        IdentifierSpecified = true,
                        DataType = fields[0].DataType,
                        DataTypeNode = fields[0].DataTypeNode,
                        Parent = fields[0].Parent
                    });
                }

                template.AddReplacement("_LengthInBits_", lengthInBits);
                template.AddReplacement("_IsOptionSet_", isOptionSet ? " IsOptionSet=\"true\"" : "");
            }

            AddTemplate(
                template,
                "<!-- Documentation -->",
                null,
                new DataTypeDesign[] { dataType },
                new LoadTemplateEventHandler(LoadTemplate_BinaryDocumentation),
                null);

            AddTemplate(
                template,
                "<!-- ListOfFields -->",
                null,
                fields,
                new LoadTemplateEventHandler(LoadTemplate_BinaryTypeFields),
                null);

            return template.WriteTemplate(context);
        }

        /// <summary>
        /// Returns the data type to use for the value of a variable or the argument of a method.
        /// </summary>
        private string GetBinaryDataType(DataTypeDesign dataType) {
            switch (dataType.BasicDataType) {
                case BasicDataType.Boolean: { return "opc:Boolean"; }
                case BasicDataType.SByte: { return "opc:SByte"; }
                case BasicDataType.Byte: { return "opc:Byte"; }
                case BasicDataType.Int16: { return "opc:Int16"; }
                case BasicDataType.UInt16: { return "opc:UInt16"; }
                case BasicDataType.Int32: { return "opc:Int32"; }
                case BasicDataType.UInt32: { return "opc:UInt32"; }
                case BasicDataType.Int64: { return "opc:Int64"; }
                case BasicDataType.UInt64: { return "opc:UInt64"; }
                case BasicDataType.Float: { return "opc:Float"; }
                case BasicDataType.Double: { return "opc:Double"; }
                case BasicDataType.String: { return "opc:String"; }
                case BasicDataType.DateTime: { return "opc:DateTime"; }
                case BasicDataType.Guid: { return "opc:Guid"; }
                case BasicDataType.ByteString: { return "opc:ByteString"; }
                case BasicDataType.XmlElement: { return "ua:XmlElement"; }
                case BasicDataType.NodeId: { return "ua:NodeId"; }
                case BasicDataType.ExpandedNodeId: { return "ua:ExpandedNodeId"; }
                case BasicDataType.StatusCode: { return "ua:StatusCode"; }
                case BasicDataType.DiagnosticInfo: { return "ua:DiagnosticInfo"; }
                case BasicDataType.QualifiedName: { return "ua:QualifiedName"; }
                case BasicDataType.LocalizedText: { return "ua:LocalizedText"; }
                case BasicDataType.DataValue: { return "ua:DataValue"; }
                case BasicDataType.Number: { return "ua:Variant"; }
                case BasicDataType.Integer: { return "ua:Variant"; }
                case BasicDataType.UInteger: { return "ua:Variant"; }
                case BasicDataType.BaseDataType: { return "ua:Variant"; }

                default:
                case BasicDataType.Enumeration:
                case BasicDataType.Structure: {
                        if (dataType.SymbolicName == new XmlQualifiedName("Structure", DefaultNamespace)) {
                            return string.Format("ua:ExtensionObject");
                        }

                        if (dataType.SymbolicName == new XmlQualifiedName("Enumeration", DefaultNamespace)) {
                            if (dataType.IsOptionSet) {
                                return GetBinaryDataType((DataTypeDesign)dataType.BaseTypeNode);
                            }

                            return string.Format("ua:Int32");
                        }

                        var prefix = "tns";

                        if (dataType.SymbolicName.Namespace != _model.TargetNamespace) {
                            if (dataType.SymbolicName.Namespace == DefaultNamespace) {
                                prefix = "ua";
                            }
                            else {
                                //            prefix = GetXmlNamespacePrefix(dataType.SymbolicName.Namespace);
                            }
                        }

                        return string.Format("{0}:{1}", prefix, dataType.SymbolicName.Name);
                    }
            }
        }

        /// <summary>
        /// Writes the code to defined a identifier for a type.
        /// </summary>
        private string LoadTemplate_BinaryTypeFields(Template template, GeneratorContext context) {

            if (!(context.Target is Parameter field)) {
                return null;
            }


            if (!(field.Parent is DataTypeDesign dataType)) {
                return null;
            }

            var basicType = dataType.BasicDataType;

            if (basicType == BasicDataType.Enumeration) {
                template.WriteNextLine(context.Prefix);
                template.Write("<opc:EnumeratedValue Name=\"{0}\" Value=\"{1}\" />", field.Name, field.Identifier);
                return null;
            }

            if (field.ValueRank != ValueRank.Scalar) {
                template.WriteNextLine(context.Prefix);
                template.Write("<opc:Field Name=\"NoOf{0}\" TypeName=\"opc:Int32\" />", field.Name);
                template.WriteNextLine(context.Prefix);
                template.Write("<opc:Field Name=\"{0}\" TypeName=\"{1}\" LengthField=\"NoOf{0}\" />", field.Name, GetBinaryDataType(field.DataTypeNode));
                return null;
            }

            template.WriteNextLine(context.Prefix);

            if (field.IsInherited) {
                template.Write("<opc:Field Name=\"{0}\" TypeName=\"{1}\" SourceType=\"{2}\" />", field.Name, GetBinaryDataType(field.DataTypeNode), GetBinaryDataType(field.Parent as DataTypeDesign));
            }
            else {
                template.Write("<opc:Field Name=\"{0}\" TypeName=\"{1}\" />", field.Name, GetBinaryDataType(field.DataTypeNode));
            }
            return null;
        }

        private string LoadTemplate_BinaryDocumentation(Template template, GeneratorContext context) {

            if (!(context.Target is DataTypeDesign dataType)) {
                return null;
            }

            if (dataType.Description == null || dataType.Description.IsAutogenerated) {
                return null;
            }

            template.WriteNextLine(context.Prefix);
            template.Write("<opc:Documentation>{0}</opc:Documentation>", dataType.Description.Value);

            return context.TemplatePath;
        }

        private void CollectFields(DataTypeDesign dataType, ValueRank valueRank, string basePath, Dictionary<string, Parameter> fields) {
            if (dataType.BasicDataType != BasicDataType.UserDefined || valueRank != ValueRank.Scalar) {
                return;
            }

            for (var parent = dataType; parent != null; parent = parent.BaseTypeNode as DataTypeDesign) {
                if (parent.Fields != null) {
                    for (var ii = 0; ii < parent.Fields.Length; ii++) {
                        var parameter = parent.Fields[ii];
                        var fieldPath = parameter.Name;

                        if (!string.IsNullOrEmpty(basePath)) {
                            fieldPath = Utils.Format("{0}_{1}", basePath, parameter.Name);
                        }

                        fields[fieldPath] = parameter;
                        CollectFields(parameter.DataTypeNode, parameter.ValueRank, fieldPath, fields);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a qualifier for the namespace to use in code.
        /// </summary>
        private string GetNamespacePrefix(string namespaceUri) {
            if (_model.Namespaces != null) {
                foreach (var ns in _model.Namespaces) {
                    if (ns.Value == namespaceUri) {
                        return string.Format("{0}", ns.Prefix);
                    }
                }
            }

            return null;
        }

        private bool IsExcluded(NodeDesign node) {
            if (_excludedCategories != null) {
                foreach (var jj in _excludedCategories) {
                    if (jj == node.ReleaseStatus.ToString()) {
                        return true;
                    }

                    if (node.Category != null && node.Category.Contains(jj)) {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a list of nodes to process.
        /// </summary>
        private List<NodeDesign> GetNodeList() {
            var nodes = new List<NodeDesign>();
            foreach (var node in _model.Items) {
                if (!IsExcluded(node) && !node.IsDeclaration) {
                    nodes.Add(node);
                }
            }
            return nodes;
        }

        /// <summary>
        /// Initializes a template to use for substitution.
        /// </summary>
        protected void AddTemplate(
            Template template,
            string replacement,
            string templatePath,
            IEnumerable targets,
            LoadTemplateEventHandler onLoad,
            WriteTemplateEventHandler onWrite) {
            template.Replacements.Add(replacement, null);

            // create a collection of targets.
            var targetList = new ArrayList();

            if (targets != null) {
                foreach (var target in targets) {
                    targetList.Add(target);
                }
            }

            var definition = new TemplateDefinition {
                TemplatePath = templatePath,
                Targets = targetList
            };

            if (onLoad != null) {
                definition.LoadTemplate += onLoad;
            }

            if (onWrite != null) {
                definition.WriteTemplate += onWrite;
            }

            template.Templates.Add(replacement, definition);
        }

    }
}
