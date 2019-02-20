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

namespace HistoricalEvents {
    #region WellTestReportState Class
#if (!OPCUA_EXCLUDE_WellTestReportState)
    /// <summary>
    /// Stores an instance of the WellTestReportType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class WellTestReportState : BaseEventState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public WellTestReportState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(HistoricalEvents.ObjectTypes.WellTestReportType, HistoricalEvents.Namespaces.HistoricalEvents, namespaceUris);
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
           "AQAAACkAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvSGlzdG9yaWNhbEV2ZW50c/////8EYIAAAQAA" +
           "AAEAGgAAAFdlbGxUZXN0UmVwb3J0VHlwZUluc3RhbmNlAQH7AAEB+wD/////DAAAADVgiQoCAAAAAAAH" +
           "AAAARXZlbnRJZAEB/AADAAAAACsAAABBIGdsb2JhbGx5IHVuaXF1ZSBpZGVudGlmaWVyIGZvciB0aGUg" +
           "ZXZlbnQuAC4ARPwAAAAAD/////8BAf////8AAAAANWCJCgIAAAAAAAkAAABFdmVudFR5cGUBAf0AAwAA" +
           "AAAiAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0eXBlLgAuAET9AAAAABH/////AQH/////" +
           "AAAAADVgiQoCAAAAAAAKAAAAU291cmNlTm9kZQEB/gADAAAAABgAAABUaGUgc291cmNlIG9mIHRoZSBl" +
           "dmVudC4ALgBE/gAAAAAR/////wEB/////wAAAAA1YIkKAgAAAAAACgAAAFNvdXJjZU5hbWUBAf8AAwAA" +
           "AAApAAAAQSBkZXNjcmlwdGlvbiBvZiB0aGUgc291cmNlIG9mIHRoZSBldmVudC4ALgBE/wAAAAAM////" +
           "/wEB/////wAAAAA1YIkKAgAAAAAABAAAAFRpbWUBAQABAwAAAAAYAAAAV2hlbiB0aGUgZXZlbnQgb2Nj" +
           "dXJyZWQuAC4ARAABAAABACYB/////wEB/////wAAAAA1YIkKAgAAAAAACwAAAFJlY2VpdmVUaW1lAQEB" +
           "AQMAAAAAPgAAAFdoZW4gdGhlIHNlcnZlciByZWNlaXZlZCB0aGUgZXZlbnQgZnJvbSB0aGUgdW5kZXJs" +
           "eWluZyBzeXN0ZW0uAC4ARAEBAAABACYB/////wEB/////wAAAAA1YIkKAgAAAAAABwAAAE1lc3NhZ2UB" +
           "AQMBAwAAAAAlAAAAQSBsb2NhbGl6ZWQgZGVzY3JpcHRpb24gb2YgdGhlIGV2ZW50LgAuAEQDAQAAABX/" +
           "////AQH/////AAAAADVgiQoCAAAAAAAIAAAAU2V2ZXJpdHkBAQQBAwAAAAAhAAAASW5kaWNhdGVzIGhv" +
           "dyB1cmdlbnQgYW4gZXZlbnQgaXMuAC4ARAQBAAAABf////8BAf////8AAAAANWCJCgIAAAABAAgAAABO" +
           "YW1lV2VsbAEBBQEDAAAAAEQAAABIdW1hbiByZWNvZ25pemFibGUgY29udGV4dCBmb3IgdGhlIHdlbGwg" +
           "dGhhdCBjb250YWlucyB0aGUgd2VsbCB0ZXN0LgAuAEQFAQAAAAz/////AQH/////AAAAADVgiQoCAAAA" +
           "AQAHAAAAVWlkV2VsbAEBBgEDAAAAAHMAAABVbmlxdWUgaWRlbnRpZmllciBmb3IgdGhlIHdlbGwuIFRo" +
           "aXMgdW5pcXVlbHkgcmVwcmVzZW50cyB0aGUgd2VsbCByZWZlcmVuY2VkIGJ5IHRoZSAocG9zc2libHkg" +
           "bm9uLXVuaXF1ZSkgTmFtZVdlbGwuAC4ARAYBAAAADP////8BAf////8AAAAANWCJCgIAAAABAAgAAABU" +
           "ZXN0RGF0ZQEBBwEDAAAAABsAAABUaGUgZGF0ZS10aW1lIG9mIHdlbGwgdGVzdC4ALgBEBwEAAAAN////" +
           "/wEB/////wAAAAA1YIkKAgAAAAEACgAAAFRlc3RSZWFzb24BAQgBAwAAAAA6AAAAVGhlIHJlYXNvbiBm" +
           "b3IgdGhlIHdlbGwgdGVzdDogaW5pdGlhbCwgcGVyaW9kaWMsIHJldmlzaW9uLgAuAEQIAQAAAAz/////" +
           "AQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// Human recognizable context for the well that contains the well test.
        /// </summary>
        public PropertyState<string> NameWell
        {
            get
            {
                return m_nameWell;
            }

            set
            {
                if (!Object.ReferenceEquals(m_nameWell, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_nameWell = value;
            }
        }

        /// <summary>
        /// Unique identifier for the well. This uniquely represents the well referenced by the (possibly non-unique) NameWell.
        /// </summary>
        public PropertyState<string> UidWell
        {
            get
            {
                return m_uidWell;
            }

            set
            {
                if (!Object.ReferenceEquals(m_uidWell, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uidWell = value;
            }
        }

        /// <summary>
        /// The date-time of well test.
        /// </summary>
        public PropertyState<DateTime> TestDate
        {
            get
            {
                return m_testDate;
            }

            set
            {
                if (!Object.ReferenceEquals(m_testDate, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_testDate = value;
            }
        }

        /// <summary>
        /// The reason for the well test: initial, periodic, revision.
        /// </summary>
        public PropertyState<string> TestReason
        {
            get
            {
                return m_testReason;
            }

            set
            {
                if (!Object.ReferenceEquals(m_testReason, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_testReason = value;
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
            if (m_nameWell != null)
            {
                children.Add(m_nameWell);
            }

            if (m_uidWell != null)
            {
                children.Add(m_uidWell);
            }

            if (m_testDate != null)
            {
                children.Add(m_testDate);
            }

            if (m_testReason != null)
            {
                children.Add(m_testReason);
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
                case HistoricalEvents.BrowseNames.NameWell:
                {
                    if (createOrReplace)
                    {
                        if (NameWell == null)
                        {
                            if (replacement == null)
                            {
                                NameWell = new PropertyState<string>(this);
                            }
                            else
                            {
                                NameWell = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = NameWell;
                    break;
                }

                case HistoricalEvents.BrowseNames.UidWell:
                {
                    if (createOrReplace)
                    {
                        if (UidWell == null)
                        {
                            if (replacement == null)
                            {
                                UidWell = new PropertyState<string>(this);
                            }
                            else
                            {
                                UidWell = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = UidWell;
                    break;
                }

                case HistoricalEvents.BrowseNames.TestDate:
                {
                    if (createOrReplace)
                    {
                        if (TestDate == null)
                        {
                            if (replacement == null)
                            {
                                TestDate = new PropertyState<DateTime>(this);
                            }
                            else
                            {
                                TestDate = (PropertyState<DateTime>)replacement;
                            }
                        }
                    }

                    instance = TestDate;
                    break;
                }

                case HistoricalEvents.BrowseNames.TestReason:
                {
                    if (createOrReplace)
                    {
                        if (TestReason == null)
                        {
                            if (replacement == null)
                            {
                                TestReason = new PropertyState<string>(this);
                            }
                            else
                            {
                                TestReason = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = TestReason;
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
        private PropertyState<string> m_nameWell;
        private PropertyState<string> m_uidWell;
        private PropertyState<DateTime> m_testDate;
        private PropertyState<string> m_testReason;
        #endregion
    }
    #endif
    #endregion

    #region FluidLevelTestReportState Class
    #if (!OPCUA_EXCLUDE_FluidLevelTestReportState)
    /// <summary>
    /// Stores an instance of the FluidLevelTestReportType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class FluidLevelTestReportState : WellTestReportState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public FluidLevelTestReportState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(HistoricalEvents.ObjectTypes.FluidLevelTestReportType, HistoricalEvents.Namespaces.HistoricalEvents, namespaceUris);
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
           "AQAAACkAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvSGlzdG9yaWNhbEV2ZW50c/////8EYIAAAQAA" +
           "AAEAIAAAAEZsdWlkTGV2ZWxUZXN0UmVwb3J0VHlwZUluc3RhbmNlAQEJAQEBCQH/////DgAAADVgiQoC" +
           "AAAAAAAHAAAARXZlbnRJZAEBCgEDAAAAACsAAABBIGdsb2JhbGx5IHVuaXF1ZSBpZGVudGlmaWVyIGZv" +
           "ciB0aGUgZXZlbnQuAC4ARAoBAAAAD/////8BAf////8AAAAANWCJCgIAAAAAAAkAAABFdmVudFR5cGUB" +
           "AQsBAwAAAAAiAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0eXBlLgAuAEQLAQAAABH/////" +
           "AQH/////AAAAADVgiQoCAAAAAAAKAAAAU291cmNlTm9kZQEBDAEDAAAAABgAAABUaGUgc291cmNlIG9m" +
           "IHRoZSBldmVudC4ALgBEDAEAAAAR/////wEB/////wAAAAA1YIkKAgAAAAAACgAAAFNvdXJjZU5hbWUB" +
           "AQ0BAwAAAAApAAAAQSBkZXNjcmlwdGlvbiBvZiB0aGUgc291cmNlIG9mIHRoZSBldmVudC4ALgBEDQEA" +
           "AAAM/////wEB/////wAAAAA1YIkKAgAAAAAABAAAAFRpbWUBAQ4BAwAAAAAYAAAAV2hlbiB0aGUgZXZl" +
           "bnQgb2NjdXJyZWQuAC4ARA4BAAABACYB/////wEB/////wAAAAA1YIkKAgAAAAAACwAAAFJlY2VpdmVU" +
           "aW1lAQEPAQMAAAAAPgAAAFdoZW4gdGhlIHNlcnZlciByZWNlaXZlZCB0aGUgZXZlbnQgZnJvbSB0aGUg" +
           "dW5kZXJseWluZyBzeXN0ZW0uAC4ARA8BAAABACYB/////wEB/////wAAAAA1YIkKAgAAAAAABwAAAE1l" +
           "c3NhZ2UBAREBAwAAAAAlAAAAQSBsb2NhbGl6ZWQgZGVzY3JpcHRpb24gb2YgdGhlIGV2ZW50LgAuAEQR" +
           "AQAAABX/////AQH/////AAAAADVgiQoCAAAAAAAIAAAAU2V2ZXJpdHkBARIBAwAAAAAhAAAASW5kaWNh" +
           "dGVzIGhvdyB1cmdlbnQgYW4gZXZlbnQgaXMuAC4ARBIBAAAABf////8BAf////8AAAAANWCJCgIAAAAB" +
           "AAgAAABOYW1lV2VsbAEBEwEDAAAAAEQAAABIdW1hbiByZWNvZ25pemFibGUgY29udGV4dCBmb3IgdGhl" +
           "IHdlbGwgdGhhdCBjb250YWlucyB0aGUgd2VsbCB0ZXN0LgAuAEQTAQAAAAz/////AQH/////AAAAADVg" +
           "iQoCAAAAAQAHAAAAVWlkV2VsbAEBFAEDAAAAAHMAAABVbmlxdWUgaWRlbnRpZmllciBmb3IgdGhlIHdl" +
           "bGwuIFRoaXMgdW5pcXVlbHkgcmVwcmVzZW50cyB0aGUgd2VsbCByZWZlcmVuY2VkIGJ5IHRoZSAocG9z" +
           "c2libHkgbm9uLXVuaXF1ZSkgTmFtZVdlbGwuAC4ARBQBAAAADP////8BAf////8AAAAANWCJCgIAAAAB" +
           "AAgAAABUZXN0RGF0ZQEBFQEDAAAAABsAAABUaGUgZGF0ZS10aW1lIG9mIHdlbGwgdGVzdC4ALgBEFQEA" +
           "AAAN/////wEB/////wAAAAA1YIkKAgAAAAEACgAAAFRlc3RSZWFzb24BARYBAwAAAAA6AAAAVGhlIHJl" +
           "YXNvbiBmb3IgdGhlIHdlbGwgdGVzdDogaW5pdGlhbCwgcGVyaW9kaWMsIHJldmlzaW9uLgAuAEQWAQAA" +
           "AAz/////AQH/////AAAAADVgiQoCAAAAAQAKAAAARmx1aWRMZXZlbAEBFwEDAAAAAGIAAABUaGUgZmx1" +
           "aWQgbGV2ZWwgYWNoaWV2ZWQgaW4gdGhlIHdlbGwuIFRoZSB2YWx1ZSBpcyBnaXZlbiBhcyBsZW5ndGgg" +
           "dW5pdHMgZnJvbSB0aGUgdG9wIG9mIHRoZSB3ZWxsLgAvAQBACRcBAAAAC/////8BAf////8CAAAAFWCJ" +
           "CgIAAAAAAAcAAABFVVJhbmdlAQEwAQAuAEQwAQAAAQB0A/////8BAf////8AAAAANWCJCgIAAAAAABAA" +
           "AABFbmdpbmVlcmluZ1VuaXRzAQEaAQMAAAAAHwAAAFRoZSB1bml0IG9mIG1lYXN1cmUgZm9yIGxlbmd0" +
           "aC4ALgBEGgEAAAEAdwP/////AQH/////AAAAADVgiQoCAAAAAQAIAAAAVGVzdGVkQnkBARsBAwAAAABL" +
           "AAAAVGhlIGJ1c2luZXNzIGFzc29jaWF0ZSB0aGF0IGNvbmR1Y3RlZCB0aGUgdGVzdC4gVGhpcyBpcyBn" +
           "ZW5lcmFsbHkgYSBwZXJzb24uAC4ARBsBAAAADP////8BAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// The fluid level achieved in the well. The value is given as length units from the top of the well.
        /// </summary>
        public AnalogItemState<double> FluidLevel
        {
            get
            {
                return m_fluidLevel;
            }

            set
            {
                if (!Object.ReferenceEquals(m_fluidLevel, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_fluidLevel = value;
            }
        }

        /// <summary>
        /// The business associate that conducted the test. This is generally a person.
        /// </summary>
        public PropertyState<string> TestedBy
        {
            get
            {
                return m_testedBy;
            }

            set
            {
                if (!Object.ReferenceEquals(m_testedBy, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_testedBy = value;
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
            if (m_fluidLevel != null)
            {
                children.Add(m_fluidLevel);
            }

            if (m_testedBy != null)
            {
                children.Add(m_testedBy);
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
                case HistoricalEvents.BrowseNames.FluidLevel:
                {
                    if (createOrReplace)
                    {
                        if (FluidLevel == null)
                        {
                            if (replacement == null)
                            {
                                FluidLevel = new AnalogItemState<double>(this);
                            }
                            else
                            {
                                FluidLevel = (AnalogItemState<double>)replacement;
                            }
                        }
                    }

                    instance = FluidLevel;
                    break;
                }

                case HistoricalEvents.BrowseNames.TestedBy:
                {
                    if (createOrReplace)
                    {
                        if (TestedBy == null)
                        {
                            if (replacement == null)
                            {
                                TestedBy = new PropertyState<string>(this);
                            }
                            else
                            {
                                TestedBy = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = TestedBy;
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
        private AnalogItemState<double> m_fluidLevel;
        private PropertyState<string> m_testedBy;
        #endregion
    }
    #endif
    #endregion

    #region InjectionTestReportState Class
    #if (!OPCUA_EXCLUDE_InjectionTestReportState)
    /// <summary>
    /// Stores an instance of the InjectionTestReportType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class InjectionTestReportState : WellTestReportState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public InjectionTestReportState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(HistoricalEvents.ObjectTypes.InjectionTestReportType, HistoricalEvents.Namespaces.HistoricalEvents, namespaceUris);
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
           "AQAAACkAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvSGlzdG9yaWNhbEV2ZW50c/////8EYIAAAQAA" +
           "AAEAHwAAAEluamVjdGlvblRlc3RSZXBvcnRUeXBlSW5zdGFuY2UBARwBAQEcAf////8OAAAANWCJCgIA" +
           "AAAAAAcAAABFdmVudElkAQEdAQMAAAAAKwAAAEEgZ2xvYmFsbHkgdW5pcXVlIGlkZW50aWZpZXIgZm9y" +
           "IHRoZSBldmVudC4ALgBEHQEAAAAP/////wEB/////wAAAAA1YIkKAgAAAAAACQAAAEV2ZW50VHlwZQEB" +
           "HgEDAAAAACIAAABUaGUgaWRlbnRpZmllciBmb3IgdGhlIGV2ZW50IHR5cGUuAC4ARB4BAAAAEf////8B" +
           "Af////8AAAAANWCJCgIAAAAAAAoAAABTb3VyY2VOb2RlAQEfAQMAAAAAGAAAAFRoZSBzb3VyY2Ugb2Yg" +
           "dGhlIGV2ZW50LgAuAEQfAQAAABH/////AQH/////AAAAADVgiQoCAAAAAAAKAAAAU291cmNlTmFtZQEB" +
           "IAEDAAAAACkAAABBIGRlc2NyaXB0aW9uIG9mIHRoZSBzb3VyY2Ugb2YgdGhlIGV2ZW50LgAuAEQgAQAA" +
           "AAz/////AQH/////AAAAADVgiQoCAAAAAAAEAAAAVGltZQEBIQEDAAAAABgAAABXaGVuIHRoZSBldmVu" +
           "dCBvY2N1cnJlZC4ALgBEIQEAAAEAJgH/////AQH/////AAAAADVgiQoCAAAAAAALAAAAUmVjZWl2ZVRp" +
           "bWUBASIBAwAAAAA+AAAAV2hlbiB0aGUgc2VydmVyIHJlY2VpdmVkIHRoZSBldmVudCBmcm9tIHRoZSB1" +
           "bmRlcmx5aW5nIHN5c3RlbS4ALgBEIgEAAAEAJgH/////AQH/////AAAAADVgiQoCAAAAAAAHAAAATWVz" +
           "c2FnZQEBJAEDAAAAACUAAABBIGxvY2FsaXplZCBkZXNjcmlwdGlvbiBvZiB0aGUgZXZlbnQuAC4ARCQB" +
           "AAAAFf////8BAf////8AAAAANWCJCgIAAAAAAAgAAABTZXZlcml0eQEBJQEDAAAAACEAAABJbmRpY2F0" +
           "ZXMgaG93IHVyZ2VudCBhbiBldmVudCBpcy4ALgBEJQEAAAAF/////wEB/////wAAAAA1YIkKAgAAAAEA" +
           "CAAAAE5hbWVXZWxsAQEmAQMAAAAARAAAAEh1bWFuIHJlY29nbml6YWJsZSBjb250ZXh0IGZvciB0aGUg" +
           "d2VsbCB0aGF0IGNvbnRhaW5zIHRoZSB3ZWxsIHRlc3QuAC4ARCYBAAAADP////8BAf////8AAAAANWCJ" +
           "CgIAAAABAAcAAABVaWRXZWxsAQEnAQMAAAAAcwAAAFVuaXF1ZSBpZGVudGlmaWVyIGZvciB0aGUgd2Vs" +
           "bC4gVGhpcyB1bmlxdWVseSByZXByZXNlbnRzIHRoZSB3ZWxsIHJlZmVyZW5jZWQgYnkgdGhlIChwb3Nz" +
           "aWJseSBub24tdW5pcXVlKSBOYW1lV2VsbC4ALgBEJwEAAAAM/////wEB/////wAAAAA1YIkKAgAAAAEA" +
           "CAAAAFRlc3REYXRlAQEoAQMAAAAAGwAAAFRoZSBkYXRlLXRpbWUgb2Ygd2VsbCB0ZXN0LgAuAEQoAQAA" +
           "AA3/////AQH/////AAAAADVgiQoCAAAAAQAKAAAAVGVzdFJlYXNvbgEBKQEDAAAAADoAAABUaGUgcmVh" +
           "c29uIGZvciB0aGUgd2VsbCB0ZXN0OiBpbml0aWFsLCBwZXJpb2RpYywgcmV2aXNpb24uAC4ARCkBAAAA" +
           "DP////8BAf////8AAAAANWCJCgIAAAABAAwAAABUZXN0RHVyYXRpb24BASoBAwAAAAAsAAAAVGhlIHRp" +
           "bWUgbGVuZ3RoICh3aXRoIHVvbSkgb2YgdGhlIHdlbGwgdGVzdC4ALwEAQAkqAQAAAAv/////AQH/////" +
           "AgAAABVgiQoCAAAAAAAHAAAARVVSYW5nZQEBMgEALgBEMgEAAAEAdAP/////AQH/////AAAAADVgiQoC" +
           "AAAAAAAQAAAARW5naW5lZXJpbmdVbml0cwEBLQEDAAAAAB0AAABUaGUgdW5pdCBvZiBtZWFzdXJlIGZv" +
           "ciB0aW1lLgAuAEQtAQAAAQB3A/////8BAf////8AAAAANWCJCgIAAAABAA0AAABJbmplY3RlZEZsdWlk" +
           "AQEuAQMAAAAAIwAAAFRoZSBmbHVpZCB0aGF0IGlzIGJlaW5nIGluamVjdGVkLiAuAC4ARC4BAAAADP//" +
           "//8BAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// The time length (with uom) of the well test.
        /// </summary>
        public AnalogItemState<double> TestDuration
        {
            get
            {
                return m_testDuration;
            }

            set
            {
                if (!Object.ReferenceEquals(m_testDuration, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_testDuration = value;
            }
        }

        /// <summary>
        /// The fluid that is being injected. .
        /// </summary>
        public PropertyState<string> InjectedFluid
        {
            get
            {
                return m_injectedFluid;
            }

            set
            {
                if (!Object.ReferenceEquals(m_injectedFluid, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_injectedFluid = value;
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
            if (m_testDuration != null)
            {
                children.Add(m_testDuration);
            }

            if (m_injectedFluid != null)
            {
                children.Add(m_injectedFluid);
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
                case HistoricalEvents.BrowseNames.TestDuration:
                {
                    if (createOrReplace)
                    {
                        if (TestDuration == null)
                        {
                            if (replacement == null)
                            {
                                TestDuration = new AnalogItemState<double>(this);
                            }
                            else
                            {
                                TestDuration = (AnalogItemState<double>)replacement;
                            }
                        }
                    }

                    instance = TestDuration;
                    break;
                }

                case HistoricalEvents.BrowseNames.InjectedFluid:
                {
                    if (createOrReplace)
                    {
                        if (InjectedFluid == null)
                        {
                            if (replacement == null)
                            {
                                InjectedFluid = new PropertyState<string>(this);
                            }
                            else
                            {
                                InjectedFluid = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = InjectedFluid;
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
        private AnalogItemState<double> m_testDuration;
        private PropertyState<string> m_injectedFluid;
        #endregion
    }
    #endif
    #endregion

    #region WellState Class
    #if (!OPCUA_EXCLUDE_WellState)
    /// <summary>
    /// Stores an instance of the WellType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class WellState : BaseObjectState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public WellState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(HistoricalEvents.ObjectTypes.WellType, HistoricalEvents.Namespaces.HistoricalEvents, namespaceUris);
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
           "AQAAACkAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvSGlzdG9yaWNhbEV2ZW50c/////8EYIAAAQAA" +
           "AAEAEAAAAFdlbGxUeXBlSW5zdGFuY2UBATQBAQE0Af////8AAAAA";
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