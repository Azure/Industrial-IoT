/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
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
using Opc.Ua;

namespace SimpleEvents {
    #region SystemCycleStatusEventState Class
#if (!OPCUA_EXCLUDE_SystemCycleStatusEventState)
    /// <summary>
    /// Stores an instance of the SystemCycleStatusEventType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SystemCycleStatusEventState : SystemEventState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleStatusEventState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return Opc.Ua.NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleStatusEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context) {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <summary>
        /// Initializes the instance with a node.
        /// </summary>
        protected override void Initialize(ISystemContext context, NodeState source) {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACUAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvU2ltcGxlRXZlbnRz/////wRggAIBAAAAAQAi" +
           "AAAAU3lzdGVtQ3ljbGVTdGF0dXNFdmVudFR5cGVJbnN0YW5jZQEB6wABAesA6wAAAP////8KAAAAFWCJ" +
           "CgIAAAAAAAcAAABFdmVudElkAQHsAAAuAETsAAAAAA//////AQH/////AAAAABVgiQoCAAAAAAAJAAAA" +
           "RXZlbnRUeXBlAQHtAAAuAETtAAAAABH/////AQH/////AAAAABVgiQoCAAAAAAAKAAAAU291cmNlTm9k" +
           "ZQEB7gAALgBE7gAAAAAR/////wEB/////wAAAAAVYIkKAgAAAAAACgAAAFNvdXJjZU5hbWUBAe8AAC4A" +
           "RO8AAAAADP////8BAf////8AAAAAFWCJCgIAAAAAAAQAAABUaW1lAQHwAAAuAETwAAAAAQAmAf////8B" +
           "Af////8AAAAAFWCJCgIAAAAAAAsAAABSZWNlaXZlVGltZQEB8QAALgBE8QAAAAEAJgH/////AQH/////" +
           "AAAAABVgiQoCAAAAAAAHAAAATWVzc2FnZQEB8wAALgBE8wAAAAAV/////wEB/////wAAAAAVYIkKAgAA" +
           "AAAACAAAAFNldmVyaXR5AQH0AAAuAET0AAAAAAX/////AQH/////AAAAABVgiQoCAAAAAQAHAAAAQ3lj" +
           "bGVJZAEB9QAALgBE9QAAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEACwAAAEN1cnJlbnRTdGVwAQH2" +
           "AAAuAET2AAAAAQG3AP////8BAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<string> CycleId {
            get {
                return m_cycleId;
            }

            set {
                if (!Object.ReferenceEquals(m_cycleId, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_cycleId = value;
            }
        }

        /// <remarks />
        public PropertyState<CycleStepDataType> CurrentStep {
            get {
                return m_currentStep;
            }

            set {
                if (!Object.ReferenceEquals(m_currentStep, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_currentStep = value;
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
            IList<BaseInstanceState> children) {
            if (m_cycleId != null) {
                children.Add(m_cycleId);
            }

            if (m_currentStep != null) {
                children.Add(m_currentStep);
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
            BaseInstanceState replacement) {
            if (QualifiedName.IsNull(browseName)) {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name) {
                case SimpleEvents.BrowseNames.CycleId: {
                        if (createOrReplace) {
                            if (CycleId == null) {
                                if (replacement == null) {
                                    CycleId = new PropertyState<string>(this);
                                }
                                else {
                                    CycleId = (PropertyState<string>)replacement;
                                }
                            }
                        }

                        instance = CycleId;
                        break;
                    }

                case SimpleEvents.BrowseNames.CurrentStep: {
                        if (createOrReplace) {
                            if (CurrentStep == null) {
                                if (replacement == null) {
                                    CurrentStep = new PropertyState<CycleStepDataType>(this);
                                }
                                else {
                                    CurrentStep = (PropertyState<CycleStepDataType>)replacement;
                                }
                            }
                        }

                        instance = CurrentStep;
                        break;
                    }
            }

            if (instance != null) {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private PropertyState<string> m_cycleId;
        private PropertyState<CycleStepDataType> m_currentStep;
        #endregion
    }
#endif
    #endregion

    #region SystemCycleStartedEventState Class
#if (!OPCUA_EXCLUDE_SystemCycleStartedEventState)
    /// <summary>
    /// Stores an instance of the SystemCycleStartedEventType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SystemCycleStartedEventState : SystemCycleStatusEventState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleStartedEventState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return Opc.Ua.NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleStartedEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context) {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <summary>
        /// Initializes the instance with a node.
        /// </summary>
        protected override void Initialize(ISystemContext context, NodeState source) {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACUAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvU2ltcGxlRXZlbnRz/////wRggAIBAAAAAQAj" +
           "AAAAU3lzdGVtQ3ljbGVTdGFydGVkRXZlbnRUeXBlSW5zdGFuY2UBAbgAAQG4ALgAAAD/////CwAAABVg" +
           "iQoCAAAAAAAHAAAARXZlbnRJZAEBuQAALgBEuQAAAAAP/////wEB/////wAAAAAVYIkKAgAAAAAACQAA" +
           "AEV2ZW50VHlwZQEBugAALgBEugAAAAAR/////wEB/////wAAAAAVYIkKAgAAAAAACgAAAFNvdXJjZU5v" +
           "ZGUBAbsAAC4ARLsAAAAAEf////8BAf////8AAAAAFWCJCgIAAAAAAAoAAABTb3VyY2VOYW1lAQG8AAAu" +
           "AES8AAAAAAz/////AQH/////AAAAABVgiQoCAAAAAAAEAAAAVGltZQEBvQAALgBEvQAAAAEAJgH/////" +
           "AQH/////AAAAABVgiQoCAAAAAAALAAAAUmVjZWl2ZVRpbWUBAb4AAC4ARL4AAAABACYB/////wEB////" +
           "/wAAAAAVYIkKAgAAAAAABwAAAE1lc3NhZ2UBAcAAAC4ARMAAAAAAFf////8BAf////8AAAAAFWCJCgIA" +
           "AAAAAAgAAABTZXZlcml0eQEBwQAALgBEwQAAAAAF/////wEB/////wAAAAAVYIkKAgAAAAEABwAAAEN5" +
           "Y2xlSWQBAcIAAC4ARMIAAAAADP////8BAf////8AAAAAFWCJCgIAAAABAAsAAABDdXJyZW50U3RlcAEB" +
           "9wAALgBE9wAAAAEBtwD/////AQH/////AAAAABdgiQoCAAAAAQAFAAAAU3RlcHMBAcQAAC4ARMQAAAAB" +
           "AbcAAQAAAAEAAAAAAAAAAQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<CycleStepDataType[]> Steps {
            get {
                return m_steps;
            }

            set {
                if (!Object.ReferenceEquals(m_steps, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_steps = value;
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
            IList<BaseInstanceState> children) {
            if (m_steps != null) {
                children.Add(m_steps);
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
            BaseInstanceState replacement) {
            if (QualifiedName.IsNull(browseName)) {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name) {
                case SimpleEvents.BrowseNames.Steps: {
                        if (createOrReplace) {
                            if (Steps == null) {
                                if (replacement == null) {
                                    Steps = new PropertyState<CycleStepDataType[]>(this);
                                }
                                else {
                                    Steps = (PropertyState<CycleStepDataType[]>)replacement;
                                }
                            }
                        }

                        instance = Steps;
                        break;
                    }
            }

            if (instance != null) {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private PropertyState<CycleStepDataType[]> m_steps;
        #endregion
    }
#endif
    #endregion

    #region SystemCycleAbortedEventState Class
#if (!OPCUA_EXCLUDE_SystemCycleAbortedEventState)
    /// <summary>
    /// Stores an instance of the SystemCycleAbortedEventType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SystemCycleAbortedEventState : SystemCycleStatusEventState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleAbortedEventState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return Opc.Ua.NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleAbortedEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context) {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <summary>
        /// Initializes the instance with a node.
        /// </summary>
        protected override void Initialize(ISystemContext context, NodeState source) {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACUAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvU2ltcGxlRXZlbnRz/////wRggAIBAAAAAQAj" +
           "AAAAU3lzdGVtQ3ljbGVBYm9ydGVkRXZlbnRUeXBlSW5zdGFuY2UBAcUAAQHFAMUAAAD/////CwAAABVg" +
           "iQoCAAAAAAAHAAAARXZlbnRJZAEBxgAALgBExgAAAAAP/////wEB/////wAAAAAVYIkKAgAAAAAACQAA" +
           "AEV2ZW50VHlwZQEBxwAALgBExwAAAAAR/////wEB/////wAAAAAVYIkKAgAAAAAACgAAAFNvdXJjZU5v" +
           "ZGUBAcgAAC4ARMgAAAAAEf////8BAf////8AAAAAFWCJCgIAAAAAAAoAAABTb3VyY2VOYW1lAQHJAAAu" +
           "AETJAAAAAAz/////AQH/////AAAAABVgiQoCAAAAAAAEAAAAVGltZQEBygAALgBEygAAAAEAJgH/////" +
           "AQH/////AAAAABVgiQoCAAAAAAALAAAAUmVjZWl2ZVRpbWUBAcsAAC4ARMsAAAABACYB/////wEB////" +
           "/wAAAAAVYIkKAgAAAAAABwAAAE1lc3NhZ2UBAc0AAC4ARM0AAAAAFf////8BAf////8AAAAAFWCJCgIA" +
           "AAAAAAgAAABTZXZlcml0eQEBzgAALgBEzgAAAAAF/////wEB/////wAAAAAVYIkKAgAAAAEABwAAAEN5" +
           "Y2xlSWQBAc8AAC4ARM8AAAAADP////8BAf////8AAAAAFWCJCgIAAAABAAsAAABDdXJyZW50U3RlcAEB" +
           "+AAALgBE+AAAAAEBtwD/////AQH/////AAAAABVgiQoCAAAAAQAFAAAARXJyb3IBAfkAAC4ARPkAAAAA" +
           "E/////8BAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<StatusCode> Error {
            get {
                return m_error;
            }

            set {
                if (!Object.ReferenceEquals(m_error, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_error = value;
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
            IList<BaseInstanceState> children) {
            if (m_error != null) {
                children.Add(m_error);
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
            BaseInstanceState replacement) {
            if (QualifiedName.IsNull(browseName)) {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name) {
                case SimpleEvents.BrowseNames.Error: {
                        if (createOrReplace) {
                            if (Error == null) {
                                if (replacement == null) {
                                    Error = new PropertyState<StatusCode>(this);
                                }
                                else {
                                    Error = (PropertyState<StatusCode>)replacement;
                                }
                            }
                        }

                        instance = Error;
                        break;
                    }
            }

            if (instance != null) {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private PropertyState<StatusCode> m_error;
        #endregion
    }
#endif
    #endregion

    #region SystemCycleFinishedEventState Class
#if (!OPCUA_EXCLUDE_SystemCycleFinishedEventState)
    /// <summary>
    /// Stores an instance of the SystemCycleFinishedEventType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SystemCycleFinishedEventState : SystemCycleStatusEventState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleFinishedEventState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return Opc.Ua.NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleFinishedEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context) {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <summary>
        /// Initializes the instance with a node.
        /// </summary>
        protected override void Initialize(ISystemContext context, NodeState source) {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACUAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvU2ltcGxlRXZlbnRz/////wRggAIBAAAAAQAk" +
           "AAAAU3lzdGVtQ3ljbGVGaW5pc2hlZEV2ZW50VHlwZUluc3RhbmNlAQHSAAEB0gDSAAAA/////woAAAAV" +
           "YIkKAgAAAAAABwAAAEV2ZW50SWQBAdMAAC4ARNMAAAAAD/////8BAf////8AAAAAFWCJCgIAAAAAAAkA" +
           "AABFdmVudFR5cGUBAdQAAC4ARNQAAAAAEf////8BAf////8AAAAAFWCJCgIAAAAAAAoAAABTb3VyY2VO" +
           "b2RlAQHVAAAuAETVAAAAABH/////AQH/////AAAAABVgiQoCAAAAAAAKAAAAU291cmNlTmFtZQEB1gAA" +
           "LgBE1gAAAAAM/////wEB/////wAAAAAVYIkKAgAAAAAABAAAAFRpbWUBAdcAAC4ARNcAAAABACYB////" +
           "/wEB/////wAAAAAVYIkKAgAAAAAACwAAAFJlY2VpdmVUaW1lAQHYAAAuAETYAAAAAQAmAf////8BAf//" +
           "//8AAAAAFWCJCgIAAAAAAAcAAABNZXNzYWdlAQHaAAAuAETaAAAAABX/////AQH/////AAAAABVgiQoC" +
           "AAAAAAAIAAAAU2V2ZXJpdHkBAdsAAC4ARNsAAAAABf////8BAf////8AAAAAFWCJCgIAAAABAAcAAABD" +
           "eWNsZUlkAQHcAAAuAETcAAAAAAz/////AQH/////AAAAABVgiQoCAAAAAQALAAAAQ3VycmVudFN0ZXAB" +
           "AfoAAC4ARPoAAAABAbcA/////wEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        #endregion

        #region Private Fields
        #endregion
    }
#endif
    #endregion
}