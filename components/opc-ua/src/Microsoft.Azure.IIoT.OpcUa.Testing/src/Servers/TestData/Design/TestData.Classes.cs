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

namespace TestData {
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using Opc.Ua;

    #region GenerateValuesMethodState Class
#if (!OPCUA_EXCLUDE_GenerateValuesMethodState)
    /// <summary>
    /// Stores an instance of the GenerateValuesMethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class GenerateValuesMethodState : MethodState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public GenerateValuesMethodState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent) {
            return new GenerateValuesMethodState(parent);
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
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGGCCgQAAAABABgAAABHZW5lcmF0ZVZh" +
           "bHVlc01ldGhvZFR5cGUBAZkkAC8BAZkkmSQAAAEB/////wEAAAAVYKkKAgAAAAAADgAAAElucHV0QXJn" +
           "dW1lbnRzAQGaJAAuAESaJAAAlgEAAAABACoBAUYAAAAKAAAASXRlcmF0aW9ucwAH/////wAAAAADAAAA" +
           "ACUAAABUaGUgbnVtYmVyIG9mIG5ldyB2YWx1ZXMgdG8gZ2VuZXJhdGUuAQAoAQEAAAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public GenerateValuesMethodStateMethodCallHandler OnCall;
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
            IList<object> outputArguments) {
            if (OnCall == null) {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            uint iterations = (uint)inputArguments[0];

            if (OnCall != null) {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    iterations);
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
    public delegate ServiceResult GenerateValuesMethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        uint iterations);
#endif
    #endregion

    #region GenerateValuesEventState Class
#if (!OPCUA_EXCLUDE_GenerateValuesEventState)
    /// <summary>
    /// Stores an instance of the GenerateValuesEventType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class GenerateValuesEventState : BaseEventState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public GenerateValuesEventState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return Opc.Ua.NodeId.Create(TestData.ObjectTypes.GenerateValuesEventType, TestData.Namespaces.TestData, namespaceUris);
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
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGCAAAEAAAABAB8AAABHZW5lcmF0ZVZh" +
           "bHVlc0V2ZW50VHlwZUluc3RhbmNlAQGbJAEBmyT/////CgAAADVgiQoCAAAAAAAHAAAARXZlbnRJZAEB" +
           "nCQDAAAAACsAAABBIGdsb2JhbGx5IHVuaXF1ZSBpZGVudGlmaWVyIGZvciB0aGUgZXZlbnQuAC4ARJwk" +
           "AAAAD/////8BAf////8AAAAANWCJCgIAAAAAAAkAAABFdmVudFR5cGUBAZ0kAwAAAAAiAAAAVGhlIGlk" +
           "ZW50aWZpZXIgZm9yIHRoZSBldmVudCB0eXBlLgAuAESdJAAAABH/////AQH/////AAAAADVgiQoCAAAA" +
           "AAAKAAAAU291cmNlTm9kZQEBniQDAAAAABgAAABUaGUgc291cmNlIG9mIHRoZSBldmVudC4ALgBEniQA" +
           "AAAR/////wEB/////wAAAAA1YIkKAgAAAAAACgAAAFNvdXJjZU5hbWUBAZ8kAwAAAAApAAAAQSBkZXNj" +
           "cmlwdGlvbiBvZiB0aGUgc291cmNlIG9mIHRoZSBldmVudC4ALgBEnyQAAAAM/////wEB/////wAAAAA1" +
           "YIkKAgAAAAAABAAAAFRpbWUBAaAkAwAAAAAYAAAAV2hlbiB0aGUgZXZlbnQgb2NjdXJyZWQuAC4ARKAk" +
           "AAABACYB/////wEB/////wAAAAA1YIkKAgAAAAAACwAAAFJlY2VpdmVUaW1lAQGhJAMAAAAAPgAAAFdo" +
           "ZW4gdGhlIHNlcnZlciByZWNlaXZlZCB0aGUgZXZlbnQgZnJvbSB0aGUgdW5kZXJseWluZyBzeXN0ZW0u" +
           "AC4ARKEkAAABACYB/////wEB/////wAAAAA1YIkKAgAAAAAABwAAAE1lc3NhZ2UBAaMkAwAAAAAlAAAA" +
           "QSBsb2NhbGl6ZWQgZGVzY3JpcHRpb24gb2YgdGhlIGV2ZW50LgAuAESjJAAAABX/////AQH/////AAAA" +
           "ADVgiQoCAAAAAAAIAAAAU2V2ZXJpdHkBAaQkAwAAAAAhAAAASW5kaWNhdGVzIGhvdyB1cmdlbnQgYW4g" +
           "ZXZlbnQgaXMuAC4ARKQkAAAABf////8BAf////8AAAAAFWCJCgIAAAABAAoAAABJdGVyYXRpb25zAQGl" +
           "JAAuAESlJAAAAAf/////AQH/////AAAAABVgiQoCAAAAAQANAAAATmV3VmFsdWVDb3VudAEBpiQALgBE" +
           "piQAAAAH/////wEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the Iterations Property.
        /// </summary>
        public PropertyState<uint> Iterations {
            get {
                return m_iterations;
            }

            set {
                if (!Object.ReferenceEquals(m_iterations, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_iterations = value;
            }
        }

        /// <summary>
        /// A description for the NewValueCount Property.
        /// </summary>
        public PropertyState<uint> NewValueCount {
            get {
                return m_newValueCount;
            }

            set {
                if (!Object.ReferenceEquals(m_newValueCount, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_newValueCount = value;
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
            if (m_iterations != null) {
                children.Add(m_iterations);
            }

            if (m_newValueCount != null) {
                children.Add(m_newValueCount);
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
                case TestData.BrowseNames.Iterations: {
                        if (createOrReplace) {
                            if (Iterations == null) {
                                if (replacement == null) {
                                    Iterations = new PropertyState<uint>(this);
                                }
                                else {
                                    Iterations = (PropertyState<uint>)replacement;
                                }
                            }
                        }

                        instance = Iterations;
                        break;
                    }

                case TestData.BrowseNames.NewValueCount: {
                        if (createOrReplace) {
                            if (NewValueCount == null) {
                                if (replacement == null) {
                                    NewValueCount = new PropertyState<uint>(this);
                                }
                                else {
                                    NewValueCount = (PropertyState<uint>)replacement;
                                }
                            }
                        }

                        instance = NewValueCount;
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
        private PropertyState<uint> m_iterations;
        private PropertyState<uint> m_newValueCount;
        #endregion
    }
#endif
    #endregion

    #region TestDataObjectState Class
#if !OPCUA_EXCLUDE_TestDataObjectState
    /// <summary>
    /// Stores an instance of the TestDataObjectType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class TestDataObjectState : BaseObjectState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public TestDataObjectState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return NodeId.Create(ObjectTypes.TestDataObjectType, Namespaces.TestData, namespaceUris);
        }

#if !OPCUA_EXCLUDE_InitializationStrings
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
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGCAAAEAAAABABoAAABUZXN0RGF0YU9i" +
           "amVjdFR5cGVJbnN0YW5jZQEBpyQBAackAQAAAAAkAAEBqyQDAAAANWCJCgIAAAABABAAAABTaW11bGF0" +
           "aW9uQWN0aXZlAQGoJAMAAAAARwAAAElmIHRydWUgdGhlIHNlcnZlciB3aWxsIHByb2R1Y2UgbmV3IHZh" +
           "bHVlcyBmb3IgZWFjaCBtb25pdG9yZWQgdmFyaWFibGUuAC4ARKgkAAAAAf////8BAf////8AAAAABGGC" +
           "CgQAAAABAA4AAABHZW5lcmF0ZVZhbHVlcwEBqSQALwEBqSSpJAAAAQH/////AQAAABVgqQoCAAAAAAAO" +
           "AAAASW5wdXRBcmd1bWVudHMBAaokAC4ARKokAACWAQAAAAEAKgEBRgAAAAoAAABJdGVyYXRpb25zAAf/" +
           "////AAAAAAMAAAAAJQAAAFRoZSBudW1iZXIgb2YgbmV3IHZhbHVlcyB0byBnZW5lcmF0ZS4BACgBAQAA" +
           "AAEB/////wAAAAAEYIAKAQAAAAEADQAAAEN5Y2xlQ29tcGxldGUBAaskAC8BAEELqyQAAAEAAAAAJAEB" +
           "AackFwAAADVgiQoCAAAAAAAHAAAARXZlbnRJZAEBrCQDAAAAACsAAABBIGdsb2JhbGx5IHVuaXF1ZSBp" +
           "ZGVudGlmaWVyIGZvciB0aGUgZXZlbnQuAC4ARKwkAAAAD/////8BAf////8AAAAANWCJCgIAAAAAAAkA" +
           "AABFdmVudFR5cGUBAa0kAwAAAAAiAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0eXBlLgAu" +
           "AEStJAAAABH/////AQH/////AAAAADVgiQoCAAAAAAAKAAAAU291cmNlTm9kZQEBriQDAAAAABgAAABU" +
           "aGUgc291cmNlIG9mIHRoZSBldmVudC4ALgBEriQAAAAR/////wEB/////wAAAAA1YIkKAgAAAAAACgAA" +
           "AFNvdXJjZU5hbWUBAa8kAwAAAAApAAAAQSBkZXNjcmlwdGlvbiBvZiB0aGUgc291cmNlIG9mIHRoZSBl" +
           "dmVudC4ALgBEryQAAAAM/////wEB/////wAAAAA1YIkKAgAAAAAABAAAAFRpbWUBAbAkAwAAAAAYAAAA" +
           "V2hlbiB0aGUgZXZlbnQgb2NjdXJyZWQuAC4ARLAkAAABACYB/////wEB/////wAAAAA1YIkKAgAAAAAA" +
           "CwAAAFJlY2VpdmVUaW1lAQGxJAMAAAAAPgAAAFdoZW4gdGhlIHNlcnZlciByZWNlaXZlZCB0aGUgZXZl" +
           "bnQgZnJvbSB0aGUgdW5kZXJseWluZyBzeXN0ZW0uAC4ARLEkAAABACYB/////wEB/////wAAAAA1YIkK" +
           "AgAAAAAABwAAAE1lc3NhZ2UBAbMkAwAAAAAlAAAAQSBsb2NhbGl6ZWQgZGVzY3JpcHRpb24gb2YgdGhl" +
           "IGV2ZW50LgAuAESzJAAAABX/////AQH/////AAAAADVgiQoCAAAAAAAIAAAAU2V2ZXJpdHkBAbQkAwAA" +
           "AAAhAAAASW5kaWNhdGVzIGhvdyB1cmdlbnQgYW4gZXZlbnQgaXMuAC4ARLQkAAAABf////8BAf////8A" +
           "AAAAFWCJCgIAAAAAABAAAABDb25kaXRpb25DbGFzc0lkAQE6LQAuAEQ6LQAAABH/////AQH/////AAAA" +
           "ABVgiQoCAAAAAAASAAAAQ29uZGl0aW9uQ2xhc3NOYW1lAQE7LQAuAEQ7LQAAABX/////AQH/////AAAA" +
           "ABVgiQoCAAAAAAANAAAAQ29uZGl0aW9uTmFtZQEBJS0ALgBEJS0AAAAM/////wEB/////wAAAAAVYIkK" +
           "AgAAAAAACAAAAEJyYW5jaElkAQG1JAAuAES1JAAAABH/////AQH/////AAAAABVgiQoCAAAAAAAGAAAA" +
           "UmV0YWluAQG2JAAuAES2JAAAAAH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAARW5hYmxlZFN0YXRl" +
           "AQG3JAAvAQAjI7ckAAAAFf////8BAQIAAAABACwjAAEBzCQBACwjAAEB1CQBAAAAFWCJCgIAAAAAAAIA" +
           "AABJZAEBuCQALgBEuCQAAAAB/////wEB/////wAAAAAVYIkKAgAAAAAABwAAAFF1YWxpdHkBAb0kAC8B" +
           "ACojvSQAAAAT/////wEB/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVzdGFtcAEBviQALgBE" +
           "viQAAAEAJgH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAATGFzdFNldmVyaXR5AQHBJAAvAQAqI8Ek" +
           "AAAABf////8BAf////8BAAAAFWCJCgIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAcIkAC4ARMIkAAAB" +
           "ACYB/////wEB/////wAAAAAVYIkKAgAAAAAABwAAAENvbW1lbnQBAcMkAC8BACojwyQAAAAV/////wEB" +
           "/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVzdGFtcAEBxCQALgBExCQAAAEAJgH/////AQH/" +
           "////AAAAABVgiQoCAAAAAAAMAAAAQ2xpZW50VXNlcklkAQHFJAAuAETFJAAAAAz/////AQH/////AAAA" +
           "AARhggoEAAAAAAAHAAAARGlzYWJsZQEBxyQALwEARCPHJAAAAQEBAAAAAQD5CwABAPMKAAAAAARhggoE" +
           "AAAAAAAGAAAARW5hYmxlAQHGJAAvAQBDI8YkAAABAQEAAAABAPkLAAEA8woAAAAABGGCCgQAAAAAAAoA" +
           "AABBZGRDb21tZW50AQHIJAAvAQBFI8gkAAABAQEAAAABAPkLAAEADQsBAAAAFWCpCgIAAAAAAA4AAABJ" +
           "bnB1dEFyZ3VtZW50cwEBySQALgBEySQAAJYCAAAAAQAqAQFGAAAABwAAAEV2ZW50SWQAD/////8AAAAA" +
           "AwAAAAAoAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0byBjb21tZW50LgEAKgEBQgAAAAcA" +
           "AABDb21tZW50ABX/////AAAAAAMAAAAAJAAAAFRoZSBjb21tZW50IHRvIGFkZCB0byB0aGUgY29uZGl0" +
           "aW9uLgEAKAEBAAAAAQH/////AAAAABVgiQoCAAAAAAAKAAAAQWNrZWRTdGF0ZQEBzCQALwEAIyPMJAAA" +
           "ABX/////AQEBAAAAAQAsIwEBAbckAQAAABVgiQoCAAAAAAACAAAASWQBAc0kAC4ARM0kAAAAAf////8B" +
           "Af////8AAAAABGGCCgQAAAAAAAsAAABBY2tub3dsZWRnZQEB3CQALwEAlyPcJAAAAQEBAAAAAQD5CwAB" +
           "APAiAQAAABVgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAd0kAC4ARN0kAACWAgAAAAEAKgEBRgAA" +
           "AAcAAABFdmVudElkAA//////AAAAAAMAAAAAKAAAAFRoZSBpZGVudGlmaWVyIGZvciB0aGUgZXZlbnQg" +
           "dG8gY29tbWVudC4BACoBAUIAAAAHAAAAQ29tbWVudAAV/////wAAAAADAAAAACQAAABUaGUgY29tbWVu" +
           "dCB0byBhZGQgdG8gdGhlIGNvbmRpdGlvbi4BACgBAQAAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <summary>
        /// If true the server will produce new values for each monitored variable.
        /// </summary>
        public PropertyState<bool> SimulationActive {
            get => m_simulationActive;

            set {
                if (!ReferenceEquals(m_simulationActive, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_simulationActive = value;
            }
        }

        /// <summary>
        /// A description for the GenerateValuesMethodType Method.
        /// </summary>
        public GenerateValuesMethodState GenerateValues {
            get => m_generateValuesMethod;

            set {
                if (!ReferenceEquals(m_generateValuesMethod, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_generateValuesMethod = value;
            }
        }

        /// <summary>
        /// A description for the CycleComplete Object.
        /// </summary>
        public AcknowledgeableConditionState CycleComplete {
            get => m_cycleComplete;

            set {
                if (!ReferenceEquals(m_cycleComplete, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_cycleComplete = value;
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
            if (m_simulationActive != null) {
                children.Add(m_simulationActive);
            }

            if (m_generateValuesMethod != null) {
                children.Add(m_generateValuesMethod);
            }

            if (m_cycleComplete != null) {
                children.Add(m_cycleComplete);
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
                case BrowseNames.SimulationActive: {
                        if (createOrReplace) {
                            if (SimulationActive == null) {
                                if (replacement == null) {
                                    SimulationActive = new PropertyState<bool>(this);
                                }
                                else {
                                    SimulationActive = (PropertyState<bool>)replacement;
                                }
                            }
                        }

                        instance = SimulationActive;
                        break;
                    }

                case BrowseNames.GenerateValues: {
                        if (createOrReplace) {
                            if (GenerateValues == null) {
                                if (replacement == null) {
                                    GenerateValues = new GenerateValuesMethodState(this);
                                }
                                else {
                                    GenerateValues = (GenerateValuesMethodState)replacement;
                                }
                            }
                        }

                        instance = GenerateValues;
                        break;
                    }

                case BrowseNames.CycleComplete: {
                        if (createOrReplace) {
                            if (CycleComplete == null) {
                                if (replacement == null) {
                                    CycleComplete = new AcknowledgeableConditionState(this);
                                }
                                else {
                                    CycleComplete = (AcknowledgeableConditionState)replacement;
                                }
                            }
                        }

                        instance = CycleComplete;
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
        private PropertyState<bool> m_simulationActive;
        private GenerateValuesMethodState m_generateValuesMethod;
        private AcknowledgeableConditionState m_cycleComplete;
        #endregion
    }
#endif
    #endregion

    #region ScalarValue1MethodState Class
#if (!OPCUA_EXCLUDE_ScalarValue1MethodState)
    /// <summary>
    /// Stores an instance of the ScalarValue1MethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ScalarValue1MethodState : MethodState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public ScalarValue1MethodState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent) {
            return new ScalarValue1MethodState(parent);
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
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGGCCgQAAAABABYAAABTY2FsYXJWYWx1" +
           "ZTFNZXRob2RUeXBlAQHhJAAvAQHhJOEkAAABAf////8CAAAAFWCpCgIAAAAAAA4AAABJbnB1dEFyZ3Vt" +
           "ZW50cwEB4iQALgBE4iQAAJYLAAAAAQAqAQEYAAAACQAAAEJvb2xlYW5JbgAB/////wAAAAAAAQAqAQEW" +
           "AAAABwAAAFNCeXRlSW4AAv////8AAAAAAAEAKgEBFQAAAAYAAABCeXRlSW4AA/////8AAAAAAAEAKgEB" +
           "FgAAAAcAAABJbnQxNkluAAT/////AAAAAAABACoBARcAAAAIAAAAVUludDE2SW4ABf////8AAAAAAAEA" +
           "KgEBFgAAAAcAAABJbnQzMkluAAb/////AAAAAAABACoBARcAAAAIAAAAVUludDMySW4AB/////8AAAAA" +
           "AAEAKgEBFgAAAAcAAABJbnQ2NEluAAj/////AAAAAAABACoBARcAAAAIAAAAVUludDY0SW4ACf////8A" +
           "AAAAAAEAKgEBFgAAAAcAAABGbG9hdEluAAr/////AAAAAAABACoBARcAAAAIAAAARG91YmxlSW4AC///" +
           "//8AAAAAAAEAKAEBAAAAAQH/////AAAAABVgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQHjJAAu" +
           "AETjJAAAlgsAAAABACoBARkAAAAKAAAAQm9vbGVhbk91dAAB/////wAAAAAAAQAqAQEXAAAACAAAAFNC" +
           "eXRlT3V0AAL/////AAAAAAABACoBARYAAAAHAAAAQnl0ZU91dAAD/////wAAAAAAAQAqAQEXAAAACAAA" +
           "AEludDE2T3V0AAT/////AAAAAAABACoBARgAAAAJAAAAVUludDE2T3V0AAX/////AAAAAAABACoBARcA" +
           "AAAIAAAASW50MzJPdXQABv////8AAAAAAAEAKgEBGAAAAAkAAABVSW50MzJPdXQAB/////8AAAAAAAEA" +
           "KgEBFwAAAAgAAABJbnQ2NE91dAAI/////wAAAAAAAQAqAQEYAAAACQAAAFVJbnQ2NE91dAAJ/////wAA" +
           "AAAAAQAqAQEXAAAACAAAAEZsb2F0T3V0AAr/////AAAAAAABACoBARgAAAAJAAAARG91YmxlT3V0AAv/" +
           "////AAAAAAABACgBAQAAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public ScalarValue1MethodStateMethodCallHandler OnCall;
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
            IList<object> outputArguments) {
            if (OnCall == null) {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            bool booleanIn = (bool)inputArguments[0];
            sbyte sByteIn = (sbyte)inputArguments[1];
            byte byteIn = (byte)inputArguments[2];
            short int16In = (short)inputArguments[3];
            ushort uInt16In = (ushort)inputArguments[4];
            int int32In = (int)inputArguments[5];
            uint uInt32In = (uint)inputArguments[6];
            long int64In = (long)inputArguments[7];
            ulong uInt64In = (ulong)inputArguments[8];
            float floatIn = (float)inputArguments[9];
            double doubleIn = (double)inputArguments[10];

            bool booleanOut = (bool)outputArguments[0];
            sbyte sByteOut = (sbyte)outputArguments[1];
            byte byteOut = (byte)outputArguments[2];
            short int16Out = (short)outputArguments[3];
            ushort uInt16Out = (ushort)outputArguments[4];
            int int32Out = (int)outputArguments[5];
            uint uInt32Out = (uint)outputArguments[6];
            long int64Out = (long)outputArguments[7];
            ulong uInt64Out = (ulong)outputArguments[8];
            float floatOut = (float)outputArguments[9];
            double doubleOut = (double)outputArguments[10];

            if (OnCall != null) {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    booleanIn,
                    sByteIn,
                    byteIn,
                    int16In,
                    uInt16In,
                    int32In,
                    uInt32In,
                    int64In,
                    uInt64In,
                    floatIn,
                    doubleIn,
                    ref booleanOut,
                    ref sByteOut,
                    ref byteOut,
                    ref int16Out,
                    ref uInt16Out,
                    ref int32Out,
                    ref uInt32Out,
                    ref int64Out,
                    ref uInt64Out,
                    ref floatOut,
                    ref doubleOut);
            }

            outputArguments[0] = booleanOut;
            outputArguments[1] = sByteOut;
            outputArguments[2] = byteOut;
            outputArguments[3] = int16Out;
            outputArguments[4] = uInt16Out;
            outputArguments[5] = int32Out;
            outputArguments[6] = uInt32Out;
            outputArguments[7] = int64Out;
            outputArguments[8] = uInt64Out;
            outputArguments[9] = floatOut;
            outputArguments[10] = doubleOut;

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
    public delegate ServiceResult ScalarValue1MethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        bool booleanIn,
        sbyte sByteIn,
        byte byteIn,
        short int16In,
        ushort uInt16In,
        int int32In,
        uint uInt32In,
        long int64In,
        ulong uInt64In,
        float floatIn,
        double doubleIn,
        ref bool booleanOut,
        ref sbyte sByteOut,
        ref byte byteOut,
        ref short int16Out,
        ref ushort uInt16Out,
        ref int int32Out,
        ref uint uInt32Out,
        ref long int64Out,
        ref ulong uInt64Out,
        ref float floatOut,
        ref double doubleOut);
#endif
    #endregion

    #region ScalarValue2MethodState Class
#if (!OPCUA_EXCLUDE_ScalarValue2MethodState)
    /// <summary>
    /// Stores an instance of the ScalarValue2MethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ScalarValue2MethodState : MethodState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public ScalarValue2MethodState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent) {
            return new ScalarValue2MethodState(parent);
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
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGGCCgQAAAABABYAAABTY2FsYXJWYWx1" +
           "ZTJNZXRob2RUeXBlAQHkJAAvAQHkJOQkAAABAf////8CAAAAFWCpCgIAAAAAAA4AAABJbnB1dEFyZ3Vt" +
           "ZW50cwEB5SQALgBE5SQAAJYKAAAAAQAqAQEXAAAACAAAAFN0cmluZ0luAAz/////AAAAAAABACoBARkA" +
           "AAAKAAAARGF0ZVRpbWVJbgAN/////wAAAAAAAQAqAQEVAAAABgAAAEd1aWRJbgAO/////wAAAAAAAQAq" +
           "AQEbAAAADAAAAEJ5dGVTdHJpbmdJbgAP/////wAAAAAAAQAqAQEbAAAADAAAAFhtbEVsZW1lbnRJbgAQ" +
           "/////wAAAAAAAQAqAQEXAAAACAAAAE5vZGVJZEluABH/////AAAAAAABACoBAR8AAAAQAAAARXhwYW5k" +
           "ZWROb2RlSWRJbgAS/////wAAAAAAAQAqAQEeAAAADwAAAFF1YWxpZmllZE5hbWVJbgAU/////wAAAAAA" +
           "AQAqAQEeAAAADwAAAExvY2FsaXplZFRleHRJbgAV/////wAAAAAAAQAqAQEbAAAADAAAAFN0YXR1c0Nv" +
           "ZGVJbgAT/////wAAAAAAAQAoAQEAAAABAf////8AAAAAFWCpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVu" +
           "dHMBAeYkAC4AROYkAACWCgAAAAEAKgEBGAAAAAkAAABTdHJpbmdPdXQADP////8AAAAAAAEAKgEBGgAA" +
           "AAsAAABEYXRlVGltZU91dAAN/////wAAAAAAAQAqAQEWAAAABwAAAEd1aWRPdXQADv////8AAAAAAAEA" +
           "KgEBHAAAAA0AAABCeXRlU3RyaW5nT3V0AA//////AAAAAAABACoBARwAAAANAAAAWG1sRWxlbWVudE91" +
           "dAAQ/////wAAAAAAAQAqAQEYAAAACQAAAE5vZGVJZE91dAAR/////wAAAAAAAQAqAQEgAAAAEQAAAEV4" +
           "cGFuZGVkTm9kZUlkT3V0ABL/////AAAAAAABACoBAR8AAAAQAAAAUXVhbGlmaWVkTmFtZU91dAAU////" +
           "/wAAAAAAAQAqAQEfAAAAEAAAAExvY2FsaXplZFRleHRPdXQAFf////8AAAAAAAEAKgEBHAAAAA0AAABT" +
           "dGF0dXNDb2RlT3V0ABP/////AAAAAAABACgBAQAAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public ScalarValue2MethodStateMethodCallHandler OnCall;
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
            IList<object> outputArguments) {
            if (OnCall == null) {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            string stringIn = (string)inputArguments[0];
            DateTime dateTimeIn = (DateTime)inputArguments[1];
            Uuid guidIn = (Uuid)inputArguments[2];
            byte[] byteStringIn = (byte[])inputArguments[3];
            XmlElement xmlElementIn = (XmlElement)inputArguments[4];
            NodeId nodeIdIn = (NodeId)inputArguments[5];
            ExpandedNodeId expandedNodeIdIn = (ExpandedNodeId)inputArguments[6];
            QualifiedName qualifiedNameIn = (QualifiedName)inputArguments[7];
            LocalizedText localizedTextIn = (LocalizedText)inputArguments[8];
            StatusCode statusCodeIn = (StatusCode)inputArguments[9];

            string stringOut = (string)outputArguments[0];
            DateTime dateTimeOut = (DateTime)outputArguments[1];
            Uuid guidOut = (Uuid)outputArguments[2];
            byte[] byteStringOut = (byte[])outputArguments[3];
            XmlElement xmlElementOut = (XmlElement)outputArguments[4];
            NodeId nodeIdOut = (NodeId)outputArguments[5];
            ExpandedNodeId expandedNodeIdOut = (ExpandedNodeId)outputArguments[6];
            QualifiedName qualifiedNameOut = (QualifiedName)outputArguments[7];
            LocalizedText localizedTextOut = (LocalizedText)outputArguments[8];
            StatusCode statusCodeOut = (StatusCode)outputArguments[9];

            if (OnCall != null) {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    stringIn,
                    dateTimeIn,
                    guidIn,
                    byteStringIn,
                    xmlElementIn,
                    nodeIdIn,
                    expandedNodeIdIn,
                    qualifiedNameIn,
                    localizedTextIn,
                    statusCodeIn,
                    ref stringOut,
                    ref dateTimeOut,
                    ref guidOut,
                    ref byteStringOut,
                    ref xmlElementOut,
                    ref nodeIdOut,
                    ref expandedNodeIdOut,
                    ref qualifiedNameOut,
                    ref localizedTextOut,
                    ref statusCodeOut);
            }

            outputArguments[0] = stringOut;
            outputArguments[1] = dateTimeOut;
            outputArguments[2] = guidOut;
            outputArguments[3] = byteStringOut;
            outputArguments[4] = xmlElementOut;
            outputArguments[5] = nodeIdOut;
            outputArguments[6] = expandedNodeIdOut;
            outputArguments[7] = qualifiedNameOut;
            outputArguments[8] = localizedTextOut;
            outputArguments[9] = statusCodeOut;

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
    public delegate ServiceResult ScalarValue2MethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        string stringIn,
        DateTime dateTimeIn,
        Uuid guidIn,
        byte[] byteStringIn,
        XmlElement xmlElementIn,
        NodeId nodeIdIn,
        ExpandedNodeId expandedNodeIdIn,
        QualifiedName qualifiedNameIn,
        LocalizedText localizedTextIn,
        StatusCode statusCodeIn,
        ref string stringOut,
        ref DateTime dateTimeOut,
        ref Uuid guidOut,
        ref byte[] byteStringOut,
        ref XmlElement xmlElementOut,
        ref NodeId nodeIdOut,
        ref ExpandedNodeId expandedNodeIdOut,
        ref QualifiedName qualifiedNameOut,
        ref LocalizedText localizedTextOut,
        ref StatusCode statusCodeOut);
#endif
    #endregion

    #region ScalarValue3MethodState Class
#if (!OPCUA_EXCLUDE_ScalarValue3MethodState)
    /// <summary>
    /// Stores an instance of the ScalarValue3MethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ScalarValue3MethodState : MethodState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public ScalarValue3MethodState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent) {
            return new ScalarValue3MethodState(parent);
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
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGGCCgQAAAABABYAAABTY2FsYXJWYWx1" +
           "ZTNNZXRob2RUeXBlAQHnJAAvAQHnJOckAAABAf////8CAAAAFWCpCgIAAAAAAA4AAABJbnB1dEFyZ3Vt" +
           "ZW50cwEB6CQALgBE6CQAAJYDAAAAAQAqAQEYAAAACQAAAFZhcmlhbnRJbgAY/////wAAAAAAAQAqAQEc" +
           "AAAADQAAAEVudW1lcmF0aW9uSW4AHf////8AAAAAAAEAKgEBGgAAAAsAAABTdHJ1Y3R1cmVJbgAW////" +
           "/wAAAAAAAQAoAQEAAAABAf////8AAAAAFWCpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAekkAC4A" +
           "ROkkAACWAwAAAAEAKgEBGQAAAAoAAABWYXJpYW50T3V0ABj/////AAAAAAABACoBAR0AAAAOAAAARW51" +
           "bWVyYXRpb25PdXQAHf////8AAAAAAAEAKgEBGwAAAAwAAABTdHJ1Y3R1cmVPdXQAFv////8AAAAAAAEA" +
           "KAEBAAAAAQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public ScalarValue3MethodStateMethodCallHandler OnCall;
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
            IList<object> outputArguments) {
            if (OnCall == null) {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            object variantIn = (object)inputArguments[0];
            int enumerationIn = (int)inputArguments[1];
            ExtensionObject structureIn = (ExtensionObject)inputArguments[2];

            object variantOut = (object)outputArguments[0];
            int enumerationOut = (int)outputArguments[1];
            ExtensionObject structureOut = (ExtensionObject)outputArguments[2];

            if (OnCall != null) {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    variantIn,
                    enumerationIn,
                    structureIn,
                    ref variantOut,
                    ref enumerationOut,
                    ref structureOut);
            }

            outputArguments[0] = variantOut;
            outputArguments[1] = enumerationOut;
            outputArguments[2] = structureOut;

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
    public delegate ServiceResult ScalarValue3MethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        object variantIn,
        int enumerationIn,
        ExtensionObject structureIn,
        ref object variantOut,
        ref int enumerationOut,
        ref ExtensionObject structureOut);
#endif
    #endregion

    #region ScalarValueObjectState Class
#if !OPCUA_EXCLUDE_ScalarValueObjectState
    /// <summary>
    /// Stores an instance of the ScalarValueObjectType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ScalarValueObjectState : TestDataObjectState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public ScalarValueObjectState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return NodeId.Create(ObjectTypes.ScalarValueObjectType, Namespaces.TestData, namespaceUris);
        }

#if !OPCUA_EXCLUDE_InitializationStrings
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
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGCAAAEAAAABAB0AAABTY2FsYXJWYWx1" +
           "ZU9iamVjdFR5cGVJbnN0YW5jZQEB6iQBAeokAQAAAAAkAAEB7iQeAAAANWCJCgIAAAABABAAAABTaW11" +
           "bGF0aW9uQWN0aXZlAQHrJAMAAAAARwAAAElmIHRydWUgdGhlIHNlcnZlciB3aWxsIHByb2R1Y2UgbmV3" +
           "IHZhbHVlcyBmb3IgZWFjaCBtb25pdG9yZWQgdmFyaWFibGUuAC4AROskAAAAAf////8BAf////8AAAAA" +
           "BGGCCgQAAAABAA4AAABHZW5lcmF0ZVZhbHVlcwEB7CQALwEBqSTsJAAAAQH/////AQAAABVgqQoCAAAA" +
           "AAAOAAAASW5wdXRBcmd1bWVudHMBAe0kAC4ARO0kAACWAQAAAAEAKgEBRgAAAAoAAABJdGVyYXRpb25z" +
           "AAf/////AAAAAAMAAAAAJQAAAFRoZSBudW1iZXIgb2YgbmV3IHZhbHVlcyB0byBnZW5lcmF0ZS4BACgB" +
           "AQAAAAEB/////wAAAAAEYIAKAQAAAAEADQAAAEN5Y2xlQ29tcGxldGUBAe4kAC8BAEEL7iQAAAEAAAAA" +
           "JAEBAeokFwAAADVgiQoCAAAAAAAHAAAARXZlbnRJZAEB7yQDAAAAACsAAABBIGdsb2JhbGx5IHVuaXF1" +
           "ZSBpZGVudGlmaWVyIGZvciB0aGUgZXZlbnQuAC4ARO8kAAAAD/////8BAf////8AAAAANWCJCgIAAAAA" +
           "AAkAAABFdmVudFR5cGUBAfAkAwAAAAAiAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0eXBl" +
           "LgAuAETwJAAAABH/////AQH/////AAAAADVgiQoCAAAAAAAKAAAAU291cmNlTm9kZQEB8SQDAAAAABgA" +
           "AABUaGUgc291cmNlIG9mIHRoZSBldmVudC4ALgBE8SQAAAAR/////wEB/////wAAAAA1YIkKAgAAAAAA" +
           "CgAAAFNvdXJjZU5hbWUBAfIkAwAAAAApAAAAQSBkZXNjcmlwdGlvbiBvZiB0aGUgc291cmNlIG9mIHRo" +
           "ZSBldmVudC4ALgBE8iQAAAAM/////wEB/////wAAAAA1YIkKAgAAAAAABAAAAFRpbWUBAfMkAwAAAAAY" +
           "AAAAV2hlbiB0aGUgZXZlbnQgb2NjdXJyZWQuAC4ARPMkAAABACYB/////wEB/////wAAAAA1YIkKAgAA" +
           "AAAACwAAAFJlY2VpdmVUaW1lAQH0JAMAAAAAPgAAAFdoZW4gdGhlIHNlcnZlciByZWNlaXZlZCB0aGUg" +
           "ZXZlbnQgZnJvbSB0aGUgdW5kZXJseWluZyBzeXN0ZW0uAC4ARPQkAAABACYB/////wEB/////wAAAAA1" +
           "YIkKAgAAAAAABwAAAE1lc3NhZ2UBAfYkAwAAAAAlAAAAQSBsb2NhbGl6ZWQgZGVzY3JpcHRpb24gb2Yg" +
           "dGhlIGV2ZW50LgAuAET2JAAAABX/////AQH/////AAAAADVgiQoCAAAAAAAIAAAAU2V2ZXJpdHkBAfck" +
           "AwAAAAAhAAAASW5kaWNhdGVzIGhvdyB1cmdlbnQgYW4gZXZlbnQgaXMuAC4ARPckAAAABf////8BAf//" +
           "//8AAAAAFWCJCgIAAAAAABAAAABDb25kaXRpb25DbGFzc0lkAQE8LQAuAEQ8LQAAABH/////AQH/////" +
           "AAAAABVgiQoCAAAAAAASAAAAQ29uZGl0aW9uQ2xhc3NOYW1lAQE9LQAuAEQ9LQAAABX/////AQH/////" +
           "AAAAABVgiQoCAAAAAAANAAAAQ29uZGl0aW9uTmFtZQEBJi0ALgBEJi0AAAAM/////wEB/////wAAAAAV" +
           "YIkKAgAAAAAACAAAAEJyYW5jaElkAQH4JAAuAET4JAAAABH/////AQH/////AAAAABVgiQoCAAAAAAAG" +
           "AAAAUmV0YWluAQH5JAAuAET5JAAAAAH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAARW5hYmxlZFN0" +
           "YXRlAQH6JAAvAQAjI/okAAAAFf////8BAQIAAAABACwjAAEBDyUBACwjAAEBFyUBAAAAFWCJCgIAAAAA" +
           "AAIAAABJZAEB+yQALgBE+yQAAAAB/////wEB/////wAAAAAVYIkKAgAAAAAABwAAAFF1YWxpdHkBAQAl" +
           "AC8BACojACUAAAAT/////wEB/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVzdGFtcAEBASUA" +
           "LgBEASUAAAEAJgH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAATGFzdFNldmVyaXR5AQEEJQAvAQAq" +
           "IwQlAAAABf////8BAf////8BAAAAFWCJCgIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAQUlAC4ARAUl" +
           "AAABACYB/////wEB/////wAAAAAVYIkKAgAAAAAABwAAAENvbW1lbnQBAQYlAC8BACojBiUAAAAV////" +
           "/wEB/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVzdGFtcAEBByUALgBEByUAAAEAJgH/////" +
           "AQH/////AAAAABVgiQoCAAAAAAAMAAAAQ2xpZW50VXNlcklkAQEIJQAuAEQIJQAAAAz/////AQH/////" +
           "AAAAAARhggoEAAAAAAAHAAAARGlzYWJsZQEBCiUALwEARCMKJQAAAQEBAAAAAQD5CwABAPMKAAAAAARh" +
           "ggoEAAAAAAAGAAAARW5hYmxlAQEJJQAvAQBDIwklAAABAQEAAAABAPkLAAEA8woAAAAABGGCCgQAAAAA" +
           "AAoAAABBZGRDb21tZW50AQELJQAvAQBFIwslAAABAQEAAAABAPkLAAEADQsBAAAAFWCpCgIAAAAAAA4A" +
           "AABJbnB1dEFyZ3VtZW50cwEBDCUALgBEDCUAAJYCAAAAAQAqAQFGAAAABwAAAEV2ZW50SWQAD/////8A" +
           "AAAAAwAAAAAoAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0byBjb21tZW50LgEAKgEBQgAA" +
           "AAcAAABDb21tZW50ABX/////AAAAAAMAAAAAJAAAAFRoZSBjb21tZW50IHRvIGFkZCB0byB0aGUgY29u" +
           "ZGl0aW9uLgEAKAEBAAAAAQH/////AAAAABVgiQoCAAAAAAAKAAAAQWNrZWRTdGF0ZQEBDyUALwEAIyMP" +
           "JQAAABX/////AQEBAAAAAQAsIwEBAfokAQAAABVgiQoCAAAAAAACAAAASWQBARAlAC4ARBAlAAAAAf//" +
           "//8BAf////8AAAAABGGCCgQAAAAAAAsAAABBY2tub3dsZWRnZQEBHyUALwEAlyMfJQAAAQEBAAAAAQD5" +
           "CwABAPAiAQAAABVgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBASAlAC4ARCAlAACWAgAAAAEAKgEB" +
           "RgAAAAcAAABFdmVudElkAA//////AAAAAAMAAAAAKAAAAFRoZSBpZGVudGlmaWVyIGZvciB0aGUgZXZl" +
           "bnQgdG8gY29tbWVudC4BACoBAUIAAAAHAAAAQ29tbWVudAAV/////wAAAAADAAAAACQAAABUaGUgY29t" +
           "bWVudCB0byBhZGQgdG8gdGhlIGNvbmRpdGlvbi4BACgBAQAAAAEB/////wAAAAAVYIkKAgAAAAEADAAA" +
           "AEJvb2xlYW5WYWx1ZQEBIyUALwA/IyUAAAAB/////wEB/////wAAAAAVYIkKAgAAAAEACgAAAFNCeXRl" +
           "VmFsdWUBASQlAC8APyQlAAAAAv////8BAf////8AAAAAFWCJCgIAAAABAAkAAABCeXRlVmFsdWUBASUl" +
           "AC8APyUlAAAAA/////8BAf////8AAAAAFWCJCgIAAAABAAoAAABJbnQxNlZhbHVlAQEmJQAvAD8mJQAA" +
           "AAT/////AQH/////AAAAABVgiQoCAAAAAQALAAAAVUludDE2VmFsdWUBASclAC8APyclAAAABf////8B" +
           "Af////8AAAAAFWCJCgIAAAABAAoAAABJbnQzMlZhbHVlAQEoJQAvAD8oJQAAAAb/////AQH/////AAAA" +
           "ABVgiQoCAAAAAQALAAAAVUludDMyVmFsdWUBASklAC8APyklAAAAB/////8BAf////8AAAAAFWCJCgIA" +
           "AAABAAoAAABJbnQ2NFZhbHVlAQEqJQAvAD8qJQAAAAj/////AQH/////AAAAABVgiQoCAAAAAQALAAAA" +
           "VUludDY0VmFsdWUBASslAC8APyslAAAACf////8BAf////8AAAAAFWCJCgIAAAABAAoAAABGbG9hdFZh" +
           "bHVlAQEsJQAvAD8sJQAAAAr/////AQH/////AAAAABVgiQoCAAAAAQALAAAARG91YmxlVmFsdWUBAS0l" +
           "AC8APy0lAAAAC/////8BAf////8AAAAAFWCJCgIAAAABAAsAAABTdHJpbmdWYWx1ZQEBLiUALwA/LiUA" +
           "AAAM/////wEB/////wAAAAAVYIkKAgAAAAEADQAAAERhdGVUaW1lVmFsdWUBAS8lAC8APy8lAAAADf//" +
           "//8BAf////8AAAAAFWCJCgIAAAABAAkAAABHdWlkVmFsdWUBATAlAC8APzAlAAAADv////8BAf////8A" +
           "AAAAFWCJCgIAAAABAA8AAABCeXRlU3RyaW5nVmFsdWUBATElAC8APzElAAAAD/////8BAf////8AAAAA" +
           "FWCJCgIAAAABAA8AAABYbWxFbGVtZW50VmFsdWUBATIlAC8APzIlAAAAEP////8BAf////8AAAAAFWCJ" +
           "CgIAAAABAAsAAABOb2RlSWRWYWx1ZQEBMyUALwA/MyUAAAAR/////wEB/////wAAAAAVYIkKAgAAAAEA" +
           "EwAAAEV4cGFuZGVkTm9kZUlkVmFsdWUBATQlAC8APzQlAAAAEv////8BAf////8AAAAAFWCJCgIAAAAB" +
           "ABIAAABRdWFsaWZpZWROYW1lVmFsdWUBATUlAC8APzUlAAAAFP////8BAf////8AAAAAFWCJCgIAAAAB" +
           "ABIAAABMb2NhbGl6ZWRUZXh0VmFsdWUBATYlAC8APzYlAAAAFf////8BAf////8AAAAAFWCJCgIAAAAB" +
           "AA8AAABTdGF0dXNDb2RlVmFsdWUBATclAC8APzclAAAAE/////8BAf////8AAAAAFWCJCgIAAAABAAwA" +
           "AABWYXJpYW50VmFsdWUBATglAC8APzglAAAAGP////8BAf////8AAAAAFWCJCgIAAAABABAAAABFbnVt" +
           "ZXJhdGlvblZhbHVlAQE5JQAvAD85JQAAAB3/////AQH/////AAAAABVgiQoCAAAAAQAOAAAAU3RydWN0" +
           "dXJlVmFsdWUBATolAC8APzolAAAAFv////8BAf////8AAAAAFWCJCgIAAAABAAsAAABOdW1iZXJWYWx1" +
           "ZQEBOyUALwA/OyUAAAAa/////wEB/////wAAAAAVYIkKAgAAAAEADAAAAEludGVnZXJWYWx1ZQEBPCUA" +
           "LwA/PCUAAAAb/////wEB/////wAAAAAVYIkKAgAAAAEADQAAAFVJbnRlZ2VyVmFsdWUBAT0lAC8APz0l" +
           "AAAAHP////8BAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the BooleanValue Variable.
        /// </summary>
        public BaseDataVariableState<bool> BooleanValue {
            get => m_booleanValue;

            set {
                if (!ReferenceEquals(m_booleanValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_booleanValue = value;
            }
        }

        /// <summary>
        /// A description for the SByteValue Variable.
        /// </summary>
        public BaseDataVariableState<sbyte> SByteValue {
            get => m_sByteValue;

            set {
                if (!ReferenceEquals(m_sByteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_sByteValue = value;
            }
        }

        /// <summary>
        /// A description for the ByteValue Variable.
        /// </summary>
        public BaseDataVariableState<byte> ByteValue {
            get => m_byteValue;

            set {
                if (!ReferenceEquals(m_byteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_byteValue = value;
            }
        }

        /// <summary>
        /// A description for the Int16Value Variable.
        /// </summary>
        public BaseDataVariableState<short> Int16Value {
            get => m_int16Value;

            set {
                if (!ReferenceEquals(m_int16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int16Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt16Value Variable.
        /// </summary>
        public BaseDataVariableState<ushort> UInt16Value {
            get => m_uInt16Value;

            set {
                if (!ReferenceEquals(m_uInt16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt16Value = value;
            }
        }

        /// <summary>
        /// A description for the Int32Value Variable.
        /// </summary>
        public BaseDataVariableState<int> Int32Value {
            get => m_int32Value;

            set {
                if (!ReferenceEquals(m_int32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int32Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt32Value Variable.
        /// </summary>
        public BaseDataVariableState<uint> UInt32Value {
            get => m_uInt32Value;

            set {
                if (!ReferenceEquals(m_uInt32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt32Value = value;
            }
        }

        /// <summary>
        /// A description for the Int64Value Variable.
        /// </summary>
        public BaseDataVariableState<long> Int64Value {
            get => m_int64Value;

            set {
                if (!ReferenceEquals(m_int64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int64Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt64Value Variable.
        /// </summary>
        public BaseDataVariableState<ulong> UInt64Value {
            get => m_uInt64Value;

            set {
                if (!ReferenceEquals(m_uInt64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt64Value = value;
            }
        }

        /// <summary>
        /// A description for the FloatValue Variable.
        /// </summary>
        public BaseDataVariableState<float> FloatValue {
            get => m_floatValue;

            set {
                if (!ReferenceEquals(m_floatValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_floatValue = value;
            }
        }

        /// <summary>
        /// A description for the DoubleValue Variable.
        /// </summary>
        public BaseDataVariableState<double> DoubleValue {
            get => m_doubleValue;

            set {
                if (!ReferenceEquals(m_doubleValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_doubleValue = value;
            }
        }

        /// <summary>
        /// A description for the StringValue Variable.
        /// </summary>
        public BaseDataVariableState<string> StringValue {
            get => m_stringValue;

            set {
                if (!ReferenceEquals(m_stringValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_stringValue = value;
            }
        }

        /// <summary>
        /// A description for the DateTimeValue Variable.
        /// </summary>
        public BaseDataVariableState<DateTime> DateTimeValue {
            get => m_dateTimeValue;

            set {
                if (!ReferenceEquals(m_dateTimeValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_dateTimeValue = value;
            }
        }

        /// <summary>
        /// A description for the GuidValue Variable.
        /// </summary>
        public BaseDataVariableState<Guid> GuidValue {
            get => m_guidValue;

            set {
                if (!ReferenceEquals(m_guidValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_guidValue = value;
            }
        }

        /// <summary>
        /// A description for the ByteStringValue Variable.
        /// </summary>
        public BaseDataVariableState<byte[]> ByteStringValue {
            get => m_byteStringValue;

            set {
                if (!ReferenceEquals(m_byteStringValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_byteStringValue = value;
            }
        }

        /// <summary>
        /// A description for the XmlElementValue Variable.
        /// </summary>
        public BaseDataVariableState<XmlElement> XmlElementValue {
            get => m_xmlElementValue;

            set {
                if (!ReferenceEquals(m_xmlElementValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_xmlElementValue = value;
            }
        }

        /// <summary>
        /// A description for the NodeIdValue Variable.
        /// </summary>
        public BaseDataVariableState<NodeId> NodeIdValue {
            get => m_nodeIdValue;

            set {
                if (!ReferenceEquals(m_nodeIdValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_nodeIdValue = value;
            }
        }

        /// <summary>
        /// A description for the ExpandedNodeIdValue Variable.
        /// </summary>
        public BaseDataVariableState<ExpandedNodeId> ExpandedNodeIdValue {
            get => m_expandedNodeIdValue;

            set {
                if (!ReferenceEquals(m_expandedNodeIdValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_expandedNodeIdValue = value;
            }
        }

        /// <summary>
        /// A description for the QualifiedNameValue Variable.
        /// </summary>
        public BaseDataVariableState<QualifiedName> QualifiedNameValue {
            get => m_qualifiedNameValue;

            set {
                if (!ReferenceEquals(m_qualifiedNameValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_qualifiedNameValue = value;
            }
        }

        /// <summary>
        /// A description for the LocalizedTextValue Variable.
        /// </summary>
        public BaseDataVariableState<LocalizedText> LocalizedTextValue {
            get => m_localizedTextValue;

            set {
                if (!ReferenceEquals(m_localizedTextValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_localizedTextValue = value;
            }
        }

        /// <summary>
        /// A description for the StatusCodeValue Variable.
        /// </summary>
        public BaseDataVariableState<StatusCode> StatusCodeValue {
            get => m_statusCodeValue;

            set {
                if (!ReferenceEquals(m_statusCodeValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_statusCodeValue = value;
            }
        }

        /// <summary>
        /// A description for the VariantValue Variable.
        /// </summary>
        public BaseDataVariableState VariantValue {
            get => m_variantValue;

            set {
                if (!ReferenceEquals(m_variantValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_variantValue = value;
            }
        }

        /// <summary>
        /// A description for the EnumerationValue Variable.
        /// </summary>
        public BaseDataVariableState<int> EnumerationValue {
            get => m_enumerationValue;

            set {
                if (!ReferenceEquals(m_enumerationValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_enumerationValue = value;
            }
        }

        /// <summary>
        /// A description for the StructureValue Variable.
        /// </summary>
        public BaseDataVariableState<ExtensionObject> StructureValue {
            get => m_structureValue;

            set {
                if (!ReferenceEquals(m_structureValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_structureValue = value;
            }
        }

        /// <summary>
        /// A description for the NumberValue Variable.
        /// </summary>
        public BaseDataVariableState NumberValue {
            get => m_numberValue;

            set {
                if (!ReferenceEquals(m_numberValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_numberValue = value;
            }
        }

        /// <summary>
        /// A description for the IntegerValue Variable.
        /// </summary>
        public BaseDataVariableState IntegerValue {
            get => m_integerValue;

            set {
                if (!ReferenceEquals(m_integerValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_integerValue = value;
            }
        }

        /// <summary>
        /// A description for the UIntegerValue Variable.
        /// </summary>
        public BaseDataVariableState UIntegerValue {
            get => m_uIntegerValue;

            set {
                if (!ReferenceEquals(m_uIntegerValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uIntegerValue = value;
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
            if (m_booleanValue != null) {
                children.Add(m_booleanValue);
            }

            if (m_sByteValue != null) {
                children.Add(m_sByteValue);
            }

            if (m_byteValue != null) {
                children.Add(m_byteValue);
            }

            if (m_int16Value != null) {
                children.Add(m_int16Value);
            }

            if (m_uInt16Value != null) {
                children.Add(m_uInt16Value);
            }

            if (m_int32Value != null) {
                children.Add(m_int32Value);
            }

            if (m_uInt32Value != null) {
                children.Add(m_uInt32Value);
            }

            if (m_int64Value != null) {
                children.Add(m_int64Value);
            }

            if (m_uInt64Value != null) {
                children.Add(m_uInt64Value);
            }

            if (m_floatValue != null) {
                children.Add(m_floatValue);
            }

            if (m_doubleValue != null) {
                children.Add(m_doubleValue);
            }

            if (m_stringValue != null) {
                children.Add(m_stringValue);
            }

            if (m_dateTimeValue != null) {
                children.Add(m_dateTimeValue);
            }

            if (m_guidValue != null) {
                children.Add(m_guidValue);
            }

            if (m_byteStringValue != null) {
                children.Add(m_byteStringValue);
            }

            if (m_xmlElementValue != null) {
                children.Add(m_xmlElementValue);
            }

            if (m_nodeIdValue != null) {
                children.Add(m_nodeIdValue);
            }

            if (m_expandedNodeIdValue != null) {
                children.Add(m_expandedNodeIdValue);
            }

            if (m_qualifiedNameValue != null) {
                children.Add(m_qualifiedNameValue);
            }

            if (m_localizedTextValue != null) {
                children.Add(m_localizedTextValue);
            }

            if (m_statusCodeValue != null) {
                children.Add(m_statusCodeValue);
            }

            if (m_variantValue != null) {
                children.Add(m_variantValue);
            }

            if (m_enumerationValue != null) {
                children.Add(m_enumerationValue);
            }

            if (m_structureValue != null) {
                children.Add(m_structureValue);
            }

            if (m_numberValue != null) {
                children.Add(m_numberValue);
            }

            if (m_integerValue != null) {
                children.Add(m_integerValue);
            }

            if (m_uIntegerValue != null) {
                children.Add(m_uIntegerValue);
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
                case BrowseNames.BooleanValue: {
                        if (createOrReplace) {
                            if (BooleanValue == null) {
                                if (replacement == null) {
                                    BooleanValue = new BaseDataVariableState<bool>(this);
                                }
                                else {
                                    BooleanValue = (BaseDataVariableState<bool>)replacement;
                                }
                            }
                        }

                        instance = BooleanValue;
                        break;
                    }

                case BrowseNames.SByteValue: {
                        if (createOrReplace) {
                            if (SByteValue == null) {
                                if (replacement == null) {
                                    SByteValue = new BaseDataVariableState<sbyte>(this);
                                }
                                else {
                                    SByteValue = (BaseDataVariableState<sbyte>)replacement;
                                }
                            }
                        }

                        instance = SByteValue;
                        break;
                    }

                case BrowseNames.ByteValue: {
                        if (createOrReplace) {
                            if (ByteValue == null) {
                                if (replacement == null) {
                                    ByteValue = new BaseDataVariableState<byte>(this);
                                }
                                else {
                                    ByteValue = (BaseDataVariableState<byte>)replacement;
                                }
                            }
                        }

                        instance = ByteValue;
                        break;
                    }

                case BrowseNames.Int16Value: {
                        if (createOrReplace) {
                            if (Int16Value == null) {
                                if (replacement == null) {
                                    Int16Value = new BaseDataVariableState<short>(this);
                                }
                                else {
                                    Int16Value = (BaseDataVariableState<short>)replacement;
                                }
                            }
                        }

                        instance = Int16Value;
                        break;
                    }

                case BrowseNames.UInt16Value: {
                        if (createOrReplace) {
                            if (UInt16Value == null) {
                                if (replacement == null) {
                                    UInt16Value = new BaseDataVariableState<ushort>(this);
                                }
                                else {
                                    UInt16Value = (BaseDataVariableState<ushort>)replacement;
                                }
                            }
                        }

                        instance = UInt16Value;
                        break;
                    }

                case BrowseNames.Int32Value: {
                        if (createOrReplace) {
                            if (Int32Value == null) {
                                if (replacement == null) {
                                    Int32Value = new BaseDataVariableState<int>(this);
                                }
                                else {
                                    Int32Value = (BaseDataVariableState<int>)replacement;
                                }
                            }
                        }

                        instance = Int32Value;
                        break;
                    }

                case BrowseNames.UInt32Value: {
                        if (createOrReplace) {
                            if (UInt32Value == null) {
                                if (replacement == null) {
                                    UInt32Value = new BaseDataVariableState<uint>(this);
                                }
                                else {
                                    UInt32Value = (BaseDataVariableState<uint>)replacement;
                                }
                            }
                        }

                        instance = UInt32Value;
                        break;
                    }

                case BrowseNames.Int64Value: {
                        if (createOrReplace) {
                            if (Int64Value == null) {
                                if (replacement == null) {
                                    Int64Value = new BaseDataVariableState<long>(this);
                                }
                                else {
                                    Int64Value = (BaseDataVariableState<long>)replacement;
                                }
                            }
                        }

                        instance = Int64Value;
                        break;
                    }

                case BrowseNames.UInt64Value: {
                        if (createOrReplace) {
                            if (UInt64Value == null) {
                                if (replacement == null) {
                                    UInt64Value = new BaseDataVariableState<ulong>(this);
                                }
                                else {
                                    UInt64Value = (BaseDataVariableState<ulong>)replacement;
                                }
                            }
                        }

                        instance = UInt64Value;
                        break;
                    }

                case BrowseNames.FloatValue: {
                        if (createOrReplace) {
                            if (FloatValue == null) {
                                if (replacement == null) {
                                    FloatValue = new BaseDataVariableState<float>(this);
                                }
                                else {
                                    FloatValue = (BaseDataVariableState<float>)replacement;
                                }
                            }
                        }

                        instance = FloatValue;
                        break;
                    }

                case BrowseNames.DoubleValue: {
                        if (createOrReplace) {
                            if (DoubleValue == null) {
                                if (replacement == null) {
                                    DoubleValue = new BaseDataVariableState<double>(this);
                                }
                                else {
                                    DoubleValue = (BaseDataVariableState<double>)replacement;
                                }
                            }
                        }

                        instance = DoubleValue;
                        break;
                    }

                case BrowseNames.StringValue: {
                        if (createOrReplace) {
                            if (StringValue == null) {
                                if (replacement == null) {
                                    StringValue = new BaseDataVariableState<string>(this);
                                }
                                else {
                                    StringValue = (BaseDataVariableState<string>)replacement;
                                }
                            }
                        }

                        instance = StringValue;
                        break;
                    }

                case BrowseNames.DateTimeValue: {
                        if (createOrReplace) {
                            if (DateTimeValue == null) {
                                if (replacement == null) {
                                    DateTimeValue = new BaseDataVariableState<DateTime>(this);
                                }
                                else {
                                    DateTimeValue = (BaseDataVariableState<DateTime>)replacement;
                                }
                            }
                        }

                        instance = DateTimeValue;
                        break;
                    }

                case BrowseNames.GuidValue: {
                        if (createOrReplace) {
                            if (GuidValue == null) {
                                if (replacement == null) {
                                    GuidValue = new BaseDataVariableState<Guid>(this);
                                }
                                else {
                                    GuidValue = (BaseDataVariableState<Guid>)replacement;
                                }
                            }
                        }

                        instance = GuidValue;
                        break;
                    }

                case BrowseNames.ByteStringValue: {
                        if (createOrReplace) {
                            if (ByteStringValue == null) {
                                if (replacement == null) {
                                    ByteStringValue = new BaseDataVariableState<byte[]>(this);
                                }
                                else {
                                    ByteStringValue = (BaseDataVariableState<byte[]>)replacement;
                                }
                            }
                        }

                        instance = ByteStringValue;
                        break;
                    }

                case BrowseNames.XmlElementValue: {
                        if (createOrReplace) {
                            if (XmlElementValue == null) {
                                if (replacement == null) {
                                    XmlElementValue = new BaseDataVariableState<XmlElement>(this);
                                }
                                else {
                                    XmlElementValue = (BaseDataVariableState<XmlElement>)replacement;
                                }
                            }
                        }

                        instance = XmlElementValue;
                        break;
                    }

                case BrowseNames.NodeIdValue: {
                        if (createOrReplace) {
                            if (NodeIdValue == null) {
                                if (replacement == null) {
                                    NodeIdValue = new BaseDataVariableState<NodeId>(this);
                                }
                                else {
                                    NodeIdValue = (BaseDataVariableState<NodeId>)replacement;
                                }
                            }
                        }

                        instance = NodeIdValue;
                        break;
                    }

                case BrowseNames.ExpandedNodeIdValue: {
                        if (createOrReplace) {
                            if (ExpandedNodeIdValue == null) {
                                if (replacement == null) {
                                    ExpandedNodeIdValue = new BaseDataVariableState<ExpandedNodeId>(this);
                                }
                                else {
                                    ExpandedNodeIdValue = (BaseDataVariableState<ExpandedNodeId>)replacement;
                                }
                            }
                        }

                        instance = ExpandedNodeIdValue;
                        break;
                    }

                case BrowseNames.QualifiedNameValue: {
                        if (createOrReplace) {
                            if (QualifiedNameValue == null) {
                                if (replacement == null) {
                                    QualifiedNameValue = new BaseDataVariableState<QualifiedName>(this);
                                }
                                else {
                                    QualifiedNameValue = (BaseDataVariableState<QualifiedName>)replacement;
                                }
                            }
                        }

                        instance = QualifiedNameValue;
                        break;
                    }

                case BrowseNames.LocalizedTextValue: {
                        if (createOrReplace) {
                            if (LocalizedTextValue == null) {
                                if (replacement == null) {
                                    LocalizedTextValue = new BaseDataVariableState<LocalizedText>(this);
                                }
                                else {
                                    LocalizedTextValue = (BaseDataVariableState<LocalizedText>)replacement;
                                }
                            }
                        }

                        instance = LocalizedTextValue;
                        break;
                    }

                case BrowseNames.StatusCodeValue: {
                        if (createOrReplace) {
                            if (StatusCodeValue == null) {
                                if (replacement == null) {
                                    StatusCodeValue = new BaseDataVariableState<StatusCode>(this);
                                }
                                else {
                                    StatusCodeValue = (BaseDataVariableState<StatusCode>)replacement;
                                }
                            }
                        }

                        instance = StatusCodeValue;
                        break;
                    }

                case BrowseNames.VariantValue: {
                        if (createOrReplace) {
                            if (VariantValue == null) {
                                if (replacement == null) {
                                    VariantValue = new BaseDataVariableState(this);
                                }
                                else {
                                    VariantValue = (BaseDataVariableState)replacement;
                                }
                            }
                        }

                        instance = VariantValue;
                        break;
                    }

                case BrowseNames.EnumerationValue: {
                        if (createOrReplace) {
                            if (EnumerationValue == null) {
                                if (replacement == null) {
                                    EnumerationValue = new BaseDataVariableState<int>(this);
                                }
                                else {
                                    EnumerationValue = (BaseDataVariableState<int>)replacement;
                                }
                            }
                        }

                        instance = EnumerationValue;
                        break;
                    }

                case BrowseNames.StructureValue: {
                        if (createOrReplace) {
                            if (StructureValue == null) {
                                if (replacement == null) {
                                    StructureValue = new BaseDataVariableState<ExtensionObject>(this);
                                }
                                else {
                                    StructureValue = (BaseDataVariableState<ExtensionObject>)replacement;
                                }
                            }
                        }

                        instance = StructureValue;
                        break;
                    }

                case BrowseNames.NumberValue: {
                        if (createOrReplace) {
                            if (NumberValue == null) {
                                if (replacement == null) {
                                    NumberValue = new BaseDataVariableState(this);
                                }
                                else {
                                    NumberValue = (BaseDataVariableState)replacement;
                                }
                            }
                        }

                        instance = NumberValue;
                        break;
                    }

                case BrowseNames.IntegerValue: {
                        if (createOrReplace) {
                            if (IntegerValue == null) {
                                if (replacement == null) {
                                    IntegerValue = new BaseDataVariableState(this);
                                }
                                else {
                                    IntegerValue = (BaseDataVariableState)replacement;
                                }
                            }
                        }

                        instance = IntegerValue;
                        break;
                    }

                case BrowseNames.UIntegerValue: {
                        if (createOrReplace) {
                            if (UIntegerValue == null) {
                                if (replacement == null) {
                                    UIntegerValue = new BaseDataVariableState(this);
                                }
                                else {
                                    UIntegerValue = (BaseDataVariableState)replacement;
                                }
                            }
                        }

                        instance = UIntegerValue;
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
        private BaseDataVariableState<bool> m_booleanValue;
        private BaseDataVariableState<sbyte> m_sByteValue;
        private BaseDataVariableState<byte> m_byteValue;
        private BaseDataVariableState<short> m_int16Value;
        private BaseDataVariableState<ushort> m_uInt16Value;
        private BaseDataVariableState<int> m_int32Value;
        private BaseDataVariableState<uint> m_uInt32Value;
        private BaseDataVariableState<long> m_int64Value;
        private BaseDataVariableState<ulong> m_uInt64Value;
        private BaseDataVariableState<float> m_floatValue;
        private BaseDataVariableState<double> m_doubleValue;
        private BaseDataVariableState<string> m_stringValue;
        private BaseDataVariableState<DateTime> m_dateTimeValue;
        private BaseDataVariableState<Guid> m_guidValue;
        private BaseDataVariableState<byte[]> m_byteStringValue;
        private BaseDataVariableState<XmlElement> m_xmlElementValue;
        private BaseDataVariableState<NodeId> m_nodeIdValue;
        private BaseDataVariableState<ExpandedNodeId> m_expandedNodeIdValue;
        private BaseDataVariableState<QualifiedName> m_qualifiedNameValue;
        private BaseDataVariableState<LocalizedText> m_localizedTextValue;
        private BaseDataVariableState<StatusCode> m_statusCodeValue;
        private BaseDataVariableState m_variantValue;
        private BaseDataVariableState<int> m_enumerationValue;
        private BaseDataVariableState<ExtensionObject> m_structureValue;
        private BaseDataVariableState m_numberValue;
        private BaseDataVariableState m_integerValue;
        private BaseDataVariableState m_uIntegerValue;
        #endregion
    }
#endif
    #endregion

    #region AnalogScalarValueObjectState Class
#if !OPCUA_EXCLUDE_AnalogScalarValueObjectState
    /// <summary>
    /// Stores an instance of the AnalogScalarValueObjectType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class AnalogScalarValueObjectState : TestDataObjectState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public AnalogScalarValueObjectState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return NodeId.Create(ObjectTypes.AnalogScalarValueObjectType, Namespaces.TestData, namespaceUris);
        }

#if !OPCUA_EXCLUDE_InitializationStrings
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
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGCAAAEAAAABACMAAABBbmFsb2dTY2Fs" +
           "YXJWYWx1ZU9iamVjdFR5cGVJbnN0YW5jZQEBPiUBAT4lAQAAAAAkAAEBQiUQAAAANWCJCgIAAAABABAA" +
           "AABTaW11bGF0aW9uQWN0aXZlAQE/JQMAAAAARwAAAElmIHRydWUgdGhlIHNlcnZlciB3aWxsIHByb2R1" +
           "Y2UgbmV3IHZhbHVlcyBmb3IgZWFjaCBtb25pdG9yZWQgdmFyaWFibGUuAC4ARD8lAAAAAf////8BAf//" +
           "//8AAAAABGGCCgQAAAABAA4AAABHZW5lcmF0ZVZhbHVlcwEBQCUALwEBqSRAJQAAAQH/////AQAAABVg" +
           "qQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAUElAC4AREElAACWAQAAAAEAKgEBRgAAAAoAAABJdGVy" +
           "YXRpb25zAAf/////AAAAAAMAAAAAJQAAAFRoZSBudW1iZXIgb2YgbmV3IHZhbHVlcyB0byBnZW5lcmF0" +
           "ZS4BACgBAQAAAAEB/////wAAAAAEYIAKAQAAAAEADQAAAEN5Y2xlQ29tcGxldGUBAUIlAC8BAEELQiUA" +
           "AAEAAAAAJAEBAT4lFwAAADVgiQoCAAAAAAAHAAAARXZlbnRJZAEBQyUDAAAAACsAAABBIGdsb2JhbGx5" +
           "IHVuaXF1ZSBpZGVudGlmaWVyIGZvciB0aGUgZXZlbnQuAC4AREMlAAAAD/////8BAf////8AAAAANWCJ" +
           "CgIAAAAAAAkAAABFdmVudFR5cGUBAUQlAwAAAAAiAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVu" +
           "dCB0eXBlLgAuAEREJQAAABH/////AQH/////AAAAADVgiQoCAAAAAAAKAAAAU291cmNlTm9kZQEBRSUD" +
           "AAAAABgAAABUaGUgc291cmNlIG9mIHRoZSBldmVudC4ALgBERSUAAAAR/////wEB/////wAAAAA1YIkK" +
           "AgAAAAAACgAAAFNvdXJjZU5hbWUBAUYlAwAAAAApAAAAQSBkZXNjcmlwdGlvbiBvZiB0aGUgc291cmNl" +
           "IG9mIHRoZSBldmVudC4ALgBERiUAAAAM/////wEB/////wAAAAA1YIkKAgAAAAAABAAAAFRpbWUBAUcl" +
           "AwAAAAAYAAAAV2hlbiB0aGUgZXZlbnQgb2NjdXJyZWQuAC4AREclAAABACYB/////wEB/////wAAAAA1" +
           "YIkKAgAAAAAACwAAAFJlY2VpdmVUaW1lAQFIJQMAAAAAPgAAAFdoZW4gdGhlIHNlcnZlciByZWNlaXZl" +
           "ZCB0aGUgZXZlbnQgZnJvbSB0aGUgdW5kZXJseWluZyBzeXN0ZW0uAC4AREglAAABACYB/////wEB////" +
           "/wAAAAA1YIkKAgAAAAAABwAAAE1lc3NhZ2UBAUolAwAAAAAlAAAAQSBsb2NhbGl6ZWQgZGVzY3JpcHRp" +
           "b24gb2YgdGhlIGV2ZW50LgAuAERKJQAAABX/////AQH/////AAAAADVgiQoCAAAAAAAIAAAAU2V2ZXJp" +
           "dHkBAUslAwAAAAAhAAAASW5kaWNhdGVzIGhvdyB1cmdlbnQgYW4gZXZlbnQgaXMuAC4AREslAAAABf//" +
           "//8BAf////8AAAAAFWCJCgIAAAAAABAAAABDb25kaXRpb25DbGFzc0lkAQE+LQAuAEQ+LQAAABH/////" +
           "AQH/////AAAAABVgiQoCAAAAAAASAAAAQ29uZGl0aW9uQ2xhc3NOYW1lAQE/LQAuAEQ/LQAAABX/////" +
           "AQH/////AAAAABVgiQoCAAAAAAANAAAAQ29uZGl0aW9uTmFtZQEBJy0ALgBEJy0AAAAM/////wEB////" +
           "/wAAAAAVYIkKAgAAAAAACAAAAEJyYW5jaElkAQFMJQAuAERMJQAAABH/////AQH/////AAAAABVgiQoC" +
           "AAAAAAAGAAAAUmV0YWluAQFNJQAuAERNJQAAAAH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAARW5h" +
           "YmxlZFN0YXRlAQFOJQAvAQAjI04lAAAAFf////8BAQIAAAABACwjAAEBYyUBACwjAAEBayUBAAAAFWCJ" +
           "CgIAAAAAAAIAAABJZAEBTyUALgBETyUAAAAB/////wEB/////wAAAAAVYIkKAgAAAAAABwAAAFF1YWxp" +
           "dHkBAVQlAC8BACojVCUAAAAT/////wEB/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVzdGFt" +
           "cAEBVSUALgBEVSUAAAEAJgH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAATGFzdFNldmVyaXR5AQFY" +
           "JQAvAQAqI1glAAAABf////8BAf////8BAAAAFWCJCgIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAVkl" +
           "AC4ARFklAAABACYB/////wEB/////wAAAAAVYIkKAgAAAAAABwAAAENvbW1lbnQBAVolAC8BACojWiUA" +
           "AAAV/////wEB/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVzdGFtcAEBWyUALgBEWyUAAAEA" +
           "JgH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAAQ2xpZW50VXNlcklkAQFcJQAuAERcJQAAAAz/////" +
           "AQH/////AAAAAARhggoEAAAAAAAHAAAARGlzYWJsZQEBXiUALwEARCNeJQAAAQEBAAAAAQD5CwABAPMK" +
           "AAAAAARhggoEAAAAAAAGAAAARW5hYmxlAQFdJQAvAQBDI10lAAABAQEAAAABAPkLAAEA8woAAAAABGGC" +
           "CgQAAAAAAAoAAABBZGRDb21tZW50AQFfJQAvAQBFI18lAAABAQEAAAABAPkLAAEADQsBAAAAFWCpCgIA" +
           "AAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBYCUALgBEYCUAAJYCAAAAAQAqAQFGAAAABwAAAEV2ZW50SWQA" +
           "D/////8AAAAAAwAAAAAoAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0byBjb21tZW50LgEA" +
           "KgEBQgAAAAcAAABDb21tZW50ABX/////AAAAAAMAAAAAJAAAAFRoZSBjb21tZW50IHRvIGFkZCB0byB0" +
           "aGUgY29uZGl0aW9uLgEAKAEBAAAAAQH/////AAAAABVgiQoCAAAAAAAKAAAAQWNrZWRTdGF0ZQEBYyUA" +
           "LwEAIyNjJQAAABX/////AQEBAAAAAQAsIwEBAU4lAQAAABVgiQoCAAAAAAACAAAASWQBAWQlAC4ARGQl" +
           "AAAAAf////8BAf////8AAAAABGGCCgQAAAAAAAsAAABBY2tub3dsZWRnZQEBcyUALwEAlyNzJQAAAQEB" +
           "AAAAAQD5CwABAPAiAQAAABVgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAXQlAC4ARHQlAACWAgAA" +
           "AAEAKgEBRgAAAAcAAABFdmVudElkAA//////AAAAAAMAAAAAKAAAAFRoZSBpZGVudGlmaWVyIGZvciB0" +
           "aGUgZXZlbnQgdG8gY29tbWVudC4BACoBAUIAAAAHAAAAQ29tbWVudAAV/////wAAAAADAAAAACQAAABU" +
           "aGUgY29tbWVudCB0byBhZGQgdG8gdGhlIGNvbmRpdGlvbi4BACgBAQAAAAEB/////wAAAAAVYIkKAgAA" +
           "AAEACgAAAFNCeXRlVmFsdWUBAXclAC8BAEAJdyUAAAAC/////wEB/////wEAAAAVYIkKAgAAAAAABwAA" +
           "AEVVUmFuZ2UBAXolAC4ARHolAAABAHQD/////wEB/////wAAAAAVYIkKAgAAAAEACQAAAEJ5dGVWYWx1" +
           "ZQEBfSUALwEAQAl9JQAAAAP/////AQH/////AQAAABVgiQoCAAAAAAAHAAAARVVSYW5nZQEBgCUALgBE" +
           "gCUAAAEAdAP/////AQH/////AAAAABVgiQoCAAAAAQAKAAAASW50MTZWYWx1ZQEBgyUALwEAQAmDJQAA" +
           "AAT/////AQH/////AQAAABVgiQoCAAAAAAAHAAAARVVSYW5nZQEBhiUALgBEhiUAAAEAdAP/////AQH/" +
           "////AAAAABVgiQoCAAAAAQALAAAAVUludDE2VmFsdWUBAYklAC8BAEAJiSUAAAAF/////wEB/////wEA" +
           "AAAVYIkKAgAAAAAABwAAAEVVUmFuZ2UBAYwlAC4ARIwlAAABAHQD/////wEB/////wAAAAAVYIkKAgAA" +
           "AAEACgAAAEludDMyVmFsdWUBAY8lAC8BAEAJjyUAAAAG/////wEB/////wEAAAAVYIkKAgAAAAAABwAA" +
           "AEVVUmFuZ2UBAZIlAC4ARJIlAAABAHQD/////wEB/////wAAAAAVYIkKAgAAAAEACwAAAFVJbnQzMlZh" +
           "bHVlAQGVJQAvAQBACZUlAAAAB/////8BAf////8BAAAAFWCJCgIAAAAAAAcAAABFVVJhbmdlAQGYJQAu" +
           "AESYJQAAAQB0A/////8BAf////8AAAAAFWCJCgIAAAABAAoAAABJbnQ2NFZhbHVlAQGbJQAvAQBACZsl" +
           "AAAACP////8BAf////8BAAAAFWCJCgIAAAAAAAcAAABFVVJhbmdlAQGeJQAuAESeJQAAAQB0A/////8B" +
           "Af////8AAAAAFWCJCgIAAAABAAsAAABVSW50NjRWYWx1ZQEBoSUALwEAQAmhJQAAAAn/////AQH/////" +
           "AQAAABVgiQoCAAAAAAAHAAAARVVSYW5nZQEBpCUALgBEpCUAAAEAdAP/////AQH/////AAAAABVgiQoC" +
           "AAAAAQAKAAAARmxvYXRWYWx1ZQEBpyUALwEAQAmnJQAAAAr/////AQH/////AQAAABVgiQoCAAAAAAAH" +
           "AAAARVVSYW5nZQEBqiUALgBEqiUAAAEAdAP/////AQH/////AAAAABVgiQoCAAAAAQALAAAARG91Ymxl" +
           "VmFsdWUBAa0lAC8BAEAJrSUAAAAL/////wEB/////wEAAAAVYIkKAgAAAAAABwAAAEVVUmFuZ2UBAbAl" +
           "AC4ARLAlAAABAHQD/////wEB/////wAAAAAVYIkKAgAAAAEACwAAAE51bWJlclZhbHVlAQGzJQAvAQBA" +
           "CbMlAAAAGv////8BAf////8BAAAAFWCJCgIAAAAAAAcAAABFVVJhbmdlAQG2JQAuAES2JQAAAQB0A///" +
           "//8BAf////8AAAAAFWCJCgIAAAABAAwAAABJbnRlZ2VyVmFsdWUBAbklAC8BAEAJuSUAAAAb/////wEB" +
           "/////wEAAAAVYIkKAgAAAAAABwAAAEVVUmFuZ2UBAbwlAC4ARLwlAAABAHQD/////wEB/////wAAAAAV" +
           "YIkKAgAAAAEADQAAAFVJbnRlZ2VyVmFsdWUBAb8lAC8BAEAJvyUAAAAc/////wEB/////wEAAAAVYIkK" +
           "AgAAAAAABwAAAEVVUmFuZ2UBAcIlAC4ARMIlAAABAHQD/////wEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the SByteValue Variable.
        /// </summary>
        public AnalogItemState<sbyte> SByteValue {
            get => m_sByteValue;

            set {
                if (!ReferenceEquals(m_sByteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_sByteValue = value;
            }
        }

        /// <summary>
        /// A description for the ByteValue Variable.
        /// </summary>
        public AnalogItemState<byte> ByteValue {
            get => m_byteValue;

            set {
                if (!ReferenceEquals(m_byteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_byteValue = value;
            }
        }

        /// <summary>
        /// A description for the Int16Value Variable.
        /// </summary>
        public AnalogItemState<short> Int16Value {
            get => m_int16Value;

            set {
                if (!ReferenceEquals(m_int16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int16Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt16Value Variable.
        /// </summary>
        public AnalogItemState<ushort> UInt16Value {
            get => m_uInt16Value;

            set {
                if (!ReferenceEquals(m_uInt16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt16Value = value;
            }
        }

        /// <summary>
        /// A description for the Int32Value Variable.
        /// </summary>
        public AnalogItemState<int> Int32Value {
            get => m_int32Value;

            set {
                if (!ReferenceEquals(m_int32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int32Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt32Value Variable.
        /// </summary>
        public AnalogItemState<uint> UInt32Value {
            get => m_uInt32Value;

            set {
                if (!ReferenceEquals(m_uInt32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt32Value = value;
            }
        }

        /// <summary>
        /// A description for the Int64Value Variable.
        /// </summary>
        public AnalogItemState<long> Int64Value {
            get => m_int64Value;

            set {
                if (!ReferenceEquals(m_int64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int64Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt64Value Variable.
        /// </summary>
        public AnalogItemState<ulong> UInt64Value {
            get => m_uInt64Value;

            set {
                if (!ReferenceEquals(m_uInt64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt64Value = value;
            }
        }

        /// <summary>
        /// A description for the FloatValue Variable.
        /// </summary>
        public AnalogItemState<float> FloatValue {
            get => m_floatValue;

            set {
                if (!ReferenceEquals(m_floatValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_floatValue = value;
            }
        }

        /// <summary>
        /// A description for the DoubleValue Variable.
        /// </summary>
        public AnalogItemState<double> DoubleValue {
            get => m_doubleValue;

            set {
                if (!ReferenceEquals(m_doubleValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_doubleValue = value;
            }
        }

        /// <summary>
        /// A description for the NumberValue Variable.
        /// </summary>
        public AnalogItemState NumberValue {
            get => m_numberValue;

            set {
                if (!ReferenceEquals(m_numberValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_numberValue = value;
            }
        }

        /// <summary>
        /// A description for the IntegerValue Variable.
        /// </summary>
        public AnalogItemState IntegerValue {
            get => m_integerValue;

            set {
                if (!ReferenceEquals(m_integerValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_integerValue = value;
            }
        }

        /// <summary>
        /// A description for the UIntegerValue Variable.
        /// </summary>
        public AnalogItemState UIntegerValue {
            get => m_uIntegerValue;

            set {
                if (!ReferenceEquals(m_uIntegerValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uIntegerValue = value;
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
            if (m_sByteValue != null) {
                children.Add(m_sByteValue);
            }

            if (m_byteValue != null) {
                children.Add(m_byteValue);
            }

            if (m_int16Value != null) {
                children.Add(m_int16Value);
            }

            if (m_uInt16Value != null) {
                children.Add(m_uInt16Value);
            }

            if (m_int32Value != null) {
                children.Add(m_int32Value);
            }

            if (m_uInt32Value != null) {
                children.Add(m_uInt32Value);
            }

            if (m_int64Value != null) {
                children.Add(m_int64Value);
            }

            if (m_uInt64Value != null) {
                children.Add(m_uInt64Value);
            }

            if (m_floatValue != null) {
                children.Add(m_floatValue);
            }

            if (m_doubleValue != null) {
                children.Add(m_doubleValue);
            }

            if (m_numberValue != null) {
                children.Add(m_numberValue);
            }

            if (m_integerValue != null) {
                children.Add(m_integerValue);
            }

            if (m_uIntegerValue != null) {
                children.Add(m_uIntegerValue);
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
                case BrowseNames.SByteValue: {
                        if (createOrReplace) {
                            if (SByteValue == null) {
                                if (replacement == null) {
                                    SByteValue = new AnalogItemState<sbyte>(this);
                                }
                                else {
                                    SByteValue = (AnalogItemState<sbyte>)replacement;
                                }
                            }
                        }

                        instance = SByteValue;
                        break;
                    }

                case BrowseNames.ByteValue: {
                        if (createOrReplace) {
                            if (ByteValue == null) {
                                if (replacement == null) {
                                    ByteValue = new AnalogItemState<byte>(this);
                                }
                                else {
                                    ByteValue = (AnalogItemState<byte>)replacement;
                                }
                            }
                        }

                        instance = ByteValue;
                        break;
                    }

                case BrowseNames.Int16Value: {
                        if (createOrReplace) {
                            if (Int16Value == null) {
                                if (replacement == null) {
                                    Int16Value = new AnalogItemState<short>(this);
                                }
                                else {
                                    Int16Value = (AnalogItemState<short>)replacement;
                                }
                            }
                        }

                        instance = Int16Value;
                        break;
                    }

                case BrowseNames.UInt16Value: {
                        if (createOrReplace) {
                            if (UInt16Value == null) {
                                if (replacement == null) {
                                    UInt16Value = new AnalogItemState<ushort>(this);
                                }
                                else {
                                    UInt16Value = (AnalogItemState<ushort>)replacement;
                                }
                            }
                        }

                        instance = UInt16Value;
                        break;
                    }

                case BrowseNames.Int32Value: {
                        if (createOrReplace) {
                            if (Int32Value == null) {
                                if (replacement == null) {
                                    Int32Value = new AnalogItemState<int>(this);
                                }
                                else {
                                    Int32Value = (AnalogItemState<int>)replacement;
                                }
                            }
                        }

                        instance = Int32Value;
                        break;
                    }

                case BrowseNames.UInt32Value: {
                        if (createOrReplace) {
                            if (UInt32Value == null) {
                                if (replacement == null) {
                                    UInt32Value = new AnalogItemState<uint>(this);
                                }
                                else {
                                    UInt32Value = (AnalogItemState<uint>)replacement;
                                }
                            }
                        }

                        instance = UInt32Value;
                        break;
                    }

                case BrowseNames.Int64Value: {
                        if (createOrReplace) {
                            if (Int64Value == null) {
                                if (replacement == null) {
                                    Int64Value = new AnalogItemState<long>(this);
                                }
                                else {
                                    Int64Value = (AnalogItemState<long>)replacement;
                                }
                            }
                        }

                        instance = Int64Value;
                        break;
                    }

                case BrowseNames.UInt64Value: {
                        if (createOrReplace) {
                            if (UInt64Value == null) {
                                if (replacement == null) {
                                    UInt64Value = new AnalogItemState<ulong>(this);
                                }
                                else {
                                    UInt64Value = (AnalogItemState<ulong>)replacement;
                                }
                            }
                        }

                        instance = UInt64Value;
                        break;
                    }

                case BrowseNames.FloatValue: {
                        if (createOrReplace) {
                            if (FloatValue == null) {
                                if (replacement == null) {
                                    FloatValue = new AnalogItemState<float>(this);
                                }
                                else {
                                    FloatValue = (AnalogItemState<float>)replacement;
                                }
                            }
                        }

                        instance = FloatValue;
                        break;
                    }

                case BrowseNames.DoubleValue: {
                        if (createOrReplace) {
                            if (DoubleValue == null) {
                                if (replacement == null) {
                                    DoubleValue = new AnalogItemState<double>(this);
                                }
                                else {
                                    DoubleValue = (AnalogItemState<double>)replacement;
                                }
                            }
                        }

                        instance = DoubleValue;
                        break;
                    }

                case BrowseNames.NumberValue: {
                        if (createOrReplace) {
                            if (NumberValue == null) {
                                if (replacement == null) {
                                    NumberValue = new AnalogItemState(this);
                                }
                                else {
                                    NumberValue = (AnalogItemState)replacement;
                                }
                            }
                        }

                        instance = NumberValue;
                        break;
                    }

                case BrowseNames.IntegerValue: {
                        if (createOrReplace) {
                            if (IntegerValue == null) {
                                if (replacement == null) {
                                    IntegerValue = new AnalogItemState(this);
                                }
                                else {
                                    IntegerValue = (AnalogItemState)replacement;
                                }
                            }
                        }

                        instance = IntegerValue;
                        break;
                    }

                case BrowseNames.UIntegerValue: {
                        if (createOrReplace) {
                            if (UIntegerValue == null) {
                                if (replacement == null) {
                                    UIntegerValue = new AnalogItemState(this);
                                }
                                else {
                                    UIntegerValue = (AnalogItemState)replacement;
                                }
                            }
                        }

                        instance = UIntegerValue;
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
        private AnalogItemState<sbyte> m_sByteValue;
        private AnalogItemState<byte> m_byteValue;
        private AnalogItemState<short> m_int16Value;
        private AnalogItemState<ushort> m_uInt16Value;
        private AnalogItemState<int> m_int32Value;
        private AnalogItemState<uint> m_uInt32Value;
        private AnalogItemState<long> m_int64Value;
        private AnalogItemState<ulong> m_uInt64Value;
        private AnalogItemState<float> m_floatValue;
        private AnalogItemState<double> m_doubleValue;
        private AnalogItemState m_numberValue;
        private AnalogItemState m_integerValue;
        private AnalogItemState m_uIntegerValue;
        #endregion
    }
#endif
    #endregion

    #region ArrayValue1MethodState Class
#if (!OPCUA_EXCLUDE_ArrayValue1MethodState)
    /// <summary>
    /// Stores an instance of the ArrayValue1MethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ArrayValue1MethodState : MethodState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public ArrayValue1MethodState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent) {
            return new ArrayValue1MethodState(parent);
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
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGGCCgQAAAABABUAAABBcnJheVZhbHVl" +
           "MU1ldGhvZFR5cGUBAcYlAC8BAcYlxiUAAAEB/////wIAAAAVYKkKAgAAAAAADgAAAElucHV0QXJndW1l" +
           "bnRzAQHHJQAuAETHJQAAlgsAAAABACoBARgAAAAJAAAAQm9vbGVhbkluAAEBAAAAAAAAAAABACoBARYA" +
           "AAAHAAAAU0J5dGVJbgACAQAAAAAAAAAAAQAqAQEVAAAABgAAAEJ5dGVJbgADAQAAAAAAAAAAAQAqAQEW" +
           "AAAABwAAAEludDE2SW4ABAEAAAAAAAAAAAEAKgEBFwAAAAgAAABVSW50MTZJbgAFAQAAAAAAAAAAAQAq" +
           "AQEWAAAABwAAAEludDMySW4ABgEAAAAAAAAAAAEAKgEBFwAAAAgAAABVSW50MzJJbgAHAQAAAAAAAAAA" +
           "AQAqAQEWAAAABwAAAEludDY0SW4ACAEAAAAAAAAAAAEAKgEBFwAAAAgAAABVSW50NjRJbgAJAQAAAAAA" +
           "AAAAAQAqAQEWAAAABwAAAEZsb2F0SW4ACgEAAAAAAAAAAAEAKgEBFwAAAAgAAABEb3VibGVJbgALAQAA" +
           "AAAAAAAAAQAoAQEAAAABAf////8AAAAAFWCpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAcglAC4A" +
           "RMglAACWCwAAAAEAKgEBGQAAAAoAAABCb29sZWFuT3V0AAEBAAAAAAAAAAABACoBARcAAAAIAAAAU0J5" +
           "dGVPdXQAAgEAAAAAAAAAAAEAKgEBFgAAAAcAAABCeXRlT3V0AAMBAAAAAAAAAAABACoBARcAAAAIAAAA" +
           "SW50MTZPdXQABAEAAAAAAAAAAAEAKgEBGAAAAAkAAABVSW50MTZPdXQABQEAAAAAAAAAAAEAKgEBFwAA" +
           "AAgAAABJbnQzMk91dAAGAQAAAAAAAAAAAQAqAQEYAAAACQAAAFVJbnQzMk91dAAHAQAAAAAAAAAAAQAq" +
           "AQEXAAAACAAAAEludDY0T3V0AAgBAAAAAAAAAAABACoBARgAAAAJAAAAVUludDY0T3V0AAkBAAAAAAAA" +
           "AAABACoBARcAAAAIAAAARmxvYXRPdXQACgEAAAAAAAAAAAEAKgEBGAAAAAkAAABEb3VibGVPdXQACwEA" +
           "AAAAAAAAAAEAKAEBAAAAAQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public ArrayValue1MethodStateMethodCallHandler OnCall;
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
            IList<object> outputArguments) {
            if (OnCall == null) {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            bool[] booleanIn = (bool[])inputArguments[0];
            sbyte[] sByteIn = (sbyte[])inputArguments[1];
            byte[] byteIn = (byte[])inputArguments[2];
            short[] int16In = (short[])inputArguments[3];
            ushort[] uInt16In = (ushort[])inputArguments[4];
            int[] int32In = (int[])inputArguments[5];
            uint[] uInt32In = (uint[])inputArguments[6];
            long[] int64In = (long[])inputArguments[7];
            ulong[] uInt64In = (ulong[])inputArguments[8];
            float[] floatIn = (float[])inputArguments[9];
            double[] doubleIn = (double[])inputArguments[10];

            bool[] booleanOut = (bool[])outputArguments[0];
            sbyte[] sByteOut = (sbyte[])outputArguments[1];
            byte[] byteOut = (byte[])outputArguments[2];
            short[] int16Out = (short[])outputArguments[3];
            ushort[] uInt16Out = (ushort[])outputArguments[4];
            int[] int32Out = (int[])outputArguments[5];
            uint[] uInt32Out = (uint[])outputArguments[6];
            long[] int64Out = (long[])outputArguments[7];
            ulong[] uInt64Out = (ulong[])outputArguments[8];
            float[] floatOut = (float[])outputArguments[9];
            double[] doubleOut = (double[])outputArguments[10];

            if (OnCall != null) {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    booleanIn,
                    sByteIn,
                    byteIn,
                    int16In,
                    uInt16In,
                    int32In,
                    uInt32In,
                    int64In,
                    uInt64In,
                    floatIn,
                    doubleIn,
                    ref booleanOut,
                    ref sByteOut,
                    ref byteOut,
                    ref int16Out,
                    ref uInt16Out,
                    ref int32Out,
                    ref uInt32Out,
                    ref int64Out,
                    ref uInt64Out,
                    ref floatOut,
                    ref doubleOut);
            }

            outputArguments[0] = booleanOut;
            outputArguments[1] = sByteOut;
            outputArguments[2] = byteOut;
            outputArguments[3] = int16Out;
            outputArguments[4] = uInt16Out;
            outputArguments[5] = int32Out;
            outputArguments[6] = uInt32Out;
            outputArguments[7] = int64Out;
            outputArguments[8] = uInt64Out;
            outputArguments[9] = floatOut;
            outputArguments[10] = doubleOut;

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
    public delegate ServiceResult ArrayValue1MethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        bool[] booleanIn,
        sbyte[] sByteIn,
        byte[] byteIn,
        short[] int16In,
        ushort[] uInt16In,
        int[] int32In,
        uint[] uInt32In,
        long[] int64In,
        ulong[] uInt64In,
        float[] floatIn,
        double[] doubleIn,
        ref bool[] booleanOut,
        ref sbyte[] sByteOut,
        ref byte[] byteOut,
        ref short[] int16Out,
        ref ushort[] uInt16Out,
        ref int[] int32Out,
        ref uint[] uInt32Out,
        ref long[] int64Out,
        ref ulong[] uInt64Out,
        ref float[] floatOut,
        ref double[] doubleOut);
#endif
    #endregion

    #region ArrayValue2MethodState Class
#if (!OPCUA_EXCLUDE_ArrayValue2MethodState)
    /// <summary>
    /// Stores an instance of the ArrayValue2MethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ArrayValue2MethodState : MethodState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public ArrayValue2MethodState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent) {
            return new ArrayValue2MethodState(parent);
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
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGGCCgQAAAABABUAAABBcnJheVZhbHVl" +
           "Mk1ldGhvZFR5cGUBAcklAC8BAcklySUAAAEB/////wIAAAAVYKkKAgAAAAAADgAAAElucHV0QXJndW1l" +
           "bnRzAQHKJQAuAETKJQAAlgoAAAABACoBARcAAAAIAAAAU3RyaW5nSW4ADAEAAAAAAAAAAAEAKgEBGQAA" +
           "AAoAAABEYXRlVGltZUluAA0BAAAAAAAAAAABACoBARUAAAAGAAAAR3VpZEluAA4BAAAAAAAAAAABACoB" +
           "ARsAAAAMAAAAQnl0ZVN0cmluZ0luAA8BAAAAAAAAAAABACoBARsAAAAMAAAAWG1sRWxlbWVudEluABAB" +
           "AAAAAAAAAAABACoBARcAAAAIAAAATm9kZUlkSW4AEQEAAAAAAAAAAAEAKgEBHwAAABAAAABFeHBhbmRl" +
           "ZE5vZGVJZEluABIBAAAAAAAAAAABACoBAR4AAAAPAAAAUXVhbGlmaWVkTmFtZUluABQBAAAAAAAAAAAB" +
           "ACoBAR4AAAAPAAAATG9jYWxpemVkVGV4dEluABUBAAAAAAAAAAABACoBARsAAAAMAAAAU3RhdHVzQ29k" +
           "ZUluABMBAAAAAAAAAAABACgBAQAAAAEB/////wAAAAAVYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50" +
           "cwEByyUALgBEyyUAAJYKAAAAAQAqAQEYAAAACQAAAFN0cmluZ091dAAMAQAAAAAAAAAAAQAqAQEaAAAA" +
           "CwAAAERhdGVUaW1lT3V0AA0BAAAAAAAAAAABACoBARYAAAAHAAAAR3VpZE91dAAOAQAAAAAAAAAAAQAq" +
           "AQEcAAAADQAAAEJ5dGVTdHJpbmdPdXQADwEAAAAAAAAAAAEAKgEBHAAAAA0AAABYbWxFbGVtZW50T3V0" +
           "ABABAAAAAAAAAAABACoBARgAAAAJAAAATm9kZUlkT3V0ABEBAAAAAAAAAAABACoBASAAAAARAAAARXhw" +
           "YW5kZWROb2RlSWRPdXQAEgEAAAAAAAAAAAEAKgEBHwAAABAAAABRdWFsaWZpZWROYW1lT3V0ABQBAAAA" +
           "AAAAAAABACoBAR8AAAAQAAAATG9jYWxpemVkVGV4dE91dAAVAQAAAAAAAAAAAQAqAQEcAAAADQAAAFN0" +
           "YXR1c0NvZGVPdXQAEwEAAAAAAAAAAAEAKAEBAAAAAQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public ArrayValue2MethodStateMethodCallHandler OnCall;
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
            IList<object> outputArguments) {
            if (OnCall == null) {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            string[] stringIn = (string[])inputArguments[0];
            DateTime[] dateTimeIn = (DateTime[])inputArguments[1];
            Uuid[] guidIn = (Uuid[])inputArguments[2];
            byte[][] byteStringIn = (byte[][])inputArguments[3];
            XmlElement[] xmlElementIn = (XmlElement[])inputArguments[4];
            NodeId[] nodeIdIn = (NodeId[])inputArguments[5];
            ExpandedNodeId[] expandedNodeIdIn = (ExpandedNodeId[])inputArguments[6];
            QualifiedName[] qualifiedNameIn = (QualifiedName[])inputArguments[7];
            LocalizedText[] localizedTextIn = (LocalizedText[])inputArguments[8];
            StatusCode[] statusCodeIn = (StatusCode[])inputArguments[9];

            string[] stringOut = (string[])outputArguments[0];
            DateTime[] dateTimeOut = (DateTime[])outputArguments[1];
            Uuid[] guidOut = (Uuid[])outputArguments[2];
            byte[][] byteStringOut = (byte[][])outputArguments[3];
            XmlElement[] xmlElementOut = (XmlElement[])outputArguments[4];
            NodeId[] nodeIdOut = (NodeId[])outputArguments[5];
            ExpandedNodeId[] expandedNodeIdOut = (ExpandedNodeId[])outputArguments[6];
            QualifiedName[] qualifiedNameOut = (QualifiedName[])outputArguments[7];
            LocalizedText[] localizedTextOut = (LocalizedText[])outputArguments[8];
            StatusCode[] statusCodeOut = (StatusCode[])outputArguments[9];

            if (OnCall != null) {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    stringIn,
                    dateTimeIn,
                    guidIn,
                    byteStringIn,
                    xmlElementIn,
                    nodeIdIn,
                    expandedNodeIdIn,
                    qualifiedNameIn,
                    localizedTextIn,
                    statusCodeIn,
                    ref stringOut,
                    ref dateTimeOut,
                    ref guidOut,
                    ref byteStringOut,
                    ref xmlElementOut,
                    ref nodeIdOut,
                    ref expandedNodeIdOut,
                    ref qualifiedNameOut,
                    ref localizedTextOut,
                    ref statusCodeOut);
            }

            outputArguments[0] = stringOut;
            outputArguments[1] = dateTimeOut;
            outputArguments[2] = guidOut;
            outputArguments[3] = byteStringOut;
            outputArguments[4] = xmlElementOut;
            outputArguments[5] = nodeIdOut;
            outputArguments[6] = expandedNodeIdOut;
            outputArguments[7] = qualifiedNameOut;
            outputArguments[8] = localizedTextOut;
            outputArguments[9] = statusCodeOut;

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
    public delegate ServiceResult ArrayValue2MethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        string[] stringIn,
        DateTime[] dateTimeIn,
        Uuid[] guidIn,
        byte[][] byteStringIn,
        XmlElement[] xmlElementIn,
        NodeId[] nodeIdIn,
        ExpandedNodeId[] expandedNodeIdIn,
        QualifiedName[] qualifiedNameIn,
        LocalizedText[] localizedTextIn,
        StatusCode[] statusCodeIn,
        ref string[] stringOut,
        ref DateTime[] dateTimeOut,
        ref Uuid[] guidOut,
        ref byte[][] byteStringOut,
        ref XmlElement[] xmlElementOut,
        ref NodeId[] nodeIdOut,
        ref ExpandedNodeId[] expandedNodeIdOut,
        ref QualifiedName[] qualifiedNameOut,
        ref LocalizedText[] localizedTextOut,
        ref StatusCode[] statusCodeOut);
#endif
    #endregion

    #region ArrayValue3MethodState Class
#if (!OPCUA_EXCLUDE_ArrayValue3MethodState)
    /// <summary>
    /// Stores an instance of the ArrayValue3MethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ArrayValue3MethodState : MethodState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public ArrayValue3MethodState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent) {
            return new ArrayValue3MethodState(parent);
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
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGGCCgQAAAABABUAAABBcnJheVZhbHVl" +
           "M01ldGhvZFR5cGUBAcwlAC8BAcwlzCUAAAEB/////wIAAAAVYKkKAgAAAAAADgAAAElucHV0QXJndW1l" +
           "bnRzAQHNJQAuAETNJQAAlgMAAAABACoBARgAAAAJAAAAVmFyaWFudEluABgBAAAAAAAAAAABACoBARwA" +
           "AAANAAAARW51bWVyYXRpb25JbgAdAQAAAAAAAAAAAQAqAQEaAAAACwAAAFN0cnVjdHVyZUluABYBAAAA" +
           "AAAAAAABACgBAQAAAAEB/////wAAAAAVYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBziUALgBE" +
           "ziUAAJYDAAAAAQAqAQEZAAAACgAAAFZhcmlhbnRPdXQAGAEAAAAAAAAAAAEAKgEBHQAAAA4AAABFbnVt" +
           "ZXJhdGlvbk91dAAdAQAAAAAAAAAAAQAqAQEbAAAADAAAAFN0cnVjdHVyZU91dAAWAQAAAAAAAAAAAQAo" +
           "AQEAAAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public ArrayValue3MethodStateMethodCallHandler OnCall;
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
            IList<object> outputArguments) {
            if (OnCall == null) {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            Variant[] variantIn = (Variant[])inputArguments[0];
            int[] enumerationIn = (int[])inputArguments[1];
            ExtensionObject[] structureIn = (ExtensionObject[])inputArguments[2];

            Variant[] variantOut = (Variant[])outputArguments[0];
            int[] enumerationOut = (int[])outputArguments[1];
            ExtensionObject[] structureOut = (ExtensionObject[])outputArguments[2];

            if (OnCall != null) {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    variantIn,
                    enumerationIn,
                    structureIn,
                    ref variantOut,
                    ref enumerationOut,
                    ref structureOut);
            }

            outputArguments[0] = variantOut;
            outputArguments[1] = enumerationOut;
            outputArguments[2] = structureOut;

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
    public delegate ServiceResult ArrayValue3MethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        Variant[] variantIn,
        int[] enumerationIn,
        ExtensionObject[] structureIn,
        ref Variant[] variantOut,
        ref int[] enumerationOut,
        ref ExtensionObject[] structureOut);
#endif
    #endregion

    #region ArrayValueObjectState Class
#if !OPCUA_EXCLUDE_ArrayValueObjectState
    /// <summary>
    /// Stores an instance of the ArrayValueObjectType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ArrayValueObjectState : TestDataObjectState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public ArrayValueObjectState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return NodeId.Create(ObjectTypes.ArrayValueObjectType, Namespaces.TestData, namespaceUris);
        }

#if !OPCUA_EXCLUDE_InitializationStrings
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
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGCAAAEAAAABABwAAABBcnJheVZhbHVl" +
           "T2JqZWN0VHlwZUluc3RhbmNlAQHPJQEBzyUBAAAAACQAAQHTJR4AAAA1YIkKAgAAAAEAEAAAAFNpbXVs" +
           "YXRpb25BY3RpdmUBAdAlAwAAAABHAAAASWYgdHJ1ZSB0aGUgc2VydmVyIHdpbGwgcHJvZHVjZSBuZXcg" +
           "dmFsdWVzIGZvciBlYWNoIG1vbml0b3JlZCB2YXJpYWJsZS4ALgBE0CUAAAAB/////wEB/////wAAAAAE" +
           "YYIKBAAAAAEADgAAAEdlbmVyYXRlVmFsdWVzAQHRJQAvAQGpJNElAAABAf////8BAAAAFWCpCgIAAAAA" +
           "AA4AAABJbnB1dEFyZ3VtZW50cwEB0iUALgBE0iUAAJYBAAAAAQAqAQFGAAAACgAAAEl0ZXJhdGlvbnMA" +
           "B/////8AAAAAAwAAAAAlAAAAVGhlIG51bWJlciBvZiBuZXcgdmFsdWVzIHRvIGdlbmVyYXRlLgEAKAEB" +
           "AAAAAQH/////AAAAAARggAoBAAAAAQANAAAAQ3ljbGVDb21wbGV0ZQEB0yUALwEAQQvTJQAAAQAAAAAk" +
           "AQEBzyUXAAAANWCJCgIAAAAAAAcAAABFdmVudElkAQHUJQMAAAAAKwAAAEEgZ2xvYmFsbHkgdW5pcXVl" +
           "IGlkZW50aWZpZXIgZm9yIHRoZSBldmVudC4ALgBE1CUAAAAP/////wEB/////wAAAAA1YIkKAgAAAAAA" +
           "CQAAAEV2ZW50VHlwZQEB1SUDAAAAACIAAABUaGUgaWRlbnRpZmllciBmb3IgdGhlIGV2ZW50IHR5cGUu" +
           "AC4ARNUlAAAAEf////8BAf////8AAAAANWCJCgIAAAAAAAoAAABTb3VyY2VOb2RlAQHWJQMAAAAAGAAA" +
           "AFRoZSBzb3VyY2Ugb2YgdGhlIGV2ZW50LgAuAETWJQAAABH/////AQH/////AAAAADVgiQoCAAAAAAAK" +
           "AAAAU291cmNlTmFtZQEB1yUDAAAAACkAAABBIGRlc2NyaXB0aW9uIG9mIHRoZSBzb3VyY2Ugb2YgdGhl" +
           "IGV2ZW50LgAuAETXJQAAAAz/////AQH/////AAAAADVgiQoCAAAAAAAEAAAAVGltZQEB2CUDAAAAABgA" +
           "AABXaGVuIHRoZSBldmVudCBvY2N1cnJlZC4ALgBE2CUAAAEAJgH/////AQH/////AAAAADVgiQoCAAAA" +
           "AAALAAAAUmVjZWl2ZVRpbWUBAdklAwAAAAA+AAAAV2hlbiB0aGUgc2VydmVyIHJlY2VpdmVkIHRoZSBl" +
           "dmVudCBmcm9tIHRoZSB1bmRlcmx5aW5nIHN5c3RlbS4ALgBE2SUAAAEAJgH/////AQH/////AAAAADVg" +
           "iQoCAAAAAAAHAAAATWVzc2FnZQEB2yUDAAAAACUAAABBIGxvY2FsaXplZCBkZXNjcmlwdGlvbiBvZiB0" +
           "aGUgZXZlbnQuAC4ARNslAAAAFf////8BAf////8AAAAANWCJCgIAAAAAAAgAAABTZXZlcml0eQEB3CUD" +
           "AAAAACEAAABJbmRpY2F0ZXMgaG93IHVyZ2VudCBhbiBldmVudCBpcy4ALgBE3CUAAAAF/////wEB////" +
           "/wAAAAAVYIkKAgAAAAAAEAAAAENvbmRpdGlvbkNsYXNzSWQBAUAtAC4AREAtAAAAEf////8BAf////8A" +
           "AAAAFWCJCgIAAAAAABIAAABDb25kaXRpb25DbGFzc05hbWUBAUEtAC4AREEtAAAAFf////8BAf////8A" +
           "AAAAFWCJCgIAAAAAAA0AAABDb25kaXRpb25OYW1lAQEoLQAuAEQoLQAAAAz/////AQH/////AAAAABVg" +
           "iQoCAAAAAAAIAAAAQnJhbmNoSWQBAd0lAC4ARN0lAAAAEf////8BAf////8AAAAAFWCJCgIAAAAAAAYA" +
           "AABSZXRhaW4BAd4lAC4ARN4lAAAAAf////8BAf////8AAAAAFWCJCgIAAAAAAAwAAABFbmFibGVkU3Rh" +
           "dGUBAd8lAC8BACMj3yUAAAAV/////wEBAgAAAAEALCMAAQH0JQEALCMAAQH8JQEAAAAVYIkKAgAAAAAA" +
           "AgAAAElkAQHgJQAuAETgJQAAAAH/////AQH/////AAAAABVgiQoCAAAAAAAHAAAAUXVhbGl0eQEB5SUA" +
           "LwEAKiPlJQAAABP/////AQH/////AQAAABVgiQoCAAAAAAAPAAAAU291cmNlVGltZXN0YW1wAQHmJQAu" +
           "AETmJQAAAQAmAf////8BAf////8AAAAAFWCJCgIAAAAAAAwAAABMYXN0U2V2ZXJpdHkBAeklAC8BACoj" +
           "6SUAAAAF/////wEB/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVzdGFtcAEB6iUALgBE6iUA" +
           "AAEAJgH/////AQH/////AAAAABVgiQoCAAAAAAAHAAAAQ29tbWVudAEB6yUALwEAKiPrJQAAABX/////" +
           "AQH/////AQAAABVgiQoCAAAAAAAPAAAAU291cmNlVGltZXN0YW1wAQHsJQAuAETsJQAAAQAmAf////8B" +
           "Af////8AAAAAFWCJCgIAAAAAAAwAAABDbGllbnRVc2VySWQBAe0lAC4ARO0lAAAADP////8BAf////8A" +
           "AAAABGGCCgQAAAAAAAcAAABEaXNhYmxlAQHvJQAvAQBEI+8lAAABAQEAAAABAPkLAAEA8woAAAAABGGC" +
           "CgQAAAAAAAYAAABFbmFibGUBAe4lAC8BAEMj7iUAAAEBAQAAAAEA+QsAAQDzCgAAAAAEYYIKBAAAAAAA" +
           "CgAAAEFkZENvbW1lbnQBAfAlAC8BAEUj8CUAAAEBAQAAAAEA+QsAAQANCwEAAAAVYKkKAgAAAAAADgAA" +
           "AElucHV0QXJndW1lbnRzAQHxJQAuAETxJQAAlgIAAAABACoBAUYAAAAHAAAARXZlbnRJZAAP/////wAA" +
           "AAADAAAAACgAAABUaGUgaWRlbnRpZmllciBmb3IgdGhlIGV2ZW50IHRvIGNvbW1lbnQuAQAqAQFCAAAA" +
           "BwAAAENvbW1lbnQAFf////8AAAAAAwAAAAAkAAAAVGhlIGNvbW1lbnQgdG8gYWRkIHRvIHRoZSBjb25k" +
           "aXRpb24uAQAoAQEAAAABAf////8AAAAAFWCJCgIAAAAAAAoAAABBY2tlZFN0YXRlAQH0JQAvAQAjI/Ql" +
           "AAAAFf////8BAQEAAAABACwjAQEB3yUBAAAAFWCJCgIAAAAAAAIAAABJZAEB9SUALgBE9SUAAAAB////" +
           "/wEB/////wAAAAAEYYIKBAAAAAAACwAAAEFja25vd2xlZGdlAQEEJgAvAQCXIwQmAAABAQEAAAABAPkL" +
           "AAEA8CIBAAAAFWCpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBBSYALgBEBSYAAJYCAAAAAQAqAQFG" +
           "AAAABwAAAEV2ZW50SWQAD/////8AAAAAAwAAAAAoAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVu" +
           "dCB0byBjb21tZW50LgEAKgEBQgAAAAcAAABDb21tZW50ABX/////AAAAAAMAAAAAJAAAAFRoZSBjb21t" +
           "ZW50IHRvIGFkZCB0byB0aGUgY29uZGl0aW9uLgEAKAEBAAAAAQH/////AAAAABVgiQoCAAAAAQAMAAAA" +
           "Qm9vbGVhblZhbHVlAQEIJgAvAD8IJgAAAAEBAAAAAQH/////AAAAABVgiQoCAAAAAQAKAAAAU0J5dGVW" +
           "YWx1ZQEBCSYALwA/CSYAAAACAQAAAAEB/////wAAAAAVYIkKAgAAAAEACQAAAEJ5dGVWYWx1ZQEBCiYA" +
           "LwA/CiYAAAADAQAAAAEB/////wAAAAAVYIkKAgAAAAEACgAAAEludDE2VmFsdWUBAQsmAC8APwsmAAAA" +
           "BAEAAAABAf////8AAAAAFWCJCgIAAAABAAsAAABVSW50MTZWYWx1ZQEBDCYALwA/DCYAAAAFAQAAAAEB" +
           "/////wAAAAAVYIkKAgAAAAEACgAAAEludDMyVmFsdWUBAQ0mAC8APw0mAAAABgEAAAABAf////8AAAAA" +
           "FWCJCgIAAAABAAsAAABVSW50MzJWYWx1ZQEBDiYALwA/DiYAAAAHAQAAAAEB/////wAAAAAVYIkKAgAA" +
           "AAEACgAAAEludDY0VmFsdWUBAQ8mAC8APw8mAAAACAEAAAABAf////8AAAAAFWCJCgIAAAABAAsAAABV" +
           "SW50NjRWYWx1ZQEBECYALwA/ECYAAAAJAQAAAAEB/////wAAAAAVYIkKAgAAAAEACgAAAEZsb2F0VmFs" +
           "dWUBAREmAC8APxEmAAAACgEAAAABAf////8AAAAAFWCJCgIAAAABAAsAAABEb3VibGVWYWx1ZQEBEiYA" +
           "LwA/EiYAAAALAQAAAAEB/////wAAAAAVYIkKAgAAAAEACwAAAFN0cmluZ1ZhbHVlAQETJgAvAD8TJgAA" +
           "AAwBAAAAAQH/////AAAAABVgiQoCAAAAAQANAAAARGF0ZVRpbWVWYWx1ZQEBFCYALwA/FCYAAAANAQAA" +
           "AAEB/////wAAAAAVYIkKAgAAAAEACQAAAEd1aWRWYWx1ZQEBFSYALwA/FSYAAAAOAQAAAAEB/////wAA" +
           "AAAVYIkKAgAAAAEADwAAAEJ5dGVTdHJpbmdWYWx1ZQEBFiYALwA/FiYAAAAPAQAAAAEB/////wAAAAAV" +
           "YIkKAgAAAAEADwAAAFhtbEVsZW1lbnRWYWx1ZQEBFyYALwA/FyYAAAAQAQAAAAEB/////wAAAAAVYIkK" +
           "AgAAAAEACwAAAE5vZGVJZFZhbHVlAQEYJgAvAD8YJgAAABEBAAAAAQH/////AAAAABVgiQoCAAAAAQAT" +
           "AAAARXhwYW5kZWROb2RlSWRWYWx1ZQEBGSYALwA/GSYAAAASAQAAAAEB/////wAAAAAVYIkKAgAAAAEA" +
           "EgAAAFF1YWxpZmllZE5hbWVWYWx1ZQEBGiYALwA/GiYAAAAUAQAAAAEB/////wAAAAAVYIkKAgAAAAEA" +
           "EgAAAExvY2FsaXplZFRleHRWYWx1ZQEBGyYALwA/GyYAAAAVAQAAAAEB/////wAAAAAVYIkKAgAAAAEA" +
           "DwAAAFN0YXR1c0NvZGVWYWx1ZQEBHCYALwA/HCYAAAATAQAAAAEB/////wAAAAAVYIkKAgAAAAEADAAA" +
           "AFZhcmlhbnRWYWx1ZQEBHSYALwA/HSYAAAAYAQAAAAEB/////wAAAAAVYIkKAgAAAAEAEAAAAEVudW1l" +
           "cmF0aW9uVmFsdWUBAR4mAC8APx4mAAAAHQEAAAABAf////8AAAAAFWCJCgIAAAABAA4AAABTdHJ1Y3R1" +
           "cmVWYWx1ZQEBHyYALwA/HyYAAAAWAQAAAAEB/////wAAAAAVYIkKAgAAAAEACwAAAE51bWJlclZhbHVl" +
           "AQEgJgAvAD8gJgAAABoBAAAAAQH/////AAAAABVgiQoCAAAAAQAMAAAASW50ZWdlclZhbHVlAQEhJgAv" +
           "AD8hJgAAABsBAAAAAQH/////AAAAABVgiQoCAAAAAQANAAAAVUludGVnZXJWYWx1ZQEBIiYALwA/IiYA" +
           "AAAcAQAAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the BooleanValue Variable.
        /// </summary>
        public BaseDataVariableState<bool[]> BooleanValue {
            get => m_booleanValue;

            set {
                if (!ReferenceEquals(m_booleanValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_booleanValue = value;
            }
        }

        /// <summary>
        /// A description for the SByteValue Variable.
        /// </summary>
        public BaseDataVariableState<sbyte[]> SByteValue {
            get => m_sByteValue;

            set {
                if (!ReferenceEquals(m_sByteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_sByteValue = value;
            }
        }

        /// <summary>
        /// A description for the ByteValue Variable.
        /// </summary>
        public BaseDataVariableState<byte[]> ByteValue {
            get => m_byteValue;

            set {
                if (!ReferenceEquals(m_byteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_byteValue = value;
            }
        }

        /// <summary>
        /// A description for the Int16Value Variable.
        /// </summary>
        public BaseDataVariableState<short[]> Int16Value {
            get => m_int16Value;

            set {
                if (!ReferenceEquals(m_int16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int16Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt16Value Variable.
        /// </summary>
        public BaseDataVariableState<ushort[]> UInt16Value {
            get => m_uInt16Value;

            set {
                if (!ReferenceEquals(m_uInt16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt16Value = value;
            }
        }

        /// <summary>
        /// A description for the Int32Value Variable.
        /// </summary>
        public BaseDataVariableState<int[]> Int32Value {
            get => m_int32Value;

            set {
                if (!ReferenceEquals(m_int32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int32Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt32Value Variable.
        /// </summary>
        public BaseDataVariableState<uint[]> UInt32Value {
            get => m_uInt32Value;

            set {
                if (!ReferenceEquals(m_uInt32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt32Value = value;
            }
        }

        /// <summary>
        /// A description for the Int64Value Variable.
        /// </summary>
        public BaseDataVariableState<long[]> Int64Value {
            get => m_int64Value;

            set {
                if (!ReferenceEquals(m_int64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int64Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt64Value Variable.
        /// </summary>
        public BaseDataVariableState<ulong[]> UInt64Value {
            get => m_uInt64Value;

            set {
                if (!ReferenceEquals(m_uInt64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt64Value = value;
            }
        }

        /// <summary>
        /// A description for the FloatValue Variable.
        /// </summary>
        public BaseDataVariableState<float[]> FloatValue {
            get => m_floatValue;

            set {
                if (!ReferenceEquals(m_floatValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_floatValue = value;
            }
        }

        /// <summary>
        /// A description for the DoubleValue Variable.
        /// </summary>
        public BaseDataVariableState<double[]> DoubleValue {
            get => m_doubleValue;

            set {
                if (!ReferenceEquals(m_doubleValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_doubleValue = value;
            }
        }

        /// <summary>
        /// A description for the StringValue Variable.
        /// </summary>
        public BaseDataVariableState<string[]> StringValue {
            get => m_stringValue;

            set {
                if (!ReferenceEquals(m_stringValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_stringValue = value;
            }
        }

        /// <summary>
        /// A description for the DateTimeValue Variable.
        /// </summary>
        public BaseDataVariableState<DateTime[]> DateTimeValue {
            get => m_dateTimeValue;

            set {
                if (!ReferenceEquals(m_dateTimeValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_dateTimeValue = value;
            }
        }

        /// <summary>
        /// A description for the GuidValue Variable.
        /// </summary>
        public BaseDataVariableState<Guid[]> GuidValue {
            get => m_guidValue;

            set {
                if (!ReferenceEquals(m_guidValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_guidValue = value;
            }
        }

        /// <summary>
        /// A description for the ByteStringValue Variable.
        /// </summary>
        public BaseDataVariableState<byte[][]> ByteStringValue {
            get => m_byteStringValue;

            set {
                if (!ReferenceEquals(m_byteStringValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_byteStringValue = value;
            }
        }

        /// <summary>
        /// A description for the XmlElementValue Variable.
        /// </summary>
        public BaseDataVariableState<XmlElement[]> XmlElementValue {
            get => m_xmlElementValue;

            set {
                if (!ReferenceEquals(m_xmlElementValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_xmlElementValue = value;
            }
        }

        /// <summary>
        /// A description for the NodeIdValue Variable.
        /// </summary>
        public BaseDataVariableState<NodeId[]> NodeIdValue {
            get => m_nodeIdValue;

            set {
                if (!ReferenceEquals(m_nodeIdValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_nodeIdValue = value;
            }
        }

        /// <summary>
        /// A description for the ExpandedNodeIdValue Variable.
        /// </summary>
        public BaseDataVariableState<ExpandedNodeId[]> ExpandedNodeIdValue {
            get => m_expandedNodeIdValue;

            set {
                if (!ReferenceEquals(m_expandedNodeIdValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_expandedNodeIdValue = value;
            }
        }

        /// <summary>
        /// A description for the QualifiedNameValue Variable.
        /// </summary>
        public BaseDataVariableState<QualifiedName[]> QualifiedNameValue {
            get => m_qualifiedNameValue;

            set {
                if (!ReferenceEquals(m_qualifiedNameValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_qualifiedNameValue = value;
            }
        }

        /// <summary>
        /// A description for the LocalizedTextValue Variable.
        /// </summary>
        public BaseDataVariableState<LocalizedText[]> LocalizedTextValue {
            get => m_localizedTextValue;

            set {
                if (!ReferenceEquals(m_localizedTextValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_localizedTextValue = value;
            }
        }

        /// <summary>
        /// A description for the StatusCodeValue Variable.
        /// </summary>
        public BaseDataVariableState<StatusCode[]> StatusCodeValue {
            get => m_statusCodeValue;

            set {
                if (!ReferenceEquals(m_statusCodeValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_statusCodeValue = value;
            }
        }

        /// <summary>
        /// A description for the VariantValue Variable.
        /// </summary>
        public BaseDataVariableState<object[]> VariantValue {
            get => m_variantValue;

            set {
                if (!ReferenceEquals(m_variantValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_variantValue = value;
            }
        }

        /// <summary>
        /// A description for the EnumerationValue Variable.
        /// </summary>
        public BaseDataVariableState<int[]> EnumerationValue {
            get => m_enumerationValue;

            set {
                if (!ReferenceEquals(m_enumerationValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_enumerationValue = value;
            }
        }

        /// <summary>
        /// A description for the StructureValue Variable.
        /// </summary>
        public BaseDataVariableState<ExtensionObject[]> StructureValue {
            get => m_structureValue;

            set {
                if (!ReferenceEquals(m_structureValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_structureValue = value;
            }
        }

        /// <summary>
        /// A description for the NumberValue Variable.
        /// </summary>
        public BaseDataVariableState<object[]> NumberValue {
            get => m_numberValue;

            set {
                if (!ReferenceEquals(m_numberValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_numberValue = value;
            }
        }

        /// <summary>
        /// A description for the IntegerValue Variable.
        /// </summary>
        public BaseDataVariableState<object[]> IntegerValue {
            get => m_integerValue;

            set {
                if (!ReferenceEquals(m_integerValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_integerValue = value;
            }
        }

        /// <summary>
        /// A description for the UIntegerValue Variable.
        /// </summary>
        public BaseDataVariableState<object[]> UIntegerValue {
            get => m_uIntegerValue;

            set {
                if (!ReferenceEquals(m_uIntegerValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uIntegerValue = value;
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
            if (m_booleanValue != null) {
                children.Add(m_booleanValue);
            }

            if (m_sByteValue != null) {
                children.Add(m_sByteValue);
            }

            if (m_byteValue != null) {
                children.Add(m_byteValue);
            }

            if (m_int16Value != null) {
                children.Add(m_int16Value);
            }

            if (m_uInt16Value != null) {
                children.Add(m_uInt16Value);
            }

            if (m_int32Value != null) {
                children.Add(m_int32Value);
            }

            if (m_uInt32Value != null) {
                children.Add(m_uInt32Value);
            }

            if (m_int64Value != null) {
                children.Add(m_int64Value);
            }

            if (m_uInt64Value != null) {
                children.Add(m_uInt64Value);
            }

            if (m_floatValue != null) {
                children.Add(m_floatValue);
            }

            if (m_doubleValue != null) {
                children.Add(m_doubleValue);
            }

            if (m_stringValue != null) {
                children.Add(m_stringValue);
            }

            if (m_dateTimeValue != null) {
                children.Add(m_dateTimeValue);
            }

            if (m_guidValue != null) {
                children.Add(m_guidValue);
            }

            if (m_byteStringValue != null) {
                children.Add(m_byteStringValue);
            }

            if (m_xmlElementValue != null) {
                children.Add(m_xmlElementValue);
            }

            if (m_nodeIdValue != null) {
                children.Add(m_nodeIdValue);
            }

            if (m_expandedNodeIdValue != null) {
                children.Add(m_expandedNodeIdValue);
            }

            if (m_qualifiedNameValue != null) {
                children.Add(m_qualifiedNameValue);
            }

            if (m_localizedTextValue != null) {
                children.Add(m_localizedTextValue);
            }

            if (m_statusCodeValue != null) {
                children.Add(m_statusCodeValue);
            }

            if (m_variantValue != null) {
                children.Add(m_variantValue);
            }

            if (m_enumerationValue != null) {
                children.Add(m_enumerationValue);
            }

            if (m_structureValue != null) {
                children.Add(m_structureValue);
            }

            if (m_numberValue != null) {
                children.Add(m_numberValue);
            }

            if (m_integerValue != null) {
                children.Add(m_integerValue);
            }

            if (m_uIntegerValue != null) {
                children.Add(m_uIntegerValue);
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
                case BrowseNames.BooleanValue: {
                        if (createOrReplace) {
                            if (BooleanValue == null) {
                                if (replacement == null) {
                                    BooleanValue = new BaseDataVariableState<bool[]>(this);
                                }
                                else {
                                    BooleanValue = (BaseDataVariableState<bool[]>)replacement;
                                }
                            }
                        }

                        instance = BooleanValue;
                        break;
                    }

                case BrowseNames.SByteValue: {
                        if (createOrReplace) {
                            if (SByteValue == null) {
                                if (replacement == null) {
                                    SByteValue = new BaseDataVariableState<sbyte[]>(this);
                                }
                                else {
                                    SByteValue = (BaseDataVariableState<sbyte[]>)replacement;
                                }
                            }
                        }

                        instance = SByteValue;
                        break;
                    }

                case BrowseNames.ByteValue: {
                        if (createOrReplace) {
                            if (ByteValue == null) {
                                if (replacement == null) {
                                    ByteValue = new BaseDataVariableState<byte[]>(this);
                                }
                                else {
                                    ByteValue = (BaseDataVariableState<byte[]>)replacement;
                                }
                            }
                        }

                        instance = ByteValue;
                        break;
                    }

                case BrowseNames.Int16Value: {
                        if (createOrReplace) {
                            if (Int16Value == null) {
                                if (replacement == null) {
                                    Int16Value = new BaseDataVariableState<short[]>(this);
                                }
                                else {
                                    Int16Value = (BaseDataVariableState<short[]>)replacement;
                                }
                            }
                        }

                        instance = Int16Value;
                        break;
                    }

                case BrowseNames.UInt16Value: {
                        if (createOrReplace) {
                            if (UInt16Value == null) {
                                if (replacement == null) {
                                    UInt16Value = new BaseDataVariableState<ushort[]>(this);
                                }
                                else {
                                    UInt16Value = (BaseDataVariableState<ushort[]>)replacement;
                                }
                            }
                        }

                        instance = UInt16Value;
                        break;
                    }

                case BrowseNames.Int32Value: {
                        if (createOrReplace) {
                            if (Int32Value == null) {
                                if (replacement == null) {
                                    Int32Value = new BaseDataVariableState<int[]>(this);
                                }
                                else {
                                    Int32Value = (BaseDataVariableState<int[]>)replacement;
                                }
                            }
                        }

                        instance = Int32Value;
                        break;
                    }

                case BrowseNames.UInt32Value: {
                        if (createOrReplace) {
                            if (UInt32Value == null) {
                                if (replacement == null) {
                                    UInt32Value = new BaseDataVariableState<uint[]>(this);
                                }
                                else {
                                    UInt32Value = (BaseDataVariableState<uint[]>)replacement;
                                }
                            }
                        }

                        instance = UInt32Value;
                        break;
                    }

                case BrowseNames.Int64Value: {
                        if (createOrReplace) {
                            if (Int64Value == null) {
                                if (replacement == null) {
                                    Int64Value = new BaseDataVariableState<long[]>(this);
                                }
                                else {
                                    Int64Value = (BaseDataVariableState<long[]>)replacement;
                                }
                            }
                        }

                        instance = Int64Value;
                        break;
                    }

                case BrowseNames.UInt64Value: {
                        if (createOrReplace) {
                            if (UInt64Value == null) {
                                if (replacement == null) {
                                    UInt64Value = new BaseDataVariableState<ulong[]>(this);
                                }
                                else {
                                    UInt64Value = (BaseDataVariableState<ulong[]>)replacement;
                                }
                            }
                        }

                        instance = UInt64Value;
                        break;
                    }

                case BrowseNames.FloatValue: {
                        if (createOrReplace) {
                            if (FloatValue == null) {
                                if (replacement == null) {
                                    FloatValue = new BaseDataVariableState<float[]>(this);
                                }
                                else {
                                    FloatValue = (BaseDataVariableState<float[]>)replacement;
                                }
                            }
                        }

                        instance = FloatValue;
                        break;
                    }

                case BrowseNames.DoubleValue: {
                        if (createOrReplace) {
                            if (DoubleValue == null) {
                                if (replacement == null) {
                                    DoubleValue = new BaseDataVariableState<double[]>(this);
                                }
                                else {
                                    DoubleValue = (BaseDataVariableState<double[]>)replacement;
                                }
                            }
                        }

                        instance = DoubleValue;
                        break;
                    }

                case BrowseNames.StringValue: {
                        if (createOrReplace) {
                            if (StringValue == null) {
                                if (replacement == null) {
                                    StringValue = new BaseDataVariableState<string[]>(this);
                                }
                                else {
                                    StringValue = (BaseDataVariableState<string[]>)replacement;
                                }
                            }
                        }

                        instance = StringValue;
                        break;
                    }

                case BrowseNames.DateTimeValue: {
                        if (createOrReplace) {
                            if (DateTimeValue == null) {
                                if (replacement == null) {
                                    DateTimeValue = new BaseDataVariableState<DateTime[]>(this);
                                }
                                else {
                                    DateTimeValue = (BaseDataVariableState<DateTime[]>)replacement;
                                }
                            }
                        }

                        instance = DateTimeValue;
                        break;
                    }

                case BrowseNames.GuidValue: {
                        if (createOrReplace) {
                            if (GuidValue == null) {
                                if (replacement == null) {
                                    GuidValue = new BaseDataVariableState<Guid[]>(this);
                                }
                                else {
                                    GuidValue = (BaseDataVariableState<Guid[]>)replacement;
                                }
                            }
                        }

                        instance = GuidValue;
                        break;
                    }

                case BrowseNames.ByteStringValue: {
                        if (createOrReplace) {
                            if (ByteStringValue == null) {
                                if (replacement == null) {
                                    ByteStringValue = new BaseDataVariableState<byte[][]>(this);
                                }
                                else {
                                    ByteStringValue = (BaseDataVariableState<byte[][]>)replacement;
                                }
                            }
                        }

                        instance = ByteStringValue;
                        break;
                    }

                case BrowseNames.XmlElementValue: {
                        if (createOrReplace) {
                            if (XmlElementValue == null) {
                                if (replacement == null) {
                                    XmlElementValue = new BaseDataVariableState<XmlElement[]>(this);
                                }
                                else {
                                    XmlElementValue = (BaseDataVariableState<XmlElement[]>)replacement;
                                }
                            }
                        }

                        instance = XmlElementValue;
                        break;
                    }

                case BrowseNames.NodeIdValue: {
                        if (createOrReplace) {
                            if (NodeIdValue == null) {
                                if (replacement == null) {
                                    NodeIdValue = new BaseDataVariableState<NodeId[]>(this);
                                }
                                else {
                                    NodeIdValue = (BaseDataVariableState<NodeId[]>)replacement;
                                }
                            }
                        }

                        instance = NodeIdValue;
                        break;
                    }

                case BrowseNames.ExpandedNodeIdValue: {
                        if (createOrReplace) {
                            if (ExpandedNodeIdValue == null) {
                                if (replacement == null) {
                                    ExpandedNodeIdValue = new BaseDataVariableState<ExpandedNodeId[]>(this);
                                }
                                else {
                                    ExpandedNodeIdValue = (BaseDataVariableState<ExpandedNodeId[]>)replacement;
                                }
                            }
                        }

                        instance = ExpandedNodeIdValue;
                        break;
                    }

                case BrowseNames.QualifiedNameValue: {
                        if (createOrReplace) {
                            if (QualifiedNameValue == null) {
                                if (replacement == null) {
                                    QualifiedNameValue = new BaseDataVariableState<QualifiedName[]>(this);
                                }
                                else {
                                    QualifiedNameValue = (BaseDataVariableState<QualifiedName[]>)replacement;
                                }
                            }
                        }

                        instance = QualifiedNameValue;
                        break;
                    }

                case BrowseNames.LocalizedTextValue: {
                        if (createOrReplace) {
                            if (LocalizedTextValue == null) {
                                if (replacement == null) {
                                    LocalizedTextValue = new BaseDataVariableState<LocalizedText[]>(this);
                                }
                                else {
                                    LocalizedTextValue = (BaseDataVariableState<LocalizedText[]>)replacement;
                                }
                            }
                        }

                        instance = LocalizedTextValue;
                        break;
                    }

                case BrowseNames.StatusCodeValue: {
                        if (createOrReplace) {
                            if (StatusCodeValue == null) {
                                if (replacement == null) {
                                    StatusCodeValue = new BaseDataVariableState<StatusCode[]>(this);
                                }
                                else {
                                    StatusCodeValue = (BaseDataVariableState<StatusCode[]>)replacement;
                                }
                            }
                        }

                        instance = StatusCodeValue;
                        break;
                    }

                case BrowseNames.VariantValue: {
                        if (createOrReplace) {
                            if (VariantValue == null) {
                                if (replacement == null) {
                                    VariantValue = new BaseDataVariableState<object[]>(this);
                                }
                                else {
                                    VariantValue = (BaseDataVariableState<object[]>)replacement;
                                }
                            }
                        }

                        instance = VariantValue;
                        break;
                    }

                case BrowseNames.EnumerationValue: {
                        if (createOrReplace) {
                            if (EnumerationValue == null) {
                                if (replacement == null) {
                                    EnumerationValue = new BaseDataVariableState<int[]>(this);
                                }
                                else {
                                    EnumerationValue = (BaseDataVariableState<int[]>)replacement;
                                }
                            }
                        }

                        instance = EnumerationValue;
                        break;
                    }

                case BrowseNames.StructureValue: {
                        if (createOrReplace) {
                            if (StructureValue == null) {
                                if (replacement == null) {
                                    StructureValue = new BaseDataVariableState<ExtensionObject[]>(this);
                                }
                                else {
                                    StructureValue = (BaseDataVariableState<ExtensionObject[]>)replacement;
                                }
                            }
                        }

                        instance = StructureValue;
                        break;
                    }

                case BrowseNames.NumberValue: {
                        if (createOrReplace) {
                            if (NumberValue == null) {
                                if (replacement == null) {
                                    NumberValue = new BaseDataVariableState<object[]>(this);
                                }
                                else {
                                    NumberValue = (BaseDataVariableState<object[]>)replacement;
                                }
                            }
                        }

                        instance = NumberValue;
                        break;
                    }

                case BrowseNames.IntegerValue: {
                        if (createOrReplace) {
                            if (IntegerValue == null) {
                                if (replacement == null) {
                                    IntegerValue = new BaseDataVariableState<object[]>(this);
                                }
                                else {
                                    IntegerValue = (BaseDataVariableState<object[]>)replacement;
                                }
                            }
                        }

                        instance = IntegerValue;
                        break;
                    }

                case BrowseNames.UIntegerValue: {
                        if (createOrReplace) {
                            if (UIntegerValue == null) {
                                if (replacement == null) {
                                    UIntegerValue = new BaseDataVariableState<object[]>(this);
                                }
                                else {
                                    UIntegerValue = (BaseDataVariableState<object[]>)replacement;
                                }
                            }
                        }

                        instance = UIntegerValue;
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
        private BaseDataVariableState<bool[]> m_booleanValue;
        private BaseDataVariableState<sbyte[]> m_sByteValue;
        private BaseDataVariableState<byte[]> m_byteValue;
        private BaseDataVariableState<short[]> m_int16Value;
        private BaseDataVariableState<ushort[]> m_uInt16Value;
        private BaseDataVariableState<int[]> m_int32Value;
        private BaseDataVariableState<uint[]> m_uInt32Value;
        private BaseDataVariableState<long[]> m_int64Value;
        private BaseDataVariableState<ulong[]> m_uInt64Value;
        private BaseDataVariableState<float[]> m_floatValue;
        private BaseDataVariableState<double[]> m_doubleValue;
        private BaseDataVariableState<string[]> m_stringValue;
        private BaseDataVariableState<DateTime[]> m_dateTimeValue;
        private BaseDataVariableState<Guid[]> m_guidValue;
        private BaseDataVariableState<byte[][]> m_byteStringValue;
        private BaseDataVariableState<XmlElement[]> m_xmlElementValue;
        private BaseDataVariableState<NodeId[]> m_nodeIdValue;
        private BaseDataVariableState<ExpandedNodeId[]> m_expandedNodeIdValue;
        private BaseDataVariableState<QualifiedName[]> m_qualifiedNameValue;
        private BaseDataVariableState<LocalizedText[]> m_localizedTextValue;
        private BaseDataVariableState<StatusCode[]> m_statusCodeValue;
        private BaseDataVariableState<object[]> m_variantValue;
        private BaseDataVariableState<int[]> m_enumerationValue;
        private BaseDataVariableState<ExtensionObject[]> m_structureValue;
        private BaseDataVariableState<object[]> m_numberValue;
        private BaseDataVariableState<object[]> m_integerValue;
        private BaseDataVariableState<object[]> m_uIntegerValue;
        #endregion
    }
#endif
    #endregion

    #region AnalogArrayValueObjectState Class
#if !OPCUA_EXCLUDE_AnalogArrayValueObjectState
    /// <summary>
    /// Stores an instance of the AnalogArrayValueObjectType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class AnalogArrayValueObjectState : TestDataObjectState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public AnalogArrayValueObjectState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return NodeId.Create(ObjectTypes.AnalogArrayValueObjectType, Namespaces.TestData, namespaceUris);
        }

#if !OPCUA_EXCLUDE_InitializationStrings
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
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGCAAAEAAAABACIAAABBbmFsb2dBcnJh" +
           "eVZhbHVlT2JqZWN0VHlwZUluc3RhbmNlAQEjJgEBIyYBAAAAACQAAQEnJhAAAAA1YIkKAgAAAAEAEAAA" +
           "AFNpbXVsYXRpb25BY3RpdmUBASQmAwAAAABHAAAASWYgdHJ1ZSB0aGUgc2VydmVyIHdpbGwgcHJvZHVj" +
           "ZSBuZXcgdmFsdWVzIGZvciBlYWNoIG1vbml0b3JlZCB2YXJpYWJsZS4ALgBEJCYAAAAB/////wEB////" +
           "/wAAAAAEYYIKBAAAAAEADgAAAEdlbmVyYXRlVmFsdWVzAQElJgAvAQGpJCUmAAABAf////8BAAAAFWCp" +
           "CgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBJiYALgBEJiYAAJYBAAAAAQAqAQFGAAAACgAAAEl0ZXJh" +
           "dGlvbnMAB/////8AAAAAAwAAAAAlAAAAVGhlIG51bWJlciBvZiBuZXcgdmFsdWVzIHRvIGdlbmVyYXRl" +
           "LgEAKAEBAAAAAQH/////AAAAAARggAoBAAAAAQANAAAAQ3ljbGVDb21wbGV0ZQEBJyYALwEAQQsnJgAA" +
           "AQAAAAAkAQEBIyYXAAAANWCJCgIAAAAAAAcAAABFdmVudElkAQEoJgMAAAAAKwAAAEEgZ2xvYmFsbHkg" +
           "dW5pcXVlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudC4ALgBEKCYAAAAP/////wEB/////wAAAAA1YIkK" +
           "AgAAAAAACQAAAEV2ZW50VHlwZQEBKSYDAAAAACIAAABUaGUgaWRlbnRpZmllciBmb3IgdGhlIGV2ZW50" +
           "IHR5cGUuAC4ARCkmAAAAEf////8BAf////8AAAAANWCJCgIAAAAAAAoAAABTb3VyY2VOb2RlAQEqJgMA" +
           "AAAAGAAAAFRoZSBzb3VyY2Ugb2YgdGhlIGV2ZW50LgAuAEQqJgAAABH/////AQH/////AAAAADVgiQoC" +
           "AAAAAAAKAAAAU291cmNlTmFtZQEBKyYDAAAAACkAAABBIGRlc2NyaXB0aW9uIG9mIHRoZSBzb3VyY2Ug" +
           "b2YgdGhlIGV2ZW50LgAuAEQrJgAAAAz/////AQH/////AAAAADVgiQoCAAAAAAAEAAAAVGltZQEBLCYD" +
           "AAAAABgAAABXaGVuIHRoZSBldmVudCBvY2N1cnJlZC4ALgBELCYAAAEAJgH/////AQH/////AAAAADVg" +
           "iQoCAAAAAAALAAAAUmVjZWl2ZVRpbWUBAS0mAwAAAAA+AAAAV2hlbiB0aGUgc2VydmVyIHJlY2VpdmVk" +
           "IHRoZSBldmVudCBmcm9tIHRoZSB1bmRlcmx5aW5nIHN5c3RlbS4ALgBELSYAAAEAJgH/////AQH/////" +
           "AAAAADVgiQoCAAAAAAAHAAAATWVzc2FnZQEBLyYDAAAAACUAAABBIGxvY2FsaXplZCBkZXNjcmlwdGlv" +
           "biBvZiB0aGUgZXZlbnQuAC4ARC8mAAAAFf////8BAf////8AAAAANWCJCgIAAAAAAAgAAABTZXZlcml0" +
           "eQEBMCYDAAAAACEAAABJbmRpY2F0ZXMgaG93IHVyZ2VudCBhbiBldmVudCBpcy4ALgBEMCYAAAAF////" +
           "/wEB/////wAAAAAVYIkKAgAAAAAAEAAAAENvbmRpdGlvbkNsYXNzSWQBAUItAC4AREItAAAAEf////8B" +
           "Af////8AAAAAFWCJCgIAAAAAABIAAABDb25kaXRpb25DbGFzc05hbWUBAUMtAC4AREMtAAAAFf////8B" +
           "Af////8AAAAAFWCJCgIAAAAAAA0AAABDb25kaXRpb25OYW1lAQEpLQAuAEQpLQAAAAz/////AQH/////" +
           "AAAAABVgiQoCAAAAAAAIAAAAQnJhbmNoSWQBATEmAC4ARDEmAAAAEf////8BAf////8AAAAAFWCJCgIA" +
           "AAAAAAYAAABSZXRhaW4BATImAC4ARDImAAAAAf////8BAf////8AAAAAFWCJCgIAAAAAAAwAAABFbmFi" +
           "bGVkU3RhdGUBATMmAC8BACMjMyYAAAAV/////wEBAgAAAAEALCMAAQFIJgEALCMAAQFQJgEAAAAVYIkK" +
           "AgAAAAAAAgAAAElkAQE0JgAuAEQ0JgAAAAH/////AQH/////AAAAABVgiQoCAAAAAAAHAAAAUXVhbGl0" +
           "eQEBOSYALwEAKiM5JgAAABP/////AQH/////AQAAABVgiQoCAAAAAAAPAAAAU291cmNlVGltZXN0YW1w" +
           "AQE6JgAuAEQ6JgAAAQAmAf////8BAf////8AAAAAFWCJCgIAAAAAAAwAAABMYXN0U2V2ZXJpdHkBAT0m" +
           "AC8BACojPSYAAAAF/////wEB/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVzdGFtcAEBPiYA" +
           "LgBEPiYAAAEAJgH/////AQH/////AAAAABVgiQoCAAAAAAAHAAAAQ29tbWVudAEBPyYALwEAKiM/JgAA" +
           "ABX/////AQH/////AQAAABVgiQoCAAAAAAAPAAAAU291cmNlVGltZXN0YW1wAQFAJgAuAERAJgAAAQAm" +
           "Af////8BAf////8AAAAAFWCJCgIAAAAAAAwAAABDbGllbnRVc2VySWQBAUEmAC4AREEmAAAADP////8B" +
           "Af////8AAAAABGGCCgQAAAAAAAcAAABEaXNhYmxlAQFDJgAvAQBEI0MmAAABAQEAAAABAPkLAAEA8woA" +
           "AAAABGGCCgQAAAAAAAYAAABFbmFibGUBAUImAC8BAEMjQiYAAAEBAQAAAAEA+QsAAQDzCgAAAAAEYYIK" +
           "BAAAAAAACgAAAEFkZENvbW1lbnQBAUQmAC8BAEUjRCYAAAEBAQAAAAEA+QsAAQANCwEAAAAVYKkKAgAA" +
           "AAAADgAAAElucHV0QXJndW1lbnRzAQFFJgAuAERFJgAAlgIAAAABACoBAUYAAAAHAAAARXZlbnRJZAAP" +
           "/////wAAAAADAAAAACgAAABUaGUgaWRlbnRpZmllciBmb3IgdGhlIGV2ZW50IHRvIGNvbW1lbnQuAQAq" +
           "AQFCAAAABwAAAENvbW1lbnQAFf////8AAAAAAwAAAAAkAAAAVGhlIGNvbW1lbnQgdG8gYWRkIHRvIHRo" +
           "ZSBjb25kaXRpb24uAQAoAQEAAAABAf////8AAAAAFWCJCgIAAAAAAAoAAABBY2tlZFN0YXRlAQFIJgAv" +
           "AQAjI0gmAAAAFf////8BAQEAAAABACwjAQEBMyYBAAAAFWCJCgIAAAAAAAIAAABJZAEBSSYALgBESSYA" +
           "AAAB/////wEB/////wAAAAAEYYIKBAAAAAAACwAAAEFja25vd2xlZGdlAQFYJgAvAQCXI1gmAAABAQEA" +
           "AAABAPkLAAEA8CIBAAAAFWCpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBWSYALgBEWSYAAJYCAAAA" +
           "AQAqAQFGAAAABwAAAEV2ZW50SWQAD/////8AAAAAAwAAAAAoAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRo" +
           "ZSBldmVudCB0byBjb21tZW50LgEAKgEBQgAAAAcAAABDb21tZW50ABX/////AAAAAAMAAAAAJAAAAFRo" +
           "ZSBjb21tZW50IHRvIGFkZCB0byB0aGUgY29uZGl0aW9uLgEAKAEBAAAAAQH/////AAAAABVgiQoCAAAA" +
           "AQAKAAAAU0J5dGVWYWx1ZQEBXCYALwEAQAlcJgAAAAIBAAAAAQH/////AQAAABVgiQoCAAAAAAAHAAAA" +
           "RVVSYW5nZQEBXyYALgBEXyYAAAEAdAP/////AQH/////AAAAABVgiQoCAAAAAQAJAAAAQnl0ZVZhbHVl" +
           "AQFiJgAvAQBACWImAAAAAwEAAAABAf////8BAAAAFWCJCgIAAAAAAAcAAABFVVJhbmdlAQFlJgAuAERl" +
           "JgAAAQB0A/////8BAf////8AAAAAFWCJCgIAAAABAAoAAABJbnQxNlZhbHVlAQFoJgAvAQBACWgmAAAA" +
           "BAEAAAABAf////8BAAAAFWCJCgIAAAAAAAcAAABFVVJhbmdlAQFrJgAuAERrJgAAAQB0A/////8BAf//" +
           "//8AAAAAFWCJCgIAAAABAAsAAABVSW50MTZWYWx1ZQEBbiYALwEAQAluJgAAAAUBAAAAAQH/////AQAA" +
           "ABVgiQoCAAAAAAAHAAAARVVSYW5nZQEBcSYALgBEcSYAAAEAdAP/////AQH/////AAAAABVgiQoCAAAA" +
           "AQAKAAAASW50MzJWYWx1ZQEBdCYALwEAQAl0JgAAAAYBAAAAAQH/////AQAAABVgiQoCAAAAAAAHAAAA" +
           "RVVSYW5nZQEBdyYALgBEdyYAAAEAdAP/////AQH/////AAAAABVgiQoCAAAAAQALAAAAVUludDMyVmFs" +
           "dWUBAXomAC8BAEAJeiYAAAAHAQAAAAEB/////wEAAAAVYIkKAgAAAAAABwAAAEVVUmFuZ2UBAX0mAC4A" +
           "RH0mAAABAHQD/////wEB/////wAAAAAVYIkKAgAAAAEACgAAAEludDY0VmFsdWUBAYAmAC8BAEAJgCYA" +
           "AAAIAQAAAAEB/////wEAAAAVYIkKAgAAAAAABwAAAEVVUmFuZ2UBAYMmAC4ARIMmAAABAHQD/////wEB" +
           "/////wAAAAAVYIkKAgAAAAEACwAAAFVJbnQ2NFZhbHVlAQGGJgAvAQBACYYmAAAACQEAAAABAf////8B" +
           "AAAAFWCJCgIAAAAAAAcAAABFVVJhbmdlAQGJJgAuAESJJgAAAQB0A/////8BAf////8AAAAAFWCJCgIA" +
           "AAABAAoAAABGbG9hdFZhbHVlAQGMJgAvAQBACYwmAAAACgEAAAABAf////8BAAAAFWCJCgIAAAAAAAcA" +
           "AABFVVJhbmdlAQGPJgAuAESPJgAAAQB0A/////8BAf////8AAAAAFWCJCgIAAAABAAsAAABEb3VibGVW" +
           "YWx1ZQEBkiYALwEAQAmSJgAAAAsBAAAAAQH/////AQAAABVgiQoCAAAAAAAHAAAARVVSYW5nZQEBlSYA" +
           "LgBElSYAAAEAdAP/////AQH/////AAAAABVgiQoCAAAAAQALAAAATnVtYmVyVmFsdWUBAZgmAC8BAEAJ" +
           "mCYAAAAaAQAAAAEB/////wEAAAAVYIkKAgAAAAAABwAAAEVVUmFuZ2UBAZsmAC4ARJsmAAABAHQD////" +
           "/wEB/////wAAAAAVYIkKAgAAAAEADAAAAEludGVnZXJWYWx1ZQEBniYALwEAQAmeJgAAABsBAAAAAQH/" +
           "////AQAAABVgiQoCAAAAAAAHAAAARVVSYW5nZQEBoSYALgBEoSYAAAEAdAP/////AQH/////AAAAABVg" +
           "iQoCAAAAAQANAAAAVUludGVnZXJWYWx1ZQEBpCYALwEAQAmkJgAAABwBAAAAAQH/////AQAAABVgiQoC" +
           "AAAAAAAHAAAARVVSYW5nZQEBpyYALgBEpyYAAAEAdAP/////AQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the SByteValue Variable.
        /// </summary>
        public AnalogItemState<sbyte[]> SByteValue {
            get => m_sByteValue;

            set {
                if (!ReferenceEquals(m_sByteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_sByteValue = value;
            }
        }

        /// <summary>
        /// A description for the ByteValue Variable.
        /// </summary>
        public AnalogItemState<byte[]> ByteValue {
            get => m_byteValue;

            set {
                if (!ReferenceEquals(m_byteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_byteValue = value;
            }
        }

        /// <summary>
        /// A description for the Int16Value Variable.
        /// </summary>
        public AnalogItemState<short[]> Int16Value {
            get => m_int16Value;

            set {
                if (!ReferenceEquals(m_int16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int16Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt16Value Variable.
        /// </summary>
        public AnalogItemState<ushort[]> UInt16Value {
            get => m_uInt16Value;

            set {
                if (!ReferenceEquals(m_uInt16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt16Value = value;
            }
        }

        /// <summary>
        /// A description for the Int32Value Variable.
        /// </summary>
        public AnalogItemState<int[]> Int32Value {
            get => m_int32Value;

            set {
                if (!ReferenceEquals(m_int32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int32Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt32Value Variable.
        /// </summary>
        public AnalogItemState<uint[]> UInt32Value {
            get => m_uInt32Value;

            set {
                if (!ReferenceEquals(m_uInt32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt32Value = value;
            }
        }

        /// <summary>
        /// A description for the Int64Value Variable.
        /// </summary>
        public AnalogItemState<long[]> Int64Value {
            get => m_int64Value;

            set {
                if (!ReferenceEquals(m_int64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int64Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt64Value Variable.
        /// </summary>
        public AnalogItemState<ulong[]> UInt64Value {
            get => m_uInt64Value;

            set {
                if (!ReferenceEquals(m_uInt64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt64Value = value;
            }
        }

        /// <summary>
        /// A description for the FloatValue Variable.
        /// </summary>
        public AnalogItemState<float[]> FloatValue {
            get => m_floatValue;

            set {
                if (!ReferenceEquals(m_floatValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_floatValue = value;
            }
        }

        /// <summary>
        /// A description for the DoubleValue Variable.
        /// </summary>
        public AnalogItemState<double[]> DoubleValue {
            get => m_doubleValue;

            set {
                if (!ReferenceEquals(m_doubleValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_doubleValue = value;
            }
        }

        /// <summary>
        /// A description for the NumberValue Variable.
        /// </summary>
        public AnalogItemState<object[]> NumberValue {
            get => m_numberValue;

            set {
                if (!ReferenceEquals(m_numberValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_numberValue = value;
            }
        }

        /// <summary>
        /// A description for the IntegerValue Variable.
        /// </summary>
        public AnalogItemState<object[]> IntegerValue {
            get => m_integerValue;

            set {
                if (!ReferenceEquals(m_integerValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_integerValue = value;
            }
        }

        /// <summary>
        /// A description for the UIntegerValue Variable.
        /// </summary>
        public AnalogItemState<object[]> UIntegerValue {
            get => m_uIntegerValue;

            set {
                if (!ReferenceEquals(m_uIntegerValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uIntegerValue = value;
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
            if (m_sByteValue != null) {
                children.Add(m_sByteValue);
            }

            if (m_byteValue != null) {
                children.Add(m_byteValue);
            }

            if (m_int16Value != null) {
                children.Add(m_int16Value);
            }

            if (m_uInt16Value != null) {
                children.Add(m_uInt16Value);
            }

            if (m_int32Value != null) {
                children.Add(m_int32Value);
            }

            if (m_uInt32Value != null) {
                children.Add(m_uInt32Value);
            }

            if (m_int64Value != null) {
                children.Add(m_int64Value);
            }

            if (m_uInt64Value != null) {
                children.Add(m_uInt64Value);
            }

            if (m_floatValue != null) {
                children.Add(m_floatValue);
            }

            if (m_doubleValue != null) {
                children.Add(m_doubleValue);
            }

            if (m_numberValue != null) {
                children.Add(m_numberValue);
            }

            if (m_integerValue != null) {
                children.Add(m_integerValue);
            }

            if (m_uIntegerValue != null) {
                children.Add(m_uIntegerValue);
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
                case BrowseNames.SByteValue: {
                        if (createOrReplace) {
                            if (SByteValue == null) {
                                if (replacement == null) {
                                    SByteValue = new AnalogItemState<sbyte[]>(this);
                                }
                                else {
                                    SByteValue = (AnalogItemState<sbyte[]>)replacement;
                                }
                            }
                        }

                        instance = SByteValue;
                        break;
                    }

                case BrowseNames.ByteValue: {
                        if (createOrReplace) {
                            if (ByteValue == null) {
                                if (replacement == null) {
                                    ByteValue = new AnalogItemState<byte[]>(this);
                                }
                                else {
                                    ByteValue = (AnalogItemState<byte[]>)replacement;
                                }
                            }
                        }

                        instance = ByteValue;
                        break;
                    }

                case BrowseNames.Int16Value: {
                        if (createOrReplace) {
                            if (Int16Value == null) {
                                if (replacement == null) {
                                    Int16Value = new AnalogItemState<short[]>(this);
                                }
                                else {
                                    Int16Value = (AnalogItemState<short[]>)replacement;
                                }
                            }
                        }

                        instance = Int16Value;
                        break;
                    }

                case BrowseNames.UInt16Value: {
                        if (createOrReplace) {
                            if (UInt16Value == null) {
                                if (replacement == null) {
                                    UInt16Value = new AnalogItemState<ushort[]>(this);
                                }
                                else {
                                    UInt16Value = (AnalogItemState<ushort[]>)replacement;
                                }
                            }
                        }

                        instance = UInt16Value;
                        break;
                    }

                case BrowseNames.Int32Value: {
                        if (createOrReplace) {
                            if (Int32Value == null) {
                                if (replacement == null) {
                                    Int32Value = new AnalogItemState<int[]>(this);
                                }
                                else {
                                    Int32Value = (AnalogItemState<int[]>)replacement;
                                }
                            }
                        }

                        instance = Int32Value;
                        break;
                    }

                case BrowseNames.UInt32Value: {
                        if (createOrReplace) {
                            if (UInt32Value == null) {
                                if (replacement == null) {
                                    UInt32Value = new AnalogItemState<uint[]>(this);
                                }
                                else {
                                    UInt32Value = (AnalogItemState<uint[]>)replacement;
                                }
                            }
                        }

                        instance = UInt32Value;
                        break;
                    }

                case BrowseNames.Int64Value: {
                        if (createOrReplace) {
                            if (Int64Value == null) {
                                if (replacement == null) {
                                    Int64Value = new AnalogItemState<long[]>(this);
                                }
                                else {
                                    Int64Value = (AnalogItemState<long[]>)replacement;
                                }
                            }
                        }

                        instance = Int64Value;
                        break;
                    }

                case BrowseNames.UInt64Value: {
                        if (createOrReplace) {
                            if (UInt64Value == null) {
                                if (replacement == null) {
                                    UInt64Value = new AnalogItemState<ulong[]>(this);
                                }
                                else {
                                    UInt64Value = (AnalogItemState<ulong[]>)replacement;
                                }
                            }
                        }

                        instance = UInt64Value;
                        break;
                    }

                case BrowseNames.FloatValue: {
                        if (createOrReplace) {
                            if (FloatValue == null) {
                                if (replacement == null) {
                                    FloatValue = new AnalogItemState<float[]>(this);
                                }
                                else {
                                    FloatValue = (AnalogItemState<float[]>)replacement;
                                }
                            }
                        }

                        instance = FloatValue;
                        break;
                    }

                case BrowseNames.DoubleValue: {
                        if (createOrReplace) {
                            if (DoubleValue == null) {
                                if (replacement == null) {
                                    DoubleValue = new AnalogItemState<double[]>(this);
                                }
                                else {
                                    DoubleValue = (AnalogItemState<double[]>)replacement;
                                }
                            }
                        }

                        instance = DoubleValue;
                        break;
                    }

                case BrowseNames.NumberValue: {
                        if (createOrReplace) {
                            if (NumberValue == null) {
                                if (replacement == null) {
                                    NumberValue = new AnalogItemState<object[]>(this);
                                }
                                else {
                                    NumberValue = (AnalogItemState<object[]>)replacement;
                                }
                            }
                        }

                        instance = NumberValue;
                        break;
                    }

                case BrowseNames.IntegerValue: {
                        if (createOrReplace) {
                            if (IntegerValue == null) {
                                if (replacement == null) {
                                    IntegerValue = new AnalogItemState<object[]>(this);
                                }
                                else {
                                    IntegerValue = (AnalogItemState<object[]>)replacement;
                                }
                            }
                        }

                        instance = IntegerValue;
                        break;
                    }

                case BrowseNames.UIntegerValue: {
                        if (createOrReplace) {
                            if (UIntegerValue == null) {
                                if (replacement == null) {
                                    UIntegerValue = new AnalogItemState<object[]>(this);
                                }
                                else {
                                    UIntegerValue = (AnalogItemState<object[]>)replacement;
                                }
                            }
                        }

                        instance = UIntegerValue;
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
        private AnalogItemState<sbyte[]> m_sByteValue;
        private AnalogItemState<byte[]> m_byteValue;
        private AnalogItemState<short[]> m_int16Value;
        private AnalogItemState<ushort[]> m_uInt16Value;
        private AnalogItemState<int[]> m_int32Value;
        private AnalogItemState<uint[]> m_uInt32Value;
        private AnalogItemState<long[]> m_int64Value;
        private AnalogItemState<ulong[]> m_uInt64Value;
        private AnalogItemState<float[]> m_floatValue;
        private AnalogItemState<double[]> m_doubleValue;
        private AnalogItemState<object[]> m_numberValue;
        private AnalogItemState<object[]> m_integerValue;
        private AnalogItemState<object[]> m_uIntegerValue;
        #endregion
    }
#endif
    #endregion

    #region UserScalarValueObjectState Class
#if !OPCUA_EXCLUDE_UserScalarValueObjectState
    /// <summary>
    /// Stores an instance of the UserScalarValueObjectType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class UserScalarValueObjectState : TestDataObjectState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public UserScalarValueObjectState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return NodeId.Create(ObjectTypes.UserScalarValueObjectType, Namespaces.TestData, namespaceUris);
        }

#if !OPCUA_EXCLUDE_InitializationStrings
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
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGCAAAEAAAABACEAAABVc2VyU2NhbGFy" +
           "VmFsdWVPYmplY3RUeXBlSW5zdGFuY2UBAcEmAQHBJgEAAAAAJAABAcUmGQAAADVgiQoCAAAAAQAQAAAA" +
           "U2ltdWxhdGlvbkFjdGl2ZQEBwiYDAAAAAEcAAABJZiB0cnVlIHRoZSBzZXJ2ZXIgd2lsbCBwcm9kdWNl" +
           "IG5ldyB2YWx1ZXMgZm9yIGVhY2ggbW9uaXRvcmVkIHZhcmlhYmxlLgAuAETCJgAAAAH/////AQH/////" +
           "AAAAAARhggoEAAAAAQAOAAAAR2VuZXJhdGVWYWx1ZXMBAcMmAC8BAakkwyYAAAEB/////wEAAAAVYKkK" +
           "AgAAAAAADgAAAElucHV0QXJndW1lbnRzAQHEJgAuAETEJgAAlgEAAAABACoBAUYAAAAKAAAASXRlcmF0" +
           "aW9ucwAH/////wAAAAADAAAAACUAAABUaGUgbnVtYmVyIG9mIG5ldyB2YWx1ZXMgdG8gZ2VuZXJhdGUu" +
           "AQAoAQEAAAABAf////8AAAAABGCACgEAAAABAA0AAABDeWNsZUNvbXBsZXRlAQHFJgAvAQBBC8UmAAAB" +
           "AAAAACQBAQHBJhcAAAA1YIkKAgAAAAAABwAAAEV2ZW50SWQBAcYmAwAAAAArAAAAQSBnbG9iYWxseSB1" +
           "bmlxdWUgaWRlbnRpZmllciBmb3IgdGhlIGV2ZW50LgAuAETGJgAAAA//////AQH/////AAAAADVgiQoC" +
           "AAAAAAAJAAAARXZlbnRUeXBlAQHHJgMAAAAAIgAAAFRoZSBpZGVudGlmaWVyIGZvciB0aGUgZXZlbnQg" +
           "dHlwZS4ALgBExyYAAAAR/////wEB/////wAAAAA1YIkKAgAAAAAACgAAAFNvdXJjZU5vZGUBAcgmAwAA" +
           "AAAYAAAAVGhlIHNvdXJjZSBvZiB0aGUgZXZlbnQuAC4ARMgmAAAAEf////8BAf////8AAAAANWCJCgIA" +
           "AAAAAAoAAABTb3VyY2VOYW1lAQHJJgMAAAAAKQAAAEEgZGVzY3JpcHRpb24gb2YgdGhlIHNvdXJjZSBv" +
           "ZiB0aGUgZXZlbnQuAC4ARMkmAAAADP////8BAf////8AAAAANWCJCgIAAAAAAAQAAABUaW1lAQHKJgMA" +
           "AAAAGAAAAFdoZW4gdGhlIGV2ZW50IG9jY3VycmVkLgAuAETKJgAAAQAmAf////8BAf////8AAAAANWCJ" +
           "CgIAAAAAAAsAAABSZWNlaXZlVGltZQEByyYDAAAAAD4AAABXaGVuIHRoZSBzZXJ2ZXIgcmVjZWl2ZWQg" +
           "dGhlIGV2ZW50IGZyb20gdGhlIHVuZGVybHlpbmcgc3lzdGVtLgAuAETLJgAAAQAmAf////8BAf////8A" +
           "AAAANWCJCgIAAAAAAAcAAABNZXNzYWdlAQHNJgMAAAAAJQAAAEEgbG9jYWxpemVkIGRlc2NyaXB0aW9u" +
           "IG9mIHRoZSBldmVudC4ALgBEzSYAAAAV/////wEB/////wAAAAA1YIkKAgAAAAAACAAAAFNldmVyaXR5" +
           "AQHOJgMAAAAAIQAAAEluZGljYXRlcyBob3cgdXJnZW50IGFuIGV2ZW50IGlzLgAuAETOJgAAAAX/////" +
           "AQH/////AAAAABVgiQoCAAAAAAAQAAAAQ29uZGl0aW9uQ2xhc3NJZAEBRC0ALgBERC0AAAAR/////wEB" +
           "/////wAAAAAVYIkKAgAAAAAAEgAAAENvbmRpdGlvbkNsYXNzTmFtZQEBRS0ALgBERS0AAAAV/////wEB" +
           "/////wAAAAAVYIkKAgAAAAAADQAAAENvbmRpdGlvbk5hbWUBASotAC4ARCotAAAADP////8BAf////8A" +
           "AAAAFWCJCgIAAAAAAAgAAABCcmFuY2hJZAEBzyYALgBEzyYAAAAR/////wEB/////wAAAAAVYIkKAgAA" +
           "AAAABgAAAFJldGFpbgEB0CYALgBE0CYAAAAB/////wEB/////wAAAAAVYIkKAgAAAAAADAAAAEVuYWJs" +
           "ZWRTdGF0ZQEB0SYALwEAIyPRJgAAABX/////AQECAAAAAQAsIwABAeYmAQAsIwABAe4mAQAAABVgiQoC" +
           "AAAAAAACAAAASWQBAdImAC4ARNImAAAAAf////8BAf////8AAAAAFWCJCgIAAAAAAAcAAABRdWFsaXR5" +
           "AQHXJgAvAQAqI9cmAAAAE/////8BAf////8BAAAAFWCJCgIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXAB" +
           "AdgmAC4ARNgmAAABACYB/////wEB/////wAAAAAVYIkKAgAAAAAADAAAAExhc3RTZXZlcml0eQEB2yYA" +
           "LwEAKiPbJgAAAAX/////AQH/////AQAAABVgiQoCAAAAAAAPAAAAU291cmNlVGltZXN0YW1wAQHcJgAu" +
           "AETcJgAAAQAmAf////8BAf////8AAAAAFWCJCgIAAAAAAAcAAABDb21tZW50AQHdJgAvAQAqI90mAAAA" +
           "Ff////8BAf////8BAAAAFWCJCgIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAd4mAC4ARN4mAAABACYB" +
           "/////wEB/////wAAAAAVYIkKAgAAAAAADAAAAENsaWVudFVzZXJJZAEB3yYALgBE3yYAAAAM/////wEB" +
           "/////wAAAAAEYYIKBAAAAAAABwAAAERpc2FibGUBAeEmAC8BAEQj4SYAAAEBAQAAAAEA+QsAAQDzCgAA" +
           "AAAEYYIKBAAAAAAABgAAAEVuYWJsZQEB4CYALwEAQyPgJgAAAQEBAAAAAQD5CwABAPMKAAAAAARhggoE" +
           "AAAAAAAKAAAAQWRkQ29tbWVudAEB4iYALwEARSPiJgAAAQEBAAAAAQD5CwABAA0LAQAAABVgqQoCAAAA" +
           "AAAOAAAASW5wdXRBcmd1bWVudHMBAeMmAC4AROMmAACWAgAAAAEAKgEBRgAAAAcAAABFdmVudElkAA//" +
           "////AAAAAAMAAAAAKAAAAFRoZSBpZGVudGlmaWVyIGZvciB0aGUgZXZlbnQgdG8gY29tbWVudC4BACoB" +
           "AUIAAAAHAAAAQ29tbWVudAAV/////wAAAAADAAAAACQAAABUaGUgY29tbWVudCB0byBhZGQgdG8gdGhl" +
           "IGNvbmRpdGlvbi4BACgBAQAAAAEB/////wAAAAAVYIkKAgAAAAAACgAAAEFja2VkU3RhdGUBAeYmAC8B" +
           "ACMj5iYAAAAV/////wEBAQAAAAEALCMBAQHRJgEAAAAVYIkKAgAAAAAAAgAAAElkAQHnJgAuAETnJgAA" +
           "AAH/////AQH/////AAAAAARhggoEAAAAAAALAAAAQWNrbm93bGVkZ2UBAfYmAC8BAJcj9iYAAAEBAQAA" +
           "AAEA+QsAAQDwIgEAAAAVYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQH3JgAuAET3JgAAlgIAAAAB" +
           "ACoBAUYAAAAHAAAARXZlbnRJZAAP/////wAAAAADAAAAACgAAABUaGUgaWRlbnRpZmllciBmb3IgdGhl" +
           "IGV2ZW50IHRvIGNvbW1lbnQuAQAqAQFCAAAABwAAAENvbW1lbnQAFf////8AAAAAAwAAAAAkAAAAVGhl" +
           "IGNvbW1lbnQgdG8gYWRkIHRvIHRoZSBjb25kaXRpb24uAQAoAQEAAAABAf////8AAAAAFWCJCgIAAAAB" +
           "AAwAAABCb29sZWFuVmFsdWUBAfomAC8AP/omAAABAaom/////wEB/////wAAAAAVYIkKAgAAAAEACgAA" +
           "AFNCeXRlVmFsdWUBAfsmAC8AP/smAAABAasm/////wEB/////wAAAAAVYIkKAgAAAAEACQAAAEJ5dGVW" +
           "YWx1ZQEB/CYALwA//CYAAAEBrCb/////AQH/////AAAAABVgiQoCAAAAAQAKAAAASW50MTZWYWx1ZQEB" +
           "/SYALwA//SYAAAEBrSb/////AQH/////AAAAABVgiQoCAAAAAQALAAAAVUludDE2VmFsdWUBAf4mAC8A" +
           "P/4mAAABAa4m/////wEB/////wAAAAAVYIkKAgAAAAEACgAAAEludDMyVmFsdWUBAf8mAC8AP/8mAAAB" +
           "Aa8m/////wEB/////wAAAAAVYIkKAgAAAAEACwAAAFVJbnQzMlZhbHVlAQEAJwAvAD8AJwAAAQGwJv//" +
           "//8BAf////8AAAAAFWCJCgIAAAABAAoAAABJbnQ2NFZhbHVlAQEBJwAvAD8BJwAAAQGxJv////8BAf//" +
           "//8AAAAAFWCJCgIAAAABAAsAAABVSW50NjRWYWx1ZQEBAicALwA/AicAAAEBsib/////AQH/////AAAA" +
           "ABVgiQoCAAAAAQAKAAAARmxvYXRWYWx1ZQEBAycALwA/AycAAAEBsyb/////AQH/////AAAAABVgiQoC" +
           "AAAAAQALAAAARG91YmxlVmFsdWUBAQQnAC8APwQnAAABAbQm/////wEB/////wAAAAAVYIkKAgAAAAEA" +
           "CwAAAFN0cmluZ1ZhbHVlAQEFJwAvAD8FJwAAAQG1Jv////8BAf////8AAAAAFWCJCgIAAAABAA0AAABE" +
           "YXRlVGltZVZhbHVlAQEGJwAvAD8GJwAAAQG2Jv////8BAf////8AAAAAFWCJCgIAAAABAAkAAABHdWlk" +
           "VmFsdWUBAQcnAC8APwcnAAABAbcm/////wEB/////wAAAAAVYIkKAgAAAAEADwAAAEJ5dGVTdHJpbmdW" +
           "YWx1ZQEBCCcALwA/CCcAAAEBuCb/////AQH/////AAAAABVgiQoCAAAAAQAPAAAAWG1sRWxlbWVudFZh" +
           "bHVlAQEJJwAvAD8JJwAAAQG5Jv////8BAf////8AAAAAFWCJCgIAAAABAAsAAABOb2RlSWRWYWx1ZQEB" +
           "CicALwA/CicAAAEBuib/////AQH/////AAAAABVgiQoCAAAAAQATAAAARXhwYW5kZWROb2RlSWRWYWx1" +
           "ZQEBCycALwA/CycAAAEBuyb/////AQH/////AAAAABVgiQoCAAAAAQASAAAAUXVhbGlmaWVkTmFtZVZh" +
           "bHVlAQEMJwAvAD8MJwAAAQG8Jv////8BAf////8AAAAAFWCJCgIAAAABABIAAABMb2NhbGl6ZWRUZXh0" +
           "VmFsdWUBAQ0nAC8APw0nAAABAb0m/////wEB/////wAAAAAVYIkKAgAAAAEADwAAAFN0YXR1c0NvZGVW" +
           "YWx1ZQEBDicALwA/DicAAAEBvib/////AQH/////AAAAABVgiQoCAAAAAQAMAAAAVmFyaWFudFZhbHVl" +
           "AQEPJwAvAD8PJwAAAQG/Jv////8BAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the BooleanValue Variable.
        /// </summary>
        public BaseDataVariableState<bool> BooleanValue {
            get => m_booleanValue;

            set {
                if (!ReferenceEquals(m_booleanValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_booleanValue = value;
            }
        }

        /// <summary>
        /// A description for the SByteValue Variable.
        /// </summary>
        public BaseDataVariableState<sbyte> SByteValue {
            get => m_sByteValue;

            set {
                if (!ReferenceEquals(m_sByteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_sByteValue = value;
            }
        }

        /// <summary>
        /// A description for the ByteValue Variable.
        /// </summary>
        public BaseDataVariableState<byte> ByteValue {
            get => m_byteValue;

            set {
                if (!ReferenceEquals(m_byteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_byteValue = value;
            }
        }

        /// <summary>
        /// A description for the Int16Value Variable.
        /// </summary>
        public BaseDataVariableState<short> Int16Value {
            get => m_int16Value;

            set {
                if (!ReferenceEquals(m_int16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int16Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt16Value Variable.
        /// </summary>
        public BaseDataVariableState<ushort> UInt16Value {
            get => m_uInt16Value;

            set {
                if (!ReferenceEquals(m_uInt16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt16Value = value;
            }
        }

        /// <summary>
        /// A description for the Int32Value Variable.
        /// </summary>
        public BaseDataVariableState<int> Int32Value {
            get => m_int32Value;

            set {
                if (!ReferenceEquals(m_int32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int32Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt32Value Variable.
        /// </summary>
        public BaseDataVariableState<uint> UInt32Value {
            get => m_uInt32Value;

            set {
                if (!ReferenceEquals(m_uInt32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt32Value = value;
            }
        }

        /// <summary>
        /// A description for the Int64Value Variable.
        /// </summary>
        public BaseDataVariableState<long> Int64Value {
            get => m_int64Value;

            set {
                if (!ReferenceEquals(m_int64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int64Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt64Value Variable.
        /// </summary>
        public BaseDataVariableState<ulong> UInt64Value {
            get => m_uInt64Value;

            set {
                if (!ReferenceEquals(m_uInt64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt64Value = value;
            }
        }

        /// <summary>
        /// A description for the FloatValue Variable.
        /// </summary>
        public BaseDataVariableState<float> FloatValue {
            get => m_floatValue;

            set {
                if (!ReferenceEquals(m_floatValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_floatValue = value;
            }
        }

        /// <summary>
        /// A description for the DoubleValue Variable.
        /// </summary>
        public BaseDataVariableState<double> DoubleValue {
            get => m_doubleValue;

            set {
                if (!ReferenceEquals(m_doubleValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_doubleValue = value;
            }
        }

        /// <summary>
        /// A description for the StringValue Variable.
        /// </summary>
        public BaseDataVariableState<string> StringValue {
            get => m_stringValue;

            set {
                if (!ReferenceEquals(m_stringValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_stringValue = value;
            }
        }

        /// <summary>
        /// A description for the DateTimeValue Variable.
        /// </summary>
        public BaseDataVariableState<DateTime> DateTimeValue {
            get => m_dateTimeValue;

            set {
                if (!ReferenceEquals(m_dateTimeValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_dateTimeValue = value;
            }
        }

        /// <summary>
        /// A description for the GuidValue Variable.
        /// </summary>
        public BaseDataVariableState<Guid> GuidValue {
            get => m_guidValue;

            set {
                if (!ReferenceEquals(m_guidValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_guidValue = value;
            }
        }

        /// <summary>
        /// A description for the ByteStringValue Variable.
        /// </summary>
        public BaseDataVariableState<byte[]> ByteStringValue {
            get => m_byteStringValue;

            set {
                if (!ReferenceEquals(m_byteStringValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_byteStringValue = value;
            }
        }

        /// <summary>
        /// A description for the XmlElementValue Variable.
        /// </summary>
        public BaseDataVariableState<XmlElement> XmlElementValue {
            get => m_xmlElementValue;

            set {
                if (!ReferenceEquals(m_xmlElementValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_xmlElementValue = value;
            }
        }

        /// <summary>
        /// A description for the NodeIdValue Variable.
        /// </summary>
        public BaseDataVariableState<NodeId> NodeIdValue {
            get => m_nodeIdValue;

            set {
                if (!ReferenceEquals(m_nodeIdValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_nodeIdValue = value;
            }
        }

        /// <summary>
        /// A description for the ExpandedNodeIdValue Variable.
        /// </summary>
        public BaseDataVariableState<ExpandedNodeId> ExpandedNodeIdValue {
            get => m_expandedNodeIdValue;

            set {
                if (!ReferenceEquals(m_expandedNodeIdValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_expandedNodeIdValue = value;
            }
        }

        /// <summary>
        /// A description for the QualifiedNameValue Variable.
        /// </summary>
        public BaseDataVariableState<QualifiedName> QualifiedNameValue {
            get => m_qualifiedNameValue;

            set {
                if (!ReferenceEquals(m_qualifiedNameValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_qualifiedNameValue = value;
            }
        }

        /// <summary>
        /// A description for the LocalizedTextValue Variable.
        /// </summary>
        public BaseDataVariableState<LocalizedText> LocalizedTextValue {
            get => m_localizedTextValue;

            set {
                if (!ReferenceEquals(m_localizedTextValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_localizedTextValue = value;
            }
        }

        /// <summary>
        /// A description for the StatusCodeValue Variable.
        /// </summary>
        public BaseDataVariableState<StatusCode> StatusCodeValue {
            get => m_statusCodeValue;

            set {
                if (!ReferenceEquals(m_statusCodeValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_statusCodeValue = value;
            }
        }

        /// <summary>
        /// A description for the VariantValue Variable.
        /// </summary>
        public BaseDataVariableState VariantValue {
            get => m_variantValue;

            set {
                if (!ReferenceEquals(m_variantValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_variantValue = value;
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
            if (m_booleanValue != null) {
                children.Add(m_booleanValue);
            }

            if (m_sByteValue != null) {
                children.Add(m_sByteValue);
            }

            if (m_byteValue != null) {
                children.Add(m_byteValue);
            }

            if (m_int16Value != null) {
                children.Add(m_int16Value);
            }

            if (m_uInt16Value != null) {
                children.Add(m_uInt16Value);
            }

            if (m_int32Value != null) {
                children.Add(m_int32Value);
            }

            if (m_uInt32Value != null) {
                children.Add(m_uInt32Value);
            }

            if (m_int64Value != null) {
                children.Add(m_int64Value);
            }

            if (m_uInt64Value != null) {
                children.Add(m_uInt64Value);
            }

            if (m_floatValue != null) {
                children.Add(m_floatValue);
            }

            if (m_doubleValue != null) {
                children.Add(m_doubleValue);
            }

            if (m_stringValue != null) {
                children.Add(m_stringValue);
            }

            if (m_dateTimeValue != null) {
                children.Add(m_dateTimeValue);
            }

            if (m_guidValue != null) {
                children.Add(m_guidValue);
            }

            if (m_byteStringValue != null) {
                children.Add(m_byteStringValue);
            }

            if (m_xmlElementValue != null) {
                children.Add(m_xmlElementValue);
            }

            if (m_nodeIdValue != null) {
                children.Add(m_nodeIdValue);
            }

            if (m_expandedNodeIdValue != null) {
                children.Add(m_expandedNodeIdValue);
            }

            if (m_qualifiedNameValue != null) {
                children.Add(m_qualifiedNameValue);
            }

            if (m_localizedTextValue != null) {
                children.Add(m_localizedTextValue);
            }

            if (m_statusCodeValue != null) {
                children.Add(m_statusCodeValue);
            }

            if (m_variantValue != null) {
                children.Add(m_variantValue);
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
                case BrowseNames.BooleanValue: {
                        if (createOrReplace) {
                            if (BooleanValue == null) {
                                if (replacement == null) {
                                    BooleanValue = new BaseDataVariableState<bool>(this);
                                }
                                else {
                                    BooleanValue = (BaseDataVariableState<bool>)replacement;
                                }
                            }
                        }

                        instance = BooleanValue;
                        break;
                    }

                case BrowseNames.SByteValue: {
                        if (createOrReplace) {
                            if (SByteValue == null) {
                                if (replacement == null) {
                                    SByteValue = new BaseDataVariableState<sbyte>(this);
                                }
                                else {
                                    SByteValue = (BaseDataVariableState<sbyte>)replacement;
                                }
                            }
                        }

                        instance = SByteValue;
                        break;
                    }

                case BrowseNames.ByteValue: {
                        if (createOrReplace) {
                            if (ByteValue == null) {
                                if (replacement == null) {
                                    ByteValue = new BaseDataVariableState<byte>(this);
                                }
                                else {
                                    ByteValue = (BaseDataVariableState<byte>)replacement;
                                }
                            }
                        }

                        instance = ByteValue;
                        break;
                    }

                case BrowseNames.Int16Value: {
                        if (createOrReplace) {
                            if (Int16Value == null) {
                                if (replacement == null) {
                                    Int16Value = new BaseDataVariableState<short>(this);
                                }
                                else {
                                    Int16Value = (BaseDataVariableState<short>)replacement;
                                }
                            }
                        }

                        instance = Int16Value;
                        break;
                    }

                case BrowseNames.UInt16Value: {
                        if (createOrReplace) {
                            if (UInt16Value == null) {
                                if (replacement == null) {
                                    UInt16Value = new BaseDataVariableState<ushort>(this);
                                }
                                else {
                                    UInt16Value = (BaseDataVariableState<ushort>)replacement;
                                }
                            }
                        }

                        instance = UInt16Value;
                        break;
                    }

                case BrowseNames.Int32Value: {
                        if (createOrReplace) {
                            if (Int32Value == null) {
                                if (replacement == null) {
                                    Int32Value = new BaseDataVariableState<int>(this);
                                }
                                else {
                                    Int32Value = (BaseDataVariableState<int>)replacement;
                                }
                            }
                        }

                        instance = Int32Value;
                        break;
                    }

                case BrowseNames.UInt32Value: {
                        if (createOrReplace) {
                            if (UInt32Value == null) {
                                if (replacement == null) {
                                    UInt32Value = new BaseDataVariableState<uint>(this);
                                }
                                else {
                                    UInt32Value = (BaseDataVariableState<uint>)replacement;
                                }
                            }
                        }

                        instance = UInt32Value;
                        break;
                    }

                case BrowseNames.Int64Value: {
                        if (createOrReplace) {
                            if (Int64Value == null) {
                                if (replacement == null) {
                                    Int64Value = new BaseDataVariableState<long>(this);
                                }
                                else {
                                    Int64Value = (BaseDataVariableState<long>)replacement;
                                }
                            }
                        }

                        instance = Int64Value;
                        break;
                    }

                case BrowseNames.UInt64Value: {
                        if (createOrReplace) {
                            if (UInt64Value == null) {
                                if (replacement == null) {
                                    UInt64Value = new BaseDataVariableState<ulong>(this);
                                }
                                else {
                                    UInt64Value = (BaseDataVariableState<ulong>)replacement;
                                }
                            }
                        }

                        instance = UInt64Value;
                        break;
                    }

                case BrowseNames.FloatValue: {
                        if (createOrReplace) {
                            if (FloatValue == null) {
                                if (replacement == null) {
                                    FloatValue = new BaseDataVariableState<float>(this);
                                }
                                else {
                                    FloatValue = (BaseDataVariableState<float>)replacement;
                                }
                            }
                        }

                        instance = FloatValue;
                        break;
                    }

                case BrowseNames.DoubleValue: {
                        if (createOrReplace) {
                            if (DoubleValue == null) {
                                if (replacement == null) {
                                    DoubleValue = new BaseDataVariableState<double>(this);
                                }
                                else {
                                    DoubleValue = (BaseDataVariableState<double>)replacement;
                                }
                            }
                        }

                        instance = DoubleValue;
                        break;
                    }

                case BrowseNames.StringValue: {
                        if (createOrReplace) {
                            if (StringValue == null) {
                                if (replacement == null) {
                                    StringValue = new BaseDataVariableState<string>(this);
                                }
                                else {
                                    StringValue = (BaseDataVariableState<string>)replacement;
                                }
                            }
                        }

                        instance = StringValue;
                        break;
                    }

                case BrowseNames.DateTimeValue: {
                        if (createOrReplace) {
                            if (DateTimeValue == null) {
                                if (replacement == null) {
                                    DateTimeValue = new BaseDataVariableState<DateTime>(this);
                                }
                                else {
                                    DateTimeValue = (BaseDataVariableState<DateTime>)replacement;
                                }
                            }
                        }

                        instance = DateTimeValue;
                        break;
                    }

                case BrowseNames.GuidValue: {
                        if (createOrReplace) {
                            if (GuidValue == null) {
                                if (replacement == null) {
                                    GuidValue = new BaseDataVariableState<Guid>(this);
                                }
                                else {
                                    GuidValue = (BaseDataVariableState<Guid>)replacement;
                                }
                            }
                        }

                        instance = GuidValue;
                        break;
                    }

                case BrowseNames.ByteStringValue: {
                        if (createOrReplace) {
                            if (ByteStringValue == null) {
                                if (replacement == null) {
                                    ByteStringValue = new BaseDataVariableState<byte[]>(this);
                                }
                                else {
                                    ByteStringValue = (BaseDataVariableState<byte[]>)replacement;
                                }
                            }
                        }

                        instance = ByteStringValue;
                        break;
                    }

                case BrowseNames.XmlElementValue: {
                        if (createOrReplace) {
                            if (XmlElementValue == null) {
                                if (replacement == null) {
                                    XmlElementValue = new BaseDataVariableState<XmlElement>(this);
                                }
                                else {
                                    XmlElementValue = (BaseDataVariableState<XmlElement>)replacement;
                                }
                            }
                        }

                        instance = XmlElementValue;
                        break;
                    }

                case BrowseNames.NodeIdValue: {
                        if (createOrReplace) {
                            if (NodeIdValue == null) {
                                if (replacement == null) {
                                    NodeIdValue = new BaseDataVariableState<NodeId>(this);
                                }
                                else {
                                    NodeIdValue = (BaseDataVariableState<NodeId>)replacement;
                                }
                            }
                        }

                        instance = NodeIdValue;
                        break;
                    }

                case BrowseNames.ExpandedNodeIdValue: {
                        if (createOrReplace) {
                            if (ExpandedNodeIdValue == null) {
                                if (replacement == null) {
                                    ExpandedNodeIdValue = new BaseDataVariableState<ExpandedNodeId>(this);
                                }
                                else {
                                    ExpandedNodeIdValue = (BaseDataVariableState<ExpandedNodeId>)replacement;
                                }
                            }
                        }

                        instance = ExpandedNodeIdValue;
                        break;
                    }

                case BrowseNames.QualifiedNameValue: {
                        if (createOrReplace) {
                            if (QualifiedNameValue == null) {
                                if (replacement == null) {
                                    QualifiedNameValue = new BaseDataVariableState<QualifiedName>(this);
                                }
                                else {
                                    QualifiedNameValue = (BaseDataVariableState<QualifiedName>)replacement;
                                }
                            }
                        }

                        instance = QualifiedNameValue;
                        break;
                    }

                case BrowseNames.LocalizedTextValue: {
                        if (createOrReplace) {
                            if (LocalizedTextValue == null) {
                                if (replacement == null) {
                                    LocalizedTextValue = new BaseDataVariableState<LocalizedText>(this);
                                }
                                else {
                                    LocalizedTextValue = (BaseDataVariableState<LocalizedText>)replacement;
                                }
                            }
                        }

                        instance = LocalizedTextValue;
                        break;
                    }

                case BrowseNames.StatusCodeValue: {
                        if (createOrReplace) {
                            if (StatusCodeValue == null) {
                                if (replacement == null) {
                                    StatusCodeValue = new BaseDataVariableState<StatusCode>(this);
                                }
                                else {
                                    StatusCodeValue = (BaseDataVariableState<StatusCode>)replacement;
                                }
                            }
                        }

                        instance = StatusCodeValue;
                        break;
                    }

                case BrowseNames.VariantValue: {
                        if (createOrReplace) {
                            if (VariantValue == null) {
                                if (replacement == null) {
                                    VariantValue = new BaseDataVariableState(this);
                                }
                                else {
                                    VariantValue = (BaseDataVariableState)replacement;
                                }
                            }
                        }

                        instance = VariantValue;
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
        private BaseDataVariableState<bool> m_booleanValue;
        private BaseDataVariableState<sbyte> m_sByteValue;
        private BaseDataVariableState<byte> m_byteValue;
        private BaseDataVariableState<short> m_int16Value;
        private BaseDataVariableState<ushort> m_uInt16Value;
        private BaseDataVariableState<int> m_int32Value;
        private BaseDataVariableState<uint> m_uInt32Value;
        private BaseDataVariableState<long> m_int64Value;
        private BaseDataVariableState<ulong> m_uInt64Value;
        private BaseDataVariableState<float> m_floatValue;
        private BaseDataVariableState<double> m_doubleValue;
        private BaseDataVariableState<string> m_stringValue;
        private BaseDataVariableState<DateTime> m_dateTimeValue;
        private BaseDataVariableState<Guid> m_guidValue;
        private BaseDataVariableState<byte[]> m_byteStringValue;
        private BaseDataVariableState<XmlElement> m_xmlElementValue;
        private BaseDataVariableState<NodeId> m_nodeIdValue;
        private BaseDataVariableState<ExpandedNodeId> m_expandedNodeIdValue;
        private BaseDataVariableState<QualifiedName> m_qualifiedNameValue;
        private BaseDataVariableState<LocalizedText> m_localizedTextValue;
        private BaseDataVariableState<StatusCode> m_statusCodeValue;
        private BaseDataVariableState m_variantValue;
        #endregion
    }
#endif
    #endregion

    #region UserScalarValue1MethodState Class
#if (!OPCUA_EXCLUDE_UserScalarValue1MethodState)
    /// <summary>
    /// Stores an instance of the UserScalarValue1MethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class UserScalarValue1MethodState : MethodState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public UserScalarValue1MethodState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent) {
            return new UserScalarValue1MethodState(parent);
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
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGGCCgQAAAABABoAAABVc2VyU2NhbGFy" +
           "VmFsdWUxTWV0aG9kVHlwZQEBECcALwEBECcQJwAAAQH/////AgAAABVgqQoCAAAAAAAOAAAASW5wdXRB" +
           "cmd1bWVudHMBAREnAC4ARBEnAACWDAAAAAEAKgEBGgAAAAkAAABCb29sZWFuSW4BAaom/////wAAAAAA" +
           "AQAqAQEYAAAABwAAAFNCeXRlSW4BAasm/////wAAAAAAAQAqAQEXAAAABgAAAEJ5dGVJbgEBrCb/////" +
           "AAAAAAABACoBARgAAAAHAAAASW50MTZJbgEBrSb/////AAAAAAABACoBARkAAAAIAAAAVUludDE2SW4B" +
           "Aa4m/////wAAAAAAAQAqAQEYAAAABwAAAEludDMySW4BAa8m/////wAAAAAAAQAqAQEZAAAACAAAAFVJ" +
           "bnQzMkluAQGwJv////8AAAAAAAEAKgEBGAAAAAcAAABJbnQ2NEluAQGxJv////8AAAAAAAEAKgEBGQAA" +
           "AAgAAABVSW50NjRJbgEBsib/////AAAAAAABACoBARgAAAAHAAAARmxvYXRJbgEBsyb/////AAAAAAAB" +
           "ACoBARkAAAAIAAAARG91YmxlSW4BAbQm/////wAAAAAAAQAqAQEZAAAACAAAAFN0cmluZ0luAQG1Jv//" +
           "//8AAAAAAAEAKAEBAAAAAQH/////AAAAABVgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQESJwAu" +
           "AEQSJwAAlgwAAAABACoBARsAAAAKAAAAQm9vbGVhbk91dAEBqib/////AAAAAAABACoBARkAAAAIAAAA" +
           "U0J5dGVPdXQBAasm/////wAAAAAAAQAqAQEYAAAABwAAAEJ5dGVPdXQBAawm/////wAAAAAAAQAqAQEZ" +
           "AAAACAAAAEludDE2T3V0AQGtJv////8AAAAAAAEAKgEBGgAAAAkAAABVSW50MTZPdXQBAa4m/////wAA" +
           "AAAAAQAqAQEZAAAACAAAAEludDMyT3V0AQGvJv////8AAAAAAAEAKgEBGgAAAAkAAABVSW50MzJPdXQB" +
           "AbAm/////wAAAAAAAQAqAQEZAAAACAAAAEludDY0T3V0AQGxJv////8AAAAAAAEAKgEBGgAAAAkAAABV" +
           "SW50NjRPdXQBAbIm/////wAAAAAAAQAqAQEZAAAACAAAAEZsb2F0T3V0AQGzJv////8AAAAAAAEAKgEB" +
           "GgAAAAkAAABEb3VibGVPdXQBAbQm/////wAAAAAAAQAqAQEaAAAACQAAAFN0cmluZ091dAEBtSb/////" +
           "AAAAAAABACgBAQAAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public UserScalarValue1MethodStateMethodCallHandler OnCall;
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
            IList<object> outputArguments) {
            if (OnCall == null) {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            bool booleanIn = (bool)inputArguments[0];
            sbyte sByteIn = (sbyte)inputArguments[1];
            byte byteIn = (byte)inputArguments[2];
            short int16In = (short)inputArguments[3];
            ushort uInt16In = (ushort)inputArguments[4];
            int int32In = (int)inputArguments[5];
            uint uInt32In = (uint)inputArguments[6];
            long int64In = (long)inputArguments[7];
            ulong uInt64In = (ulong)inputArguments[8];
            float floatIn = (float)inputArguments[9];
            double doubleIn = (double)inputArguments[10];
            string stringIn = (string)inputArguments[11];

            bool booleanOut = (bool)outputArguments[0];
            sbyte sByteOut = (sbyte)outputArguments[1];
            byte byteOut = (byte)outputArguments[2];
            short int16Out = (short)outputArguments[3];
            ushort uInt16Out = (ushort)outputArguments[4];
            int int32Out = (int)outputArguments[5];
            uint uInt32Out = (uint)outputArguments[6];
            long int64Out = (long)outputArguments[7];
            ulong uInt64Out = (ulong)outputArguments[8];
            float floatOut = (float)outputArguments[9];
            double doubleOut = (double)outputArguments[10];
            string stringOut = (string)outputArguments[11];

            if (OnCall != null) {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    booleanIn,
                    sByteIn,
                    byteIn,
                    int16In,
                    uInt16In,
                    int32In,
                    uInt32In,
                    int64In,
                    uInt64In,
                    floatIn,
                    doubleIn,
                    stringIn,
                    ref booleanOut,
                    ref sByteOut,
                    ref byteOut,
                    ref int16Out,
                    ref uInt16Out,
                    ref int32Out,
                    ref uInt32Out,
                    ref int64Out,
                    ref uInt64Out,
                    ref floatOut,
                    ref doubleOut,
                    ref stringOut);
            }

            outputArguments[0] = booleanOut;
            outputArguments[1] = sByteOut;
            outputArguments[2] = byteOut;
            outputArguments[3] = int16Out;
            outputArguments[4] = uInt16Out;
            outputArguments[5] = int32Out;
            outputArguments[6] = uInt32Out;
            outputArguments[7] = int64Out;
            outputArguments[8] = uInt64Out;
            outputArguments[9] = floatOut;
            outputArguments[10] = doubleOut;
            outputArguments[11] = stringOut;

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
    public delegate ServiceResult UserScalarValue1MethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        bool booleanIn,
        sbyte sByteIn,
        byte byteIn,
        short int16In,
        ushort uInt16In,
        int int32In,
        uint uInt32In,
        long int64In,
        ulong uInt64In,
        float floatIn,
        double doubleIn,
        string stringIn,
        ref bool booleanOut,
        ref sbyte sByteOut,
        ref byte byteOut,
        ref short int16Out,
        ref ushort uInt16Out,
        ref int int32Out,
        ref uint uInt32Out,
        ref long int64Out,
        ref ulong uInt64Out,
        ref float floatOut,
        ref double doubleOut,
        ref string stringOut);
#endif
    #endregion

    #region UserScalarValue2MethodState Class
#if (!OPCUA_EXCLUDE_UserScalarValue2MethodState)
    /// <summary>
    /// Stores an instance of the UserScalarValue2MethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class UserScalarValue2MethodState : MethodState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public UserScalarValue2MethodState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent) {
            return new UserScalarValue2MethodState(parent);
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
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGGCCgQAAAABABoAAABVc2VyU2NhbGFy" +
           "VmFsdWUyTWV0aG9kVHlwZQEBEycALwEBEycTJwAAAQH/////AgAAABVgqQoCAAAAAAAOAAAASW5wdXRB" +
           "cmd1bWVudHMBARQnAC4ARBQnAACWCgAAAAEAKgEBGwAAAAoAAABEYXRlVGltZUluAQG2Jv////8AAAAA" +
           "AAEAKgEBFwAAAAYAAABHdWlkSW4BAbcm/////wAAAAAAAQAqAQEdAAAADAAAAEJ5dGVTdHJpbmdJbgEB" +
           "uCb/////AAAAAAABACoBAR0AAAAMAAAAWG1sRWxlbWVudEluAQG5Jv////8AAAAAAAEAKgEBGQAAAAgA" +
           "AABOb2RlSWRJbgEBuib/////AAAAAAABACoBASEAAAAQAAAARXhwYW5kZWROb2RlSWRJbgEBuyb/////" +
           "AAAAAAABACoBASAAAAAPAAAAUXVhbGlmaWVkTmFtZUluAQG8Jv////8AAAAAAAEAKgEBIAAAAA8AAABM" +
           "b2NhbGl6ZWRUZXh0SW4BAb0m/////wAAAAAAAQAqAQEdAAAADAAAAFN0YXR1c0NvZGVJbgEBvib/////" +
           "AAAAAAABACoBARoAAAAJAAAAVmFyaWFudEluAQG/Jv////8AAAAAAAEAKAEBAAAAAQH/////AAAAABVg" +
           "qQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQEVJwAuAEQVJwAAlgoAAAABACoBARwAAAALAAAARGF0" +
           "ZVRpbWVPdXQBAbYm/////wAAAAAAAQAqAQEYAAAABwAAAEd1aWRPdXQBAbcm/////wAAAAAAAQAqAQEe" +
           "AAAADQAAAEJ5dGVTdHJpbmdPdXQBAbgm/////wAAAAAAAQAqAQEeAAAADQAAAFhtbEVsZW1lbnRPdXQB" +
           "Abkm/////wAAAAAAAQAqAQEaAAAACQAAAE5vZGVJZE91dAEBuib/////AAAAAAABACoBASIAAAARAAAA" +
           "RXhwYW5kZWROb2RlSWRPdXQBAbsm/////wAAAAAAAQAqAQEhAAAAEAAAAFF1YWxpZmllZE5hbWVPdXQB" +
           "Abwm/////wAAAAAAAQAqAQEhAAAAEAAAAExvY2FsaXplZFRleHRPdXQBAb0m/////wAAAAAAAQAqAQEe" +
           "AAAADQAAAFN0YXR1c0NvZGVPdXQBAb4m/////wAAAAAAAQAqAQEbAAAACgAAAFZhcmlhbnRPdXQBAb8m" +
           "/////wAAAAAAAQAoAQEAAAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public UserScalarValue2MethodStateMethodCallHandler OnCall;
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
            IList<object> outputArguments) {
            if (OnCall == null) {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            DateTime dateTimeIn = (DateTime)inputArguments[0];
            Uuid guidIn = (Uuid)inputArguments[1];
            byte[] byteStringIn = (byte[])inputArguments[2];
            XmlElement xmlElementIn = (XmlElement)inputArguments[3];
            NodeId nodeIdIn = (NodeId)inputArguments[4];
            ExpandedNodeId expandedNodeIdIn = (ExpandedNodeId)inputArguments[5];
            QualifiedName qualifiedNameIn = (QualifiedName)inputArguments[6];
            LocalizedText localizedTextIn = (LocalizedText)inputArguments[7];
            StatusCode statusCodeIn = (StatusCode)inputArguments[8];
            object variantIn = (object)inputArguments[9];

            DateTime dateTimeOut = (DateTime)outputArguments[0];
            Uuid guidOut = (Uuid)outputArguments[1];
            byte[] byteStringOut = (byte[])outputArguments[2];
            XmlElement xmlElementOut = (XmlElement)outputArguments[3];
            NodeId nodeIdOut = (NodeId)outputArguments[4];
            ExpandedNodeId expandedNodeIdOut = (ExpandedNodeId)outputArguments[5];
            QualifiedName qualifiedNameOut = (QualifiedName)outputArguments[6];
            LocalizedText localizedTextOut = (LocalizedText)outputArguments[7];
            StatusCode statusCodeOut = (StatusCode)outputArguments[8];
            object variantOut = (object)outputArguments[9];

            if (OnCall != null) {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    dateTimeIn,
                    guidIn,
                    byteStringIn,
                    xmlElementIn,
                    nodeIdIn,
                    expandedNodeIdIn,
                    qualifiedNameIn,
                    localizedTextIn,
                    statusCodeIn,
                    variantIn,
                    ref dateTimeOut,
                    ref guidOut,
                    ref byteStringOut,
                    ref xmlElementOut,
                    ref nodeIdOut,
                    ref expandedNodeIdOut,
                    ref qualifiedNameOut,
                    ref localizedTextOut,
                    ref statusCodeOut,
                    ref variantOut);
            }

            outputArguments[0] = dateTimeOut;
            outputArguments[1] = guidOut;
            outputArguments[2] = byteStringOut;
            outputArguments[3] = xmlElementOut;
            outputArguments[4] = nodeIdOut;
            outputArguments[5] = expandedNodeIdOut;
            outputArguments[6] = qualifiedNameOut;
            outputArguments[7] = localizedTextOut;
            outputArguments[8] = statusCodeOut;
            outputArguments[9] = variantOut;

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
    public delegate ServiceResult UserScalarValue2MethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        DateTime dateTimeIn,
        Uuid guidIn,
        byte[] byteStringIn,
        XmlElement xmlElementIn,
        NodeId nodeIdIn,
        ExpandedNodeId expandedNodeIdIn,
        QualifiedName qualifiedNameIn,
        LocalizedText localizedTextIn,
        StatusCode statusCodeIn,
        object variantIn,
        ref DateTime dateTimeOut,
        ref Uuid guidOut,
        ref byte[] byteStringOut,
        ref XmlElement xmlElementOut,
        ref NodeId nodeIdOut,
        ref ExpandedNodeId expandedNodeIdOut,
        ref QualifiedName qualifiedNameOut,
        ref LocalizedText localizedTextOut,
        ref StatusCode statusCodeOut,
        ref object variantOut);
#endif
    #endregion

    #region UserArrayValueObjectState Class
#if !OPCUA_EXCLUDE_UserArrayValueObjectState
    /// <summary>
    /// Stores an instance of the UserArrayValueObjectType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class UserArrayValueObjectState : TestDataObjectState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public UserArrayValueObjectState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return NodeId.Create(ObjectTypes.UserArrayValueObjectType, Namespaces.TestData, namespaceUris);
        }

#if !OPCUA_EXCLUDE_InitializationStrings
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
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGCAAAEAAAABACAAAABVc2VyQXJyYXlW" +
           "YWx1ZU9iamVjdFR5cGVJbnN0YW5jZQEBFycBARcnAQAAAAAkAAEBGycZAAAANWCJCgIAAAABABAAAABT" +
           "aW11bGF0aW9uQWN0aXZlAQEYJwMAAAAARwAAAElmIHRydWUgdGhlIHNlcnZlciB3aWxsIHByb2R1Y2Ug" +
           "bmV3IHZhbHVlcyBmb3IgZWFjaCBtb25pdG9yZWQgdmFyaWFibGUuAC4ARBgnAAAAAf////8BAf////8A" +
           "AAAABGGCCgQAAAABAA4AAABHZW5lcmF0ZVZhbHVlcwEBGScALwEBqSQZJwAAAQH/////AQAAABVgqQoC" +
           "AAAAAAAOAAAASW5wdXRBcmd1bWVudHMBARonAC4ARBonAACWAQAAAAEAKgEBRgAAAAoAAABJdGVyYXRp" +
           "b25zAAf/////AAAAAAMAAAAAJQAAAFRoZSBudW1iZXIgb2YgbmV3IHZhbHVlcyB0byBnZW5lcmF0ZS4B" +
           "ACgBAQAAAAEB/////wAAAAAEYIAKAQAAAAEADQAAAEN5Y2xlQ29tcGxldGUBARsnAC8BAEELGycAAAEA" +
           "AAAAJAEBARcnFwAAADVgiQoCAAAAAAAHAAAARXZlbnRJZAEBHCcDAAAAACsAAABBIGdsb2JhbGx5IHVu" +
           "aXF1ZSBpZGVudGlmaWVyIGZvciB0aGUgZXZlbnQuAC4ARBwnAAAAD/////8BAf////8AAAAANWCJCgIA" +
           "AAAAAAkAAABFdmVudFR5cGUBAR0nAwAAAAAiAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0" +
           "eXBlLgAuAEQdJwAAABH/////AQH/////AAAAADVgiQoCAAAAAAAKAAAAU291cmNlTm9kZQEBHicDAAAA" +
           "ABgAAABUaGUgc291cmNlIG9mIHRoZSBldmVudC4ALgBEHicAAAAR/////wEB/////wAAAAA1YIkKAgAA" +
           "AAAACgAAAFNvdXJjZU5hbWUBAR8nAwAAAAApAAAAQSBkZXNjcmlwdGlvbiBvZiB0aGUgc291cmNlIG9m" +
           "IHRoZSBldmVudC4ALgBEHycAAAAM/////wEB/////wAAAAA1YIkKAgAAAAAABAAAAFRpbWUBASAnAwAA" +
           "AAAYAAAAV2hlbiB0aGUgZXZlbnQgb2NjdXJyZWQuAC4ARCAnAAABACYB/////wEB/////wAAAAA1YIkK" +
           "AgAAAAAACwAAAFJlY2VpdmVUaW1lAQEhJwMAAAAAPgAAAFdoZW4gdGhlIHNlcnZlciByZWNlaXZlZCB0" +
           "aGUgZXZlbnQgZnJvbSB0aGUgdW5kZXJseWluZyBzeXN0ZW0uAC4ARCEnAAABACYB/////wEB/////wAA" +
           "AAA1YIkKAgAAAAAABwAAAE1lc3NhZ2UBASMnAwAAAAAlAAAAQSBsb2NhbGl6ZWQgZGVzY3JpcHRpb24g" +
           "b2YgdGhlIGV2ZW50LgAuAEQjJwAAABX/////AQH/////AAAAADVgiQoCAAAAAAAIAAAAU2V2ZXJpdHkB" +
           "ASQnAwAAAAAhAAAASW5kaWNhdGVzIGhvdyB1cmdlbnQgYW4gZXZlbnQgaXMuAC4ARCQnAAAABf////8B" +
           "Af////8AAAAAFWCJCgIAAAAAABAAAABDb25kaXRpb25DbGFzc0lkAQFGLQAuAERGLQAAABH/////AQH/" +
           "////AAAAABVgiQoCAAAAAAASAAAAQ29uZGl0aW9uQ2xhc3NOYW1lAQFHLQAuAERHLQAAABX/////AQH/" +
           "////AAAAABVgiQoCAAAAAAANAAAAQ29uZGl0aW9uTmFtZQEBKy0ALgBEKy0AAAAM/////wEB/////wAA" +
           "AAAVYIkKAgAAAAAACAAAAEJyYW5jaElkAQElJwAuAEQlJwAAABH/////AQH/////AAAAABVgiQoCAAAA" +
           "AAAGAAAAUmV0YWluAQEmJwAuAEQmJwAAAAH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAARW5hYmxl" +
           "ZFN0YXRlAQEnJwAvAQAjIycnAAAAFf////8BAQIAAAABACwjAAEBPCcBACwjAAEBRCcBAAAAFWCJCgIA" +
           "AAAAAAIAAABJZAEBKCcALgBEKCcAAAAB/////wEB/////wAAAAAVYIkKAgAAAAAABwAAAFF1YWxpdHkB" +
           "AS0nAC8BACojLScAAAAT/////wEB/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVzdGFtcAEB" +
           "LicALgBELicAAAEAJgH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAATGFzdFNldmVyaXR5AQExJwAv" +
           "AQAqIzEnAAAABf////8BAf////8BAAAAFWCJCgIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABATInAC4A" +
           "RDInAAABACYB/////wEB/////wAAAAAVYIkKAgAAAAAABwAAAENvbW1lbnQBATMnAC8BACojMycAAAAV" +
           "/////wEB/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVzdGFtcAEBNCcALgBENCcAAAEAJgH/" +
           "////AQH/////AAAAABVgiQoCAAAAAAAMAAAAQ2xpZW50VXNlcklkAQE1JwAuAEQ1JwAAAAz/////AQH/" +
           "////AAAAAARhggoEAAAAAAAHAAAARGlzYWJsZQEBNycALwEARCM3JwAAAQEBAAAAAQD5CwABAPMKAAAA" +
           "AARhggoEAAAAAAAGAAAARW5hYmxlAQE2JwAvAQBDIzYnAAABAQEAAAABAPkLAAEA8woAAAAABGGCCgQA" +
           "AAAAAAoAAABBZGRDb21tZW50AQE4JwAvAQBFIzgnAAABAQEAAAABAPkLAAEADQsBAAAAFWCpCgIAAAAA" +
           "AA4AAABJbnB1dEFyZ3VtZW50cwEBOScALgBEOScAAJYCAAAAAQAqAQFGAAAABwAAAEV2ZW50SWQAD///" +
           "//8AAAAAAwAAAAAoAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0byBjb21tZW50LgEAKgEB" +
           "QgAAAAcAAABDb21tZW50ABX/////AAAAAAMAAAAAJAAAAFRoZSBjb21tZW50IHRvIGFkZCB0byB0aGUg" +
           "Y29uZGl0aW9uLgEAKAEBAAAAAQH/////AAAAABVgiQoCAAAAAAAKAAAAQWNrZWRTdGF0ZQEBPCcALwEA" +
           "IyM8JwAAABX/////AQEBAAAAAQAsIwEBAScnAQAAABVgiQoCAAAAAAACAAAASWQBAT0nAC4ARD0nAAAA" +
           "Af////8BAf////8AAAAABGGCCgQAAAAAAAsAAABBY2tub3dsZWRnZQEBTCcALwEAlyNMJwAAAQEBAAAA" +
           "AQD5CwABAPAiAQAAABVgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAU0nAC4ARE0nAACWAgAAAAEA" +
           "KgEBRgAAAAcAAABFdmVudElkAA//////AAAAAAMAAAAAKAAAAFRoZSBpZGVudGlmaWVyIGZvciB0aGUg" +
           "ZXZlbnQgdG8gY29tbWVudC4BACoBAUIAAAAHAAAAQ29tbWVudAAV/////wAAAAADAAAAACQAAABUaGUg" +
           "Y29tbWVudCB0byBhZGQgdG8gdGhlIGNvbmRpdGlvbi4BACgBAQAAAAEB/////wAAAAAVYIkKAgAAAAEA" +
           "DAAAAEJvb2xlYW5WYWx1ZQEBUCcALwA/UCcAAAEBqiYBAAAAAQH/////AAAAABVgiQoCAAAAAQAKAAAA" +
           "U0J5dGVWYWx1ZQEBUScALwA/UScAAAEBqyYBAAAAAQH/////AAAAABVgiQoCAAAAAQAJAAAAQnl0ZVZh" +
           "bHVlAQFSJwAvAD9SJwAAAQGsJgEAAAABAf////8AAAAAFWCJCgIAAAABAAoAAABJbnQxNlZhbHVlAQFT" +
           "JwAvAD9TJwAAAQGtJgEAAAABAf////8AAAAAFWCJCgIAAAABAAsAAABVSW50MTZWYWx1ZQEBVCcALwA/" +
           "VCcAAAEBriYBAAAAAQH/////AAAAABVgiQoCAAAAAQAKAAAASW50MzJWYWx1ZQEBVScALwA/VScAAAEB" +
           "ryYBAAAAAQH/////AAAAABVgiQoCAAAAAQALAAAAVUludDMyVmFsdWUBAVYnAC8AP1YnAAABAbAmAQAA" +
           "AAEB/////wAAAAAVYIkKAgAAAAEACgAAAEludDY0VmFsdWUBAVcnAC8AP1cnAAABAbEmAQAAAAEB////" +
           "/wAAAAAVYIkKAgAAAAEACwAAAFVJbnQ2NFZhbHVlAQFYJwAvAD9YJwAAAQGyJgEAAAABAf////8AAAAA" +
           "FWCJCgIAAAABAAoAAABGbG9hdFZhbHVlAQFZJwAvAD9ZJwAAAQGzJgEAAAABAf////8AAAAAFWCJCgIA" +
           "AAABAAsAAABEb3VibGVWYWx1ZQEBWicALwA/WicAAAEBtCYBAAAAAQH/////AAAAABVgiQoCAAAAAQAL" +
           "AAAAU3RyaW5nVmFsdWUBAVsnAC8AP1snAAABAbUmAQAAAAEB/////wAAAAAVYIkKAgAAAAEADQAAAERh" +
           "dGVUaW1lVmFsdWUBAVwnAC8AP1wnAAABAbYmAQAAAAEB/////wAAAAAVYIkKAgAAAAEACQAAAEd1aWRW" +
           "YWx1ZQEBXScALwA/XScAAAEBtyYBAAAAAQH/////AAAAABVgiQoCAAAAAQAPAAAAQnl0ZVN0cmluZ1Zh" +
           "bHVlAQFeJwAvAD9eJwAAAQG4JgEAAAABAf////8AAAAAFWCJCgIAAAABAA8AAABYbWxFbGVtZW50VmFs" +
           "dWUBAV8nAC8AP18nAAABAbkmAQAAAAEB/////wAAAAAVYIkKAgAAAAEACwAAAE5vZGVJZFZhbHVlAQFg" +
           "JwAvAD9gJwAAAQG6JgEAAAABAf////8AAAAAFWCJCgIAAAABABMAAABFeHBhbmRlZE5vZGVJZFZhbHVl" +
           "AQFhJwAvAD9hJwAAAQG7JgEAAAABAf////8AAAAAFWCJCgIAAAABABIAAABRdWFsaWZpZWROYW1lVmFs" +
           "dWUBAWInAC8AP2InAAABAbwmAQAAAAEB/////wAAAAAVYIkKAgAAAAEAEgAAAExvY2FsaXplZFRleHRW" +
           "YWx1ZQEBYycALwA/YycAAAEBvSYBAAAAAQH/////AAAAABVgiQoCAAAAAQAPAAAAU3RhdHVzQ29kZVZh" +
           "bHVlAQFkJwAvAD9kJwAAAQG+JgEAAAABAf////8AAAAAFWCJCgIAAAABAAwAAABWYXJpYW50VmFsdWUB" +
           "AWUnAC8AP2UnAAABAb8mAQAAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the BooleanValue Variable.
        /// </summary>
        public BaseDataVariableState<bool[]> BooleanValue {
            get => m_booleanValue;

            set {
                if (!ReferenceEquals(m_booleanValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_booleanValue = value;
            }
        }

        /// <summary>
        /// A description for the SByteValue Variable.
        /// </summary>
        public BaseDataVariableState<sbyte[]> SByteValue {
            get => m_sByteValue;

            set {
                if (!ReferenceEquals(m_sByteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_sByteValue = value;
            }
        }

        /// <summary>
        /// A description for the ByteValue Variable.
        /// </summary>
        public BaseDataVariableState<byte[]> ByteValue {
            get => m_byteValue;

            set {
                if (!ReferenceEquals(m_byteValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_byteValue = value;
            }
        }

        /// <summary>
        /// A description for the Int16Value Variable.
        /// </summary>
        public BaseDataVariableState<short[]> Int16Value {
            get => m_int16Value;

            set {
                if (!ReferenceEquals(m_int16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int16Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt16Value Variable.
        /// </summary>
        public BaseDataVariableState<ushort[]> UInt16Value {
            get => m_uInt16Value;

            set {
                if (!ReferenceEquals(m_uInt16Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt16Value = value;
            }
        }

        /// <summary>
        /// A description for the Int32Value Variable.
        /// </summary>
        public BaseDataVariableState<int[]> Int32Value {
            get => m_int32Value;

            set {
                if (!ReferenceEquals(m_int32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int32Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt32Value Variable.
        /// </summary>
        public BaseDataVariableState<uint[]> UInt32Value {
            get => m_uInt32Value;

            set {
                if (!ReferenceEquals(m_uInt32Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt32Value = value;
            }
        }

        /// <summary>
        /// A description for the Int64Value Variable.
        /// </summary>
        public BaseDataVariableState<long[]> Int64Value {
            get => m_int64Value;

            set {
                if (!ReferenceEquals(m_int64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_int64Value = value;
            }
        }

        /// <summary>
        /// A description for the UInt64Value Variable.
        /// </summary>
        public BaseDataVariableState<ulong[]> UInt64Value {
            get => m_uInt64Value;

            set {
                if (!ReferenceEquals(m_uInt64Value, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uInt64Value = value;
            }
        }

        /// <summary>
        /// A description for the FloatValue Variable.
        /// </summary>
        public BaseDataVariableState<float[]> FloatValue {
            get => m_floatValue;

            set {
                if (!ReferenceEquals(m_floatValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_floatValue = value;
            }
        }

        /// <summary>
        /// A description for the DoubleValue Variable.
        /// </summary>
        public BaseDataVariableState<double[]> DoubleValue {
            get => m_doubleValue;

            set {
                if (!ReferenceEquals(m_doubleValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_doubleValue = value;
            }
        }

        /// <summary>
        /// A description for the StringValue Variable.
        /// </summary>
        public BaseDataVariableState<string[]> StringValue {
            get => m_stringValue;

            set {
                if (!ReferenceEquals(m_stringValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_stringValue = value;
            }
        }

        /// <summary>
        /// A description for the DateTimeValue Variable.
        /// </summary>
        public BaseDataVariableState<DateTime[]> DateTimeValue {
            get => m_dateTimeValue;

            set {
                if (!ReferenceEquals(m_dateTimeValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_dateTimeValue = value;
            }
        }

        /// <summary>
        /// A description for the GuidValue Variable.
        /// </summary>
        public BaseDataVariableState<Guid[]> GuidValue {
            get => m_guidValue;

            set {
                if (!ReferenceEquals(m_guidValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_guidValue = value;
            }
        }

        /// <summary>
        /// A description for the ByteStringValue Variable.
        /// </summary>
        public BaseDataVariableState<byte[][]> ByteStringValue {
            get => m_byteStringValue;

            set {
                if (!ReferenceEquals(m_byteStringValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_byteStringValue = value;
            }
        }

        /// <summary>
        /// A description for the XmlElementValue Variable.
        /// </summary>
        public BaseDataVariableState<XmlElement[]> XmlElementValue {
            get => m_xmlElementValue;

            set {
                if (!ReferenceEquals(m_xmlElementValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_xmlElementValue = value;
            }
        }

        /// <summary>
        /// A description for the NodeIdValue Variable.
        /// </summary>
        public BaseDataVariableState<NodeId[]> NodeIdValue {
            get => m_nodeIdValue;

            set {
                if (!ReferenceEquals(m_nodeIdValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_nodeIdValue = value;
            }
        }

        /// <summary>
        /// A description for the ExpandedNodeIdValue Variable.
        /// </summary>
        public BaseDataVariableState<ExpandedNodeId[]> ExpandedNodeIdValue {
            get => m_expandedNodeIdValue;

            set {
                if (!ReferenceEquals(m_expandedNodeIdValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_expandedNodeIdValue = value;
            }
        }

        /// <summary>
        /// A description for the QualifiedNameValue Variable.
        /// </summary>
        public BaseDataVariableState<QualifiedName[]> QualifiedNameValue {
            get => m_qualifiedNameValue;

            set {
                if (!ReferenceEquals(m_qualifiedNameValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_qualifiedNameValue = value;
            }
        }

        /// <summary>
        /// A description for the LocalizedTextValue Variable.
        /// </summary>
        public BaseDataVariableState<LocalizedText[]> LocalizedTextValue {
            get => m_localizedTextValue;

            set {
                if (!ReferenceEquals(m_localizedTextValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_localizedTextValue = value;
            }
        }

        /// <summary>
        /// A description for the StatusCodeValue Variable.
        /// </summary>
        public BaseDataVariableState<StatusCode[]> StatusCodeValue {
            get => m_statusCodeValue;

            set {
                if (!ReferenceEquals(m_statusCodeValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_statusCodeValue = value;
            }
        }

        /// <summary>
        /// A description for the VariantValue Variable.
        /// </summary>
        public BaseDataVariableState<object[]> VariantValue {
            get => m_variantValue;

            set {
                if (!ReferenceEquals(m_variantValue, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_variantValue = value;
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
            if (m_booleanValue != null) {
                children.Add(m_booleanValue);
            }

            if (m_sByteValue != null) {
                children.Add(m_sByteValue);
            }

            if (m_byteValue != null) {
                children.Add(m_byteValue);
            }

            if (m_int16Value != null) {
                children.Add(m_int16Value);
            }

            if (m_uInt16Value != null) {
                children.Add(m_uInt16Value);
            }

            if (m_int32Value != null) {
                children.Add(m_int32Value);
            }

            if (m_uInt32Value != null) {
                children.Add(m_uInt32Value);
            }

            if (m_int64Value != null) {
                children.Add(m_int64Value);
            }

            if (m_uInt64Value != null) {
                children.Add(m_uInt64Value);
            }

            if (m_floatValue != null) {
                children.Add(m_floatValue);
            }

            if (m_doubleValue != null) {
                children.Add(m_doubleValue);
            }

            if (m_stringValue != null) {
                children.Add(m_stringValue);
            }

            if (m_dateTimeValue != null) {
                children.Add(m_dateTimeValue);
            }

            if (m_guidValue != null) {
                children.Add(m_guidValue);
            }

            if (m_byteStringValue != null) {
                children.Add(m_byteStringValue);
            }

            if (m_xmlElementValue != null) {
                children.Add(m_xmlElementValue);
            }

            if (m_nodeIdValue != null) {
                children.Add(m_nodeIdValue);
            }

            if (m_expandedNodeIdValue != null) {
                children.Add(m_expandedNodeIdValue);
            }

            if (m_qualifiedNameValue != null) {
                children.Add(m_qualifiedNameValue);
            }

            if (m_localizedTextValue != null) {
                children.Add(m_localizedTextValue);
            }

            if (m_statusCodeValue != null) {
                children.Add(m_statusCodeValue);
            }

            if (m_variantValue != null) {
                children.Add(m_variantValue);
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
                case BrowseNames.BooleanValue: {
                        if (createOrReplace) {
                            if (BooleanValue == null) {
                                if (replacement == null) {
                                    BooleanValue = new BaseDataVariableState<bool[]>(this);
                                }
                                else {
                                    BooleanValue = (BaseDataVariableState<bool[]>)replacement;
                                }
                            }
                        }

                        instance = BooleanValue;
                        break;
                    }

                case BrowseNames.SByteValue: {
                        if (createOrReplace) {
                            if (SByteValue == null) {
                                if (replacement == null) {
                                    SByteValue = new BaseDataVariableState<sbyte[]>(this);
                                }
                                else {
                                    SByteValue = (BaseDataVariableState<sbyte[]>)replacement;
                                }
                            }
                        }

                        instance = SByteValue;
                        break;
                    }

                case BrowseNames.ByteValue: {
                        if (createOrReplace) {
                            if (ByteValue == null) {
                                if (replacement == null) {
                                    ByteValue = new BaseDataVariableState<byte[]>(this);
                                }
                                else {
                                    ByteValue = (BaseDataVariableState<byte[]>)replacement;
                                }
                            }
                        }

                        instance = ByteValue;
                        break;
                    }

                case BrowseNames.Int16Value: {
                        if (createOrReplace) {
                            if (Int16Value == null) {
                                if (replacement == null) {
                                    Int16Value = new BaseDataVariableState<short[]>(this);
                                }
                                else {
                                    Int16Value = (BaseDataVariableState<short[]>)replacement;
                                }
                            }
                        }

                        instance = Int16Value;
                        break;
                    }

                case BrowseNames.UInt16Value: {
                        if (createOrReplace) {
                            if (UInt16Value == null) {
                                if (replacement == null) {
                                    UInt16Value = new BaseDataVariableState<ushort[]>(this);
                                }
                                else {
                                    UInt16Value = (BaseDataVariableState<ushort[]>)replacement;
                                }
                            }
                        }

                        instance = UInt16Value;
                        break;
                    }

                case BrowseNames.Int32Value: {
                        if (createOrReplace) {
                            if (Int32Value == null) {
                                if (replacement == null) {
                                    Int32Value = new BaseDataVariableState<int[]>(this);
                                }
                                else {
                                    Int32Value = (BaseDataVariableState<int[]>)replacement;
                                }
                            }
                        }

                        instance = Int32Value;
                        break;
                    }

                case BrowseNames.UInt32Value: {
                        if (createOrReplace) {
                            if (UInt32Value == null) {
                                if (replacement == null) {
                                    UInt32Value = new BaseDataVariableState<uint[]>(this);
                                }
                                else {
                                    UInt32Value = (BaseDataVariableState<uint[]>)replacement;
                                }
                            }
                        }

                        instance = UInt32Value;
                        break;
                    }

                case BrowseNames.Int64Value: {
                        if (createOrReplace) {
                            if (Int64Value == null) {
                                if (replacement == null) {
                                    Int64Value = new BaseDataVariableState<long[]>(this);
                                }
                                else {
                                    Int64Value = (BaseDataVariableState<long[]>)replacement;
                                }
                            }
                        }

                        instance = Int64Value;
                        break;
                    }

                case BrowseNames.UInt64Value: {
                        if (createOrReplace) {
                            if (UInt64Value == null) {
                                if (replacement == null) {
                                    UInt64Value = new BaseDataVariableState<ulong[]>(this);
                                }
                                else {
                                    UInt64Value = (BaseDataVariableState<ulong[]>)replacement;
                                }
                            }
                        }

                        instance = UInt64Value;
                        break;
                    }

                case BrowseNames.FloatValue: {
                        if (createOrReplace) {
                            if (FloatValue == null) {
                                if (replacement == null) {
                                    FloatValue = new BaseDataVariableState<float[]>(this);
                                }
                                else {
                                    FloatValue = (BaseDataVariableState<float[]>)replacement;
                                }
                            }
                        }

                        instance = FloatValue;
                        break;
                    }

                case BrowseNames.DoubleValue: {
                        if (createOrReplace) {
                            if (DoubleValue == null) {
                                if (replacement == null) {
                                    DoubleValue = new BaseDataVariableState<double[]>(this);
                                }
                                else {
                                    DoubleValue = (BaseDataVariableState<double[]>)replacement;
                                }
                            }
                        }

                        instance = DoubleValue;
                        break;
                    }

                case BrowseNames.StringValue: {
                        if (createOrReplace) {
                            if (StringValue == null) {
                                if (replacement == null) {
                                    StringValue = new BaseDataVariableState<string[]>(this);
                                }
                                else {
                                    StringValue = (BaseDataVariableState<string[]>)replacement;
                                }
                            }
                        }

                        instance = StringValue;
                        break;
                    }

                case BrowseNames.DateTimeValue: {
                        if (createOrReplace) {
                            if (DateTimeValue == null) {
                                if (replacement == null) {
                                    DateTimeValue = new BaseDataVariableState<DateTime[]>(this);
                                }
                                else {
                                    DateTimeValue = (BaseDataVariableState<DateTime[]>)replacement;
                                }
                            }
                        }

                        instance = DateTimeValue;
                        break;
                    }

                case BrowseNames.GuidValue: {
                        if (createOrReplace) {
                            if (GuidValue == null) {
                                if (replacement == null) {
                                    GuidValue = new BaseDataVariableState<Guid[]>(this);
                                }
                                else {
                                    GuidValue = (BaseDataVariableState<Guid[]>)replacement;
                                }
                            }
                        }

                        instance = GuidValue;
                        break;
                    }

                case BrowseNames.ByteStringValue: {
                        if (createOrReplace) {
                            if (ByteStringValue == null) {
                                if (replacement == null) {
                                    ByteStringValue = new BaseDataVariableState<byte[][]>(this);
                                }
                                else {
                                    ByteStringValue = (BaseDataVariableState<byte[][]>)replacement;
                                }
                            }
                        }

                        instance = ByteStringValue;
                        break;
                    }

                case BrowseNames.XmlElementValue: {
                        if (createOrReplace) {
                            if (XmlElementValue == null) {
                                if (replacement == null) {
                                    XmlElementValue = new BaseDataVariableState<XmlElement[]>(this);
                                }
                                else {
                                    XmlElementValue = (BaseDataVariableState<XmlElement[]>)replacement;
                                }
                            }
                        }

                        instance = XmlElementValue;
                        break;
                    }

                case BrowseNames.NodeIdValue: {
                        if (createOrReplace) {
                            if (NodeIdValue == null) {
                                if (replacement == null) {
                                    NodeIdValue = new BaseDataVariableState<NodeId[]>(this);
                                }
                                else {
                                    NodeIdValue = (BaseDataVariableState<NodeId[]>)replacement;
                                }
                            }
                        }

                        instance = NodeIdValue;
                        break;
                    }

                case BrowseNames.ExpandedNodeIdValue: {
                        if (createOrReplace) {
                            if (ExpandedNodeIdValue == null) {
                                if (replacement == null) {
                                    ExpandedNodeIdValue = new BaseDataVariableState<ExpandedNodeId[]>(this);
                                }
                                else {
                                    ExpandedNodeIdValue = (BaseDataVariableState<ExpandedNodeId[]>)replacement;
                                }
                            }
                        }

                        instance = ExpandedNodeIdValue;
                        break;
                    }

                case BrowseNames.QualifiedNameValue: {
                        if (createOrReplace) {
                            if (QualifiedNameValue == null) {
                                if (replacement == null) {
                                    QualifiedNameValue = new BaseDataVariableState<QualifiedName[]>(this);
                                }
                                else {
                                    QualifiedNameValue = (BaseDataVariableState<QualifiedName[]>)replacement;
                                }
                            }
                        }

                        instance = QualifiedNameValue;
                        break;
                    }

                case BrowseNames.LocalizedTextValue: {
                        if (createOrReplace) {
                            if (LocalizedTextValue == null) {
                                if (replacement == null) {
                                    LocalizedTextValue = new BaseDataVariableState<LocalizedText[]>(this);
                                }
                                else {
                                    LocalizedTextValue = (BaseDataVariableState<LocalizedText[]>)replacement;
                                }
                            }
                        }

                        instance = LocalizedTextValue;
                        break;
                    }

                case BrowseNames.StatusCodeValue: {
                        if (createOrReplace) {
                            if (StatusCodeValue == null) {
                                if (replacement == null) {
                                    StatusCodeValue = new BaseDataVariableState<StatusCode[]>(this);
                                }
                                else {
                                    StatusCodeValue = (BaseDataVariableState<StatusCode[]>)replacement;
                                }
                            }
                        }

                        instance = StatusCodeValue;
                        break;
                    }

                case BrowseNames.VariantValue: {
                        if (createOrReplace) {
                            if (VariantValue == null) {
                                if (replacement == null) {
                                    VariantValue = new BaseDataVariableState<object[]>(this);
                                }
                                else {
                                    VariantValue = (BaseDataVariableState<object[]>)replacement;
                                }
                            }
                        }

                        instance = VariantValue;
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
        private BaseDataVariableState<bool[]> m_booleanValue;
        private BaseDataVariableState<sbyte[]> m_sByteValue;
        private BaseDataVariableState<byte[]> m_byteValue;
        private BaseDataVariableState<short[]> m_int16Value;
        private BaseDataVariableState<ushort[]> m_uInt16Value;
        private BaseDataVariableState<int[]> m_int32Value;
        private BaseDataVariableState<uint[]> m_uInt32Value;
        private BaseDataVariableState<long[]> m_int64Value;
        private BaseDataVariableState<ulong[]> m_uInt64Value;
        private BaseDataVariableState<float[]> m_floatValue;
        private BaseDataVariableState<double[]> m_doubleValue;
        private BaseDataVariableState<string[]> m_stringValue;
        private BaseDataVariableState<DateTime[]> m_dateTimeValue;
        private BaseDataVariableState<Guid[]> m_guidValue;
        private BaseDataVariableState<byte[][]> m_byteStringValue;
        private BaseDataVariableState<XmlElement[]> m_xmlElementValue;
        private BaseDataVariableState<NodeId[]> m_nodeIdValue;
        private BaseDataVariableState<ExpandedNodeId[]> m_expandedNodeIdValue;
        private BaseDataVariableState<QualifiedName[]> m_qualifiedNameValue;
        private BaseDataVariableState<LocalizedText[]> m_localizedTextValue;
        private BaseDataVariableState<StatusCode[]> m_statusCodeValue;
        private BaseDataVariableState<object[]> m_variantValue;
        #endregion
    }
#endif
    #endregion

    #region UserArrayValue1MethodState Class
#if (!OPCUA_EXCLUDE_UserArrayValue1MethodState)
    /// <summary>
    /// Stores an instance of the UserArrayValue1MethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class UserArrayValue1MethodState : MethodState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public UserArrayValue1MethodState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent) {
            return new UserArrayValue1MethodState(parent);
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
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGGCCgQAAAABABkAAABVc2VyQXJyYXlW" +
           "YWx1ZTFNZXRob2RUeXBlAQFmJwAvAQFmJ2YnAAABAf////8CAAAAFWCpCgIAAAAAAA4AAABJbnB1dEFy" +
           "Z3VtZW50cwEBZycALgBEZycAAJYMAAAAAQAqAQEaAAAACQAAAEJvb2xlYW5JbgEBqiYBAAAAAAAAAAAB" +
           "ACoBARgAAAAHAAAAU0J5dGVJbgEBqyYBAAAAAAAAAAABACoBARcAAAAGAAAAQnl0ZUluAQGsJgEAAAAA" +
           "AAAAAAEAKgEBGAAAAAcAAABJbnQxNkluAQGtJgEAAAAAAAAAAAEAKgEBGQAAAAgAAABVSW50MTZJbgEB" +
           "riYBAAAAAAAAAAABACoBARgAAAAHAAAASW50MzJJbgEBryYBAAAAAAAAAAABACoBARkAAAAIAAAAVUlu" +
           "dDMySW4BAbAmAQAAAAAAAAAAAQAqAQEYAAAABwAAAEludDY0SW4BAbEmAQAAAAAAAAAAAQAqAQEZAAAA" +
           "CAAAAFVJbnQ2NEluAQGyJgEAAAAAAAAAAAEAKgEBGAAAAAcAAABGbG9hdEluAQGzJgEAAAAAAAAAAAEA" +
           "KgEBGQAAAAgAAABEb3VibGVJbgEBtCYBAAAAAAAAAAABACoBARkAAAAIAAAAU3RyaW5nSW4BAbUmAQAA" +
           "AAAAAAAAAQAoAQEAAAABAf////8AAAAAFWCpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAWgnAC4A" +
           "RGgnAACWDAAAAAEAKgEBGwAAAAoAAABCb29sZWFuT3V0AQGqJgEAAAAAAAAAAAEAKgEBGQAAAAgAAABT" +
           "Qnl0ZU91dAEBqyYBAAAAAAAAAAABACoBARgAAAAHAAAAQnl0ZU91dAEBrCYBAAAAAAAAAAABACoBARkA" +
           "AAAIAAAASW50MTZPdXQBAa0mAQAAAAAAAAAAAQAqAQEaAAAACQAAAFVJbnQxNk91dAEBriYBAAAAAAAA" +
           "AAABACoBARkAAAAIAAAASW50MzJPdXQBAa8mAQAAAAAAAAAAAQAqAQEaAAAACQAAAFVJbnQzMk91dAEB" +
           "sCYBAAAAAAAAAAABACoBARkAAAAIAAAASW50NjRPdXQBAbEmAQAAAAAAAAAAAQAqAQEaAAAACQAAAFVJ" +
           "bnQ2NE91dAEBsiYBAAAAAAAAAAABACoBARkAAAAIAAAARmxvYXRPdXQBAbMmAQAAAAAAAAAAAQAqAQEa" +
           "AAAACQAAAERvdWJsZU91dAEBtCYBAAAAAAAAAAABACoBARoAAAAJAAAAU3RyaW5nT3V0AQG1JgEAAAAA" +
           "AAAAAAEAKAEBAAAAAQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public UserArrayValue1MethodStateMethodCallHandler OnCall;
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
            IList<object> outputArguments) {
            if (OnCall == null) {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            bool[] booleanIn = (bool[])inputArguments[0];
            sbyte[] sByteIn = (sbyte[])inputArguments[1];
            byte[] byteIn = (byte[])inputArguments[2];
            short[] int16In = (short[])inputArguments[3];
            ushort[] uInt16In = (ushort[])inputArguments[4];
            int[] int32In = (int[])inputArguments[5];
            uint[] uInt32In = (uint[])inputArguments[6];
            long[] int64In = (long[])inputArguments[7];
            ulong[] uInt64In = (ulong[])inputArguments[8];
            float[] floatIn = (float[])inputArguments[9];
            double[] doubleIn = (double[])inputArguments[10];
            string[] stringIn = (string[])inputArguments[11];

            bool[] booleanOut = (bool[])outputArguments[0];
            sbyte[] sByteOut = (sbyte[])outputArguments[1];
            byte[] byteOut = (byte[])outputArguments[2];
            short[] int16Out = (short[])outputArguments[3];
            ushort[] uInt16Out = (ushort[])outputArguments[4];
            int[] int32Out = (int[])outputArguments[5];
            uint[] uInt32Out = (uint[])outputArguments[6];
            long[] int64Out = (long[])outputArguments[7];
            ulong[] uInt64Out = (ulong[])outputArguments[8];
            float[] floatOut = (float[])outputArguments[9];
            double[] doubleOut = (double[])outputArguments[10];
            string[] stringOut = (string[])outputArguments[11];

            if (OnCall != null) {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    booleanIn,
                    sByteIn,
                    byteIn,
                    int16In,
                    uInt16In,
                    int32In,
                    uInt32In,
                    int64In,
                    uInt64In,
                    floatIn,
                    doubleIn,
                    stringIn,
                    ref booleanOut,
                    ref sByteOut,
                    ref byteOut,
                    ref int16Out,
                    ref uInt16Out,
                    ref int32Out,
                    ref uInt32Out,
                    ref int64Out,
                    ref uInt64Out,
                    ref floatOut,
                    ref doubleOut,
                    ref stringOut);
            }

            outputArguments[0] = booleanOut;
            outputArguments[1] = sByteOut;
            outputArguments[2] = byteOut;
            outputArguments[3] = int16Out;
            outputArguments[4] = uInt16Out;
            outputArguments[5] = int32Out;
            outputArguments[6] = uInt32Out;
            outputArguments[7] = int64Out;
            outputArguments[8] = uInt64Out;
            outputArguments[9] = floatOut;
            outputArguments[10] = doubleOut;
            outputArguments[11] = stringOut;

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
    public delegate ServiceResult UserArrayValue1MethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        bool[] booleanIn,
        sbyte[] sByteIn,
        byte[] byteIn,
        short[] int16In,
        ushort[] uInt16In,
        int[] int32In,
        uint[] uInt32In,
        long[] int64In,
        ulong[] uInt64In,
        float[] floatIn,
        double[] doubleIn,
        string[] stringIn,
        ref bool[] booleanOut,
        ref sbyte[] sByteOut,
        ref byte[] byteOut,
        ref short[] int16Out,
        ref ushort[] uInt16Out,
        ref int[] int32Out,
        ref uint[] uInt32Out,
        ref long[] int64Out,
        ref ulong[] uInt64Out,
        ref float[] floatOut,
        ref double[] doubleOut,
        ref string[] stringOut);
#endif
    #endregion

    #region UserArrayValue2MethodState Class
#if (!OPCUA_EXCLUDE_UserArrayValue2MethodState)
    /// <summary>
    /// Stores an instance of the UserArrayValue2MethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class UserArrayValue2MethodState : MethodState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public UserArrayValue2MethodState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent) {
            return new UserArrayValue2MethodState(parent);
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
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context) {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGGCCgQAAAABABkAAABVc2VyQXJyYXlW" +
           "YWx1ZTJNZXRob2RUeXBlAQFpJwAvAQFpJ2knAAABAf////8CAAAAFWCpCgIAAAAAAA4AAABJbnB1dEFy" +
           "Z3VtZW50cwEBaicALgBEaicAAJYKAAAAAQAqAQEbAAAACgAAAERhdGVUaW1lSW4BAbYmAQAAAAAAAAAA" +
           "AQAqAQEXAAAABgAAAEd1aWRJbgEBtyYBAAAAAAAAAAABACoBAR0AAAAMAAAAQnl0ZVN0cmluZ0luAQG4" +
           "JgEAAAAAAAAAAAEAKgEBHQAAAAwAAABYbWxFbGVtZW50SW4BAbkmAQAAAAAAAAAAAQAqAQEZAAAACAAA" +
           "AE5vZGVJZEluAQG6JgEAAAAAAAAAAAEAKgEBIQAAABAAAABFeHBhbmRlZE5vZGVJZEluAQG7JgEAAAAA" +
           "AAAAAAEAKgEBIAAAAA8AAABRdWFsaWZpZWROYW1lSW4BAbwmAQAAAAAAAAAAAQAqAQEgAAAADwAAAExv" +
           "Y2FsaXplZFRleHRJbgEBvSYBAAAAAAAAAAABACoBAR0AAAAMAAAAU3RhdHVzQ29kZUluAQG+JgEAAAAA" +
           "AAAAAAEAKgEBGgAAAAkAAABWYXJpYW50SW4BAb8mAQAAAAAAAAAAAQAoAQEAAAABAf////8AAAAAFWCp" +
           "CgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAWsnAC4ARGsnAACWCgAAAAEAKgEBHAAAAAsAAABEYXRl" +
           "VGltZU91dAEBtiYBAAAAAAAAAAABACoBARgAAAAHAAAAR3VpZE91dAEBtyYBAAAAAAAAAAABACoBAR4A" +
           "AAANAAAAQnl0ZVN0cmluZ091dAEBuCYBAAAAAAAAAAABACoBAR4AAAANAAAAWG1sRWxlbWVudE91dAEB" +
           "uSYBAAAAAAAAAAABACoBARoAAAAJAAAATm9kZUlkT3V0AQG6JgEAAAAAAAAAAAEAKgEBIgAAABEAAABF" +
           "eHBhbmRlZE5vZGVJZE91dAEBuyYBAAAAAAAAAAABACoBASEAAAAQAAAAUXVhbGlmaWVkTmFtZU91dAEB" +
           "vCYBAAAAAAAAAAABACoBASEAAAAQAAAATG9jYWxpemVkVGV4dE91dAEBvSYBAAAAAAAAAAABACoBAR4A" +
           "AAANAAAAU3RhdHVzQ29kZU91dAEBviYBAAAAAAAAAAABACoBARsAAAAKAAAAVmFyaWFudE91dAEBvyYB" +
           "AAAAAAAAAAABACgBAQAAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public UserArrayValue2MethodStateMethodCallHandler OnCall;
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
            IList<object> outputArguments) {
            if (OnCall == null) {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            DateTime[] dateTimeIn = (DateTime[])inputArguments[0];
            Uuid[] guidIn = (Uuid[])inputArguments[1];
            byte[][] byteStringIn = (byte[][])inputArguments[2];
            XmlElement[] xmlElementIn = (XmlElement[])inputArguments[3];
            NodeId[] nodeIdIn = (NodeId[])inputArguments[4];
            ExpandedNodeId[] expandedNodeIdIn = (ExpandedNodeId[])inputArguments[5];
            QualifiedName[] qualifiedNameIn = (QualifiedName[])inputArguments[6];
            LocalizedText[] localizedTextIn = (LocalizedText[])inputArguments[7];
            StatusCode[] statusCodeIn = (StatusCode[])inputArguments[8];
            Variant[] variantIn = (Variant[])inputArguments[9];

            DateTime[] dateTimeOut = (DateTime[])outputArguments[0];
            Uuid[] guidOut = (Uuid[])outputArguments[1];
            byte[][] byteStringOut = (byte[][])outputArguments[2];
            XmlElement[] xmlElementOut = (XmlElement[])outputArguments[3];
            NodeId[] nodeIdOut = (NodeId[])outputArguments[4];
            ExpandedNodeId[] expandedNodeIdOut = (ExpandedNodeId[])outputArguments[5];
            QualifiedName[] qualifiedNameOut = (QualifiedName[])outputArguments[6];
            LocalizedText[] localizedTextOut = (LocalizedText[])outputArguments[7];
            StatusCode[] statusCodeOut = (StatusCode[])outputArguments[8];
            Variant[] variantOut = (Variant[])outputArguments[9];

            if (OnCall != null) {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    dateTimeIn,
                    guidIn,
                    byteStringIn,
                    xmlElementIn,
                    nodeIdIn,
                    expandedNodeIdIn,
                    qualifiedNameIn,
                    localizedTextIn,
                    statusCodeIn,
                    variantIn,
                    ref dateTimeOut,
                    ref guidOut,
                    ref byteStringOut,
                    ref xmlElementOut,
                    ref nodeIdOut,
                    ref expandedNodeIdOut,
                    ref qualifiedNameOut,
                    ref localizedTextOut,
                    ref statusCodeOut,
                    ref variantOut);
            }

            outputArguments[0] = dateTimeOut;
            outputArguments[1] = guidOut;
            outputArguments[2] = byteStringOut;
            outputArguments[3] = xmlElementOut;
            outputArguments[4] = nodeIdOut;
            outputArguments[5] = expandedNodeIdOut;
            outputArguments[6] = qualifiedNameOut;
            outputArguments[7] = localizedTextOut;
            outputArguments[8] = statusCodeOut;
            outputArguments[9] = variantOut;

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
    public delegate ServiceResult UserArrayValue2MethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        DateTime[] dateTimeIn,
        Uuid[] guidIn,
        byte[][] byteStringIn,
        XmlElement[] xmlElementIn,
        NodeId[] nodeIdIn,
        ExpandedNodeId[] expandedNodeIdIn,
        QualifiedName[] qualifiedNameIn,
        LocalizedText[] localizedTextIn,
        StatusCode[] statusCodeIn,
        Variant[] variantIn,
        ref DateTime[] dateTimeOut,
        ref Uuid[] guidOut,
        ref byte[][] byteStringOut,
        ref XmlElement[] xmlElementOut,
        ref NodeId[] nodeIdOut,
        ref ExpandedNodeId[] expandedNodeIdOut,
        ref QualifiedName[] qualifiedNameOut,
        ref LocalizedText[] localizedTextOut,
        ref StatusCode[] statusCodeOut,
        ref Variant[] variantOut);
#endif
    #endregion

    #region MethodTestState Class
#if !OPCUA_EXCLUDE_MethodTestState
    /// <summary>
    /// Stores an instance of the MethodTestType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class MethodTestState : FolderState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public MethodTestState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return NodeId.Create(ObjectTypes.MethodTestType, Namespaces.TestData, namespaceUris);
        }

#if !OPCUA_EXCLUDE_InitializationStrings
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
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGCAAAEAAAABABYAAABNZXRob2RUZXN0" +
           "VHlwZUluc3RhbmNlAQFsJwEBbCf/////CgAAAARhggoEAAAAAQANAAAAU2NhbGFyTWV0aG9kMQEBbScA" +
           "LwEBbSdtJwAAAQH/////AgAAABVgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAW4nAC4ARG4nAACW" +
           "CwAAAAEAKgEBGAAAAAkAAABCb29sZWFuSW4AAf////8AAAAAAAEAKgEBFgAAAAcAAABTQnl0ZUluAAL/" +
           "////AAAAAAABACoBARUAAAAGAAAAQnl0ZUluAAP/////AAAAAAABACoBARYAAAAHAAAASW50MTZJbgAE" +
           "/////wAAAAAAAQAqAQEXAAAACAAAAFVJbnQxNkluAAX/////AAAAAAABACoBARYAAAAHAAAASW50MzJJ" +
           "bgAG/////wAAAAAAAQAqAQEXAAAACAAAAFVJbnQzMkluAAf/////AAAAAAABACoBARYAAAAHAAAASW50" +
           "NjRJbgAI/////wAAAAAAAQAqAQEXAAAACAAAAFVJbnQ2NEluAAn/////AAAAAAABACoBARYAAAAHAAAA" +
           "RmxvYXRJbgAK/////wAAAAAAAQAqAQEXAAAACAAAAERvdWJsZUluAAv/////AAAAAAABACgBAQAAAAEB" +
           "/////wAAAAAVYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBbycALgBEbycAAJYLAAAAAQAqAQEZ" +
           "AAAACgAAAEJvb2xlYW5PdXQAAf////8AAAAAAAEAKgEBFwAAAAgAAABTQnl0ZU91dAAC/////wAAAAAA" +
           "AQAqAQEWAAAABwAAAEJ5dGVPdXQAA/////8AAAAAAAEAKgEBFwAAAAgAAABJbnQxNk91dAAE/////wAA" +
           "AAAAAQAqAQEYAAAACQAAAFVJbnQxNk91dAAF/////wAAAAAAAQAqAQEXAAAACAAAAEludDMyT3V0AAb/" +
           "////AAAAAAABACoBARgAAAAJAAAAVUludDMyT3V0AAf/////AAAAAAABACoBARcAAAAIAAAASW50NjRP" +
           "dXQACP////8AAAAAAAEAKgEBGAAAAAkAAABVSW50NjRPdXQACf////8AAAAAAAEAKgEBFwAAAAgAAABG" +
           "bG9hdE91dAAK/////wAAAAAAAQAqAQEYAAAACQAAAERvdWJsZU91dAAL/////wAAAAAAAQAoAQEAAAAB" +
           "Af////8AAAAABGGCCgQAAAABAA0AAABTY2FsYXJNZXRob2QyAQFwJwAvAQFwJ3AnAAABAf////8CAAAA" +
           "FWCpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBcScALgBEcScAAJYKAAAAAQAqAQEXAAAACAAAAFN0" +
           "cmluZ0luAAz/////AAAAAAABACoBARkAAAAKAAAARGF0ZVRpbWVJbgAN/////wAAAAAAAQAqAQEVAAAA" +
           "BgAAAEd1aWRJbgAO/////wAAAAAAAQAqAQEbAAAADAAAAEJ5dGVTdHJpbmdJbgAP/////wAAAAAAAQAq" +
           "AQEbAAAADAAAAFhtbEVsZW1lbnRJbgAQ/////wAAAAAAAQAqAQEXAAAACAAAAE5vZGVJZEluABH/////" +
           "AAAAAAABACoBAR8AAAAQAAAARXhwYW5kZWROb2RlSWRJbgAS/////wAAAAAAAQAqAQEeAAAADwAAAFF1" +
           "YWxpZmllZE5hbWVJbgAU/////wAAAAAAAQAqAQEeAAAADwAAAExvY2FsaXplZFRleHRJbgAV/////wAA" +
           "AAAAAQAqAQEbAAAADAAAAFN0YXR1c0NvZGVJbgAT/////wAAAAAAAQAoAQEAAAABAf////8AAAAAFWCp" +
           "CgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAXInAC4ARHInAACWCgAAAAEAKgEBGAAAAAkAAABTdHJp" +
           "bmdPdXQADP////8AAAAAAAEAKgEBGgAAAAsAAABEYXRlVGltZU91dAAN/////wAAAAAAAQAqAQEWAAAA" +
           "BwAAAEd1aWRPdXQADv////8AAAAAAAEAKgEBHAAAAA0AAABCeXRlU3RyaW5nT3V0AA//////AAAAAAAB" +
           "ACoBARwAAAANAAAAWG1sRWxlbWVudE91dAAQ/////wAAAAAAAQAqAQEYAAAACQAAAE5vZGVJZE91dAAR" +
           "/////wAAAAAAAQAqAQEgAAAAEQAAAEV4cGFuZGVkTm9kZUlkT3V0ABL/////AAAAAAABACoBAR8AAAAQ" +
           "AAAAUXVhbGlmaWVkTmFtZU91dAAU/////wAAAAAAAQAqAQEfAAAAEAAAAExvY2FsaXplZFRleHRPdXQA" +
           "Ff////8AAAAAAAEAKgEBHAAAAA0AAABTdGF0dXNDb2RlT3V0ABP/////AAAAAAABACgBAQAAAAEB////" +
           "/wAAAAAEYYIKBAAAAAEADQAAAFNjYWxhck1ldGhvZDMBAXMnAC8BAXMncycAAAEB/////wIAAAAVYKkK" +
           "AgAAAAAADgAAAElucHV0QXJndW1lbnRzAQF0JwAuAER0JwAAlgMAAAABACoBARgAAAAJAAAAVmFyaWFu" +
           "dEluABj/////AAAAAAABACoBARwAAAANAAAARW51bWVyYXRpb25JbgAd/////wAAAAAAAQAqAQEaAAAA" +
           "CwAAAFN0cnVjdHVyZUluABb/////AAAAAAABACgBAQAAAAEB/////wAAAAAVYKkKAgAAAAAADwAAAE91" +
           "dHB1dEFyZ3VtZW50cwEBdScALgBEdScAAJYDAAAAAQAqAQEZAAAACgAAAFZhcmlhbnRPdXQAGP////8A" +
           "AAAAAAEAKgEBHQAAAA4AAABFbnVtZXJhdGlvbk91dAAd/////wAAAAAAAQAqAQEbAAAADAAAAFN0cnVj" +
           "dHVyZU91dAAW/////wAAAAAAAQAoAQEAAAABAf////8AAAAABGGCCgQAAAABAAwAAABBcnJheU1ldGhv" +
           "ZDEBAXYnAC8BAXYndicAAAEB/////wIAAAAVYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQF3JwAu" +
           "AER3JwAAlgsAAAABACoBARgAAAAJAAAAQm9vbGVhbkluAAEBAAAAAAAAAAABACoBARYAAAAHAAAAU0J5" +
           "dGVJbgACAQAAAAAAAAAAAQAqAQEVAAAABgAAAEJ5dGVJbgADAQAAAAAAAAAAAQAqAQEWAAAABwAAAElu" +
           "dDE2SW4ABAEAAAAAAAAAAAEAKgEBFwAAAAgAAABVSW50MTZJbgAFAQAAAAAAAAAAAQAqAQEWAAAABwAA" +
           "AEludDMySW4ABgEAAAAAAAAAAAEAKgEBFwAAAAgAAABVSW50MzJJbgAHAQAAAAAAAAAAAQAqAQEWAAAA" +
           "BwAAAEludDY0SW4ACAEAAAAAAAAAAAEAKgEBFwAAAAgAAABVSW50NjRJbgAJAQAAAAAAAAAAAQAqAQEW" +
           "AAAABwAAAEZsb2F0SW4ACgEAAAAAAAAAAAEAKgEBFwAAAAgAAABEb3VibGVJbgALAQAAAAAAAAAAAQAo" +
           "AQEAAAABAf////8AAAAAFWCpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAXgnAC4ARHgnAACWCwAA" +
           "AAEAKgEBGQAAAAoAAABCb29sZWFuT3V0AAEBAAAAAAAAAAABACoBARcAAAAIAAAAU0J5dGVPdXQAAgEA" +
           "AAAAAAAAAAEAKgEBFgAAAAcAAABCeXRlT3V0AAMBAAAAAAAAAAABACoBARcAAAAIAAAASW50MTZPdXQA" +
           "BAEAAAAAAAAAAAEAKgEBGAAAAAkAAABVSW50MTZPdXQABQEAAAAAAAAAAAEAKgEBFwAAAAgAAABJbnQz" +
           "Mk91dAAGAQAAAAAAAAAAAQAqAQEYAAAACQAAAFVJbnQzMk91dAAHAQAAAAAAAAAAAQAqAQEXAAAACAAA" +
           "AEludDY0T3V0AAgBAAAAAAAAAAABACoBARgAAAAJAAAAVUludDY0T3V0AAkBAAAAAAAAAAABACoBARcA" +
           "AAAIAAAARmxvYXRPdXQACgEAAAAAAAAAAAEAKgEBGAAAAAkAAABEb3VibGVPdXQACwEAAAAAAAAAAAEA" +
           "KAEBAAAAAQH/////AAAAAARhggoEAAAAAQAMAAAAQXJyYXlNZXRob2QyAQF5JwAvAQF5J3knAAABAf//" +
           "//8CAAAAFWCpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBeicALgBEeicAAJYKAAAAAQAqAQEXAAAA" +
           "CAAAAFN0cmluZ0luAAwBAAAAAAAAAAABACoBARkAAAAKAAAARGF0ZVRpbWVJbgANAQAAAAAAAAAAAQAq" +
           "AQEVAAAABgAAAEd1aWRJbgAOAQAAAAAAAAAAAQAqAQEbAAAADAAAAEJ5dGVTdHJpbmdJbgAPAQAAAAAA" +
           "AAAAAQAqAQEbAAAADAAAAFhtbEVsZW1lbnRJbgAQAQAAAAAAAAAAAQAqAQEXAAAACAAAAE5vZGVJZElu" +
           "ABEBAAAAAAAAAAABACoBAR8AAAAQAAAARXhwYW5kZWROb2RlSWRJbgASAQAAAAAAAAAAAQAqAQEeAAAA" +
           "DwAAAFF1YWxpZmllZE5hbWVJbgAUAQAAAAAAAAAAAQAqAQEeAAAADwAAAExvY2FsaXplZFRleHRJbgAV" +
           "AQAAAAAAAAAAAQAqAQEbAAAADAAAAFN0YXR1c0NvZGVJbgATAQAAAAAAAAAAAQAoAQEAAAABAf////8A" +
           "AAAAFWCpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAXsnAC4ARHsnAACWCgAAAAEAKgEBGAAAAAkA" +
           "AABTdHJpbmdPdXQADAEAAAAAAAAAAAEAKgEBGgAAAAsAAABEYXRlVGltZU91dAANAQAAAAAAAAAAAQAq" +
           "AQEWAAAABwAAAEd1aWRPdXQADgEAAAAAAAAAAAEAKgEBHAAAAA0AAABCeXRlU3RyaW5nT3V0AA8BAAAA" +
           "AAAAAAABACoBARwAAAANAAAAWG1sRWxlbWVudE91dAAQAQAAAAAAAAAAAQAqAQEYAAAACQAAAE5vZGVJ" +
           "ZE91dAARAQAAAAAAAAAAAQAqAQEgAAAAEQAAAEV4cGFuZGVkTm9kZUlkT3V0ABIBAAAAAAAAAAABACoB" +
           "AR8AAAAQAAAAUXVhbGlmaWVkTmFtZU91dAAUAQAAAAAAAAAAAQAqAQEfAAAAEAAAAExvY2FsaXplZFRl" +
           "eHRPdXQAFQEAAAAAAAAAAAEAKgEBHAAAAA0AAABTdGF0dXNDb2RlT3V0ABMBAAAAAAAAAAABACgBAQAA" +
           "AAEB/////wAAAAAEYYIKBAAAAAEADAAAAEFycmF5TWV0aG9kMwEBfCcALwEBfCd8JwAAAQH/////AgAA" +
           "ABVgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAX0nAC4ARH0nAACWAwAAAAEAKgEBGAAAAAkAAABW" +
           "YXJpYW50SW4AGAEAAAAAAAAAAAEAKgEBHAAAAA0AAABFbnVtZXJhdGlvbkluAB0BAAAAAAAAAAABACoB" +
           "ARoAAAALAAAAU3RydWN0dXJlSW4AFgEAAAAAAAAAAAEAKAEBAAAAAQH/////AAAAABVgqQoCAAAAAAAP" +
           "AAAAT3V0cHV0QXJndW1lbnRzAQF+JwAuAER+JwAAlgMAAAABACoBARkAAAAKAAAAVmFyaWFudE91dAAY" +
           "AQAAAAAAAAAAAQAqAQEdAAAADgAAAEVudW1lcmF0aW9uT3V0AB0BAAAAAAAAAAABACoBARsAAAAMAAAA" +
           "U3RydWN0dXJlT3V0ABYBAAAAAAAAAAABACgBAQAAAAEB/////wAAAAAEYYIKBAAAAAEAEQAAAFVzZXJT" +
           "Y2FsYXJNZXRob2QxAQF/JwAvAQF/J38nAAABAf////8CAAAAFWCpCgIAAAAAAA4AAABJbnB1dEFyZ3Vt" +
           "ZW50cwEBgCcALgBEgCcAAJYMAAAAAQAqAQEaAAAACQAAAEJvb2xlYW5JbgEBqib/////AAAAAAABACoB" +
           "ARgAAAAHAAAAU0J5dGVJbgEBqyb/////AAAAAAABACoBARcAAAAGAAAAQnl0ZUluAQGsJv////8AAAAA" +
           "AAEAKgEBGAAAAAcAAABJbnQxNkluAQGtJv////8AAAAAAAEAKgEBGQAAAAgAAABVSW50MTZJbgEBrib/" +
           "////AAAAAAABACoBARgAAAAHAAAASW50MzJJbgEBryb/////AAAAAAABACoBARkAAAAIAAAAVUludDMy" +
           "SW4BAbAm/////wAAAAAAAQAqAQEYAAAABwAAAEludDY0SW4BAbEm/////wAAAAAAAQAqAQEZAAAACAAA" +
           "AFVJbnQ2NEluAQGyJv////8AAAAAAAEAKgEBGAAAAAcAAABGbG9hdEluAQGzJv////8AAAAAAAEAKgEB" +
           "GQAAAAgAAABEb3VibGVJbgEBtCb/////AAAAAAABACoBARkAAAAIAAAAU3RyaW5nSW4BAbUm/////wAA" +
           "AAAAAQAoAQEAAAABAf////8AAAAAFWCpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAYEnAC4ARIEn" +
           "AACWDAAAAAEAKgEBGwAAAAoAAABCb29sZWFuT3V0AQGqJv////8AAAAAAAEAKgEBGQAAAAgAAABTQnl0" +
           "ZU91dAEBqyb/////AAAAAAABACoBARgAAAAHAAAAQnl0ZU91dAEBrCb/////AAAAAAABACoBARkAAAAI" +
           "AAAASW50MTZPdXQBAa0m/////wAAAAAAAQAqAQEaAAAACQAAAFVJbnQxNk91dAEBrib/////AAAAAAAB" +
           "ACoBARkAAAAIAAAASW50MzJPdXQBAa8m/////wAAAAAAAQAqAQEaAAAACQAAAFVJbnQzMk91dAEBsCb/" +
           "////AAAAAAABACoBARkAAAAIAAAASW50NjRPdXQBAbEm/////wAAAAAAAQAqAQEaAAAACQAAAFVJbnQ2" +
           "NE91dAEBsib/////AAAAAAABACoBARkAAAAIAAAARmxvYXRPdXQBAbMm/////wAAAAAAAQAqAQEaAAAA" +
           "CQAAAERvdWJsZU91dAEBtCb/////AAAAAAABACoBARoAAAAJAAAAU3RyaW5nT3V0AQG1Jv////8AAAAA" +
           "AAEAKAEBAAAAAQH/////AAAAAARhggoEAAAAAQARAAAAVXNlclNjYWxhck1ldGhvZDIBAYInAC8BAYIn" +
           "gicAAAEB/////wIAAAAVYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQGDJwAuAESDJwAAlgoAAAAB" +
           "ACoBARsAAAAKAAAARGF0ZVRpbWVJbgEBtib/////AAAAAAABACoBARcAAAAGAAAAR3VpZEluAQG3Jv//" +
           "//8AAAAAAAEAKgEBHQAAAAwAAABCeXRlU3RyaW5nSW4BAbgm/////wAAAAAAAQAqAQEdAAAADAAAAFht" +
           "bEVsZW1lbnRJbgEBuSb/////AAAAAAABACoBARkAAAAIAAAATm9kZUlkSW4BAbom/////wAAAAAAAQAq" +
           "AQEhAAAAEAAAAEV4cGFuZGVkTm9kZUlkSW4BAbsm/////wAAAAAAAQAqAQEgAAAADwAAAFF1YWxpZmll" +
           "ZE5hbWVJbgEBvCb/////AAAAAAABACoBASAAAAAPAAAATG9jYWxpemVkVGV4dEluAQG9Jv////8AAAAA" +
           "AAEAKgEBHQAAAAwAAABTdGF0dXNDb2RlSW4BAb4m/////wAAAAAAAQAqAQEaAAAACQAAAFZhcmlhbnRJ" +
           "bgEBvyb/////AAAAAAABACgBAQAAAAEB/////wAAAAAVYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50" +
           "cwEBhCcALgBEhCcAAJYKAAAAAQAqAQEcAAAACwAAAERhdGVUaW1lT3V0AQG2Jv////8AAAAAAAEAKgEB" +
           "GAAAAAcAAABHdWlkT3V0AQG3Jv////8AAAAAAAEAKgEBHgAAAA0AAABCeXRlU3RyaW5nT3V0AQG4Jv//" +
           "//8AAAAAAAEAKgEBHgAAAA0AAABYbWxFbGVtZW50T3V0AQG5Jv////8AAAAAAAEAKgEBGgAAAAkAAABO" +
           "b2RlSWRPdXQBAbom/////wAAAAAAAQAqAQEiAAAAEQAAAEV4cGFuZGVkTm9kZUlkT3V0AQG7Jv////8A" +
           "AAAAAAEAKgEBIQAAABAAAABRdWFsaWZpZWROYW1lT3V0AQG8Jv////8AAAAAAAEAKgEBIQAAABAAAABM" +
           "b2NhbGl6ZWRUZXh0T3V0AQG9Jv////8AAAAAAAEAKgEBHgAAAA0AAABTdGF0dXNDb2RlT3V0AQG+Jv//" +
           "//8AAAAAAAEAKgEBGwAAAAoAAABWYXJpYW50T3V0AQG/Jv////8AAAAAAAEAKAEBAAAAAQH/////AAAA" +
           "AARhggoEAAAAAQAQAAAAVXNlckFycmF5TWV0aG9kMQEBhScALwEBhSeFJwAAAQH/////AgAAABVgqQoC" +
           "AAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAYYnAC4ARIYnAACWDAAAAAEAKgEBGgAAAAkAAABCb29sZWFu" +
           "SW4BAaomAQAAAAAAAAAAAQAqAQEYAAAABwAAAFNCeXRlSW4BAasmAQAAAAAAAAAAAQAqAQEXAAAABgAA" +
           "AEJ5dGVJbgEBrCYBAAAAAAAAAAABACoBARgAAAAHAAAASW50MTZJbgEBrSYBAAAAAAAAAAABACoBARkA" +
           "AAAIAAAAVUludDE2SW4BAa4mAQAAAAAAAAAAAQAqAQEYAAAABwAAAEludDMySW4BAa8mAQAAAAAAAAAA" +
           "AQAqAQEZAAAACAAAAFVJbnQzMkluAQGwJgEAAAAAAAAAAAEAKgEBGAAAAAcAAABJbnQ2NEluAQGxJgEA" +
           "AAAAAAAAAAEAKgEBGQAAAAgAAABVSW50NjRJbgEBsiYBAAAAAAAAAAABACoBARgAAAAHAAAARmxvYXRJ" +
           "bgEBsyYBAAAAAAAAAAABACoBARkAAAAIAAAARG91YmxlSW4BAbQmAQAAAAAAAAAAAQAqAQEZAAAACAAA" +
           "AFN0cmluZ0luAQG1JgEAAAAAAAAAAAEAKAEBAAAAAQH/////AAAAABVgqQoCAAAAAAAPAAAAT3V0cHV0" +
           "QXJndW1lbnRzAQGHJwAuAESHJwAAlgwAAAABACoBARsAAAAKAAAAQm9vbGVhbk91dAEBqiYBAAAAAAAA" +
           "AAABACoBARkAAAAIAAAAU0J5dGVPdXQBAasmAQAAAAAAAAAAAQAqAQEYAAAABwAAAEJ5dGVPdXQBAawm" +
           "AQAAAAAAAAAAAQAqAQEZAAAACAAAAEludDE2T3V0AQGtJgEAAAAAAAAAAAEAKgEBGgAAAAkAAABVSW50" +
           "MTZPdXQBAa4mAQAAAAAAAAAAAQAqAQEZAAAACAAAAEludDMyT3V0AQGvJgEAAAAAAAAAAAEAKgEBGgAA" +
           "AAkAAABVSW50MzJPdXQBAbAmAQAAAAAAAAAAAQAqAQEZAAAACAAAAEludDY0T3V0AQGxJgEAAAAAAAAA" +
           "AAEAKgEBGgAAAAkAAABVSW50NjRPdXQBAbImAQAAAAAAAAAAAQAqAQEZAAAACAAAAEZsb2F0T3V0AQGz" +
           "JgEAAAAAAAAAAAEAKgEBGgAAAAkAAABEb3VibGVPdXQBAbQmAQAAAAAAAAAAAQAqAQEaAAAACQAAAFN0" +
           "cmluZ091dAEBtSYBAAAAAAAAAAABACgBAQAAAAEB/////wAAAAAEYYIKBAAAAAEAEAAAAFVzZXJBcnJh" +
           "eU1ldGhvZDIBAYgnAC8BAYgniCcAAAEB/////wIAAAAVYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRz" +
           "AQGJJwAuAESJJwAAlgoAAAABACoBARsAAAAKAAAARGF0ZVRpbWVJbgEBtiYBAAAAAAAAAAABACoBARcA" +
           "AAAGAAAAR3VpZEluAQG3JgEAAAAAAAAAAAEAKgEBHQAAAAwAAABCeXRlU3RyaW5nSW4BAbgmAQAAAAAA" +
           "AAAAAQAqAQEdAAAADAAAAFhtbEVsZW1lbnRJbgEBuSYBAAAAAAAAAAABACoBARkAAAAIAAAATm9kZUlk" +
           "SW4BAbomAQAAAAAAAAAAAQAqAQEhAAAAEAAAAEV4cGFuZGVkTm9kZUlkSW4BAbsmAQAAAAAAAAAAAQAq" +
           "AQEgAAAADwAAAFF1YWxpZmllZE5hbWVJbgEBvCYBAAAAAAAAAAABACoBASAAAAAPAAAATG9jYWxpemVk" +
           "VGV4dEluAQG9JgEAAAAAAAAAAAEAKgEBHQAAAAwAAABTdGF0dXNDb2RlSW4BAb4mAQAAAAAAAAAAAQAq" +
           "AQEaAAAACQAAAFZhcmlhbnRJbgEBvyYBAAAAAAAAAAABACgBAQAAAAEB/////wAAAAAVYKkKAgAAAAAA" +
           "DwAAAE91dHB1dEFyZ3VtZW50cwEBiicALgBEiicAAJYKAAAAAQAqAQEcAAAACwAAAERhdGVUaW1lT3V0" +
           "AQG2JgEAAAAAAAAAAAEAKgEBGAAAAAcAAABHdWlkT3V0AQG3JgEAAAAAAAAAAAEAKgEBHgAAAA0AAABC" +
           "eXRlU3RyaW5nT3V0AQG4JgEAAAAAAAAAAAEAKgEBHgAAAA0AAABYbWxFbGVtZW50T3V0AQG5JgEAAAAA" +
           "AAAAAAEAKgEBGgAAAAkAAABOb2RlSWRPdXQBAbomAQAAAAAAAAAAAQAqAQEiAAAAEQAAAEV4cGFuZGVk" +
           "Tm9kZUlkT3V0AQG7JgEAAAAAAAAAAAEAKgEBIQAAABAAAABRdWFsaWZpZWROYW1lT3V0AQG8JgEAAAAA" +
           "AAAAAAEAKgEBIQAAABAAAABMb2NhbGl6ZWRUZXh0T3V0AQG9JgEAAAAAAAAAAAEAKgEBHgAAAA0AAABT" +
           "dGF0dXNDb2RlT3V0AQG+JgEAAAAAAAAAAAEAKgEBGwAAAAoAAABWYXJpYW50T3V0AQG/JgEAAAAAAAAA" +
           "AAEAKAEBAAAAAQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the ScalarValue1MethodType Method.
        /// </summary>
        public ScalarValue1MethodState ScalarMethod1 {
            get => m_scalarMethod1Method;

            set {
                if (!ReferenceEquals(m_scalarMethod1Method, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_scalarMethod1Method = value;
            }
        }

        /// <summary>
        /// A description for the ScalarValue2MethodType Method.
        /// </summary>
        public ScalarValue2MethodState ScalarMethod2 {
            get => m_scalarMethod2Method;

            set {
                if (!ReferenceEquals(m_scalarMethod2Method, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_scalarMethod2Method = value;
            }
        }

        /// <summary>
        /// A description for the ScalarValue3MethodType Method.
        /// </summary>
        public ScalarValue3MethodState ScalarMethod3 {
            get => m_scalarMethod3Method;

            set {
                if (!ReferenceEquals(m_scalarMethod3Method, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_scalarMethod3Method = value;
            }
        }

        /// <summary>
        /// A description for the ArrayValue1MethodType Method.
        /// </summary>
        public ArrayValue1MethodState ArrayMethod1 {
            get => m_arrayMethod1Method;

            set {
                if (!ReferenceEquals(m_arrayMethod1Method, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_arrayMethod1Method = value;
            }
        }

        /// <summary>
        /// A description for the ArrayValue2MethodType Method.
        /// </summary>
        public ArrayValue2MethodState ArrayMethod2 {
            get => m_arrayMethod2Method;

            set {
                if (!ReferenceEquals(m_arrayMethod2Method, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_arrayMethod2Method = value;
            }
        }

        /// <summary>
        /// A description for the ArrayValue3MethodType Method.
        /// </summary>
        public ArrayValue3MethodState ArrayMethod3 {
            get => m_arrayMethod3Method;

            set {
                if (!ReferenceEquals(m_arrayMethod3Method, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_arrayMethod3Method = value;
            }
        }

        /// <summary>
        /// A description for the UserScalarValue1MethodType Method.
        /// </summary>
        public UserScalarValue1MethodState UserScalarMethod1 {
            get => m_userScalarMethod1Method;

            set {
                if (!ReferenceEquals(m_userScalarMethod1Method, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_userScalarMethod1Method = value;
            }
        }

        /// <summary>
        /// A description for the UserScalarValue2MethodType Method.
        /// </summary>
        public UserScalarValue2MethodState UserScalarMethod2 {
            get => m_userScalarMethod2Method;

            set {
                if (!ReferenceEquals(m_userScalarMethod2Method, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_userScalarMethod2Method = value;
            }
        }

        /// <summary>
        /// A description for the UserArrayValue1MethodType Method.
        /// </summary>
        public UserArrayValue1MethodState UserArrayMethod1 {
            get => m_userArrayMethod1Method;

            set {
                if (!ReferenceEquals(m_userArrayMethod1Method, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_userArrayMethod1Method = value;
            }
        }

        /// <summary>
        /// A description for the UserArrayValue2MethodType Method.
        /// </summary>
        public UserArrayValue2MethodState UserArrayMethod2 {
            get => m_userArrayMethod2Method;

            set {
                if (!ReferenceEquals(m_userArrayMethod2Method, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_userArrayMethod2Method = value;
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
            if (m_scalarMethod1Method != null) {
                children.Add(m_scalarMethod1Method);
            }

            if (m_scalarMethod2Method != null) {
                children.Add(m_scalarMethod2Method);
            }

            if (m_scalarMethod3Method != null) {
                children.Add(m_scalarMethod3Method);
            }

            if (m_arrayMethod1Method != null) {
                children.Add(m_arrayMethod1Method);
            }

            if (m_arrayMethod2Method != null) {
                children.Add(m_arrayMethod2Method);
            }

            if (m_arrayMethod3Method != null) {
                children.Add(m_arrayMethod3Method);
            }

            if (m_userScalarMethod1Method != null) {
                children.Add(m_userScalarMethod1Method);
            }

            if (m_userScalarMethod2Method != null) {
                children.Add(m_userScalarMethod2Method);
            }

            if (m_userArrayMethod1Method != null) {
                children.Add(m_userArrayMethod1Method);
            }

            if (m_userArrayMethod2Method != null) {
                children.Add(m_userArrayMethod2Method);
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
                case BrowseNames.ScalarMethod1: {
                        if (createOrReplace) {
                            if (ScalarMethod1 == null) {
                                if (replacement == null) {
                                    ScalarMethod1 = new ScalarValue1MethodState(this);
                                }
                                else {
                                    ScalarMethod1 = (ScalarValue1MethodState)replacement;
                                }
                            }
                        }

                        instance = ScalarMethod1;
                        break;
                    }

                case BrowseNames.ScalarMethod2: {
                        if (createOrReplace) {
                            if (ScalarMethod2 == null) {
                                if (replacement == null) {
                                    ScalarMethod2 = new ScalarValue2MethodState(this);
                                }
                                else {
                                    ScalarMethod2 = (ScalarValue2MethodState)replacement;
                                }
                            }
                        }

                        instance = ScalarMethod2;
                        break;
                    }

                case BrowseNames.ScalarMethod3: {
                        if (createOrReplace) {
                            if (ScalarMethod3 == null) {
                                if (replacement == null) {
                                    ScalarMethod3 = new ScalarValue3MethodState(this);
                                }
                                else {
                                    ScalarMethod3 = (ScalarValue3MethodState)replacement;
                                }
                            }
                        }

                        instance = ScalarMethod3;
                        break;
                    }

                case BrowseNames.ArrayMethod1: {
                        if (createOrReplace) {
                            if (ArrayMethod1 == null) {
                                if (replacement == null) {
                                    ArrayMethod1 = new ArrayValue1MethodState(this);
                                }
                                else {
                                    ArrayMethod1 = (ArrayValue1MethodState)replacement;
                                }
                            }
                        }

                        instance = ArrayMethod1;
                        break;
                    }

                case BrowseNames.ArrayMethod2: {
                        if (createOrReplace) {
                            if (ArrayMethod2 == null) {
                                if (replacement == null) {
                                    ArrayMethod2 = new ArrayValue2MethodState(this);
                                }
                                else {
                                    ArrayMethod2 = (ArrayValue2MethodState)replacement;
                                }
                            }
                        }

                        instance = ArrayMethod2;
                        break;
                    }

                case BrowseNames.ArrayMethod3: {
                        if (createOrReplace) {
                            if (ArrayMethod3 == null) {
                                if (replacement == null) {
                                    ArrayMethod3 = new ArrayValue3MethodState(this);
                                }
                                else {
                                    ArrayMethod3 = (ArrayValue3MethodState)replacement;
                                }
                            }
                        }

                        instance = ArrayMethod3;
                        break;
                    }

                case BrowseNames.UserScalarMethod1: {
                        if (createOrReplace) {
                            if (UserScalarMethod1 == null) {
                                if (replacement == null) {
                                    UserScalarMethod1 = new UserScalarValue1MethodState(this);
                                }
                                else {
                                    UserScalarMethod1 = (UserScalarValue1MethodState)replacement;
                                }
                            }
                        }

                        instance = UserScalarMethod1;
                        break;
                    }

                case BrowseNames.UserScalarMethod2: {
                        if (createOrReplace) {
                            if (UserScalarMethod2 == null) {
                                if (replacement == null) {
                                    UserScalarMethod2 = new UserScalarValue2MethodState(this);
                                }
                                else {
                                    UserScalarMethod2 = (UserScalarValue2MethodState)replacement;
                                }
                            }
                        }

                        instance = UserScalarMethod2;
                        break;
                    }

                case BrowseNames.UserArrayMethod1: {
                        if (createOrReplace) {
                            if (UserArrayMethod1 == null) {
                                if (replacement == null) {
                                    UserArrayMethod1 = new UserArrayValue1MethodState(this);
                                }
                                else {
                                    UserArrayMethod1 = (UserArrayValue1MethodState)replacement;
                                }
                            }
                        }

                        instance = UserArrayMethod1;
                        break;
                    }

                case BrowseNames.UserArrayMethod2: {
                        if (createOrReplace) {
                            if (UserArrayMethod2 == null) {
                                if (replacement == null) {
                                    UserArrayMethod2 = new UserArrayValue2MethodState(this);
                                }
                                else {
                                    UserArrayMethod2 = (UserArrayValue2MethodState)replacement;
                                }
                            }
                        }

                        instance = UserArrayMethod2;
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
        private ScalarValue1MethodState m_scalarMethod1Method;
        private ScalarValue2MethodState m_scalarMethod2Method;
        private ScalarValue3MethodState m_scalarMethod3Method;
        private ArrayValue1MethodState m_arrayMethod1Method;
        private ArrayValue2MethodState m_arrayMethod2Method;
        private ArrayValue3MethodState m_arrayMethod3Method;
        private UserScalarValue1MethodState m_userScalarMethod1Method;
        private UserScalarValue2MethodState m_userScalarMethod2Method;
        private UserArrayValue1MethodState m_userArrayMethod1Method;
        private UserArrayValue2MethodState m_userArrayMethod2Method;
        #endregion
    }
#endif
    #endregion

    #region TestSystemConditionState Class
#if !OPCUA_EXCLUDE_TestSystemConditionState
    /// <summary>
    /// Stores an instance of the TestSystemConditionType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class TestSystemConditionState : ConditionState {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public TestSystemConditionState(NodeState parent) : base(parent) {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris) {
            return NodeId.Create(ObjectTypes.TestSystemConditionType, Namespaces.TestData, namespaceUris);
        }

#if !OPCUA_EXCLUDE_InitializationStrings
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
           "AQAAABgAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS//////BGCAAAEAAAABAB8AAABUZXN0U3lzdGVt" +
           "Q29uZGl0aW9uVHlwZUluc3RhbmNlAQGLJwEBiyf/////FgAAADVgiQoCAAAAAAAHAAAARXZlbnRJZAEB" +
           "jCcDAAAAACsAAABBIGdsb2JhbGx5IHVuaXF1ZSBpZGVudGlmaWVyIGZvciB0aGUgZXZlbnQuAC4ARIwn" +
           "AAAAD/////8BAf////8AAAAANWCJCgIAAAAAAAkAAABFdmVudFR5cGUBAY0nAwAAAAAiAAAAVGhlIGlk" +
           "ZW50aWZpZXIgZm9yIHRoZSBldmVudCB0eXBlLgAuAESNJwAAABH/////AQH/////AAAAADVgiQoCAAAA" +
           "AAAKAAAAU291cmNlTm9kZQEBjicDAAAAABgAAABUaGUgc291cmNlIG9mIHRoZSBldmVudC4ALgBEjicA" +
           "AAAR/////wEB/////wAAAAA1YIkKAgAAAAAACgAAAFNvdXJjZU5hbWUBAY8nAwAAAAApAAAAQSBkZXNj" +
           "cmlwdGlvbiBvZiB0aGUgc291cmNlIG9mIHRoZSBldmVudC4ALgBEjycAAAAM/////wEB/////wAAAAA1" +
           "YIkKAgAAAAAABAAAAFRpbWUBAZAnAwAAAAAYAAAAV2hlbiB0aGUgZXZlbnQgb2NjdXJyZWQuAC4ARJAn" +
           "AAABACYB/////wEB/////wAAAAA1YIkKAgAAAAAACwAAAFJlY2VpdmVUaW1lAQGRJwMAAAAAPgAAAFdo" +
           "ZW4gdGhlIHNlcnZlciByZWNlaXZlZCB0aGUgZXZlbnQgZnJvbSB0aGUgdW5kZXJseWluZyBzeXN0ZW0u" +
           "AC4ARJEnAAABACYB/////wEB/////wAAAAA1YIkKAgAAAAAABwAAAE1lc3NhZ2UBAZMnAwAAAAAlAAAA" +
           "QSBsb2NhbGl6ZWQgZGVzY3JpcHRpb24gb2YgdGhlIGV2ZW50LgAuAESTJwAAABX/////AQH/////AAAA" +
           "ADVgiQoCAAAAAAAIAAAAU2V2ZXJpdHkBAZQnAwAAAAAhAAAASW5kaWNhdGVzIGhvdyB1cmdlbnQgYW4g" +
           "ZXZlbnQgaXMuAC4ARJQnAAAABf////8BAf////8AAAAAFWCJCgIAAAAAABAAAABDb25kaXRpb25DbGFz" +
           "c0lkAQFILQAuAERILQAAABH/////AQH/////AAAAABVgiQoCAAAAAAASAAAAQ29uZGl0aW9uQ2xhc3NO" +
           "YW1lAQFJLQAuAERJLQAAABX/////AQH/////AAAAABVgiQoCAAAAAAANAAAAQ29uZGl0aW9uTmFtZQEB" +
           "LC0ALgBELC0AAAAM/////wEB/////wAAAAAVYIkKAgAAAAAACAAAAEJyYW5jaElkAQGVJwAuAESVJwAA" +
           "ABH/////AQH/////AAAAABVgiQoCAAAAAAAGAAAAUmV0YWluAQGWJwAuAESWJwAAAAH/////AQH/////" +
           "AAAAABVgiQoCAAAAAAAMAAAARW5hYmxlZFN0YXRlAQGXJwAvAQAjI5cnAAAAFf////8BAf////8BAAAA" +
           "FWCJCgIAAAAAAAIAAABJZAEBmCcALgBEmCcAAAAB/////wEB/////wAAAAAVYIkKAgAAAAAABwAAAFF1" +
           "YWxpdHkBAZ0nAC8BACojnScAAAAT/////wEB/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVz" +
           "dGFtcAEBnicALgBEnicAAAEAJgH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAATGFzdFNldmVyaXR5" +
           "AQGhJwAvAQAqI6EnAAAABf////8BAf////8BAAAAFWCJCgIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXAB" +
           "AaInAC4ARKInAAABACYB/////wEB/////wAAAAAVYIkKAgAAAAAABwAAAENvbW1lbnQBAaMnAC8BACoj" +
           "oycAAAAV/////wEB/////wEAAAAVYIkKAgAAAAAADwAAAFNvdXJjZVRpbWVzdGFtcAEBpCcALgBEpCcA" +
           "AAEAJgH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAAQ2xpZW50VXNlcklkAQGlJwAuAESlJwAAAAz/" +
           "////AQH/////AAAAAARhggoEAAAAAAAHAAAARGlzYWJsZQEBpycALwEARCOnJwAAAQEBAAAAAQD5CwAB" +
           "APMKAAAAAARhggoEAAAAAAAGAAAARW5hYmxlAQGmJwAvAQBDI6YnAAABAQEAAAABAPkLAAEA8woAAAAA" +
           "BGGCCgQAAAAAAAoAAABBZGRDb21tZW50AQGoJwAvAQBFI6gnAAABAQEAAAABAPkLAAEADQsBAAAAFWCp" +
           "CgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBqScALgBEqScAAJYCAAAAAQAqAQFGAAAABwAAAEV2ZW50" +
           "SWQAD/////8AAAAAAwAAAAAoAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0byBjb21tZW50" +
           "LgEAKgEBQgAAAAcAAABDb21tZW50ABX/////AAAAAAMAAAAAJAAAAFRoZSBjb21tZW50IHRvIGFkZCB0" +
           "byB0aGUgY29uZGl0aW9uLgEAKAEBAAAAAQH/////AAAAABVgiQoCAAAAAQASAAAATW9uaXRvcmVkTm9k" +
           "ZUNvdW50AQGsJwAuAESsJwAAAAb/////AQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the MonitoredNodeCount Property.
        /// </summary>
        public PropertyState<int> MonitoredNodeCount {
            get => m_monitoredNodeCount;

            set {
                if (!ReferenceEquals(m_monitoredNodeCount, value)) {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_monitoredNodeCount = value;
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
            if (m_monitoredNodeCount != null) {
                children.Add(m_monitoredNodeCount);
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
                case BrowseNames.MonitoredNodeCount: {
                        if (createOrReplace) {
                            if (MonitoredNodeCount == null) {
                                if (replacement == null) {
                                    MonitoredNodeCount = new PropertyState<int>(this);
                                }
                                else {
                                    MonitoredNodeCount = (PropertyState<int>)replacement;
                                }
                            }
                        }

                        instance = MonitoredNodeCount;
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
        private PropertyState<int> m_monitoredNodeCount;
        #endregion
    }
#endif
    #endregion
}