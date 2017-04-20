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

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Runtime.Serialization;
using Opc.Ua;

namespace Publisher
{
    #region PublishNodeMethodState Class
    #if (!OPCUA_EXCLUDE_PublishNodeMethodState)
    /// <summary>
    /// Stores an instance of the PublishNodeMethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class PublishNodeMethodState : MethodState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public PublishNodeMethodState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent)
        {
            return new PublishNodeMethodState(parent);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACYAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvUHVibGlzaGVyL/////8EYYIKBAAAAAEA" +
           "FQAAAFB1Ymxpc2hOb2RlTWV0aG9kVHlwZQEBAQAALwEBAQABAAAAAQH/////AQAAABVgqQoCAAAAAAAO" +
           "AAAASW5wdXRBcmd1bWVudHMBAQIAAC4ARAIAAACWAgAAAAEAKgEBMgAAAAYAAABOb2RlSUQADP////8A" +
           "AAAAAwAAAAAVAAAAVGhlIE5vZGVJRCB0byBwdWJsaXNoAQAqAQFVAAAAEQAAAFNlcnZlckVuZHBvaW50" +
           "VXJsAAz/////AAAAAAMAAAAALQAAAFRoZSBzZXJ2ZXIgZW5kcG9pbnQgVVJMIHRvIGdyYWIgdGhlIG5v" +
           "ZGUgZnJvbQEAKAEBAAAAAQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public PublishNodeMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Invokes the method, returns the result and output argument.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="objectId">The id of the object.</param>
        /// <param name="inputArguments">The input arguments which have been already validated.</param>
        /// <param name="outputArguments">The output arguments which have initialized with thier default values.</param>
        /// <returns></returns>
        protected override ServiceResult Call(
            ISystemContext context,
            NodeId objectId,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            string nodeID = (string)inputArguments[0];
            string serverEndpointUrl = (string)inputArguments[1];

            if (OnCall != null)
            {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    nodeID,
                    serverEndpointUrl);
            }

            return result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <summary>
    /// Used to receive notifications when the method is called.
    /// </summary>
    /// <exclude />
    public delegate ServiceResult PublishNodeMethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        string nodeID,
        string serverEndpointUrl);
    #endif
    #endregion

    #region UnPublishNodeMethodState Class
    #if (!OPCUA_EXCLUDE_UnPublishNodeMethodState)
    /// <summary>
    /// Stores an instance of the UnPublishNodeMethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class UnPublishNodeMethodState : MethodState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public UnPublishNodeMethodState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent)
        {
            return new UnPublishNodeMethodState(parent);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACYAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvUHVibGlzaGVyL/////8EYYIKBAAAAAEA" +
           "FwAAAFVuUHVibGlzaE5vZGVNZXRob2RUeXBlAQEDAAAvAQEDAAMAAAABAf////8BAAAAFWCpCgIAAAAA" +
           "AA4AAABJbnB1dEFyZ3VtZW50cwEBBAAALgBEBAAAAJYCAAAAAQAqAQEyAAAABgAAAE5vZGVJRAAM////" +
           "/wAAAAADAAAAABUAAABUaGUgTm9kZUlEIHRvIHB1Ymxpc2gBACoBAVUAAAARAAAAU2VydmVyRW5kcG9p" +
           "bnRVcmwADP////8AAAAAAwAAAAAtAAAAVGhlIHNlcnZlciBlbmRwb2ludCBVUkwgdG8gZ3JhYiB0aGUg" +
           "bm9kZSBmcm9tAQAoAQEAAAABAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public UnPublishNodeMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Invokes the method, returns the result and output argument.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="objectId">The id of the object.</param>
        /// <param name="inputArguments">The input arguments which have been already validated.</param>
        /// <param name="outputArguments">The output arguments which have initialized with thier default values.</param>
        /// <returns></returns>
        protected override ServiceResult Call(
            ISystemContext context,
            NodeId objectId,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            string nodeID = (string)inputArguments[0];
            string serverEndpointUrl = (string)inputArguments[1];

            if (OnCall != null)
            {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    nodeID,
                    serverEndpointUrl);
            }

            return result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <summary>
    /// Used to receive notifications when the method is called.
    /// </summary>
    /// <exclude />
    public delegate ServiceResult UnPublishNodeMethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        string nodeID,
        string serverEndpointUrl);
    #endif
    #endregion

    #region GetListOfPublishedNodesMethodState Class
    #if (!OPCUA_EXCLUDE_GetListOfPublishedNodesMethodState)
    /// <summary>
    /// Stores an instance of the GetListOfPublishedNodesMethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class GetListOfPublishedNodesMethodState : MethodState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public GetListOfPublishedNodesMethodState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent)
        {
            return new GetListOfPublishedNodesMethodState(parent);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACYAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvUHVibGlzaGVyL/////8EYYIKBAAAAAEA" +
           "IQAAAEdldExpc3RPZlB1Ymxpc2hlZE5vZGVzTWV0aG9kVHlwZQEBBQAALwEBBQAFAAAAAQH/////AQAA" +
           "ABVgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQEGAAAuAEQGAAAAlgEAAAABACoBAVUAAAAIAAAA" +
           "Tm9kZUxpc3QADP////8AAAAAAwAAAAA2AAAAVGhlIEpTT04tZm9ybWF0dGVkIG5vZGUgbGlzdCBjdXJy" +
           "ZW50bHkgYmVpbmcgcHVibGlzaGVkAQAoAQEAAAABAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public GetListOfPublishedNodesMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Invokes the method, returns the result and output argument.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="objectId">The id of the object.</param>
        /// <param name="inputArguments">The input arguments which have been already validated.</param>
        /// <param name="outputArguments">The output arguments which have initialized with thier default values.</param>
        /// <returns></returns>
        protected override ServiceResult Call(
            ISystemContext context,
            NodeId objectId,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            string nodeList = (string)outputArguments[0];

            if (OnCall != null)
            {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    ref nodeList);
            }

            outputArguments[0] = nodeList;

            return result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <summary>
    /// Used to receive notifications when the method is called.
    /// </summary>
    /// <exclude />
    public delegate ServiceResult GetListOfPublishedNodesMethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        ref string nodeList);
    #endif
    #endregion

    #region PublisherState Class
    #if (!OPCUA_EXCLUDE_PublisherState)
    /// <summary>
    /// Stores an instance of the PublisherType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class PublisherState : BaseObjectState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public PublisherState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Publisher.ObjectTypes.PublisherType, Publisher.Namespaces.Publisher, namespaceUris);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACYAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvUHVibGlzaGVyL/////+EYIAAAQAAAAEA" +
           "FQAAAFB1Ymxpc2hlclR5cGVJbnN0YW5jZQEBBwABAQcAAf////8EAAAABGGCCgQAAAABAAsAAABQdWJs" +
           "aXNoTm9kZQEBCAAALwEBCAAIAAAAAQH/////AQAAABVgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMB" +
           "AQkAAC4ARAkAAACWAgAAAAEAKgEBMgAAAAYAAABOb2RlSUQADP////8AAAAAAwAAAAAVAAAAVGhlIE5v" +
           "ZGVJRCB0byBwdWJsaXNoAQAqAQFVAAAAEQAAAFNlcnZlckVuZHBvaW50VXJsAAz/////AAAAAAMAAAAA" +
           "LQAAAFRoZSBzZXJ2ZXIgZW5kcG9pbnQgVVJMIHRvIGdyYWIgdGhlIG5vZGUgZnJvbQEAKAEBAAAAAQH/" +
           "////AAAAAARhggoEAAAAAQANAAAAVW5QdWJsaXNoTm9kZQEBCgAALwEBCgAKAAAAAQH/////AQAAABVg" +
           "qQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAQsAAC4ARAsAAACWAgAAAAEAKgEBMgAAAAYAAABOb2Rl" +
           "SUQADP////8AAAAAAwAAAAAVAAAAVGhlIE5vZGVJRCB0byBwdWJsaXNoAQAqAQFVAAAAEQAAAFNlcnZl" +
           "ckVuZHBvaW50VXJsAAz/////AAAAAAMAAAAALQAAAFRoZSBzZXJ2ZXIgZW5kcG9pbnQgVVJMIHRvIGdy" +
           "YWIgdGhlIG5vZGUgZnJvbQEAKAEBAAAAAQH/////AAAAAARhggoEAAAAAQAXAAAAR2V0TGlzdE9mUHVi" +
           "bGlzaGVkTm9kZXMBAQwAAC8BAQwADAAAAAEB/////wEAAAAVYKkKAgAAAAAADwAAAE91dHB1dEFyZ3Vt" +
           "ZW50cwEBDQAALgBEDQAAAJYBAAAAAQAqAQFVAAAACAAAAE5vZGVMaXN0AAz/////AAAAAAMAAAAANgAA" +
           "AFRoZSBKU09OLWZvcm1hdHRlZCBub2RlIGxpc3QgY3VycmVudGx5IGJlaW5nIHB1Ymxpc2hlZAEAKAEB" +
           "AAAAAQH/////AAAAABVgiQoCAAAAAQAQAAAAQ29ubmVjdGlvblN0cmluZwEBDgAALwA/DgAAAAAM////" +
           "/wIC/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the PublishNodeMethodType Method.
        /// </summary>
        public PublishNodeMethodState PublishNode
        {
            get
            {
                return m_publishNodeMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_publishNodeMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_publishNodeMethod = value;
            }
        }

        /// <summary>
        /// A description for the UnPublishNodeMethodType Method.
        /// </summary>
        public UnPublishNodeMethodState UnPublishNode
        {
            get
            {
                return m_unPublishNodeMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_unPublishNodeMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_unPublishNodeMethod = value;
            }
        }

        /// <summary>
        /// A description for the GetListOfPublishedNodesMethodType Method.
        /// </summary>
        public GetListOfPublishedNodesMethodState GetListOfPublishedNodes
        {
            get
            {
                return m_getListOfPublishedNodesMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_getListOfPublishedNodesMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_getListOfPublishedNodesMethod = value;
            }
        }

        /// <summary>
        /// A description for the ConnectionString Variable.
        /// </summary>
        public BaseDataVariableState<string> ConnectionString
        {
            get
            {
                return m_connectionString;
            }

            set
            {
                if (!Object.ReferenceEquals(m_connectionString, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_connectionString = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Populates a list with the children that belong to the node.
        /// </summary>
        /// <param name="context">The context for the system being accessed.</param>
        /// <param name="children">The list of children to populate.</param>
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_publishNodeMethod != null)
            {
                children.Add(m_publishNodeMethod);
            }

            if (m_unPublishNodeMethod != null)
            {
                children.Add(m_unPublishNodeMethod);
            }

            if (m_getListOfPublishedNodesMethod != null)
            {
                children.Add(m_getListOfPublishedNodesMethod);
            }

            if (m_connectionString != null)
            {
                children.Add(m_connectionString);
            }

            base.GetChildren(context, children);
        }

        /// <summary>
        /// Finds the child with the specified browse name.
        /// </summary>
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
                case Publisher.BrowseNames.PublishNode:
                {
                    if (createOrReplace)
                    {
                        if (PublishNode == null)
                        {
                            if (replacement == null)
                            {
                                PublishNode = new PublishNodeMethodState(this);
                            }
                            else
                            {
                                PublishNode = (PublishNodeMethodState)replacement;
                            }
                        }
                    }

                    instance = PublishNode;
                    break;
                }

                case Publisher.BrowseNames.UnPublishNode:
                {
                    if (createOrReplace)
                    {
                        if (UnPublishNode == null)
                        {
                            if (replacement == null)
                            {
                                UnPublishNode = new UnPublishNodeMethodState(this);
                            }
                            else
                            {
                                UnPublishNode = (UnPublishNodeMethodState)replacement;
                            }
                        }
                    }

                    instance = UnPublishNode;
                    break;
                }

                case Publisher.BrowseNames.GetListOfPublishedNodes:
                {
                    if (createOrReplace)
                    {
                        if (GetListOfPublishedNodes == null)
                        {
                            if (replacement == null)
                            {
                                GetListOfPublishedNodes = new GetListOfPublishedNodesMethodState(this);
                            }
                            else
                            {
                                GetListOfPublishedNodes = (GetListOfPublishedNodesMethodState)replacement;
                            }
                        }
                    }

                    instance = GetListOfPublishedNodes;
                    break;
                }

                case Publisher.BrowseNames.ConnectionString:
                {
                    if (createOrReplace)
                    {
                        if (ConnectionString == null)
                        {
                            if (replacement == null)
                            {
                                ConnectionString = new BaseDataVariableState<string>(this);
                            }
                            else
                            {
                                ConnectionString = (BaseDataVariableState<string>)replacement;
                            }
                        }
                    }

                    instance = ConnectionString;
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
        private PublishNodeMethodState m_publishNodeMethod;
        private UnPublishNodeMethodState m_unPublishNodeMethod;
        private GetListOfPublishedNodesMethodState m_getListOfPublishedNodesMethod;
        private BaseDataVariableState<string> m_connectionString;
        #endregion
    }
    #endif
    #endregion
}