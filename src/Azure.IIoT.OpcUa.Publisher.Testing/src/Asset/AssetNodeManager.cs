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

#nullable enable

namespace Asset
{
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using UANodeSet = Opc.Ua.Export.UANodeSet;

    public class AssetNodeManager : CustomNodeManager2
    {
        public AssetNodeManager(IServerInternal server, ApplicationConfiguration configuration,
            ILogger logger)
            : base(server, configuration)
        {
            SystemContext.NodeIdFactory = this;
            _logger = logger;
            _folder = Path.Combine(Directory.GetCurrentDirectory(), "settings");

            // create our settings folder, if required
            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder);
            }

            // in the node manager constructor, we add all namespaces
            List<string> namespaceUris = new()
            {
                "http://opcfoundation.org/UA/EdgeTranslator/"
            };

            LoadNamespaceUrisFromEmbeddedNodesetXml(namespaceUris);

            // add a seperate namespace for each asset from the WoT TD files
            foreach (var file in Directory.EnumerateFiles(_folder, "*.jsonld"))
            {
                try
                {
                    var contents = File.ReadAllText(file);

                    // parse WoT TD file contents
                    var td = JsonConvert.DeserializeObject<ThingDescription>(contents)
                        ?? throw ServiceResultException.Create(
                            StatusCodes.BadConfigurationError, "Bad description");

                    namespaceUris.Add("http://opcfoundation.org/UA/" + td.Name + "/");

                    AddNamespacesFromCompanionSpecs(namespaceUris, td);
                }
                catch (Exception ex)
                {
                    // skip this file, but log an error
                    _logger.LogError(ex, "Error");
                }
            }

            NamespaceUris = namespaceUris;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (Lock)
                {
                    foreach (var manager in _fileManagers.Values)
                    {
                        manager.Dispose();
                    }

                    foreach (var asset in _assets.Values)
                    {
                        asset.Dispose();
                    }

                    _fileManagers.Clear();
                    _assetManagement.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        public override NodeId New(ISystemContext context, NodeState node)
        {
            // for new nodes we create, pick our default namespace
            return new NodeId(Utils.IncrementIdentifier(ref _lastUsedId),
                (ushort)Server.NamespaceUris.GetIndex("http://opcfoundation.org/UA/EdgeTranslator/"));
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                // in the create address space call, we add all our nodes

                IList<IReference>? objectsFolderReferences = null;
                if (!externalReferences.TryGetValue(Opc.Ua.ObjectIds.ObjectsFolder,
                    out objectsFolderReferences))
                {
                    externalReferences[Opc.Ua.ObjectIds.ObjectsFolder]
                        = objectsFolderReferences = new List<IReference>();
                }

                AddNodesFromNodesetXml("Opc.Ua.WotCon.NodeSet2.xml");

                AddNodesForAssetManagement(objectsFolderReferences);

                foreach (var file in Directory.EnumerateFiles(_folder, "*.jsonld"))
                {
                    try
                    {
                        var contents = File.ReadAllText(file);
                        var fileName = Path.GetFileNameWithoutExtension(file);

#pragma warning disable CA2000 // Dispose objects before losing scope
                        if (!CreateAssetNode(fileName, out var assetNode))
                        {
                            throw ServiceResultException.Create(
                                StatusCodes.BadConfigurationError, "Asset already exists");
                        }
#pragma warning restore CA2000 // Dispose objects before losing scope

                        AddNodesForWoTProperties(assetNode, contents);
                    }
                    catch (Exception ex)
                    {
                        // skip this file, but log an error
                        _logger.LogError(ex, "Error");
                    }
                }

                AddReverseReferences(externalReferences);
                base.CreateAddressSpace(externalReferences);
            }
        }

        private void AddNodesForAssetManagement(IList<IReference> objectsFolderReferences)
        {
            var WoTConNamespaceIndex = (ushort)Server.NamespaceUris.GetIndex(Namespaces.WoT_Con);
            var assetManagementPassiveNode = (BaseObjectState)FindPredefinedNode(
                new NodeId(Objects.WoTAssetConnectionManagement, WoTConNamespaceIndex),
                typeof(BaseObjectState));
            _assetManagement.Create(SystemContext, assetManagementPassiveNode);

            var createAssetPassiveNode = (MethodState)FindPredefinedNode(
                new NodeId(Methods.WoTAssetConnectionManagement_CreateAsset,
                WoTConNamespaceIndex), typeof(MethodState));
            _assetManagement.CreateAsset = new(null);
            _assetManagement.CreateAsset.Create(SystemContext, createAssetPassiveNode);
            _assetManagement.CreateAsset.OnCall =
                new CreateAssetMethodStateMethodCallHandler(OnCreateAsset);

            var createAssetInputArgumentsPassiveNode = (BaseVariableState)FindPredefinedNode(
                new NodeId(Variables.WoTAssetConnectionManagementType_CreateAsset_InputArguments,
                WoTConNamespaceIndex), typeof(BaseVariableState));
            _assetManagement.CreateAsset.InputArguments = new(null);
            _assetManagement.CreateAsset.InputArguments.Create(SystemContext,
                createAssetInputArgumentsPassiveNode);

            var createAssetOutputArgumentsPassiveNode = (BaseVariableState)FindPredefinedNode(
                new NodeId(Variables.WoTAssetConnectionManagementType_CreateAsset_OutputArguments,
                WoTConNamespaceIndex), typeof(BaseVariableState));
            _assetManagement.CreateAsset.OutputArguments = new(null);
            _assetManagement.CreateAsset.OutputArguments.Create(SystemContext,
                createAssetOutputArgumentsPassiveNode);

            var deleteAssetPassiveNode = (MethodState)FindPredefinedNode(
                new NodeId(Methods.WoTAssetConnectionManagement_DeleteAsset,
                WoTConNamespaceIndex), typeof(MethodState));
            _assetManagement.DeleteAsset = new(null);
            _assetManagement.DeleteAsset.Create(SystemContext, deleteAssetPassiveNode);
            _assetManagement.DeleteAsset.OnCall =
                new DeleteAssetMethodStateMethodCallHandler(OnDeleteAsset);

            var deleteAssetInputArgumentsPassiveNode = (BaseVariableState)FindPredefinedNode(
                new NodeId(Variables.WoTAssetConnectionManagementType_DeleteAsset_InputArguments,
                WoTConNamespaceIndex), typeof(BaseVariableState));
            _assetManagement.DeleteAsset.InputArguments = new(null);
            _assetManagement.DeleteAsset.InputArguments.Create(SystemContext,
                deleteAssetInputArgumentsPassiveNode);

            // create a variable listing our supported WoT protocol bindings
#pragma warning disable CA2000 // Dispose objects before losing scope
            CreateVariable(_assetManagement, "SupportedWoTBindings",
                new ExpandedNodeId(DataTypes.UriString), WoTConNamespaceIndex, new []
                {
                    "https://www.w3.org/2019/wot/modbus",
                    "https://www.w3.org/2019/wot/opcua",
                    "https://www.w3.org/2019/wot/s7",
                    "https://www.w3.org/2019/wot/mcp",
                    "https://www.w3.org/2019/wot/eip",
                    "https://www.w3.org/2019/wot/ads"
                });
#pragma warning restore CA2000 // Dispose objects before losing scope

            // add everything to our server namespace
            objectsFolderReferences.Add(new NodeStateReference(Opc.Ua.ReferenceTypes.Organizes,
                false, _assetManagement.NodeId));
            AddPredefinedNode(SystemContext, _assetManagement);
        }

        private void AddNamespacesFromCompanionSpecs(List<string> namespaceUris, ThingDescription td)
        {
            ArgumentNullException.ThrowIfNull(td.Context);
            // check if an OPC UA companion spec is mentioned in the WoT TD file
            foreach (var ns in td.Context)
            {
                var nsUri = ns.ToString() ?? string.Empty;
                if (nsUri.Contains("https://www.w3.org/", StringComparison.InvariantCulture) &&
                    !nsUri.Contains("opcua", StringComparison.InvariantCulture))
                {
                    continue;
                }

                var namespaces = JsonConvert.DeserializeObject<OpcUaNamespaces>(nsUri);
                if (namespaces?.Namespaces == null)
                {
                    continue;
                }

                foreach (var opcuaCompanionSpecUrl in namespaces.Namespaces)
                {
                    // support local Nodesets
                    if (!opcuaCompanionSpecUrl.IsAbsoluteUri ||
                            (!opcuaCompanionSpecUrl.AbsoluteUri.Contains("http://",
                                StringComparison.InvariantCulture) &&
                             !opcuaCompanionSpecUrl.AbsoluteUri.Contains("https://",
                                StringComparison.InvariantCulture)))
                    {
                        var nodesetFile = string.Empty;
                        if (Path.IsPathFullyQualified(opcuaCompanionSpecUrl.OriginalString))
                        {
                            // absolute file path
                            nodesetFile = opcuaCompanionSpecUrl.OriginalString;
                        }
                        else
                        {
                            // relative file path
                            nodesetFile = Path.Combine(Directory.GetCurrentDirectory(),
                                opcuaCompanionSpecUrl.OriginalString);
                        }

                        _logger.LogInformation("Loading nodeset from local file {File}", nodesetFile);
                        LoadNamespaceUrisFromNodesetXml(namespaceUris, nodesetFile);
                    }
                }
            }
        }

        private void AddNodesFromCompanionSpecs(ThingDescription td)
        {
            ArgumentNullException.ThrowIfNull(td.Context);

            foreach (var ns in td.Context)
            {
                var nsUri = ns.ToString() ?? string.Empty;
                if (nsUri.Contains("https://www.w3.org/", StringComparison.InvariantCulture) &&
                    !nsUri.Contains("opcua", StringComparison.InvariantCulture))
                {
                    continue;
                }

                var namespaces = JsonConvert.DeserializeObject<OpcUaNamespaces>(nsUri);
                if (namespaces?.Namespaces == null)
                {
                    continue;
                }
                foreach (var opcuaCompanionSpecUrl in namespaces.Namespaces)
                {
                    // support local Nodesets
                    if (!opcuaCompanionSpecUrl.IsAbsoluteUri
                        || (!opcuaCompanionSpecUrl.AbsoluteUri.Contains("http://",
                                StringComparison.InvariantCulture) &&
                            !opcuaCompanionSpecUrl.AbsoluteUri.Contains("https://",
                                StringComparison.InvariantCulture)))
                    {
                        var nodesetFile = string.Empty;
                        if (Path.IsPathFullyQualified(opcuaCompanionSpecUrl.OriginalString))
                        {
                            // absolute file path
                            nodesetFile = opcuaCompanionSpecUrl.OriginalString;
                        }
                        else
                        {
                            // relative file path
                            nodesetFile = Path.Combine(Directory.GetCurrentDirectory(),
                                opcuaCompanionSpecUrl.OriginalString);
                        }

                        _logger.LogInformation("Adding node set from local nodeset file");
                        AddNodesFromNodesetXml(nodesetFile);
                    }
                }
            }
        }

        private static void LoadNamespaceUrisFromNodesetXml(List<string> namespaceUris,
            string nodesetFile)
        {
            using (FileStream stream = new(nodesetFile, FileMode.Open, FileAccess.Read))
            {
                var nodeSet = UANodeSet.Read(stream);

                if (nodeSet.NamespaceUris?.Length > 0)
                {
                    foreach (var ns in nodeSet.NamespaceUris)
                    {
                        if (!namespaceUris.Contains(ns))
                        {
                            namespaceUris.Add(ns);
                        }
                    }
                }
            }
        }

        private void LoadNamespaceUrisFromEmbeddedNodesetXml(List<string> namespaceUris)
        {
            var type = GetType().GetTypeInfo();
            var resourcePath =
$"{type.Assembly.GetName().Name}.Generated.{type.Namespace}.Design.{type.Namespace}.NodeSet2.xml";
            Stream? istrm = type.Assembly.GetManifestResourceStream(resourcePath);
            if (istrm != null)
            {
                throw new ServiceResultException("Failed to load nodeset xml from embedded resource");
            }
            using (FileStream stream = new FileInfo(resourcePath).OpenRead())
            {
                var nodeSet = UANodeSet.Read(stream);

                if (nodeSet.NamespaceUris?.Length > 0)
                {
                    foreach (var ns in nodeSet.NamespaceUris)
                    {
                        if (!namespaceUris.Contains(ns))
                        {
                            namespaceUris.Add(ns);
                        }
                    }
                }
            }
        }

        private void AddNodesFromNodesetXml(string nodesetFile)
        {
            using (Stream stream = new FileStream(nodesetFile, FileMode.Open))
            {
                var nodeSet = UANodeSet.Read(stream);

                var predefinedNodes = new NodeStateCollection();

                nodeSet.Import(SystemContext, predefinedNodes);

                for (var i = 0; i < predefinedNodes.Count; i++)
                {
                    try
                    {
                        AddPredefinedNode(SystemContext, predefinedNodes[i]);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error");
                    }
                }
            }
        }

        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                base.DeleteAddressSpace();
            }
        }

        private ServiceResult OnCreateAsset(ISystemContext _context, MethodState _method,
            NodeId _objectId, string assetName, ref NodeId assetId)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                    "Argument invalid");
            }

#pragma warning disable CA2000 // Dispose objects before losing scope
            var success = CreateAssetNode(assetName, out var assetNode);
#pragma warning restore CA2000 // Dispose objects before losing scope
            if (!success)
            {
                return new ServiceResult(StatusCodes.BadBrowseNameDuplicated,
                    new LocalizedText(assetNode.NodeId.ToString()));
            }
            assetId = assetNode.NodeId;
            return ServiceResult.Good;
        }

        private bool CreateAssetNode(string assetName, out NodeState assetNode)
        {
            lock (Lock)
            {
                // check if the asset node already exists
                var browser = _assetManagement.CreateBrowser(SystemContext, null,
                    null, false, BrowseDirection.Forward, null, null, true);
                var reference = browser.Next();
                while ((reference != null) && (reference is NodeStateReference))
                {
                    var node = reference as NodeStateReference;
                    if ((node?.Target != null) && (node.Target.DisplayName.Text == assetName))
                    {
                        assetNode = node.Target;
                        return false;
                    }

                    reference = browser.Next();
                }

                IWoTAssetTypeState asset = new(null);
                asset.Create(SystemContext, new NodeId(), new QualifiedName(assetName), null, true);

                _assetManagement.AddChild(asset);
                _fileManagers.Add(asset.NodeId, new FileManager(this, asset.WoTFile, _logger));

                AddPredefinedNode(SystemContext, asset);

                assetNode = asset;
                return true;
            }
        }

        private ServiceResult OnDeleteAsset(ISystemContext _context, MethodState _method,
            NodeId _objectId, NodeId assetId)
        {
            lock (Lock)
            {
                var asset = FindPredefinedNode(assetId, typeof(IWoTAssetTypeState));
                if (asset == null)
                {
                    return ServiceResult.Create(StatusCodes.BadNodeIdUnknown, "Not found");
                }

                var assetName = asset.DisplayName.Text;

                _fileManagers.Remove(assetId);

                DeleteNode(SystemContext, assetId);

                foreach (var file in Directory.EnumerateFiles(_folder, "*.jsonld"))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName == assetName)
                    {
                        File.Delete(file);
                    }
                }

                _tags.Remove(assetName);

                _assets.Remove(assetName);

                var i = 0;
                while (i < _uaVariables.Count)
                {
                    if (_uaVariables.Keys.ToArray()[i].StartsWith(
                        assetName + ":", StringComparison.InvariantCulture))
                    {
                        _uaVariables.Remove(_uaVariables.Keys.ToArray()[i]);
                    }
                    else
                    {
                        i++;
                    }
                }

                return ServiceResult.Good;
            }
        }

        public void AddNodesForWoTProperties(NodeState parent, string contents)
        {
            // parse WoT TD file contents
            var td = JsonConvert.DeserializeObject<ThingDescription>(contents);
            if (td?.Context == null)
            {
                return;
            }

            var newNamespace = "http://opcfoundation.org/UA/" + td.Name + "/";
            List<string> namespaceUris = new(NamespaceUris);
            if (!namespaceUris.Contains(newNamespace))
            {
                namespaceUris.Add(newNamespace);
            }

            foreach (var ns in td.Context)
            {
                var nsUri = ns.ToString() ?? string.Empty;
                if (!nsUri.Contains("https://www.w3.org/", StringComparison.InvariantCulture) &&
                    nsUri.Contains("opcua", StringComparison.InvariantCulture))
                {
                    var namespaces = JsonConvert.DeserializeObject<OpcUaNamespaces>(nsUri);
                    if (namespaces?.Namespaces == null)
                    {
                        continue;
                    }
                    foreach (var opcuaCompanionSpecUrl in namespaces.Namespaces)
                    {
                        namespaceUris.Add(opcuaCompanionSpecUrl.ToString());
                    }
                }
            }

            AddNamespacesFromCompanionSpecs(namespaceUris, td);

            NamespaceUris = namespaceUris;

            AddNodesFromCompanionSpecs(td);

            var assetId = GetOrAddAssetForThing(td);

            // create nodes for each TD property
            if (td.Properties != null)
            {
                foreach (var property in td.Properties)
                {
                    if (property.Value.Forms != null)
                    {
                        foreach (var form in property.Value.Forms)
                        {
                            var formString = form?.ToString();
                            if (formString != null)
                            {
                                AddNodeForWoTForm(parent, td, property,
                                    formString, assetId);
                            }
                        }
                    }
                }
            }
            _logger.LogInformation("Successfully parsed WoT file for asset: {AssetId}",
                assetId);
        }

        private void AddNodeForWoTForm(NodeState assetFolder, ThingDescription td,
            KeyValuePair<string, Property> property, string form, string assetId)
        {
            if (td.Base == null)
            {
                return;
            }

            string variableId;
            string variableName;
            if (string.IsNullOrEmpty(property.Value.OpcUaNodeId))
            {
                variableId = $"{assetId}:{property.Key}";
                variableName = property.Key;
            }
            else
            {
                variableId = $"{assetId}:{property.Value.OpcUaNodeId}";
                variableName = property.Value.OpcUaNodeId.Substring(
                    property.Value.OpcUaNodeId.IndexOf('=', StringComparison.InvariantCulture) + 1);
            }

            var fieldPath = string.Empty;

            // create an OPC UA variable optionally with a specified type.
            BaseDataVariableState? variable = null;
            if (!string.IsNullOrEmpty(property.Value.OpcUaType))
            {
                var dataType = ExpandedNodeId.Parse(property.Value.OpcUaType);
                if (!NodeId.IsNull(dataType))
                {
                    variable = CreateAssetTagVariable(assetFolder, variableName,
                        dataType, assetFolder.NodeId.NamespaceIndex, assetId);
                }
            }
            variable ??= CreateAssetTagVariable(assetFolder, variableName,
                new ExpandedNodeId(DataTypes.Float), assetFolder.NodeId.NamespaceIndex, assetId);
            _uaVariables.Add(variableId, variable);

            // check if we need to create a new asset first
            if (!_tags.TryGetValue(assetId, out var value))
            {
                value = new Dictionary<string, AssetTag>();
                _tags.Add(assetId, value);
            }

            if (td.Base.StartsWith("modbus+tcp://", StringComparison.InvariantCultureIgnoreCase))
            {
                // create an asset tag and add to our list
                var modbusForm = JsonConvert.DeserializeObject<ModbusForm>(form);
                if (modbusForm?.Href != null &&
                    Uri.TryCreate(modbusForm.Href, UriKind.Absolute, out var address))
                {
                    var tag = new AssetTag<ModbusForm>()
                    {
                        Form = modbusForm,
                        Name = variableId,
                        Address = address,
                        MappedUAExpandedNodeID = NodeId.ToExpandedNodeId(
                            _uaVariables[variableId].NodeId, Server.NamespaceUris).ToString(),
                        MappedUAFieldPath = fieldPath
                    };
                    value.Add(variableName, tag);
                }
            }

            // else if ... add other protocols here

            else
            {
                // create an asset tag and add to our list
                var genform = JsonConvert.DeserializeObject<GenericForm>(form);
                if (genform?.Href != null &&
                    Uri.TryCreate(genform.Href, UriKind.Absolute, out var address))
                {
                    var tag = new AssetTag<GenericForm>()
                    {
                        Form = genform,
                        Name = variableId,
                        Address = address,
                        MappedUAExpandedNodeID = NodeId.ToExpandedNodeId(
                            _uaVariables[variableId].NodeId, Server.NamespaceUris).ToString(),
                        MappedUAFieldPath = fieldPath
                    };
                    value.Add(variableName, tag);
                }
            }
        }

        private string GetOrAddAssetForThing(ThingDescription td)
        {
            ArgumentNullException.ThrowIfNull(td);
            ArgumentNullException.ThrowIfNull(td.Base);
            ArgumentNullException.ThrowIfNull(td.Name);

            if (!_assets.TryGetValue(td.Name, out var assetInterface))
            {
                var parsedUri = new Uri(td.Base);

                // Try Create modbus asset
                if (ModbusTcpAsset.TryConnect(parsedUri, _logger, out assetInterface))
                {
                    _logger.LogInformation("Created modbus asset {AssetId}", td.Name);
                }

                // else ...

                assetInterface ??= new SimulatedAsset();
                _assets.Add(td.Name, assetInterface);
            }
            return td.Name;
        }

        private BaseDataVariableState CreateVariable(NodeState parent, string name,
            ExpandedNodeId type, ushort namespaceIndex, object? value = null)
        {
            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = Opc.Ua.ReferenceTypes.Organizes,
                NodeId = new NodeId(name, namespaceIndex),
                BrowseName = new QualifiedName(name, namespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                AccessLevel = AccessLevels.CurrentRead,
                DataType = ExpandedNodeId.ToNodeId(type, Server.NamespaceUris),
                Value = value
            };

            parent?.AddChild(variable);
            parent?.AddReference(ExpandedNodeId.ToNodeId(ReferenceTypeIds.HasWoTComponent,
                Server.NamespaceUris), false, variable.NodeId);

            AddPredefinedNode(SystemContext, variable);
            return variable;
        }

        private BaseDataVariableState CreateAssetTagVariable(NodeState parent, string name,
            ExpandedNodeId type, ushort namespaceIndex, string assetId, object? value = null)
        {
            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                Handle = assetId,
                ReferenceTypeId = Opc.Ua.ReferenceTypes.Organizes,
                NodeId = new NodeId(name, namespaceIndex),
                BrowseName = new QualifiedName(name, namespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                AccessLevel = AccessLevels.CurrentRead,
                DataType = ExpandedNodeId.ToNodeId(type, Server.NamespaceUris),
                Value = value
            };

            parent?.AddChild(variable);
            parent?.AddReference(ExpandedNodeId.ToNodeId(ReferenceTypeIds.HasWoTComponent,
                Server.NamespaceUris), false, variable.NodeId);

            variable.OnSimpleReadValue = new NodeValueSimpleEventHandler(OnSimpleReadValue);
            variable.OnSimpleWriteValue = new NodeValueSimpleEventHandler(OnSimpleWriteValue);

            AddPredefinedNode(SystemContext, variable);
            return variable;
        }

        private ServiceResult OnSimpleReadValue(ISystemContext context, NodeState node,
            ref object? value)
        {
            if (!TryGetBinding(node, out var assetInterface, out var assetTag))
            {
                return ServiceResult.Create(StatusCodes.BadInvalidState, "Asset not found");
            }
            return assetInterface.Read(assetTag, ref value);
        }

        private ServiceResult OnSimpleWriteValue(ISystemContext context, NodeState node,
            ref object value)
        {
            if (!TryGetBinding(node, out var assetInterface, out var assetTag))
            {
                return ServiceResult.Create(StatusCodes.BadInvalidState, "Asset not found");
            }
            return assetInterface.Write(assetTag, ref value);
        }

        private bool TryGetBinding(NodeState node, [NotNullWhen(true)] out IAsset? assetInterface,
            [NotNullWhen(true)] out AssetTag? assetTag)
        {
            var assetId = node.Handle as string;
            if (assetId == null ||
                !_assets.TryGetValue(assetId, out assetInterface) ||
                !_tags.TryGetValue(assetId, out var tag) ||
                !tag.TryGetValue(node.SymbolicName, out assetTag))
            {
                assetInterface = null;
                assetTag = null;
                return false;
            }
            return true;
        }

        private readonly WoTAssetConnectionManagementTypeState _assetManagement = new(null);
        private readonly Dictionary<string, BaseDataVariableState> _uaVariables = new();
        private readonly Dictionary<string, IAsset> _assets = new();
        private readonly Dictionary<string, Dictionary<string, AssetTag>> _tags = new();
        private readonly Dictionary<NodeId, FileManager> _fileManagers = new();
        private readonly ILogger _logger;
        private readonly string _folder;
        private long _lastUsedId;
    }
}
