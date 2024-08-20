/* ========================================================================
 * Copyright (c) 2005-2024 The OPC Foundation, Inc. All rights reserved.
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

using Opc.Ua;
using System;
using System.Collections.Generic;

namespace Asset
{
    #region WoTAssetConnectionManagementTypeState Class
#if (!OPCUA_EXCLUDE_WoTAssetConnectionManagementTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class WoTAssetConnectionManagementTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public WoTAssetConnectionManagementTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Asset.ObjectTypes.WoTAssetConnectionManagementType, Asset.Namespaces.WoT_Con, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <remarks />
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <remarks />
        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <remarks />
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGCAAgEAAAABACgA" +
           "AABXb1RBc3NldENvbm5lY3Rpb25NYW5hZ2VtZW50VHlwZUluc3RhbmNlAQEBAAEBAQABAAAA/////wIA" +
           "AAAEYYIKBAAAAAEACwAAAENyZWF0ZUFzc2V0AQEaAAAvAQEaABoAAAABAf////8CAAAAF2CpCgIAAAAA" +
           "AA4AAABJbnB1dEFyZ3VtZW50cwEBGwAALgBEGwAAAJYBAAAAAQAqAQEYAAAACQAAAEFzc2V0TmFtZQAM" +
           "/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3Vt" +
           "ZW50cwEBHAAALgBEHAAAAJYBAAAAAQAqAQEWAAAABwAAAEFzc2V0SWQAEf////8AAAAAAAEAKAEBAAAA" +
           "AQAAAAEAAAABAf////8AAAAABGGCCgQAAAABAAsAAABEZWxldGVBc3NldAEBHQAALwEBHQAdAAAAAQH/" +
           "////AQAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAR4AAC4ARB4AAACWAQAAAAEAKgEBFgAA" +
           "AAcAAABBc3NldElkABH/////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public CreateAssetMethodState CreateAsset
        {
            get
            {
                return m_createAssetMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_createAssetMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_createAssetMethod = value;
            }
        }

        /// <remarks />
        public DeleteAssetMethodState DeleteAsset
        {
            get
            {
                return m_deleteAssetMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deleteAssetMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deleteAssetMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_createAssetMethod != null)
            {
                children.Add(m_createAssetMethod);
            }

            if (m_deleteAssetMethod != null)
            {
                children.Add(m_deleteAssetMethod);
            }

            base.GetChildren(context, children);
        }

        /// <remarks />
        protected override BaseInstanceState FindChild(
            ISystemContext context,
            QualifiedName browseName,
            bool createOrReplace,
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case Asset.BrowseNames.CreateAsset:
                    {
                        if (createOrReplace)
                        {
                            if (CreateAsset == null)
                            {
                                if (replacement == null)
                                {
                                    CreateAsset = new CreateAssetMethodState(this);
                                }
                                else
                                {
                                    CreateAsset = (CreateAssetMethodState)replacement;
                                }
                            }
                        }

                        instance = CreateAsset;
                        break;
                    }

                case Asset.BrowseNames.DeleteAsset:
                    {
                        if (createOrReplace)
                        {
                            if (DeleteAsset == null)
                            {
                                if (replacement == null)
                                {
                                    DeleteAsset = new DeleteAssetMethodState(this);
                                }
                                else
                                {
                                    DeleteAsset = (DeleteAssetMethodState)replacement;
                                }
                            }
                        }

                        instance = DeleteAsset;
                        break;
                    }
            }

            if (instance != null)
            {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private CreateAssetMethodState m_createAssetMethod;
        private DeleteAssetMethodState m_deleteAssetMethod;
        #endregion
    }
#endif
    #endregion

    #region IWoTAssetTypeState Class
#if (!OPCUA_EXCLUDE_IWoTAssetTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class IWoTAssetTypeState : BaseInterfaceState
    {
        #region Constructors
        /// <remarks />
        public IWoTAssetTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Asset.ObjectTypes.IWoTAssetType, Asset.Namespaces.WoT_Con, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <remarks />
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <remarks />
        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <remarks />
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGCAAgEAAAABABUA" +
           "AABJV29UQXNzZXRUeXBlSW5zdGFuY2UBASoAAQEqACoAAAD/////AQAAAARggAoBAAAAAQAHAAAAV29U" +
           "RmlsZQEBKwAALwEBbgArAAAA/////wsAAAAVYIkKAgAAAAAABAAAAFNpemUBASwAAC4ARCwAAAAACf//" +
           "//8BAf////8AAAAAFWCJCgIAAAAAAAgAAABXcml0YWJsZQEBLQAALgBELQAAAAAB/////wEB/////wAA" +
           "AAAVYIkKAgAAAAAADAAAAFVzZXJXcml0YWJsZQEBLgAALgBELgAAAAAB/////wEB/////wAAAAAVYIkK" +
           "AgAAAAAACQAAAE9wZW5Db3VudAEBLwAALgBELwAAAAAF/////wEB/////wAAAAAEYYIKBAAAAAAABAAA" +
           "AE9wZW4BATMAAC8BADwtMwAAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQE0" +
           "AAAuAEQ0AAAAlgEAAAABACoBARMAAAAEAAAATW9kZQAD/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB" +
           "/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBNQAALgBENQAAAJYBAAAAAQAqAQEZ" +
           "AAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKAEBAAAAAQAAAAEAAAABAf////8AAAAABGGCCgQA" +
           "AAAAAAUAAABDbG9zZQEBNgAALwEAPy02AAAAAQH/////AQAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1" +
           "bWVudHMBATcAAC4ARDcAAACWAQAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxlAAf/////AAAAAAABACgB" +
           "AQAAAAEAAAABAAAAAQH/////AAAAAARhggoEAAAAAAAEAAAAUmVhZAEBOAAALwEAQS04AAAAAQH/////" +
           "AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBATkAAC4ARDkAAACWAgAAAAEAKgEBGQAAAAoA" +
           "AABGaWxlSGFuZGxlAAf/////AAAAAAABACoBARUAAAAGAAAATGVuZ3RoAAb/////AAAAAAABACgBAQAA" +
           "AAEAAAACAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQE6AAAuAEQ6AAAA" +
           "lgEAAAABACoBARMAAAAEAAAARGF0YQAP/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAAE" +
           "YYIKBAAAAAAABQAAAFdyaXRlAQE7AAAvAQBELTsAAAABAf////8BAAAAF2CpCgIAAAAAAA4AAABJbnB1" +
           "dEFyZ3VtZW50cwEBPAAALgBEPAAAAJYCAAAAAQAqAQEZAAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAA" +
           "AAEAKgEBEwAAAAQAAABEYXRhAA//////AAAAAAABACgBAQAAAAEAAAACAAAAAQH/////AAAAAARhggoE" +
           "AAAAAAALAAAAR2V0UG9zaXRpb24BAT0AAC8BAEYtPQAAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElu" +
           "cHV0QXJndW1lbnRzAQE+AAAuAEQ+AAAAlgEAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAA" +
           "AAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEB" +
           "PwAALgBEPwAAAJYBAAAAAQAqAQEXAAAACAAAAFBvc2l0aW9uAAn/////AAAAAAABACgBAQAAAAEAAAAB" +
           "AAAAAQH/////AAAAAARhggoEAAAAAAALAAAAU2V0UG9zaXRpb24BAUAAAC8BAEktQAAAAAEB/////wEA" +
           "AAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQFBAAAuAERBAAAAlgIAAAABACoBARkAAAAKAAAA" +
           "RmlsZUhhbmRsZQAH/////wAAAAAAAQAqAQEXAAAACAAAAFBvc2l0aW9uAAn/////AAAAAAABACgBAQAA" +
           "AAEAAAACAAAAAQH/////AAAAAARhggoEAAAAAQAOAAAAQ2xvc2VBbmRVcGRhdGUBAWoAAC8BAW8AagAA" +
           "AAEB/////wEAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQFrAAAuAERrAAAAlgEAAAABACoB" +
           "ARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public WoTAssetFileTypeState WoTFile
        {
            get
            {
                return m_woTFile;
            }

            set
            {
                if (!Object.ReferenceEquals(m_woTFile, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_woTFile = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_woTFile != null)
            {
                children.Add(m_woTFile);
            }

            base.GetChildren(context, children);
        }

        /// <remarks />
        protected override BaseInstanceState FindChild(
            ISystemContext context,
            QualifiedName browseName,
            bool createOrReplace,
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case Asset.BrowseNames.WoTFile:
                    {
                        if (createOrReplace)
                        {
                            if (WoTFile == null)
                            {
                                if (replacement == null)
                                {
                                    WoTFile = new WoTAssetFileTypeState(this);
                                }
                                else
                                {
                                    WoTFile = (WoTAssetFileTypeState)replacement;
                                }
                            }
                        }

                        instance = WoTFile;
                        break;
                    }
            }

            if (instance != null)
            {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private WoTAssetFileTypeState m_woTFile;
        #endregion
    }
#endif
    #endregion

    #region WoTAssetTypeState Class
#if (!OPCUA_EXCLUDE_WoTAssetTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class WoTAssetTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public WoTAssetTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Asset.ObjectTypes.WoTAssetType, Asset.Namespaces.WoT_Con, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <remarks />
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <remarks />
        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <remarks />
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGCAAgEAAAABABQA" +
           "AABXb1RBc3NldFR5cGVJbnN0YW5jZQEBcwABAXMAcwAAAAEAAAABAMNEAAEBKgABAAAABGCACgEAAAAB" +
           "AAcAAABXb1RGaWxlAQF0AAAvAQFuAHQAAAD/////CwAAABVgiQoCAAAAAAAEAAAAU2l6ZQEBdQAALgBE" +
           "dQAAAAAJ/////wEB/////wAAAAAVYIkKAgAAAAAACAAAAFdyaXRhYmxlAQF2AAAuAER2AAAAAAH/////" +
           "AQH/////AAAAABVgiQoCAAAAAAAMAAAAVXNlcldyaXRhYmxlAQF3AAAuAER3AAAAAAH/////AQH/////" +
           "AAAAABVgiQoCAAAAAAAJAAAAT3BlbkNvdW50AQF4AAAuAER4AAAAAAX/////AQH/////AAAAAARhggoE" +
           "AAAAAAAEAAAAT3BlbgEBfAAALwEAPC18AAAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1" +
           "bWVudHMBAX0AAC4ARH0AAACWAQAAAAEAKgEBEwAAAAQAAABNb2RlAAP/////AAAAAAABACgBAQAAAAEA" +
           "AAABAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQF+AAAuAER+AAAAlgEA" +
           "AAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAA" +
           "AAAEYYIKBAAAAAAABQAAAENsb3NlAQF/AAAvAQA/LX8AAAABAf////8BAAAAF2CpCgIAAAAAAA4AAABJ" +
           "bnB1dEFyZ3VtZW50cwEBgAAALgBEgAAAAJYBAAAAAQAqAQEZAAAACgAAAEZpbGVIYW5kbGUAB/////8A" +
           "AAAAAAEAKAEBAAAAAQAAAAEAAAABAf////8AAAAABGGCCgQAAAAAAAQAAABSZWFkAQGBAAAvAQBBLYEA" +
           "AAABAf////8CAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBggAALgBEggAAAJYCAAAAAQAq" +
           "AQEZAAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKgEBFQAAAAYAAABMZW5ndGgABv////8AAAAA" +
           "AAEAKAEBAAAAAQAAAAIAAAABAf////8AAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAYMA" +
           "AC4ARIMAAACWAQAAAAEAKgEBEwAAAAQAAABEYXRhAA//////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/" +
           "////AAAAAARhggoEAAAAAAAFAAAAV3JpdGUBAYQAAC8BAEQthAAAAAEB/////wEAAAAXYKkKAgAAAAAA" +
           "DgAAAElucHV0QXJndW1lbnRzAQGFAAAuAESFAAAAlgIAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH" +
           "/////wAAAAAAAQAqAQETAAAABAAAAERhdGEAD/////8AAAAAAAEAKAEBAAAAAQAAAAIAAAABAf////8A" +
           "AAAABGGCCgQAAAAAAAsAAABHZXRQb3NpdGlvbgEBhgAALwEARi2GAAAAAQH/////AgAAABdgqQoCAAAA" +
           "AAAOAAAASW5wdXRBcmd1bWVudHMBAYcAAC4ARIcAAACWAQAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxl" +
           "AAf/////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJn" +
           "dW1lbnRzAQGIAAAuAESIAAAAlgEAAAABACoBARcAAAAIAAAAUG9zaXRpb24ACf////8AAAAAAAEAKAEB" +
           "AAAAAQAAAAEAAAABAf////8AAAAABGGCCgQAAAAAAAsAAABTZXRQb3NpdGlvbgEBiQAALwEASS2JAAAA" +
           "AQH/////AQAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAYoAAC4ARIoAAACWAgAAAAEAKgEB" +
           "GQAAAAoAAABGaWxlSGFuZGxlAAf/////AAAAAAABACoBARcAAAAIAAAAUG9zaXRpb24ACf////8AAAAA" +
           "AAEAKAEBAAAAAQAAAAIAAAABAf////8AAAAABGGCCgQAAAABAA4AAABDbG9zZUFuZFVwZGF0ZQEBiwAA" +
           "LwEBbwCLAAAAAQH/////AQAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAYwAAC4ARIwAAACW" +
           "AQAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxlAAf/////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////" +
           "AAAAAA==";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public WoTAssetFileTypeState WoTFile
        {
            get
            {
                return m_woTFile;
            }

            set
            {
                if (!Object.ReferenceEquals(m_woTFile, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_woTFile = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_woTFile != null)
            {
                children.Add(m_woTFile);
            }

            base.GetChildren(context, children);
        }

        /// <remarks />
        protected override BaseInstanceState FindChild(
            ISystemContext context,
            QualifiedName browseName,
            bool createOrReplace,
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case Asset.BrowseNames.WoTFile:
                    {
                        if (createOrReplace)
                        {
                            if (WoTFile == null)
                            {
                                if (replacement == null)
                                {
                                    WoTFile = new WoTAssetFileTypeState(this);
                                }
                                else
                                {
                                    WoTFile = (WoTAssetFileTypeState)replacement;
                                }
                            }
                        }

                        instance = WoTFile;
                        break;
                    }
            }

            if (instance != null)
            {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private WoTAssetFileTypeState m_woTFile;
        #endregion
    }
#endif
    #endregion

    #region WoTAssetFileTypeState Class
#if (!OPCUA_EXCLUDE_WoTAssetFileTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class WoTAssetFileTypeState : FileState
    {
        #region Constructors
        /// <remarks />
        public WoTAssetFileTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Asset.ObjectTypes.WoTAssetFileType, Asset.Namespaces.WoT_Con, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <remarks />
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <remarks />
        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <remarks />
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGCAAgEAAAABABgA" +
           "AABXb1RBc3NldEZpbGVUeXBlSW5zdGFuY2UBAW4AAQFuAG4AAAD/////CwAAABVgiQgCAAAAAAAEAAAA" +
           "U2l6ZQEBAAAALgBEAAn/////AQH/////AAAAABVgiQgCAAAAAAAIAAAAV3JpdGFibGUBAQAAAC4ARAAB" +
           "/////wEB/////wAAAAAVYIkIAgAAAAAADAAAAFVzZXJXcml0YWJsZQEBAAAALgBEAAH/////AQH/////" +
           "AAAAABVgiQgCAAAAAAAJAAAAT3BlbkNvdW50AQEAAAAuAEQABf////8BAf////8AAAAABGGCCAQAAAAA" +
           "AAQAAABPcGVuAQEAAAAvAQA8LQEB/////wIAAAAXYKkIAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQEA" +
           "AAAuAESWAQAAAAEAKgEBEwAAAAQAAABNb2RlAAP/////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////" +
           "AAAAABdgqQgCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQEAAAAuAESWAQAAAAEAKgEBGQAAAAoAAABG" +
           "aWxlSGFuZGxlAAf/////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAAARhgggEAAAAAAAFAAAA" +
           "Q2xvc2UBAQAAAC8BAD8tAQH/////AQAAABdgqQgCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAQAAAC4A" +
           "RJYBAAAAAQAqAQEZAAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKAEBAAAAAQAAAAEAAAABAf//" +
           "//8AAAAABGGCCAQAAAAAAAQAAABSZWFkAQEAAAAvAQBBLQEB/////wIAAAAXYKkIAgAAAAAADgAAAElu" +
           "cHV0QXJndW1lbnRzAQEAAAAuAESWAgAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxlAAf/////AAAAAAAB" +
           "ACoBARUAAAAGAAAATGVuZ3RoAAb/////AAAAAAABACgBAQAAAAEAAAACAAAAAQH/////AAAAABdgqQgC" +
           "AAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQEAAAAuAESWAQAAAAEAKgEBEwAAAAQAAABEYXRhAA//////" +
           "AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAAARhgggEAAAAAAAFAAAAV3JpdGUBAQAAAC8BAEQt" +
           "AQH/////AQAAABdgqQgCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAQAAAC4ARJYCAAAAAQAqAQEZAAAA" +
           "CgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKgEBEwAAAAQAAABEYXRhAA//////AAAAAAABACgBAQAA" +
           "AAEAAAACAAAAAQH/////AAAAAARhgggEAAAAAAALAAAAR2V0UG9zaXRpb24BAQAAAC8BAEYtAQH/////" +
           "AgAAABdgqQgCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAQAAAC4ARJYBAAAAAQAqAQEZAAAACgAAAEZp" +
           "bGVIYW5kbGUAB/////8AAAAAAAEAKAEBAAAAAQAAAAEAAAABAf////8AAAAAF2CpCAIAAAAAAA8AAABP" +
           "dXRwdXRBcmd1bWVudHMBAQAAAC4ARJYBAAAAAQAqAQEXAAAACAAAAFBvc2l0aW9uAAn/////AAAAAAAB" +
           "ACgBAQAAAAEAAAABAAAAAQH/////AAAAAARhgggEAAAAAAALAAAAU2V0UG9zaXRpb24BAQAAAC8BAEkt" +
           "AQH/////AQAAABdgqQgCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAQAAAC4ARJYCAAAAAQAqAQEZAAAA" +
           "CgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKgEBFwAAAAgAAABQb3NpdGlvbgAJ/////wAAAAAAAQAo" +
           "AQEAAAABAAAAAgAAAAEB/////wAAAAAEYYIKBAAAAAEADgAAAENsb3NlQW5kVXBkYXRlAQFvAAAvAQFv" +
           "AG8AAAABAf////8BAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBcAAALgBEcAAAAJYBAAAA" +
           "AQAqAQEZAAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKAEBAAAAAQAAAAEAAAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public CloseAndUpdateMethodState CloseAndUpdate
        {
            get
            {
                return m_closeAndUpdateMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_closeAndUpdateMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_closeAndUpdateMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_closeAndUpdateMethod != null)
            {
                children.Add(m_closeAndUpdateMethod);
            }

            base.GetChildren(context, children);
        }

        /// <remarks />
        protected override BaseInstanceState FindChild(
            ISystemContext context,
            QualifiedName browseName,
            bool createOrReplace,
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case Asset.BrowseNames.CloseAndUpdate:
                    {
                        if (createOrReplace)
                        {
                            if (CloseAndUpdate == null)
                            {
                                if (replacement == null)
                                {
                                    CloseAndUpdate = new CloseAndUpdateMethodState(this);
                                }
                                else
                                {
                                    CloseAndUpdate = (CloseAndUpdateMethodState)replacement;
                                }
                            }
                        }

                        instance = CloseAndUpdate;
                        break;
                    }
            }

            if (instance != null)
            {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private CloseAndUpdateMethodState m_closeAndUpdateMethod;
        #endregion
    }
#endif
    #endregion

    #region CreateAssetMethodState Class
#if (!OPCUA_EXCLUDE_CreateAssetMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class CreateAssetMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public CreateAssetMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new CreateAssetMethodState(parent);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <remarks />
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <remarks />
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCAAQAAAABABUA" +
           "AABDcmVhdGVBc3NldE1ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public CreateAssetMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
        protected override ServiceResult Call(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            ServiceResult _result = null;

            string assetName = (string)_inputArguments[0];

            NodeId assetId = (NodeId)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    assetName,
                    ref assetId);
            }

            _outputArguments[0] = assetId;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    public delegate ServiceResult CreateAssetMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string assetName,
        ref NodeId assetId);
#endif
    #endregion

    #region DeleteAssetMethodState Class
#if (!OPCUA_EXCLUDE_DeleteAssetMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class DeleteAssetMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public DeleteAssetMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new DeleteAssetMethodState(parent);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <remarks />
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <remarks />
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCAAQAAAABABUA" +
           "AABEZWxldGVBc3NldE1ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public DeleteAssetMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
        protected override ServiceResult Call(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            ServiceResult _result = null;

            NodeId assetId = (NodeId)_inputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    assetId);
            }

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    public delegate ServiceResult DeleteAssetMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        NodeId assetId);
#endif
    #endregion

    #region CloseAndUpdateMethodState Class
#if (!OPCUA_EXCLUDE_CloseAndUpdateMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class CloseAndUpdateMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public CloseAndUpdateMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new CloseAndUpdateMethodState(parent);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <remarks />
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <remarks />
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCAAQAAAABABgA" +
           "AABDbG9zZUFuZFVwZGF0ZU1ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public CloseAndUpdateMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
        protected override ServiceResult Call(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            ServiceResult _result = null;

            uint fileHandle = (uint)_inputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    fileHandle);
            }

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    public delegate ServiceResult CloseAndUpdateMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        uint fileHandle);
#endif
    #endregion
}
