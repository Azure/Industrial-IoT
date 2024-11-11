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
    using UANodeSet = Opc.Ua.Export.UANodeSet;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class AssetNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// Create node manager
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public AssetNodeManager(IServerInternal server, ApplicationConfiguration configuration,
            ILogger logger) : base(server, configuration)
        {
            SystemContext.NodeIdFactory = this;
            _logger = logger;

            var extension = configuration.ParseExtension<FolderConfiguration>();
            _folder = Path.Combine(extension?.CurrentDirectory
                ?? Directory.GetCurrentDirectory(), "settings");
            // create our settings folder, if required
            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder);
            }

            // in the node manager constructor, we add all namespaces
            var namespaceUris = new List<string>
            {
                Namespaces.AssetServer
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
                (ushort)Server.NamespaceUris.GetIndex(Namespaces.AssetServer));
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

                AddNodesFromEmbeddedNodesetXml();

                AddNodesForAssetManagement(objectsFolderReferences);

                foreach (var file in Directory.EnumerateFiles(_folder, "*.jsonld"))
                {
                    try
                    {
                        var contents = File.ReadAllText(file);

                        // parse WoT TD file contents
                        var td = JsonConvert.DeserializeObject<ThingDescription>(contents);
                        if (td?.Context == null || td.Name == null)
                        {
                            continue;
                        }
#pragma warning disable CA2000 // Dispose objects before losing scope
                        if (CreateAssetNode(td.Name, out var assetNode))
                        {
                            AddNodesForThingDescription(assetNode, td);
                        }
#pragma warning restore CA2000 // Dispose objects before losing scope
                    }
                    catch (Exception ex)
                    {
                        // skip this file, but log an error
                        _logger.LogError(ex, "Error parsing asset configuration file.");
                    }
                }

                AddReverseReferences(externalReferences);
                base.CreateAddressSpace(externalReferences);
            }
        }

        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                base.DeleteAddressSpace();
            }
        }

        internal ServiceResult OnCreateAsset(ISystemContext _context, MethodState _method,
            NodeId _objectId, string assetName, ref NodeId assetId)
        {
            lock (Lock)
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
        }

        internal ServiceResult OnDeleteAsset(ISystemContext _context, MethodState _method,
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

                if (_fileManagers.TryGetValue(assetId, out var filemanager))
                {
                    filemanager.Delete();  // Delete the file
                    filemanager.Dispose();
                    _fileManagers.Remove(assetId);
                }

                DeleteNode(SystemContext, assetId);

                _tags.Remove(assetName);
                _assets.Remove(assetName);

                foreach (var key in _uaVariables.Keys.Where(n => n.StartsWith(
                    assetName + ":", StringComparison.InvariantCulture)).ToList())
                {
                    _uaVariables.Remove(key);
                }

                return ServiceResult.Good;
            }
        }

        internal ServiceResult OnSimpleReadValue(ISystemContext context, NodeState node,
            ref object? value)
        {
            if (!TryGetBinding(node, out var assetInterface, out var assetTag))
            {
                return ServiceResult.Create(StatusCodes.BadInvalidState, "Asset not found");
            }
            return assetInterface.Read(assetTag, ref value);
        }

        internal ServiceResult OnSimpleWriteValue(ISystemContext context, NodeState node,
            ref object value)
        {
            if (!TryGetBinding(node, out var assetInterface, out var assetTag))
            {
                return ServiceResult.Create(StatusCodes.BadInvalidState, "Asset not found");
            }
            return assetInterface.Write(assetTag, ref value);
        }

        internal void OnDataChange(BaseVariableState variable, AssetTag assetTag,
            object? value, StatusCode statusCode, DateTime timestamp)
        {
            lock (Lock)
            {
                variable.Value = value;
                variable.StatusCode = statusCode;
                variable.Timestamp = timestamp;

                // notifies any monitored items that the value has changed.
                variable.ClearChangeMasks(SystemContext, false);
            }
        }

        /// <summary>
        /// Called after creating a MonitoredItem2.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="handle">The handle for the node.</param>
        /// <param name="monitoredItem">The monitored item.</param>
        protected override void OnMonitoredItemCreated(ServerSystemContext context,
            NodeHandle handle, MonitoredItem monitoredItem)
        {
            if (TryGetBinding(handle.Node, out var assetInterface, out var assetTag)
                && monitoredItem.MonitoringMode != MonitoringMode.Disabled
                && handle.Node is BaseVariableState source)
            {
                assetInterface.Observe(assetTag, monitoredItem.Id, (tag, value, status, timestamp)
                    => OnDataChange(source, tag, value, status, timestamp));
            }
        }

        /// <summary>
        /// Called after modifying a MonitoredItem2.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="handle">The handle for the node.</param>
        /// <param name="monitoredItem">The monitored item.</param>
        protected override void OnMonitoredItemModified(ServerSystemContext context,
            NodeHandle handle, MonitoredItem monitoredItem)
        {
            if (TryGetBinding(handle.Node, out var assetInterface, out var assetTag)
                && monitoredItem.MonitoringMode != MonitoringMode.Disabled
                && handle.Node is BaseVariableState source)
            {
                assetInterface.Unobserve(assetTag, monitoredItem.Id);
                assetInterface.Observe(assetTag, monitoredItem.Id, (tag, value, status, timestamp)
                    => OnDataChange(source, tag, value, status, timestamp));
            }
        }

        /// <summary>
        /// Called after deleting a MonitoredItem2.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="handle">The handle for the node.</param>
        /// <param name="monitoredItem">The monitored item.</param>
        protected override void OnMonitoredItemDeleted(ServerSystemContext context,
            NodeHandle handle, MonitoredItem monitoredItem)
        {
            if (TryGetBinding(handle.Node, out var assetInterface, out var assetTag)
                && handle.Node is BaseVariableState source)
            {
                assetInterface.Unobserve(assetTag, monitoredItem.Id);
            }
        }

        /// <summary>
        /// Called after changing the MonitoringMode for a MonitoredItem2.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="handle">The handle for the node.</param>
        /// <param name="monitoredItem">The monitored item.</param>
        /// <param name="previousMode">The previous monitoring mode.</param>
        /// <param name="monitoringMode">The current monitoring mode.</param>
        protected override void OnMonitoringModeChanged(ServerSystemContext context,
            NodeHandle handle, MonitoredItem monitoredItem, MonitoringMode previousMode,
            MonitoringMode monitoringMode)
        {
            if (TryGetBinding(handle.Node, out var assetInterface, out var assetTag)
                && handle.Node is BaseVariableState source)
            {
                if (previousMode != MonitoringMode.Disabled &&
                    monitoredItem.MonitoringMode == MonitoringMode.Disabled)
                {
                    assetInterface.Unobserve(assetTag, monitoredItem.Id);
                }

                if (previousMode == MonitoringMode.Disabled &&
                    monitoredItem.MonitoringMode != MonitoringMode.Disabled)
                {
                    assetInterface.Observe(assetTag, monitoredItem.Id, (tag, value, status, timestamp)
                        => OnDataChange(source, tag, value, status, timestamp));
                }
            }
        }

        public void AddNodesForThingDescription(NodeState parent, ThingDescription td)
        {
            lock (Lock)
            {
                AddNodesForThingDescriptionInternal(parent, td);
            }
        }

        private void AddNodesForThingDescriptionInternal(NodeState parent, ThingDescription td)
        {
            ArgumentNullException.ThrowIfNull(td.Context);

            // Limitation, the asset name must be the name we are using as thing name
            td.Name = parent.BrowseName.Name;

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
                                AddNodeAndAssetTagForWotProperty(parent, td,
                                    property.Key, property.Value, formString, assetId);
                            }
                        }
                    }
                }
            }
            _logger.LogInformation("Successfully parsed WoT file for asset: {AssetId}",
                assetId);
        }

        private bool CreateAssetNode(string assetName, out NodeState assetNode)
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

            var asset = new IWoTAssetTypeState(_assetManagement);
            asset.Create(SystemContext, NodeId.Null, new QualifiedName(assetName), null, true);

            _assetManagement.AddChild(asset);
            _fileManagers.Add(asset.NodeId, new FileManager(this, asset.WoTFile, _folder, _logger));

            AddPredefinedNode(SystemContext, asset);

            assetNode = asset;
            return true;
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
                new ExpandedNodeId(DataTypes.UriString), WoTConNamespaceIndex, new[]
                {
                    "https://www.w3.org/2019/wot/modbus",
                    "https://www.github.com/Azure/Industrial-IoT/sim"
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
            var stream = type.Assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                throw new ServiceResultException("Failed to load nodeset xml from embedded resource");
            }
            try
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
            finally
            {
                stream.Dispose();
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

        private void AddNodesFromEmbeddedNodesetXml()
        {
            var type = GetType().GetTypeInfo();
            var resourcePath =
$"{type.Assembly.GetName().Name}.Generated.{type.Namespace}.Design.{type.Namespace}.NodeSet2.xml";
            var stream = type.Assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                throw new ServiceResultException("Failed to load nodeset xml from embedded resource");
            }
            try
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
            finally
            {
                stream.Dispose();
            }
        }

        private void AddNodeAndAssetTagForWotProperty(NodeState assetFolder, ThingDescription td,
            string propertyName, Property property, string form, string assetId)
        {
            var assetTag = AddAssetTagForTdProperty(td, propertyName, property, form, assetId);

            // create an OPC UA variable optionally with a specified type.
            var variable = CreateAssetTagVariable(assetFolder, assetTag.Name,
                new ExpandedNodeId(DataTypes.Float), assetFolder.NodeId.NamespaceIndex,
                assetId, !property.ReadOnly);

            _uaVariables.Add($"{assetId}:{propertyName}", variable);
        }

        private AssetTag AddAssetTagForTdProperty(ThingDescription td, string propertyName,
            Property property, string form, string assetId)
        {
            // check if we need to create a new asset first
            if (!_tags.TryGetValue(assetId, out var tagList))
            {
                tagList = new Dictionary<string, AssetTag>();
                _tags.Add(assetId, tagList);
            }
            if (!Uri.TryCreate(td.Base, UriKind.Absolute, out var baseUri))
            {
                throw new FormatException("Missing uri information in thing description.");
            }
            else if (baseUri.Scheme == "modbus+tcp")
            {
                // create an asset tag and add to our list
                var modbusForm = JsonConvert.DeserializeObject<ModbusForm>(form);
                if (modbusForm?.Href != null &&
                    Uri.TryCreate(modbusForm.Href, UriKind.Relative, out var address))
                {
                    var tag = new AssetTag<ModbusForm>()
                    {
                        Form = modbusForm,
                        Name = propertyName,
                        Address = address
                    };
                    tagList.AddOrUpdate(propertyName, tag);
                    return tag;
                }
            }

            // else if ... add other protocols here

            else
            {
                // create an asset tag and add to our list
                var genform = JsonConvert.DeserializeObject<SimulatedForm>(form);
                if (genform?.Href != null &&
                    Uri.TryCreate(genform.Href, UriKind.Relative, out var address))
                {
                    var tag = new AssetTag<SimulatedForm>()
                    {
                        Form = genform,
                        Name = propertyName,
                        Address = address
                    };
                    tagList.AddOrUpdate(propertyName, tag);
                    return tag;
                }
            }
            throw new FormatException("TD Format wrong or unsupported.");
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

                // default
                assetInterface ??= new SimulatedAsset();
                _assets.Add(td.Name, assetInterface);
            }

            return td.Name;
        }

        private BaseDataVariableState CreateVariable(NodeState parent, string name,
            ExpandedNodeId type, ushort namespaceIndex, object? value)
        {
            var referenceType = ExpandedNodeId.ToNodeId(ReferenceTypeIds.HasWoTComponent,
                Server.NamespaceUris);
            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = referenceType,
                NodeId = new NodeId(name, namespaceIndex),
                BrowseName = new QualifiedName(name, namespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                AccessLevel = AccessLevels.CurrentRead,
                DataType = ExpandedNodeId.ToNodeId(type, Server.NamespaceUris),
                Value = value
            };

            parent.AddReference(referenceType, false, variable.NodeId);
            variable.AddReference(referenceType, true, parent.NodeId);

            AddPredefinedNode(SystemContext, variable);
            return variable;
        }

        private BaseDataVariableState CreateAssetTagVariable(NodeState parent, string name,
            ExpandedNodeId type, ushort namespaceIndex, string assetId, bool writeable = false)
        {
            var referenceType = ExpandedNodeId.ToNodeId(ReferenceTypeIds.HasWoTComponent,
              Server.NamespaceUris);
            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                Handle = assetId,
                ReferenceTypeId = referenceType,
                NodeId = new NodeId(name, namespaceIndex),
                BrowseName = new QualifiedName(name, namespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                AccessLevel = AccessLevels.CurrentRead,
                DataType = ExpandedNodeId.ToNodeId(type, Server.NamespaceUris),
                OnSimpleReadValue = new NodeValueSimpleEventHandler(OnSimpleReadValue)
            };
            if (writeable)
            {
                variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
                variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                variable.UserWriteMask = AttributeWriteMask.ValueForVariableType;
                variable.WriteMask = AttributeWriteMask.ValueForVariableType;
                variable.OnSimpleWriteValue = new NodeValueSimpleEventHandler(OnSimpleWriteValue);
            }

            parent.AddReference(referenceType, false, variable.NodeId);
            variable.AddReference(referenceType, true, parent.NodeId);

            AddPredefinedNode(SystemContext, variable);
            return variable;
        }

        private bool TryGetBinding(NodeState node, [NotNullWhen(true)] out IAsset? assetInterface,
            [NotNullWhen(true)] out AssetTag? assetTag)
        {
            if (node.Handle is not string assetId ||
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
