/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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

namespace PlcModel
{
    #region PlcState Class
#if (!OPCUA_EXCLUDE_PlcState)
    /// <summary>
    /// Stores an instance of the PlcType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class PlcState : BaseObjectState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public PlcState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(PlcModel.ObjectTypes.PlcType, PlcModel.Namespaces.Plc, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <summary>
        /// Initializes the instance with a node.
        /// </summary>
        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvUGxj/////4RggAIBAAAAAQAPAAAAUGxj" +
           "VHlwZUluc3RhbmNlAQHcOgEB3DrcOgAAAf////8BAAAAFWCpCgIAAAABAAkAAABQbGNTdGF0dXMBAZs6" +
           "AC8AP5s6AAAWAQHsOgK2AAAAPFBsY0RhdGFUeXBlIHhtbG5zPSJodHRwOi8vb3BjZm91bmRhdGlvbi5v" +
           "cmcvVUEvUGxjIj48VGVtcGVyYXR1cmU+PFRvcD4yMDwvVG9wPjxCb3R0b20+MjA8L0JvdHRvbT48L1Rl" +
           "bXBlcmF0dXJlPjxQcmVzc3VyZT4xMDAwMjA8L1ByZXNzdXJlPjxIZWF0ZXJTdGF0ZT5PbjwvSGVhdGVy" +
           "U3RhdGU+PC9QbGNEYXRhVHlwZT4BAbg6/////wEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public BaseDataVariableState<PlcDataType> PlcStatus
        {
            get
            {
                return m_plcStatus;
            }

            set
            {
                if (!Object.ReferenceEquals(m_plcStatus, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_plcStatus = value;
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
            if (m_plcStatus != null)
            {
                children.Add(m_plcStatus);
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
                case PlcModel.BrowseNames.PlcStatus:
                    {
                        if (createOrReplace)
                        {
                            if (PlcStatus == null)
                            {
                                if (replacement == null)
                                {
                                    PlcStatus = new BaseDataVariableState<PlcDataType>(this);
                                }
                                else
                                {
                                    PlcStatus = (BaseDataVariableState<PlcDataType>)replacement;
                                }
                            }
                        }

                        instance = PlcStatus;
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
        private BaseDataVariableState<PlcDataType> m_plcStatus;
        #endregion
    }
#endif
    #endregion
}