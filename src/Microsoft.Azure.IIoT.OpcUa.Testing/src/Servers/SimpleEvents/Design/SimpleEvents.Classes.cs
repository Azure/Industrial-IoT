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
using Opc.Ua;

namespace SimpleEvents {
    #region SystemCycleStatusEventState Class
#if (!OPCUA_EXCLUDE_SystemCycleStatusEventState)
    /// <summary>
    /// Stores an instance of the SystemCycleStatusEventType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SystemCycleStatusEventState : SystemEventState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleStatusEventState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleStatusEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
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
           "AQAAACUAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvU2ltcGxlRXZlbnRz/////wRggAABAAAAAQAi" +
           "AAAAU3lzdGVtQ3ljbGVTdGF0dXNFdmVudFR5cGVJbnN0YW5jZQEB6wABAesA/////woAAAA1YIkKAgAA" +
           "AAAABwAAAEV2ZW50SWQBAewAAwAAAAArAAAAQSBnbG9iYWxseSB1bmlxdWUgaWRlbnRpZmllciBmb3Ig" +
           "dGhlIGV2ZW50LgAuAETsAAAAAA//////AQH/////AAAAADVgiQoCAAAAAAAJAAAARXZlbnRUeXBlAQHt" +
           "AAMAAAAAIgAAAFRoZSBpZGVudGlmaWVyIGZvciB0aGUgZXZlbnQgdHlwZS4ALgBE7QAAAAAR/////wEB" +
           "/////wAAAAA1YIkKAgAAAAAACgAAAFNvdXJjZU5vZGUBAe4AAwAAAAAYAAAAVGhlIHNvdXJjZSBvZiB0" +
           "aGUgZXZlbnQuAC4ARO4AAAAAEf////8BAf////8AAAAANWCJCgIAAAAAAAoAAABTb3VyY2VOYW1lAQHv" +
           "AAMAAAAAKQAAAEEgZGVzY3JpcHRpb24gb2YgdGhlIHNvdXJjZSBvZiB0aGUgZXZlbnQuAC4ARO8AAAAA" +
           "DP////8BAf////8AAAAANWCJCgIAAAAAAAQAAABUaW1lAQHwAAMAAAAAGAAAAFdoZW4gdGhlIGV2ZW50" +
           "IG9jY3VycmVkLgAuAETwAAAAAQAmAf////8BAf////8AAAAANWCJCgIAAAAAAAsAAABSZWNlaXZlVGlt" +
           "ZQEB8QADAAAAAD4AAABXaGVuIHRoZSBzZXJ2ZXIgcmVjZWl2ZWQgdGhlIGV2ZW50IGZyb20gdGhlIHVu" +
           "ZGVybHlpbmcgc3lzdGVtLgAuAETxAAAAAQAmAf////8BAf////8AAAAANWCJCgIAAAAAAAcAAABNZXNz" +
           "YWdlAQHzAAMAAAAAJQAAAEEgbG9jYWxpemVkIGRlc2NyaXB0aW9uIG9mIHRoZSBldmVudC4ALgBE8wAA" +
           "AAAV/////wEB/////wAAAAA1YIkKAgAAAAAACAAAAFNldmVyaXR5AQH0AAMAAAAAIQAAAEluZGljYXRl" +
           "cyBob3cgdXJnZW50IGFuIGV2ZW50IGlzLgAuAET0AAAAAAX/////AQH/////AAAAABVgiQoCAAAAAQAH" +
           "AAAAQ3ljbGVJZAEB9QAALgBE9QAAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEACwAAAEN1cnJlbnRT" +
           "dGVwAQH2AAAuAET2AAAAAQG3AP////8BAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the CycleId Property.
        /// </summary>
        public PropertyState<string> CycleId
        {
            get
            {
                return m_cycleId;
            }

            set
            {
                if (!Object.ReferenceEquals(m_cycleId, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_cycleId = value;
            }
        }

        /// <summary>
        /// A description for the CurrentStep Property.
        /// </summary>
        public PropertyState<CycleStepDataType> CurrentStep
        {
            get
            {
                return m_currentStep;
            }

            set
            {
                if (!Object.ReferenceEquals(m_currentStep, value))
                {
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
            IList<BaseInstanceState> children)
        {
            if (m_cycleId != null)
            {
                children.Add(m_cycleId);
            }

            if (m_currentStep != null)
            {
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
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case SimpleEvents.BrowseNames.CycleId:
                {
                    if (createOrReplace)
                    {
                        if (CycleId == null)
                        {
                            if (replacement == null)
                            {
                                CycleId = new PropertyState<string>(this);
                            }
                            else
                            {
                                CycleId = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = CycleId;
                    break;
                }

                case SimpleEvents.BrowseNames.CurrentStep:
                {
                    if (createOrReplace)
                    {
                        if (CurrentStep == null)
                        {
                            if (replacement == null)
                            {
                                CurrentStep = new PropertyState<CycleStepDataType>(this);
                            }
                            else
                            {
                                CurrentStep = (PropertyState<CycleStepDataType>)replacement;
                            }
                        }
                    }

                    instance = CurrentStep;
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
    public partial class SystemCycleStartedEventState : SystemCycleStatusEventState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleStartedEventState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleStartedEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
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
           "AQAAACUAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvU2ltcGxlRXZlbnRz/////wRggAABAAAAAQAj" +
           "AAAAU3lzdGVtQ3ljbGVTdGFydGVkRXZlbnRUeXBlSW5zdGFuY2UBAbgAAQG4AP////8LAAAANWCJCgIA" +
           "AAAAAAcAAABFdmVudElkAQG5AAMAAAAAKwAAAEEgZ2xvYmFsbHkgdW5pcXVlIGlkZW50aWZpZXIgZm9y" +
           "IHRoZSBldmVudC4ALgBEuQAAAAAP/////wEB/////wAAAAA1YIkKAgAAAAAACQAAAEV2ZW50VHlwZQEB" +
           "ugADAAAAACIAAABUaGUgaWRlbnRpZmllciBmb3IgdGhlIGV2ZW50IHR5cGUuAC4ARLoAAAAAEf////8B" +
           "Af////8AAAAANWCJCgIAAAAAAAoAAABTb3VyY2VOb2RlAQG7AAMAAAAAGAAAAFRoZSBzb3VyY2Ugb2Yg" +
           "dGhlIGV2ZW50LgAuAES7AAAAABH/////AQH/////AAAAADVgiQoCAAAAAAAKAAAAU291cmNlTmFtZQEB" +
           "vAADAAAAACkAAABBIGRlc2NyaXB0aW9uIG9mIHRoZSBzb3VyY2Ugb2YgdGhlIGV2ZW50LgAuAES8AAAA" +
           "AAz/////AQH/////AAAAADVgiQoCAAAAAAAEAAAAVGltZQEBvQADAAAAABgAAABXaGVuIHRoZSBldmVu" +
           "dCBvY2N1cnJlZC4ALgBEvQAAAAEAJgH/////AQH/////AAAAADVgiQoCAAAAAAALAAAAUmVjZWl2ZVRp" +
           "bWUBAb4AAwAAAAA+AAAAV2hlbiB0aGUgc2VydmVyIHJlY2VpdmVkIHRoZSBldmVudCBmcm9tIHRoZSB1" +
           "bmRlcmx5aW5nIHN5c3RlbS4ALgBEvgAAAAEAJgH/////AQH/////AAAAADVgiQoCAAAAAAAHAAAATWVz" +
           "c2FnZQEBwAADAAAAACUAAABBIGxvY2FsaXplZCBkZXNjcmlwdGlvbiBvZiB0aGUgZXZlbnQuAC4ARMAA" +
           "AAAAFf////8BAf////8AAAAANWCJCgIAAAAAAAgAAABTZXZlcml0eQEBwQADAAAAACEAAABJbmRpY2F0" +
           "ZXMgaG93IHVyZ2VudCBhbiBldmVudCBpcy4ALgBEwQAAAAAF/////wEB/////wAAAAAVYIkKAgAAAAEA" +
           "BwAAAEN5Y2xlSWQBAcIAAC4ARMIAAAAADP////8BAf////8AAAAAFWCJCgIAAAABAAsAAABDdXJyZW50" +
           "U3RlcAEB9wAALgBE9wAAAAEBtwD/////AQH/////AAAAABVgiQoCAAAAAQAFAAAAU3RlcHMBAcQAAC4A" +
           "RMQAAAABAbcAAQAAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the Steps Property.
        /// </summary>
        public PropertyState<CycleStepDataType[]> Steps
        {
            get
            {
                return m_steps;
            }

            set
            {
                if (!Object.ReferenceEquals(m_steps, value))
                {
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
            IList<BaseInstanceState> children)
        {
            if (m_steps != null)
            {
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
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case SimpleEvents.BrowseNames.Steps:
                {
                    if (createOrReplace)
                    {
                        if (Steps == null)
                        {
                            if (replacement == null)
                            {
                                Steps = new PropertyState<CycleStepDataType[]>(this);
                            }
                            else
                            {
                                Steps = (PropertyState<CycleStepDataType[]>)replacement;
                            }
                        }
                    }

                    instance = Steps;
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
    public partial class SystemCycleAbortedEventState : SystemCycleStatusEventState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleAbortedEventState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleAbortedEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
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
           "AQAAACUAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvU2ltcGxlRXZlbnRz/////wRggAABAAAAAQAj" +
           "AAAAU3lzdGVtQ3ljbGVBYm9ydGVkRXZlbnRUeXBlSW5zdGFuY2UBAcUAAQHFAP////8LAAAANWCJCgIA" +
           "AAAAAAcAAABFdmVudElkAQHGAAMAAAAAKwAAAEEgZ2xvYmFsbHkgdW5pcXVlIGlkZW50aWZpZXIgZm9y" +
           "IHRoZSBldmVudC4ALgBExgAAAAAP/////wEB/////wAAAAA1YIkKAgAAAAAACQAAAEV2ZW50VHlwZQEB" +
           "xwADAAAAACIAAABUaGUgaWRlbnRpZmllciBmb3IgdGhlIGV2ZW50IHR5cGUuAC4ARMcAAAAAEf////8B" +
           "Af////8AAAAANWCJCgIAAAAAAAoAAABTb3VyY2VOb2RlAQHIAAMAAAAAGAAAAFRoZSBzb3VyY2Ugb2Yg" +
           "dGhlIGV2ZW50LgAuAETIAAAAABH/////AQH/////AAAAADVgiQoCAAAAAAAKAAAAU291cmNlTmFtZQEB" +
           "yQADAAAAACkAAABBIGRlc2NyaXB0aW9uIG9mIHRoZSBzb3VyY2Ugb2YgdGhlIGV2ZW50LgAuAETJAAAA" +
           "AAz/////AQH/////AAAAADVgiQoCAAAAAAAEAAAAVGltZQEBygADAAAAABgAAABXaGVuIHRoZSBldmVu" +
           "dCBvY2N1cnJlZC4ALgBEygAAAAEAJgH/////AQH/////AAAAADVgiQoCAAAAAAALAAAAUmVjZWl2ZVRp" +
           "bWUBAcsAAwAAAAA+AAAAV2hlbiB0aGUgc2VydmVyIHJlY2VpdmVkIHRoZSBldmVudCBmcm9tIHRoZSB1" +
           "bmRlcmx5aW5nIHN5c3RlbS4ALgBEywAAAAEAJgH/////AQH/////AAAAADVgiQoCAAAAAAAHAAAATWVz" +
           "c2FnZQEBzQADAAAAACUAAABBIGxvY2FsaXplZCBkZXNjcmlwdGlvbiBvZiB0aGUgZXZlbnQuAC4ARM0A" +
           "AAAAFf////8BAf////8AAAAANWCJCgIAAAAAAAgAAABTZXZlcml0eQEBzgADAAAAACEAAABJbmRpY2F0" +
           "ZXMgaG93IHVyZ2VudCBhbiBldmVudCBpcy4ALgBEzgAAAAAF/////wEB/////wAAAAAVYIkKAgAAAAEA" +
           "BwAAAEN5Y2xlSWQBAc8AAC4ARM8AAAAADP////8BAf////8AAAAAFWCJCgIAAAABAAsAAABDdXJyZW50" +
           "U3RlcAEB+AAALgBE+AAAAAEBtwD/////AQH/////AAAAABVgiQoCAAAAAQAFAAAARXJyb3IBAfkAAC4A" +
           "RPkAAAAAE/////8BAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the Error Property.
        /// </summary>
        public PropertyState<StatusCode> Error
        {
            get
            {
                return m_error;
            }

            set
            {
                if (!Object.ReferenceEquals(m_error, value))
                {
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
            IList<BaseInstanceState> children)
        {
            if (m_error != null)
            {
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
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case SimpleEvents.BrowseNames.Error:
                {
                    if (createOrReplace)
                    {
                        if (Error == null)
                        {
                            if (replacement == null)
                            {
                                Error = new PropertyState<StatusCode>(this);
                            }
                            else
                            {
                                Error = (PropertyState<StatusCode>)replacement;
                            }
                        }
                    }

                    instance = Error;
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
    public partial class SystemCycleFinishedEventState : SystemCycleStatusEventState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleFinishedEventState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleFinishedEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
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
           "AQAAACUAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvU2ltcGxlRXZlbnRz/////wRggAABAAAAAQAk" +
           "AAAAU3lzdGVtQ3ljbGVGaW5pc2hlZEV2ZW50VHlwZUluc3RhbmNlAQHSAAEB0gD/////CgAAADVgiQoC" +
           "AAAAAAAHAAAARXZlbnRJZAEB0wADAAAAACsAAABBIGdsb2JhbGx5IHVuaXF1ZSBpZGVudGlmaWVyIGZv" +
           "ciB0aGUgZXZlbnQuAC4ARNMAAAAAD/////8BAf////8AAAAANWCJCgIAAAAAAAkAAABFdmVudFR5cGUB" +
           "AdQAAwAAAAAiAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0eXBlLgAuAETUAAAAABH/////" +
           "AQH/////AAAAADVgiQoCAAAAAAAKAAAAU291cmNlTm9kZQEB1QADAAAAABgAAABUaGUgc291cmNlIG9m" +
           "IHRoZSBldmVudC4ALgBE1QAAAAAR/////wEB/////wAAAAA1YIkKAgAAAAAACgAAAFNvdXJjZU5hbWUB" +
           "AdYAAwAAAAApAAAAQSBkZXNjcmlwdGlvbiBvZiB0aGUgc291cmNlIG9mIHRoZSBldmVudC4ALgBE1gAA" +
           "AAAM/////wEB/////wAAAAA1YIkKAgAAAAAABAAAAFRpbWUBAdcAAwAAAAAYAAAAV2hlbiB0aGUgZXZl" +
           "bnQgb2NjdXJyZWQuAC4ARNcAAAABACYB/////wEB/////wAAAAA1YIkKAgAAAAAACwAAAFJlY2VpdmVU" +
           "aW1lAQHYAAMAAAAAPgAAAFdoZW4gdGhlIHNlcnZlciByZWNlaXZlZCB0aGUgZXZlbnQgZnJvbSB0aGUg" +
           "dW5kZXJseWluZyBzeXN0ZW0uAC4ARNgAAAABACYB/////wEB/////wAAAAA1YIkKAgAAAAAABwAAAE1l" +
           "c3NhZ2UBAdoAAwAAAAAlAAAAQSBsb2NhbGl6ZWQgZGVzY3JpcHRpb24gb2YgdGhlIGV2ZW50LgAuAETa" +
           "AAAAABX/////AQH/////AAAAADVgiQoCAAAAAAAIAAAAU2V2ZXJpdHkBAdsAAwAAAAAhAAAASW5kaWNh" +
           "dGVzIGhvdyB1cmdlbnQgYW4gZXZlbnQgaXMuAC4ARNsAAAAABf////8BAf////8AAAAAFWCJCgIAAAAB" +
           "AAcAAABDeWNsZUlkAQHcAAAuAETcAAAAAAz/////AQH/////AAAAABVgiQoCAAAAAQALAAAAQ3VycmVu" +
           "dFN0ZXABAfoAAC4ARPoAAAABAbcA/////wEB/////wAAAAA=";
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