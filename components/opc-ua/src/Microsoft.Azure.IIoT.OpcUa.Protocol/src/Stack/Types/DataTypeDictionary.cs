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

namespace Opc.Ua.Types {
    using System.Collections.Generic;
    using System.Xml;
    using System;
    using System.Linq;
    using Opc.Ua.Types.Schema;

    /// <summary>
    /// Represents a type dictionary
    /// </summary>
    public class DataTypeDictionary : ITypeResolver, ITypeDictionary {

        /// <inheritdoc/>
        public IEnumerable<DataType> Items => _datatypes.Values;

        /// <inheritdoc/>
        public string TargetNamespace => _dictionary.TargetNamespace;

        /// <inheritdoc/>
        public string TargetVersion => _dictionary.TargetVersion;

        /// <inheritdoc/>
        public DateTime TargetPublicationDate => _dictionary.TargetPublicationDate;

        /// <summary>
        /// Create data type dictionary
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="resolver"></param>
        internal DataTypeDictionary(TypeDictionary dictionary, ITypeResolver resolver) {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            _resolver = resolver;
            // import types from target dictionary.
            foreach (var datatype in _dictionary.Items) {
                AddDataType(datatype, _dictionary.TargetNamespace);
            }
            // validate types in target dictionary.
            foreach (var datatype in _dictionary.Items) {
                ValidateDataType(datatype);
            }
        }

        /// <inheritdoc/>
        public DataType TryResolve(ImportDirective import, XmlQualifiedName typeName) {
            if (import == null) {
                throw new ArgumentNullException(nameof(import));
            }
            if (typeName.IsNullOrEmpty()) {
                throw new ArgumentNullException(nameof(typeName));
            }
            // We only resolve if we own the namespace.
            if (import.Namespace == _dictionary.TargetNamespace &&
               (import.TargetVersion == null ||
                import.TargetVersion == _dictionary.TargetVersion)) {
                return ResolveType(typeName);
            }
            return null;
        }

        /// <summary>
        /// Resolve type
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public DataType ResolveType(XmlQualifiedName typeName) {
            if (typeName.IsNullOrEmpty()) {
                return null;
            }
            if (!_datatypes.TryGetValue(typeName, out var dataType)) {
                if (typeName.Namespace != _dictionary.TargetNamespace) {
                    // Try and resolve through the import directives
                    dataType = ResolveFromImports(typeName, dataType);
                }
            }
            // If type declartion, resolve the source type
            if (dataType is TypeDeclaration declaration) {
                return ResolveType(declaration.SourceType);
            }
            return dataType;
        }

        /// <summary>
        /// Resolve from imports
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        private DataType ResolveFromImports(XmlQualifiedName typeName,
            DataType dataType) {
            if (_dictionary.Import == null) {
                return null;
            }
            if (_resolver == null) {
                return null;
            }
            // Find concrete match of one of the imports
            var import = _dictionary.Import
                .FirstOrDefault(imp => imp.Namespace == typeName.Namespace);
            if (import != null) {
                dataType = _resolver.TryResolve(import, typeName);
            }
            else {
                // Try to look in all imported dictionaries and thus traverse
                // their imports.
                foreach (var import2 in _dictionary.Import) {
                    dataType = _resolver.TryResolve(import2, typeName);
                    if (dataType != null) {
                        break;
                    }
                }
            }
            return dataType;
        }

        /// <summary>
        /// Imports a datatype in the target namespace
        /// </summary>
        /// <param name="datatype"></param>
        /// <param name="targetNamespace"></param>
        private void AddDataType(DataType datatype, string targetNamespace) {
            if (datatype == null) {
                return;
            }
            datatype.QName = new XmlQualifiedName(datatype.Name, targetNamespace);
            if (!datatype.QName.IsValid()) {
                throw new FormatException($"'{datatype.Name}' is not a valid datatype name.");
            }
            if (_datatypes.ContainsKey(datatype.QName)) {
                throw new FormatException(
                    $"The datatype name '{datatype.Name}' already used by another datatype.");
            }
            if (datatype is ComplexType complexType) {
                if (complexType.Field != null) {
                    foreach (var fieldType in complexType.Field) {
                        if (fieldType.ComplexType != null) {
                            AddDataType(fieldType.ComplexType, targetNamespace);
                        }
                    }
                }
            }
            if (datatype is ServiceType serviceType) {
                if (serviceType.Request != null) {
                    foreach (var fieldType in serviceType.Request) {
                        if (fieldType.ComplexType != null) {
                            AddDataType(fieldType.ComplexType, targetNamespace);
                        }
                    }
                }
                if (serviceType.Response != null) {
                    foreach (var fieldType in serviceType.Response) {
                        if (fieldType.ComplexType != null) {
                            AddDataType(fieldType.ComplexType, targetNamespace);
                        }
                    }
                }
            }
            _datatypes.Add(datatype.QName, datatype);
        }

        /// <summary>
        /// Validates a datatype.
        /// </summary>
        /// <param name="datatype"></param>
        private void ValidateDataType(DataType datatype) {
            switch (datatype) {
                case TypeDeclaration typeDeclaration:
                    ValidateTypeDeclaration(typeDeclaration);
                    break;
                case EnumeratedType enumeratedType:
                    ValidateEnumeratedType(enumeratedType);
                    break;
                case ComplexType complexType:
                    ValidateComplexType(complexType);
                    break;
                case ServiceType serviceType:
                    ValidateServiceType(serviceType);
                    break;
            }
        }

        /// <summary>
        /// Validates the source for a type declaration.
        /// </summary>
        /// <param name="declaration"></param>
        private void ValidateTypeDeclaration(TypeDeclaration declaration) {
            if (declaration.SourceType.IsNullOrEmpty()) {
                throw new FormatException("The type declaration " +
                    $"'{declaration.Name}' source type == null.");
            }
            var type = ResolveType(declaration.SourceType);
            if (type == null) {
                throw new FormatException("The type declaration " +
                    $"'{declaration.Name}' source type was not found.");
            }
        }

        /// <summary>
        /// Validates the base type of a complex type.
        /// </summary>
        /// <param name="complexType"></param>
        /// <param name="baseType"></param>
        /// <param name="fields"></param>
        private void ValidateBaseType(ComplexType complexType,
            XmlQualifiedName baseType, Dictionary<string, FieldType> fields) {
            if (baseType.IsNullOrEmpty()) {
                return;
            }
            var parentType = ResolveType(baseType);
            if (!(parentType is ComplexType complexParent)) {
                throw new FormatException($"The base type '{baseType}' for complex type " +
                    $"'{complexType.Name}' is not a complex type.");
            }
            ValidateBaseType(complexType, complexParent.BaseType, fields);
            foreach (var field in complexParent.Field) {
                fields.Add(field.Name, field);
            }
        }

        /// <summary>
        /// Validates a complex type.
        /// </summary>
        /// <param name="complexType"></param>
        private void ValidateComplexType(ComplexType complexType) {
            if (complexType.Field == null) {
                complexType.Field = new FieldType[0];
            }
            var fields = new Dictionary<string, FieldType>();
            ValidateBaseType(complexType, complexType.BaseType, fields);
            foreach (var field in complexType.Field) {
                ValidateFieldType(complexType, fields, field);
            }
        }

        /// <summary>
        /// Validates a service type.
        /// </summary>
        /// <param name="serviceType"></param>
        private void ValidateServiceType(ServiceType serviceType) {
            var fields = new Dictionary<string, FieldType>();
            if (serviceType.Request != null && serviceType.Request.Length > 0) {
                foreach (var field in serviceType.Request) {
                    ValidateFieldType(serviceType, fields, field);
                }
            }
            if (serviceType.Response != null && serviceType.Response.Length > 0) {
                foreach (var field in serviceType.Response) {
                    ValidateFieldType(serviceType, fields, field);
                }
            }
        }

        /// <summary>
        /// Validates a field type.
        /// </summary>
        /// <param name="datatype"></param>
        /// <param name="fields"></param>
        /// <param name="field"></param>
        private void ValidateFieldType(DataType datatype,
            Dictionary<string, FieldType> fields, FieldType field) {
            if (fields.ContainsKey(field.Name)) {
                throw new FormatException($"The field '{field.Name}' in complex type" +
                    $" '{datatype.Name}' already exists");
            }
            if (field.DataType.IsNullOrEmpty()) {
                if (field.ComplexType == null) {
                    throw new FormatException($"The field '{field.Name}' in complex type " +
                        $"'{datatype.Name}' has no data type.");
                }

                ValidateDataType(field.ComplexType);
                // ensure that datatype field always has a valid value.
                field.DataType = field.ComplexType.QName;
            }
            else {
                if (field.ComplexType != null) {
                    throw new FormatException($"The field '{field.Name}' in complex type " +
                        $"'{datatype.Name}' has an ambiguous data type.");
                }
                if (ResolveType(field.DataType) == null) {
                    throw new FormatException($"The field '{field.Name}' in complex type " +
                        $"'{datatype.Name}' has an unrecognized data type '{field.DataType}'.");
                }
            }
            fields.Add(field.Name, field);
        }

        /// <summary>
        /// Validates an enumerated type.
        /// </summary>
        /// <param name="enumeratedType"></param>
        private static void ValidateEnumeratedType(EnumeratedType enumeratedType) {
            if (enumeratedType.Value == null || enumeratedType.Value.Length == 0) {
                throw new FormatException($"The enumerated type '{enumeratedType.Name}' " +
                    $"does not have any values specified.");
            }
            var nextIndex = 0;
            var values = new Dictionary<string, EnumeratedValue>();

            foreach (var value in enumeratedType.Value) {
                if (values.ContainsKey(value.Name)) {
                    throw new FormatException($"The enumerated type '{enumeratedType.Name}' " +
                        $"has a duplicate value '{value.Value}'.");
                }
                if (!value.ValueSpecified) {
                    value.Value = nextIndex;
                    value.ValueSpecified = true;
                }
                else {
                    nextIndex = value.Value + 1;
                }
                values.Add(value.Name, value);
            }
        }
        private readonly Dictionary<XmlQualifiedName, DataType> _datatypes =
            new Dictionary<XmlQualifiedName, DataType>();
        private readonly TypeDictionary _dictionary;
        private readonly ITypeResolver _resolver;
    }
}
