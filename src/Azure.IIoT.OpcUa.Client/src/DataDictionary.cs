/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
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

namespace Opc.Ua.Client
{
    using Opc.Ua.Schema;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A class that holds the configuration for a UA service.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix"), DataContract(Namespace = Namespaces.OpcUaXsd)]
    public class DataDictionary
    {
        /// <summary>
        /// The default constructor.
        /// </summary>
        /// <param name="session"></param>
        public DataDictionary(ISession session)
        {
            Initialize();
            m_session = session;
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private void Initialize()
        {
            m_session = null;
            DataTypes = new Dictionary<NodeId, QualifiedName>();
            m_validator = null;
            TypeSystemId = null;
            TypeSystemName = null;
            DictionaryId = null;
            Name = null;
        }

        /// <summary>
        /// The node id for the dictionary.
        /// </summary>
        public NodeId DictionaryId { get; private set; }

        /// <summary>
        /// The display name for the dictionary.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The node id for the type system.
        /// </summary>
        public NodeId TypeSystemId { get; private set; }

        /// <summary>
        /// The display name for the type system.
        /// </summary>
        public string TypeSystemName { get; private set; }

        /// <summary>
        /// The type dictionary.
        /// </summary>
        public Schema.Binary.TypeDictionary TypeDictionary { get; private set; }

        /// <summary>
        /// The data type dictionary DataTypes
        /// </summary>
        public Dictionary<NodeId, QualifiedName> DataTypes { get; private set; }

        /// <summary>
        /// Loads the dictionary identified by the node id.
        /// </summary>
        /// <param name="dictionaryId"></param>
        /// <param name="name"></param>
        /// <param name="schema"></param>
        /// <param name="imports"></param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionaryId"/> is <c>null</c>.</exception>
        /// <exception cref="ServiceResultException"></exception>
        public void Load(NodeId dictionaryId, string name, byte[] schema = null, IDictionary<string, byte[]> imports = null)
        {
            ArgumentNullException.ThrowIfNull(dictionaryId);

            GetTypeSystem(dictionaryId);

            if (schema == null || schema.Length == 0)
            {
                schema = ReadDictionary(dictionaryId);
            }

            if (schema == null || schema.Length == 0)
            {
                throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Cannot parse empty data dictionary.");
            }

            // Interoperability: some server may return a null terminated dictionary string, adjust length
            var zeroTerminator = Array.IndexOf<byte>(schema, 0);
            if (zeroTerminator >= 0)
            {
                Array.Resize(ref schema, zeroTerminator);
            }

            Validate(schema, imports);

            ReadDataTypes(dictionaryId);

            DictionaryId = dictionaryId;
            Name = name;
        }

        /// <summary>
        /// Retrieves the type system for the dictionary.
        /// </summary>
        /// <param name="dictionaryId"></param>
        private void GetTypeSystem(NodeId dictionaryId)
        {
            var references = m_session.NodeCache.FindReferences(dictionaryId, ReferenceTypeIds.HasComponent, true, false);
            if (references.Count > 0)
            {
                TypeSystemId = ExpandedNodeId.ToNodeId(references[0].NodeId, m_session.NamespaceUris);
                TypeSystemName = references[0].ToString();
            }
        }

        /// <summary>
        /// Retrieves the data types in the dictionary.
        /// </summary>
        /// <param name="dictionaryId"></param>
        /// <remarks>
        /// In order to allow for fast Linq matching of dictionary
        /// QNames with the data type nodes, the BrowseName of
        /// the DataType node is replaced with Value string.
        /// </remarks>
        private void ReadDataTypes(NodeId dictionaryId)
        {
            var references = m_session.NodeCache.FindReferences(dictionaryId, ReferenceTypeIds.HasComponent, false, false);
            IList<NodeId> nodeIdCollection = references.Select(node => ExpandedNodeId.ToNodeId(node.NodeId, m_session.NamespaceUris)).ToList();

            // read the value to get the names that are used in the dictionary
            m_session.ReadValues(nodeIdCollection, out var values, out var errors);

            var ii = 0;
            foreach (var reference in references)
            {
                var datatypeId = ExpandedNodeId.ToNodeId(reference.NodeId, m_session.NamespaceUris);
                if (datatypeId != null)
                {
                    if (ServiceResult.IsGood(errors[ii]))
                    {
                        var dictName = (String)values[ii].Value;
                        DataTypes[datatypeId] = new QualifiedName(dictName, datatypeId.NamespaceIndex);
                    }
                    ii++;
                }
            }
        }

        /// <summary>
        /// Reads the contents of multiple data dictionaries.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="dictionaryIds"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        public static async Task<IDictionary<NodeId, byte[]>> ReadDictionaries(
            ISessionClientMethods session,
            IList<NodeId> dictionaryIds,
            CancellationToken ct = default)
        {
            var result = new Dictionary<NodeId, byte[]>();
            if (dictionaryIds.Count == 0)
            {
                return result;
            }

            var itemsToRead = new ReadValueIdCollection();
            foreach (var nodeId in dictionaryIds)
            {
                // create item to read.
                var itemToRead = new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value,
                    IndexRange = null,
                    DataEncoding = null
                };
                itemsToRead.Add(itemToRead);
            }

            // read values.
            var readResponse = await session.ReadAsync(
                null,
                0,
                TimestampsToReturn.Neither,
                itemsToRead,
                ct).ConfigureAwait(false);

            var values = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;
            var response = readResponse.ResponseHeader;

            ClientBase.ValidateResponse(values, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            var ii = 0;
            foreach (var nodeId in dictionaryIds)
            {
                // check for error.
                if (StatusCode.IsBad(values[ii].StatusCode))
                {
                    var sr = ClientBase.GetResult(values[ii].StatusCode, 0, diagnosticInfos, response);
                    throw new ServiceResultException(sr);
                }

                // return as a byte array.
                result[nodeId] = values[ii].Value as byte[];
                ii++;
            }

            return result;
        }

        /// <summary>
        /// Reads the contents of a data dictionary.
        /// </summary>
        /// <param name="dictionaryId"></param>
        /// <exception cref="ServiceResultException"></exception>
        public byte[] ReadDictionary(NodeId dictionaryId)
        {
            // create item to read.
            var itemToRead = new ReadValueId
            {
                NodeId = dictionaryId,
                AttributeId = Attributes.Value,
                IndexRange = null,
                DataEncoding = null
            };

            var itemsToRead = new ReadValueIdCollection {
                itemToRead
            };

            // read value.
            DataValueCollection values;
            DiagnosticInfoCollection diagnosticInfos;

            var responseHeader = m_session.Read(
                null,
                0,
                TimestampsToReturn.Neither,
                itemsToRead,
                out values,
                out diagnosticInfos);

            ClientBase.ValidateResponse(values, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            // check for error.
            if (StatusCode.IsBad(values[0].StatusCode))
            {
                var result = ClientBase.GetResult(values[0].StatusCode, 0, diagnosticInfos, responseHeader);
                throw new ServiceResultException(result);
            }

            // return as a byte array.
            return values[0].Value as byte[];
        }

        /// <summary>
        /// Validates the type dictionary.
        /// </summary>
        /// <param name="dictionary">The encoded dictionary to validate.</param>
        /// <param name="imports">A table of imported namespace schemas.</param>
        /// <param name="throwOnError">Throw if an error occurred.</param>
        internal void Validate(byte[] dictionary, IDictionary<string, byte[]> imports = null, bool throwOnError = false)
        {
            var istrm = new MemoryStream(dictionary);

            if (TypeSystemId == Objects.XmlSchema_TypeSystem)
            {
                var validator = new Schema.Xml.XmlSchemaValidator(imports);

                try
                {
                    validator.Validate(istrm);
                }
                catch (Exception e) when (!throwOnError)
                {
                    Utils.LogWarning(e, "Could not validate XML schema, error is ignored.");
                }

                m_validator = validator;
            }

            if (TypeSystemId == Objects.OPCBinarySchema_TypeSystem)
            {
                var validator = new Schema.Binary.BinarySchemaValidator(imports);
                try
                {
                    validator.Validate(istrm);
                }
                catch (Exception e) when (!throwOnError)
                {
                    Utils.LogWarning(e, "Could not validate binary schema, error is ignored.");
                }

                m_validator = validator;
                TypeDictionary = validator.Dictionary;
            }
        }

        private ISession m_session;
        private SchemaValidator m_validator;
    }
}
