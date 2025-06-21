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

namespace UAModel.ISA95_JOBCONTROL_V2
{
    #region ISA95JobOrderStatusEventTypeState Class
#if (!OPCUA_EXCLUDE_ISA95JobOrderStatusEventTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ISA95JobOrderStatusEventTypeState : BaseEventState
    {
        #region Constructors
        /// <remarks />
        public ISA95JobOrderStatusEventTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95JobOrderStatusEventType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, namespaceUris);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGCAAgEAAAABACQAAABJU0E5NUpvYk9yZGVyU3RhdHVzRXZlbnRUeXBlSW5zdGFuY2UBAe4DAQHuA+4D" +
           "AAAbAAAAACkBAQHrAwA2AQEBsRMANgEBAbITADYBAQGzEwA2AQEBtBMANgEBAbUTADYBAQG2EwA2AQEB" +
           "txMANgEBAbgTADYBAQG5EwA2AQEBuhMANgEBAbsTADYBAQHNEwA2AQEBzhMANgEBAc8TADYBAQHQEwA2" +
           "AQEB0RMANgEBAdITADYBAQHTEwA2AQEB1BMANgEBAdUTADYBAQHWEwA2AQEB1xMANgEBAdwTADYBAQHd" +
           "EwA2AQEB3hMANgEBAd8TCwAAABVgiQgCAAAAAAAHAAAARXZlbnRJZAEBAAAALgBEAA//////AQH/////" +
           "AAAAABVgiQgCAAAAAAAJAAAARXZlbnRUeXBlAQEAAAAuAEQAEf////8BAf////8AAAAAFWCJCAIAAAAA" +
           "AAoAAABTb3VyY2VOb2RlAQEAAAAuAEQAEf////8BAf////8AAAAAFWCJCAIAAAAAAAoAAABTb3VyY2VO" +
           "YW1lAQEAAAAuAEQADP////8BAf////8AAAAAFWCJCAIAAAAAAAQAAABUaW1lAQEAAAAuAEQBACYB////" +
           "/wEB/////wAAAAAVYIkIAgAAAAAACwAAAFJlY2VpdmVUaW1lAQEAAAAuAEQBACYB/////wEB/////wAA" +
           "AAAVYIkIAgAAAAAABwAAAE1lc3NhZ2UBAQAAAC4ARAAV/////wEB/////wAAAAAVYIkIAgAAAAAACAAA" +
           "AFNldmVyaXR5AQEAAAAuAEQABf////8BAf////8AAAAAFWCJCgIAAAABAAgAAABKb2JPcmRlcgEBnxcA" +
           "LgBEnxcAAAEBwAv/////AwP/////AAAAABVgiQoCAAAAAQALAAAASm9iUmVzcG9uc2UBAaEXAC4ARKEX" +
           "AAABAcUL/////wMD/////wAAAAAXYIkKAgAAAAEACAAAAEpvYlN0YXRlAQGgFwAuAESgFwAAAQG+CwEA" +
           "AAABAAAAAAAAAAMD/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<ISA95JobOrderDataType> JobOrder
        {
            get
            {
                return m_jobOrder;
            }

            set
            {
                if (!Object.ReferenceEquals(m_jobOrder, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_jobOrder = value;
            }
        }

        /// <remarks />
        public PropertyState<ISA95JobResponseDataType> JobResponse
        {
            get
            {
                return m_jobResponse;
            }

            set
            {
                if (!Object.ReferenceEquals(m_jobResponse, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_jobResponse = value;
            }
        }

        /// <remarks />
        public PropertyState<ISA95StateDataType[]> JobState
        {
            get
            {
                return m_jobState;
            }

            set
            {
                if (!Object.ReferenceEquals(m_jobState, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_jobState = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_jobOrder != null)
            {
                children.Add(m_jobOrder);
            }

            if (m_jobResponse != null)
            {
                children.Add(m_jobResponse);
            }

            if (m_jobState != null)
            {
                children.Add(m_jobState);
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
                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.JobOrder:
                    {
                        if (createOrReplace)
                        {
                            if (JobOrder == null)
                            {
                                if (replacement == null)
                                {
                                    JobOrder = new PropertyState<ISA95JobOrderDataType>(this);
                                }
                                else
                                {
                                    JobOrder = (PropertyState<ISA95JobOrderDataType>)replacement;
                                }
                            }
                        }

                        instance = JobOrder;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.JobResponse:
                    {
                        if (createOrReplace)
                        {
                            if (JobResponse == null)
                            {
                                if (replacement == null)
                                {
                                    JobResponse = new PropertyState<ISA95JobResponseDataType>(this);
                                }
                                else
                                {
                                    JobResponse = (PropertyState<ISA95JobResponseDataType>)replacement;
                                }
                            }
                        }

                        instance = JobResponse;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.JobState:
                    {
                        if (createOrReplace)
                        {
                            if (JobState == null)
                            {
                                if (replacement == null)
                                {
                                    JobState = new PropertyState<ISA95StateDataType[]>(this);
                                }
                                else
                                {
                                    JobState = (PropertyState<ISA95StateDataType[]>)replacement;
                                }
                            }
                        }

                        instance = JobState;
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
        private PropertyState<ISA95JobOrderDataType> m_jobOrder;
        private PropertyState<ISA95JobResponseDataType> m_jobResponse;
        private PropertyState<ISA95StateDataType[]> m_jobState;
        #endregion
    }
#endif
    #endregion

    #region ISA95JobResponseProviderObjectTypeState Class
#if (!OPCUA_EXCLUDE_ISA95JobResponseProviderObjectTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ISA95JobResponseProviderObjectTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public ISA95JobResponseProviderObjectTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95JobResponseProviderObjectType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, namespaceUris);
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

            if (JobOrderResponseList != null)
            {
                JobOrderResponseList.Initialize(context, JobOrderResponseList_InitializationString);
            }
        }

        #region Initialization String
        private const string JobOrderResponseList_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "F2CJCgIAAAABABQAAABKb2JPcmRlclJlc3BvbnNlTGlzdAEBohcALwA/ohcAAAEBxQsBAAAAAQAAAAAA" +
           "AAADA/////8AAAAA";

        private const string InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGCAAgEAAAABACoAAABJU0E5NUpvYlJlc3BvbnNlUHJvdmlkZXJPYmplY3RUeXBlSW5zdGFuY2UBAesD" +
           "AQHrA+sDAAABAAAAACkAAQHuAwMAAAAXYIkKAgAAAAEAFAAAAEpvYk9yZGVyUmVzcG9uc2VMaXN0AQGi" +
           "FwAvAD+iFwAAAQHFCwEAAAABAAAAAAAAAAMD/////wAAAAAEYYIKBAAAAAEAHgAAAFJlcXVlc3RKb2JS" +
           "ZXNwb25zZUJ5Sm9iT3JkZXJJRAEBWhsALwEBWhtaGwAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5w" +
           "dXRBcmd1bWVudHMBAZoXAC4ARJoXAACWAQAAAAEAKgEBYAAAAAoAAABKb2JPcmRlcklEAAz/////AAAA" +
           "AAJDAAAAQ29udGFpbnMgYW4gSUQgb2YgdGhlIGpvYiBvcmRlciwgYXMgc3BlY2lmaWVkIGJ5IHRoZSBt" +
           "ZXRob2QgY2FsbGVyLgEAKAEBAAAAAQAAAAEAAAABAf////8AAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRB" +
           "cmd1bWVudHMBAZsXAC4ARJsXAACWAgAAAAEAKgEB4QAAAAsAAABKb2JSZXNwb25zZQEBxQv/////AAAA" +
           "AALBAAAAQ29udGFpbnMgaW5mb3JtYXRpb24gYWJvdXQgdGhlIGV4ZWN1dGlvbiBvZiBhIGpvYiBvcmRl" +
           "ciwgc3VjaCBhcyB0aGUgY3VycmVudCBzdGF0dXMgb2YgdGhlIGpvYiwgYWN0dWFsIG1hdGVyaWFsIGNv" +
           "bnN1bWVkLCBhY3R1YWwgbWF0ZXJpYWwgcHJvZHVjZWQsIGFjdHVhbCBlcXVpcG1lbnQgdXNlZCwgYW5k" +
           "IGpvYiBzcGVjaWZpYyBkYXRhLgEAKgEBSgAAAAwAAABSZXR1cm5TdGF0dXMACf////8AAAAAAisAAABS" +
           "ZXR1cm5zIHRoZSBzdGF0dXMgb2YgdGhlIG1ldGhvZCBleGVjdXRpb24uAQAoAQEAAAABAAAAAgAAAAEB" +
           "/////wAAAAAEYYIKBAAAAAEAIQAAAFJlcXVlc3RKb2JSZXNwb25zZUJ5Sm9iT3JkZXJTdGF0ZQEBZhsA" +
           "LwEBZhtmGwAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAYAXAC4ARIAXAACW" +
           "AQAAAAEAKgEBYQEAAA0AAABKb2JPcmRlclN0YXRlAQG+CwEAAAABAAAAAAAAAAI7AQAAQ29udGFpbnMg" +
           "YSBqb2Igc3RhdHVzIG9mIHRoZSBKb2JSZXNwb25zZSB0byBiZSByZXR1cm5lZC4gVGhlIGFycmF5IHNo" +
           "YWxsIHByb3ZpZGUgYXQgbGVhc3Qgb25lIGVudHJ5IHJlcHJlc2VudGluZyB0aGUgdG9wIGxldmVsIHN0" +
           "YXRlIGFuZCBwb3RlbnRpYWxseSBhZGRpdGlvbmFsIGVudHJpZXMgcmVwcmVzZW50aW5nIHN1YnN0YXRl" +
           "cy4gVGhlIGZpcnN0IGVudHJ5IHNoYWxsIGJlIHRoZSB0b3AgbGV2ZWwgZW50cnksIGhhdmluZyB0aGUg" +
           "QnJvd3NlUGF0aCBzZXQgdG8gbnVsbC4gVGhlIG9yZGVyIG9mIHRoZSBzdWJzdGF0ZXMgaXMgbm90IGRl" +
           "ZmluZWQuAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50" +
           "cwEBgRcALgBEgRcAAJYCAAAAAQAqAQHwAAAADAAAAEpvYlJlc3BvbnNlcwEBxQsBAAAAAQAAAAAAAAAC" +
           "ywAAAENvbnRhaW5zIGEgbGlzdCBvZiBpbmZvcm1hdGlvbiBhYm91dCB0aGUgZXhlY3V0aW9uIG9mIGEg" +
           "am9iIG9yZGVyLCBzdWNoIGFzIHRoZSBjdXJyZW50IHN0YXR1cyBvZiB0aGUgam9iLCBhY3R1YWwgbWF0" +
           "ZXJpYWwgY29uc3VtZWQsIGFjdHVhbCBtYXRlcmlhbCBwcm9kdWNlZCwgYWN0dWFsIGVxdWlwbWVudCB1" +
           "c2VkLCBhbmQgam9iIHNwZWNpZmljIGRhdGEuAQAqAQFKAAAADAAAAFJldHVyblN0YXR1cwAJ/////wAA" +
           "AAACKwAAAFJldHVybnMgdGhlIHN0YXR1cyBvZiB0aGUgbWV0aG9kIGV4ZWN1dGlvbi4BACgBAQAAAAEA" +
           "AAACAAAAAQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public BaseDataVariableState<ISA95JobResponseDataType[]> JobOrderResponseList
        {
            get
            {
                return m_jobOrderResponseList;
            }

            set
            {
                if (!Object.ReferenceEquals(m_jobOrderResponseList, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_jobOrderResponseList = value;
            }
        }

        /// <remarks />
        public RequestJobResponseByJobOrderIDMethodState RequestJobResponseByJobOrderID
        {
            get
            {
                return m_requestJobResponseByJobOrderIDMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_requestJobResponseByJobOrderIDMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_requestJobResponseByJobOrderIDMethod = value;
            }
        }

        /// <remarks />
        public RequestJobResponseByJobOrderStateMethodState RequestJobResponseByJobOrderState
        {
            get
            {
                return m_requestJobResponseByJobOrderStateMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_requestJobResponseByJobOrderStateMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_requestJobResponseByJobOrderStateMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_jobOrderResponseList != null)
            {
                children.Add(m_jobOrderResponseList);
            }

            if (m_requestJobResponseByJobOrderIDMethod != null)
            {
                children.Add(m_requestJobResponseByJobOrderIDMethod);
            }

            if (m_requestJobResponseByJobOrderStateMethod != null)
            {
                children.Add(m_requestJobResponseByJobOrderStateMethod);
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
                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.JobOrderResponseList:
                    {
                        if (createOrReplace)
                        {
                            if (JobOrderResponseList == null)
                            {
                                if (replacement == null)
                                {
                                    JobOrderResponseList = new BaseDataVariableState<ISA95JobResponseDataType[]>(this);
                                }
                                else
                                {
                                    JobOrderResponseList = (BaseDataVariableState<ISA95JobResponseDataType[]>)replacement;
                                }
                            }
                        }

                        instance = JobOrderResponseList;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.RequestJobResponseByJobOrderID:
                    {
                        if (createOrReplace)
                        {
                            if (RequestJobResponseByJobOrderID == null)
                            {
                                if (replacement == null)
                                {
                                    RequestJobResponseByJobOrderID = new RequestJobResponseByJobOrderIDMethodState(this);
                                }
                                else
                                {
                                    RequestJobResponseByJobOrderID = (RequestJobResponseByJobOrderIDMethodState)replacement;
                                }
                            }
                        }

                        instance = RequestJobResponseByJobOrderID;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.RequestJobResponseByJobOrderState:
                    {
                        if (createOrReplace)
                        {
                            if (RequestJobResponseByJobOrderState == null)
                            {
                                if (replacement == null)
                                {
                                    RequestJobResponseByJobOrderState = new RequestJobResponseByJobOrderStateMethodState(this);
                                }
                                else
                                {
                                    RequestJobResponseByJobOrderState = (RequestJobResponseByJobOrderStateMethodState)replacement;
                                }
                            }
                        }

                        instance = RequestJobResponseByJobOrderState;
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
        private BaseDataVariableState<ISA95JobResponseDataType[]> m_jobOrderResponseList;
        private RequestJobResponseByJobOrderIDMethodState m_requestJobResponseByJobOrderIDMethod;
        private RequestJobResponseByJobOrderStateMethodState m_requestJobResponseByJobOrderStateMethod;
        #endregion
    }
#endif
    #endregion

    #region ISA95JobResponseReceiverObjectTypeState Class
#if (!OPCUA_EXCLUDE_ISA95JobResponseReceiverObjectTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ISA95JobResponseReceiverObjectTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public ISA95JobResponseReceiverObjectTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95JobResponseReceiverObjectType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, namespaceUris);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGCAAgEAAAABACoAAABJU0E5NUpvYlJlc3BvbnNlUmVjZWl2ZXJPYmplY3RUeXBlSW5zdGFuY2UBAewD" +
           "AQHsA+wDAAD/////AQAAAARhggoEAAAAAQASAAAAUmVjZWl2ZUpvYlJlc3BvbnNlAQFbGwAvAQFbG1sb" +
           "AAABAf////8CAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBnBcALgBEnBcAAJYBAAAAAQAq" +
           "AQHCAAAACwAAAEpvYlJlc3BvbnNlAQHFC/////8AAAAAAqIAAABDb250YWlucyBpbmZvcm1hdGlvbiBh" +
           "Ym91dCB0aGUgZXhlY3V0aW9uIG9mIGEgam9iIG9yZGVyLCBzdWNoIGFzIGFjdHVhbCBtYXRlcmlhbCBj" +
           "b25zdW1lZCwgYWN0dWFsIG1hdGVyaWFsIHByb2R1Y2VkLCBhY3R1YWwgZXF1aXBtZW50IHVzZWQsIGFu" +
           "ZCBqb2Igc3BlY2lmaWMgZGF0YS4BACgBAQAAAAEAAAABAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAA" +
           "T3V0cHV0QXJndW1lbnRzAQGdFwAuAESdFwAAlgEAAAABACoBAUoAAAAMAAAAUmV0dXJuU3RhdHVzAAn/" +
           "////AAAAAAIrAAAAUmV0dXJucyB0aGUgc3RhdHVzIG9mIHRoZSBtZXRob2QgZXhlY3V0aW9uLgEAKAEB" +
           "AAAAAQAAAAEAAAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public ReceiveJobResponseMethodState ReceiveJobResponse
        {
            get
            {
                return m_receiveJobResponseMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_receiveJobResponseMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_receiveJobResponseMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_receiveJobResponseMethod != null)
            {
                children.Add(m_receiveJobResponseMethod);
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
                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.ReceiveJobResponse:
                    {
                        if (createOrReplace)
                        {
                            if (ReceiveJobResponse == null)
                            {
                                if (replacement == null)
                                {
                                    ReceiveJobResponse = new ReceiveJobResponseMethodState(this);
                                }
                                else
                                {
                                    ReceiveJobResponse = (ReceiveJobResponseMethodState)replacement;
                                }
                            }
                        }

                        instance = ReceiveJobResponse;
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
        private ReceiveJobResponseMethodState m_receiveJobResponseMethod;
        #endregion
    }
#endif
    #endregion

    #region ISA95EndedStateMachineTypeState Class
#if (!OPCUA_EXCLUDE_ISA95EndedStateMachineTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ISA95EndedStateMachineTypeState : FiniteStateMachineState
    {
        #region Constructors
        /// <remarks />
        public ISA95EndedStateMachineTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95EndedStateMachineType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, namespaceUris);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGCAAgEAAAABACIAAABJU0E5NUVuZGVkU3RhdGVNYWNoaW5lVHlwZUluc3RhbmNlAQHtAwEB7QPtAwAA" +
           "/////wEAAAAVYIkIAgAAAAAADAAAAEN1cnJlbnRTdGF0ZQEBAAAALwEAyAoAFf////8BAf////8BAAAA" +
           "FWCJCAIAAAAAAAIAAABJZAEBAAAALgBEABH/////AQH/////AAAAAA==";
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

    #region ISA95InterruptedStateMachineTypeState Class
#if (!OPCUA_EXCLUDE_ISA95InterruptedStateMachineTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ISA95InterruptedStateMachineTypeState : FiniteStateMachineState
    {
        #region Constructors
        /// <remarks />
        public ISA95InterruptedStateMachineTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95InterruptedStateMachineType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, namespaceUris);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGCAAgEAAAABACgAAABJU0E5NUludGVycnVwdGVkU3RhdGVNYWNoaW5lVHlwZUluc3RhbmNlAQHvAwEB" +
           "7wPvAwAA/////wEAAAAVYIkIAgAAAAAADAAAAEN1cnJlbnRTdGF0ZQEBAAAALwEAyAoAFf////8BAf//" +
           "//8BAAAAFWCJCAIAAAAAAAIAAABJZAEBAAAALgBEABH/////AQH/////AAAAAA==";
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

    #region ISA95JobOrderReceiverObjectTypeState Class
#if (!OPCUA_EXCLUDE_ISA95JobOrderReceiverObjectTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ISA95JobOrderReceiverObjectTypeState : FiniteStateMachineState
    {
        #region Constructors
        /// <remarks />
        public ISA95JobOrderReceiverObjectTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95JobOrderReceiverObjectType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, namespaceUris);
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

            if (Abort != null)
            {
                Abort.Initialize(context, Abort_InitializationString);
            }

            if (Cancel != null)
            {
                Cancel.Initialize(context, Cancel_InitializationString);
            }

            if (Clear != null)
            {
                Clear.Initialize(context, Clear_InitializationString);
            }

            if (Pause != null)
            {
                Pause.Initialize(context, Pause_InitializationString);
            }

            if (Resume != null)
            {
                Resume.Initialize(context, Resume_InitializationString);
            }

            if (RevokeStart != null)
            {
                RevokeStart.Initialize(context, RevokeStart_InitializationString);
            }

            if (Start != null)
            {
                Start.Initialize(context, Start_InitializationString);
            }

            if (Stop != null)
            {
                Stop.Initialize(context, Stop_InitializationString);
            }

            if (Store != null)
            {
                Store.Initialize(context, Store_InitializationString);
            }

            if (StoreAndStart != null)
            {
                StoreAndStart.Initialize(context, StoreAndStart_InitializationString);
            }

            if (Update != null)
            {
                Update.Initialize(context, Update_InitializationString);
            }
        }

        #region Initialization String
        private const string Abort_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCCgQAAAABAAUAAABBYm9ydAEBYhsALwEBYhtiGwAAAQEIAAAAADUBAQG4EwA1AQEBuRMANQEBAdQT" +
           "ADUBAQHVEwA1AQEB3BMANQEBAd0TADUBAQHeEwA1AQEB3xMCAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFy" +
           "Z3VtZW50cwEBrxcALgBErxcAAJYCAAAAAQAqAQGzAAAACgAAAEpvYk9yZGVySUQADP////8AAAAAApYA" +
           "AABDb250YWlucyBpbmZvcm1hdGlvbiBkZWZpbmluZyB0aGUgam9iIG9yZGVyIHdpdGggYWxsIHBhcmFt" +
           "ZXRlcnMgYW5kIGFueSBtYXRlcmlhbCwgZXF1aXBtZW50LCBvciBwaHlzaWNhbCBhc3NldCByZXF1aXJl" +
           "bWVudHMgYXNzb2NpYXRlZCB3aXRoIHRoZSBvcmRlci4BACoBAeoAAAAHAAAAQ29tbWVudAAVAQAAAAEA" +
           "AAAAAAAAAswAAABUaGUgY29tbWVudCBwcm92aWRlcyBhIGRlc2NyaXB0aW9uIG9mIHdoeSB0aGUgbWV0" +
           "aG9kIHdhcyBjYWxsZWQuIEluIG9yZGVyIHRvIHByb3ZpZGUgdGhlIGNvbW1lbnQgaW4gc2V2ZXJhbCBs" +
           "YW5ndWFnZXMsIGl0IGlzIGFuIGFycmF5IG9mIExvY2FsaXplZFRleHQuIFRoZSBhcnJheSBtYXkgYmUg" +
           "ZW1wdHksIHdoZW4gbm8gY29tbWVudCBpcyBwcm92aWRlZC4BACgBAQAAAAEAAAACAAAAAQH/////AAAA" +
           "ABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGwFwAuAESwFwAAlgEAAAABACoBAUoAAAAMAAAA" +
           "UmV0dXJuU3RhdHVzAAn/////AAAAAAIrAAAAUmV0dXJucyB0aGUgc3RhdHVzIG9mIHRoZSBtZXRob2Qg" +
           "ZXhlY3V0aW9uLgEAKAEBAAAAAQAAAAEAAAABAf////8AAAAA";

        private const string Cancel_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCCgQAAAABAAYAAABDYW5jZWwBAWMbAC8BAWMbYxsAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElu" +
           "cHV0QXJndW1lbnRzAQGxFwAuAESxFwAAlgIAAAABACoBAbMAAAAKAAAASm9iT3JkZXJJRAAM/////wAA" +
           "AAAClgAAAENvbnRhaW5zIGluZm9ybWF0aW9uIGRlZmluaW5nIHRoZSBqb2Igb3JkZXIgd2l0aCBhbGwg" +
           "cGFyYW1ldGVycyBhbmQgYW55IG1hdGVyaWFsLCBlcXVpcG1lbnQsIG9yIHBoeXNpY2FsIGFzc2V0IHJl" +
           "cXVpcmVtZW50cyBhc3NvY2lhdGVkIHdpdGggdGhlIG9yZGVyLgEAKgEB6gAAAAcAAABDb21tZW50ABUB" +
           "AAAAAQAAAAAAAAACzAAAAFRoZSBjb21tZW50IHByb3ZpZGVzIGEgZGVzY3JpcHRpb24gb2Ygd2h5IHRo" +
           "ZSBtZXRob2Qgd2FzIGNhbGxlZC4gSW4gb3JkZXIgdG8gcHJvdmlkZSB0aGUgY29tbWVudCBpbiBzZXZl" +
           "cmFsIGxhbmd1YWdlcywgaXQgaXMgYW4gYXJyYXkgb2YgTG9jYWxpemVkVGV4dC4gVGhlIGFycmF5IG1h" +
           "eSBiZSBlbXB0eSwgd2hlbiBubyBjb21tZW50IGlzIHByb3ZpZGVkLgEAKAEBAAAAAQAAAAIAAAABAf//" +
           "//8AAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAbIXAC4ARLIXAACWAQAAAAEAKgEBSgAA" +
           "AAwAAABSZXR1cm5TdGF0dXMACf////8AAAAAAisAAABSZXR1cm5zIHRoZSBzdGF0dXMgb2YgdGhlIG1l" +
           "dGhvZCBleGVjdXRpb24uAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAA=";

        private const string Clear_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCCgQAAAABAAUAAABDbGVhcgEBZBsALwEBZBtkGwAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5w" +
           "dXRBcmd1bWVudHMBAbMXAC4ARLMXAACWAgAAAAEAKgEBswAAAAoAAABKb2JPcmRlcklEAAz/////AAAA" +
           "AAKWAAAAQ29udGFpbnMgaW5mb3JtYXRpb24gZGVmaW5pbmcgdGhlIGpvYiBvcmRlciB3aXRoIGFsbCBw" +
           "YXJhbWV0ZXJzIGFuZCBhbnkgbWF0ZXJpYWwsIGVxdWlwbWVudCwgb3IgcGh5c2ljYWwgYXNzZXQgcmVx" +
           "dWlyZW1lbnRzIGFzc29jaWF0ZWQgd2l0aCB0aGUgb3JkZXIuAQAqAQHqAAAABwAAAENvbW1lbnQAFQEA" +
           "AAABAAAAAAAAAALMAAAAVGhlIGNvbW1lbnQgcHJvdmlkZXMgYSBkZXNjcmlwdGlvbiBvZiB3aHkgdGhl" +
           "IG1ldGhvZCB3YXMgY2FsbGVkLiBJbiBvcmRlciB0byBwcm92aWRlIHRoZSBjb21tZW50IGluIHNldmVy" +
           "YWwgbGFuZ3VhZ2VzLCBpdCBpcyBhbiBhcnJheSBvZiBMb2NhbGl6ZWRUZXh0LiBUaGUgYXJyYXkgbWF5" +
           "IGJlIGVtcHR5LCB3aGVuIG5vIGNvbW1lbnQgaXMgcHJvdmlkZWQuAQAoAQEAAAABAAAAAgAAAAEB////" +
           "/wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBtBcALgBEtBcAAJYBAAAAAQAqAQFKAAAA" +
           "DAAAAFJldHVyblN0YXR1cwAJ/////wAAAAACKwAAAFJldHVybnMgdGhlIHN0YXR1cyBvZiB0aGUgbWV0" +
           "aG9kIGV4ZWN1dGlvbi4BACgBAQAAAAEAAAABAAAAAQH/////AAAAAA==";

        private const string Pause_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCCgQAAAABAAUAAABQYXVzZQEBXxsALwEBXxtfGwAAAQECAAAAADUBAQG2EwA1AQEB0hMCAAAAF2Cp" +
           "CgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBqRcALgBEqRcAAJYCAAAAAQAqAQGzAAAACgAAAEpvYk9y" +
           "ZGVySUQADP////8AAAAAApYAAABDb250YWlucyBpbmZvcm1hdGlvbiBkZWZpbmluZyB0aGUgam9iIG9y" +
           "ZGVyIHdpdGggYWxsIHBhcmFtZXRlcnMgYW5kIGFueSBtYXRlcmlhbCwgZXF1aXBtZW50LCBvciBwaHlz" +
           "aWNhbCBhc3NldCByZXF1aXJlbWVudHMgYXNzb2NpYXRlZCB3aXRoIHRoZSBvcmRlci4BACoBAeoAAAAH" +
           "AAAAQ29tbWVudAAVAQAAAAEAAAAAAAAAAswAAABUaGUgY29tbWVudCBwcm92aWRlcyBhIGRlc2NyaXB0" +
           "aW9uIG9mIHdoeSB0aGUgbWV0aG9kIHdhcyBjYWxsZWQuIEluIG9yZGVyIHRvIHByb3ZpZGUgdGhlIGNv" +
           "bW1lbnQgaW4gc2V2ZXJhbCBsYW5ndWFnZXMsIGl0IGlzIGFuIGFycmF5IG9mIExvY2FsaXplZFRleHQu" +
           "IFRoZSBhcnJheSBtYXkgYmUgZW1wdHksIHdoZW4gbm8gY29tbWVudCBpcyBwcm92aWRlZC4BACgBAQAA" +
           "AAEAAAACAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGqFwAuAESqFwAA" +
           "lgEAAAABACoBAUoAAAAMAAAAUmV0dXJuU3RhdHVzAAn/////AAAAAAIrAAAAUmV0dXJucyB0aGUgc3Rh" +
           "dHVzIG9mIHRoZSBtZXRob2QgZXhlY3V0aW9uLgEAKAEBAAAAAQAAAAEAAAABAf////8AAAAA";

        private const string Resume_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCCgQAAAABAAYAAABSZXN1bWUBAWAbAC8BAWAbYBsAAAEBAgAAAAA1AQEBuhMANQEBAdYTAgAAABdg" +
           "qQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAasXAC4ARKsXAACWAgAAAAEAKgEBswAAAAoAAABKb2JP" +
           "cmRlcklEAAz/////AAAAAAKWAAAAQ29udGFpbnMgaW5mb3JtYXRpb24gZGVmaW5pbmcgdGhlIGpvYiBv" +
           "cmRlciB3aXRoIGFsbCBwYXJhbWV0ZXJzIGFuZCBhbnkgbWF0ZXJpYWwsIGVxdWlwbWVudCwgb3IgcGh5" +
           "c2ljYWwgYXNzZXQgcmVxdWlyZW1lbnRzIGFzc29jaWF0ZWQgd2l0aCB0aGUgb3JkZXIuAQAqAQHqAAAA" +
           "BwAAAENvbW1lbnQAFQEAAAABAAAAAAAAAALMAAAAVGhlIGNvbW1lbnQgcHJvdmlkZXMgYSBkZXNjcmlw" +
           "dGlvbiBvZiB3aHkgdGhlIG1ldGhvZCB3YXMgY2FsbGVkLiBJbiBvcmRlciB0byBwcm92aWRlIHRoZSBj" +
           "b21tZW50IGluIHNldmVyYWwgbGFuZ3VhZ2VzLCBpdCBpcyBhbiBhcnJheSBvZiBMb2NhbGl6ZWRUZXh0" +
           "LiBUaGUgYXJyYXkgbWF5IGJlIGVtcHR5LCB3aGVuIG5vIGNvbW1lbnQgaXMgcHJvdmlkZWQuAQAoAQEA" +
           "AAABAAAAAgAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBrBcALgBErBcA" +
           "AJYBAAAAAQAqAQFKAAAADAAAAFJldHVyblN0YXR1cwAJ/////wAAAAACKwAAAFJldHVybnMgdGhlIHN0" +
           "YXR1cyBvZiB0aGUgbWV0aG9kIGV4ZWN1dGlvbi4BACgBAQAAAAEAAAABAAAAAQH/////AAAAAA==";

        private const string RevokeStart_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCCgQAAAABAAsAAABSZXZva2VTdGFydAEBZRsALwEBZRtlGwAAAQECAAAAADUBAQGzEwA1AQEBzxMC" +
           "AAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBtRcALgBEtRcAAJYCAAAAAQAqAQGzAAAACgAA" +
           "AEpvYk9yZGVySUQADP////8AAAAAApYAAABDb250YWlucyBpbmZvcm1hdGlvbiBkZWZpbmluZyB0aGUg" +
           "am9iIG9yZGVyIHdpdGggYWxsIHBhcmFtZXRlcnMgYW5kIGFueSBtYXRlcmlhbCwgZXF1aXBtZW50LCBv" +
           "ciBwaHlzaWNhbCBhc3NldCByZXF1aXJlbWVudHMgYXNzb2NpYXRlZCB3aXRoIHRoZSBvcmRlci4BACoB" +
           "AeoAAAAHAAAAQ29tbWVudAAVAQAAAAEAAAAAAAAAAswAAABUaGUgY29tbWVudCBwcm92aWRlcyBhIGRl" +
           "c2NyaXB0aW9uIG9mIHdoeSB0aGUgbWV0aG9kIHdhcyBjYWxsZWQuIEluIG9yZGVyIHRvIHByb3ZpZGUg" +
           "dGhlIGNvbW1lbnQgaW4gc2V2ZXJhbCBsYW5ndWFnZXMsIGl0IGlzIGFuIGFycmF5IG9mIExvY2FsaXpl" +
           "ZFRleHQuIFRoZSBhcnJheSBtYXkgYmUgZW1wdHksIHdoZW4gbm8gY29tbWVudCBpcyBwcm92aWRlZC4B" +
           "ACgBAQAAAAEAAAACAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQG2FwAu" +
           "AES2FwAAlgEAAAABACoBAUoAAAAMAAAAUmV0dXJuU3RhdHVzAAn/////AAAAAAIrAAAAUmV0dXJucyB0" +
           "aGUgc3RhdHVzIG9mIHRoZSBtZXRob2QgZXhlY3V0aW9uLgEAKAEBAAAAAQAAAAEAAAABAf////8AAAAA";

        private const string Start_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCCgQAAAABAAUAAABTdGFydAEBXRsALwEBXRtdGwAAAQECAAAAADUBAQGyEwA1AQEBzhMCAAAAF2Cp" +
           "CgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBpRcALgBEpRcAAJYCAAAAAQAqAQGzAAAACgAAAEpvYk9y" +
           "ZGVySUQADP////8AAAAAApYAAABDb250YWlucyBpbmZvcm1hdGlvbiBkZWZpbmluZyB0aGUgam9iIG9y" +
           "ZGVyIHdpdGggYWxsIHBhcmFtZXRlcnMgYW5kIGFueSBtYXRlcmlhbCwgZXF1aXBtZW50LCBvciBwaHlz" +
           "aWNhbCBhc3NldCByZXF1aXJlbWVudHMgYXNzb2NpYXRlZCB3aXRoIHRoZSBvcmRlci4BACoBAeoAAAAH" +
           "AAAAQ29tbWVudAAVAQAAAAEAAAAAAAAAAswAAABUaGUgY29tbWVudCBwcm92aWRlcyBhIGRlc2NyaXB0" +
           "aW9uIG9mIHdoeSB0aGUgbWV0aG9kIHdhcyBjYWxsZWQuIEluIG9yZGVyIHRvIHByb3ZpZGUgdGhlIGNv" +
           "bW1lbnQgaW4gc2V2ZXJhbCBsYW5ndWFnZXMsIGl0IGlzIGFuIGFycmF5IG9mIExvY2FsaXplZFRleHQu" +
           "IFRoZSBhcnJheSBtYXkgYmUgZW1wdHksIHdoZW4gbm8gY29tbWVudCBpcyBwcm92aWRlZC4BACgBAQAA" +
           "AAEAAAACAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGmFwAuAESmFwAA" +
           "lgEAAAABACoBAUoAAAAMAAAAUmV0dXJuU3RhdHVzAAn/////AAAAAAIrAAAAUmV0dXJucyB0aGUgc3Rh" +
           "dHVzIG9mIHRoZSBtZXRob2QgZXhlY3V0aW9uLgEAKAEBAAAAAQAAAAEAAAABAf////8AAAAA";

        private const string Stop_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCCgQAAAABAAQAAABTdG9wAQFeGwAvAQFeG14bAAABAQQAAAAANQEBAbsTADUBAQG3EwA1AQEB0xMA" +
           "NQEBAdcTAgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAacXAC4ARKcXAACWAgAAAAEAKgEB" +
           "swAAAAoAAABKb2JPcmRlcklEAAz/////AAAAAAKWAAAAQ29udGFpbnMgaW5mb3JtYXRpb24gZGVmaW5p" +
           "bmcgdGhlIGpvYiBvcmRlciB3aXRoIGFsbCBwYXJhbWV0ZXJzIGFuZCBhbnkgbWF0ZXJpYWwsIGVxdWlw" +
           "bWVudCwgb3IgcGh5c2ljYWwgYXNzZXQgcmVxdWlyZW1lbnRzIGFzc29jaWF0ZWQgd2l0aCB0aGUgb3Jk" +
           "ZXIuAQAqAQHqAAAABwAAAENvbW1lbnQAFQEAAAABAAAAAAAAAALMAAAAVGhlIGNvbW1lbnQgcHJvdmlk" +
           "ZXMgYSBkZXNjcmlwdGlvbiBvZiB3aHkgdGhlIG1ldGhvZCB3YXMgY2FsbGVkLiBJbiBvcmRlciB0byBw" +
           "cm92aWRlIHRoZSBjb21tZW50IGluIHNldmVyYWwgbGFuZ3VhZ2VzLCBpdCBpcyBhbiBhcnJheSBvZiBM" +
           "b2NhbGl6ZWRUZXh0LiBUaGUgYXJyYXkgbWF5IGJlIGVtcHR5LCB3aGVuIG5vIGNvbW1lbnQgaXMgcHJv" +
           "dmlkZWQuAQAoAQEAAAABAAAAAgAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50" +
           "cwEBqBcALgBEqBcAAJYBAAAAAQAqAQFKAAAADAAAAFJldHVyblN0YXR1cwAJ/////wAAAAACKwAAAFJl" +
           "dHVybnMgdGhlIHN0YXR1cyBvZiB0aGUgbWV0aG9kIGV4ZWN1dGlvbi4BACgBAQAAAAEAAAABAAAAAQH/" +
           "////AAAAAA==";

        private const string Store_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCCgQAAAABAAUAAABTdG9yZQEBWRsALwEBWRtZGwAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5w" +
           "dXRBcmd1bWVudHMBAZgXAC4ARJgXAACWAgAAAAEAKgEBswAAAAgAAABKb2JPcmRlcgEBwAv/////AAAA" +
           "AAKWAAAAQ29udGFpbnMgaW5mb3JtYXRpb24gZGVmaW5pbmcgdGhlIGpvYiBvcmRlciB3aXRoIGFsbCBw" +
           "YXJhbWV0ZXJzIGFuZCBhbnkgbWF0ZXJpYWwsIGVxdWlwbWVudCwgb3IgcGh5c2ljYWwgYXNzZXQgcmVx" +
           "dWlyZW1lbnRzIGFzc29jaWF0ZWQgd2l0aCB0aGUgb3JkZXIuAQAqAQHqAAAABwAAAENvbW1lbnQAFQEA" +
           "AAABAAAAAAAAAALMAAAAVGhlIGNvbW1lbnQgcHJvdmlkZXMgYSBkZXNjcmlwdGlvbiBvZiB3aHkgdGhl" +
           "IG1ldGhvZCB3YXMgY2FsbGVkLiBJbiBvcmRlciB0byBwcm92aWRlIHRoZSBjb21tZW50IGluIHNldmVy" +
           "YWwgbGFuZ3VhZ2VzLCBpdCBpcyBhbiBhcnJheSBvZiBMb2NhbGl6ZWRUZXh0LiBUaGUgYXJyYXkgbWF5" +
           "IGJlIGVtcHR5LCB3aGVuIG5vIGNvbW1lbnQgaXMgcHJvdmlkZWQuAQAoAQEAAAABAAAAAgAAAAEB////" +
           "/wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBmRcALgBEmRcAAJYBAAAAAQAqAQFKAAAA" +
           "DAAAAFJldHVyblN0YXR1cwAJ/////wAAAAACKwAAAFJldHVybnMgdGhlIHN0YXR1cyBvZiB0aGUgbWV0" +
           "aG9kIGV4ZWN1dGlvbi4BACgBAQAAAAEAAAABAAAAAQH/////AAAAAA==";

        private const string StoreAndStart_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCCgQAAAABAA0AAABTdG9yZUFuZFN0YXJ0AQFcGwAvAQFcG1wbAAABAf////8CAAAAF2CpCgIAAAAA" +
           "AA4AAABJbnB1dEFyZ3VtZW50cwEBoxcALgBEoxcAAJYCAAAAAQAqAQGzAAAACAAAAEpvYk9yZGVyAQHA" +
           "C/////8AAAAAApYAAABDb250YWlucyBpbmZvcm1hdGlvbiBkZWZpbmluZyB0aGUgam9iIG9yZGVyIHdp" +
           "dGggYWxsIHBhcmFtZXRlcnMgYW5kIGFueSBtYXRlcmlhbCwgZXF1aXBtZW50LCBvciBwaHlzaWNhbCBh" +
           "c3NldCByZXF1aXJlbWVudHMgYXNzb2NpYXRlZCB3aXRoIHRoZSBvcmRlci4BACoBAeoAAAAHAAAAQ29t" +
           "bWVudAAVAQAAAAEAAAAAAAAAAswAAABUaGUgY29tbWVudCBwcm92aWRlcyBhIGRlc2NyaXB0aW9uIG9m" +
           "IHdoeSB0aGUgbWV0aG9kIHdhcyBjYWxsZWQuIEluIG9yZGVyIHRvIHByb3ZpZGUgdGhlIGNvbW1lbnQg" +
           "aW4gc2V2ZXJhbCBsYW5ndWFnZXMsIGl0IGlzIGFuIGFycmF5IG9mIExvY2FsaXplZFRleHQuIFRoZSBh" +
           "cnJheSBtYXkgYmUgZW1wdHksIHdoZW4gbm8gY29tbWVudCBpcyBwcm92aWRlZC4BACgBAQAAAAEAAAAC" +
           "AAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGkFwAuAESkFwAAlgEAAAAB" +
           "ACoBAUoAAAAMAAAAUmV0dXJuU3RhdHVzAAn/////AAAAAAIrAAAAUmV0dXJucyB0aGUgc3RhdHVzIG9m" +
           "IHRoZSBtZXRob2QgZXhlY3V0aW9uLgEAKAEBAAAAAQAAAAEAAAABAf////8AAAAA";

        private const string Update_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCCgQAAAABAAYAAABVcGRhdGUBAWEbAC8BAWEbYRsAAAEBBAAAAAA1AQEBtBMANQEBAbETADUBAQHN" +
           "EwA1AQEB0BMCAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBrRcALgBErRcAAJYCAAAAAQAq" +
           "AQGzAAAACAAAAEpvYk9yZGVyAQHAC/////8AAAAAApYAAABDb250YWlucyBpbmZvcm1hdGlvbiBkZWZp" +
           "bmluZyB0aGUgam9iIG9yZGVyIHdpdGggYWxsIHBhcmFtZXRlcnMgYW5kIGFueSBtYXRlcmlhbCwgZXF1" +
           "aXBtZW50LCBvciBwaHlzaWNhbCBhc3NldCByZXF1aXJlbWVudHMgYXNzb2NpYXRlZCB3aXRoIHRoZSBv" +
           "cmRlci4BACoBAeoAAAAHAAAAQ29tbWVudAAVAQAAAAEAAAAAAAAAAswAAABUaGUgY29tbWVudCBwcm92" +
           "aWRlcyBhIGRlc2NyaXB0aW9uIG9mIHdoeSB0aGUgbWV0aG9kIHdhcyBjYWxsZWQuIEluIG9yZGVyIHRv" +
           "IHByb3ZpZGUgdGhlIGNvbW1lbnQgaW4gc2V2ZXJhbCBsYW5ndWFnZXMsIGl0IGlzIGFuIGFycmF5IG9m" +
           "IExvY2FsaXplZFRleHQuIFRoZSBhcnJheSBtYXkgYmUgZW1wdHksIHdoZW4gbm8gY29tbWVudCBpcyBw" +
           "cm92aWRlZC4BACgBAQAAAAEAAAACAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1l" +
           "bnRzAQGuFwAuAESuFwAAlgEAAAABACoBAUoAAAAMAAAAUmV0dXJuU3RhdHVzAAn/////AAAAAAIrAAAA" +
           "UmV0dXJucyB0aGUgc3RhdHVzIG9mIHRoZSBtZXRob2QgZXhlY3V0aW9uLgEAKAEBAAAAAQAAAAEAAAAB" +
           "Af////8AAAAA";

        private const string InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGCAAgEAAAABACcAAABJU0E5NUpvYk9yZGVyUmVjZWl2ZXJPYmplY3RUeXBlSW5zdGFuY2UBAeoDAQHq" +
           "A+oDAAD/////FAAAABVgiQgCAAAAAAAMAAAAQ3VycmVudFN0YXRlAQEAAAAvAQDICgAV/////wEB////" +
           "/wEAAAAVYIkIAgAAAAAAAgAAAElkAQEAAAAuAEQAEf////8BAf////8AAAAABGGCCgQAAAABAAUAAABB" +
           "Ym9ydAEBYhsALwEBYhtiGwAAAQEIAAAAADUBAQG4EwA1AQEBuRMANQEBAdQTADUBAQHVEwA1AQEB3BMA" +
           "NQEBAd0TADUBAQHeEwA1AQEB3xMCAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBrxcALgBE" +
           "rxcAAJYCAAAAAQAqAQGzAAAACgAAAEpvYk9yZGVySUQADP////8AAAAAApYAAABDb250YWlucyBpbmZv" +
           "cm1hdGlvbiBkZWZpbmluZyB0aGUgam9iIG9yZGVyIHdpdGggYWxsIHBhcmFtZXRlcnMgYW5kIGFueSBt" +
           "YXRlcmlhbCwgZXF1aXBtZW50LCBvciBwaHlzaWNhbCBhc3NldCByZXF1aXJlbWVudHMgYXNzb2NpYXRl" +
           "ZCB3aXRoIHRoZSBvcmRlci4BACoBAeoAAAAHAAAAQ29tbWVudAAVAQAAAAEAAAAAAAAAAswAAABUaGUg" +
           "Y29tbWVudCBwcm92aWRlcyBhIGRlc2NyaXB0aW9uIG9mIHdoeSB0aGUgbWV0aG9kIHdhcyBjYWxsZWQu" +
           "IEluIG9yZGVyIHRvIHByb3ZpZGUgdGhlIGNvbW1lbnQgaW4gc2V2ZXJhbCBsYW5ndWFnZXMsIGl0IGlz" +
           "IGFuIGFycmF5IG9mIExvY2FsaXplZFRleHQuIFRoZSBhcnJheSBtYXkgYmUgZW1wdHksIHdoZW4gbm8g" +
           "Y29tbWVudCBpcyBwcm92aWRlZC4BACgBAQAAAAEAAAACAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAA" +
           "T3V0cHV0QXJndW1lbnRzAQGwFwAuAESwFwAAlgEAAAABACoBAUoAAAAMAAAAUmV0dXJuU3RhdHVzAAn/" +
           "////AAAAAAIrAAAAUmV0dXJucyB0aGUgc3RhdHVzIG9mIHRoZSBtZXRob2QgZXhlY3V0aW9uLgEAKAEB" +
           "AAAAAQAAAAEAAAABAf////8AAAAABGGCCgQAAAABAAYAAABDYW5jZWwBAWMbAC8BAWMbYxsAAAEB////" +
           "/wIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQGxFwAuAESxFwAAlgIAAAABACoBAbMAAAAK" +
           "AAAASm9iT3JkZXJJRAAM/////wAAAAAClgAAAENvbnRhaW5zIGluZm9ybWF0aW9uIGRlZmluaW5nIHRo" +
           "ZSBqb2Igb3JkZXIgd2l0aCBhbGwgcGFyYW1ldGVycyBhbmQgYW55IG1hdGVyaWFsLCBlcXVpcG1lbnQs" +
           "IG9yIHBoeXNpY2FsIGFzc2V0IHJlcXVpcmVtZW50cyBhc3NvY2lhdGVkIHdpdGggdGhlIG9yZGVyLgEA" +
           "KgEB6gAAAAcAAABDb21tZW50ABUBAAAAAQAAAAAAAAACzAAAAFRoZSBjb21tZW50IHByb3ZpZGVzIGEg" +
           "ZGVzY3JpcHRpb24gb2Ygd2h5IHRoZSBtZXRob2Qgd2FzIGNhbGxlZC4gSW4gb3JkZXIgdG8gcHJvdmlk" +
           "ZSB0aGUgY29tbWVudCBpbiBzZXZlcmFsIGxhbmd1YWdlcywgaXQgaXMgYW4gYXJyYXkgb2YgTG9jYWxp" +
           "emVkVGV4dC4gVGhlIGFycmF5IG1heSBiZSBlbXB0eSwgd2hlbiBubyBjb21tZW50IGlzIHByb3ZpZGVk" +
           "LgEAKAEBAAAAAQAAAAIAAAABAf////8AAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAbIX" +
           "AC4ARLIXAACWAQAAAAEAKgEBSgAAAAwAAABSZXR1cm5TdGF0dXMACf////8AAAAAAisAAABSZXR1cm5z" +
           "IHRoZSBzdGF0dXMgb2YgdGhlIG1ldGhvZCBleGVjdXRpb24uAQAoAQEAAAABAAAAAQAAAAEB/////wAA" +
           "AAAEYYIKBAAAAAEABQAAAENsZWFyAQFkGwAvAQFkG2QbAAABAf////8CAAAAF2CpCgIAAAAAAA4AAABJ" +
           "bnB1dEFyZ3VtZW50cwEBsxcALgBEsxcAAJYCAAAAAQAqAQGzAAAACgAAAEpvYk9yZGVySUQADP////8A" +
           "AAAAApYAAABDb250YWlucyBpbmZvcm1hdGlvbiBkZWZpbmluZyB0aGUgam9iIG9yZGVyIHdpdGggYWxs" +
           "IHBhcmFtZXRlcnMgYW5kIGFueSBtYXRlcmlhbCwgZXF1aXBtZW50LCBvciBwaHlzaWNhbCBhc3NldCBy" +
           "ZXF1aXJlbWVudHMgYXNzb2NpYXRlZCB3aXRoIHRoZSBvcmRlci4BACoBAeoAAAAHAAAAQ29tbWVudAAV" +
           "AQAAAAEAAAAAAAAAAswAAABUaGUgY29tbWVudCBwcm92aWRlcyBhIGRlc2NyaXB0aW9uIG9mIHdoeSB0" +
           "aGUgbWV0aG9kIHdhcyBjYWxsZWQuIEluIG9yZGVyIHRvIHByb3ZpZGUgdGhlIGNvbW1lbnQgaW4gc2V2" +
           "ZXJhbCBsYW5ndWFnZXMsIGl0IGlzIGFuIGFycmF5IG9mIExvY2FsaXplZFRleHQuIFRoZSBhcnJheSBt" +
           "YXkgYmUgZW1wdHksIHdoZW4gbm8gY29tbWVudCBpcyBwcm92aWRlZC4BACgBAQAAAAEAAAACAAAAAQH/" +
           "////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQG0FwAuAES0FwAAlgEAAAABACoBAUoA" +
           "AAAMAAAAUmV0dXJuU3RhdHVzAAn/////AAAAAAIrAAAAUmV0dXJucyB0aGUgc3RhdHVzIG9mIHRoZSBt" +
           "ZXRob2QgZXhlY3V0aW9uLgEAKAEBAAAAAQAAAAEAAAABAf////8AAAAAN2CJCgIAAAABAAsAAABFcXVp" +
           "cG1lbnRJRAEBlRcDAAAAAGYAAABEZWZpbmVzIGEgcmVhZC1vbmx5IHNldCBvZiBFcXVpcG1lbnQgQ2xh" +
           "c3MgSURzIGFuZCBFcXVpcG1lbnQgSURzIHRoYXQgbWF5IGJlIHNwZWNpZmllZCBpbiBhIGpvYiBvcmRl" +
           "ci4ALwA/lRcAAAAMAQAAAAEAAAAAAAAAAQH/////AAAAADdgiQoCAAAAAQAMAAAASm9iT3JkZXJMaXN0" +
           "AQGRFwMAAAAATAAAAERlZmluZXMgYSByZWFkLW9ubHkgbGlzdCBvZiBqb2Igb3JkZXIgaW5mb3JtYXRp" +
           "b24gYXZhaWxhYmxlIGZyb20gdGhlIHNlcnZlci4ALwA/kRcAAAEBxwsBAAAAAQAAAAAAAAABAf////8A" +
           "AAAAN2CJCgIAAAABAA8AAABNYXRlcmlhbENsYXNzSUQBAZMXAwAAAABVAAAARGVmaW5lcyBhIHJlYWQt" +
           "b25seSBzZXQgb2YgTWF0ZXJpYWwgQ2xhc3NlcyBJRHMgdGhhdCBtYXkgYmUgc3BlY2lmaWVkIGluIGEg" +
           "am9iIG9yZGVyLgAvAD+TFwAAAAwBAAAAAQAAAAAAAAABAf////8AAAAAN2CJCgIAAAABABQAAABNYXRl" +
           "cmlhbERlZmluaXRpb25JRAEBlBcDAAAAAFUAAABEZWZpbmVzIGEgcmVhZC1vbmx5IHNldCBvZiBNYXRl" +
           "cmlhbCBDbGFzc2VzIElEcyB0aGF0IG1heSBiZSBzcGVjaWZpZWQgaW4gYSBqb2Igb3JkZXIuAC8AP5QX" +
           "AAAADAEAAAABAAAAAAAAAAEB/////wAAAAAVYIkKAgAAAAEAGAAAAE1heERvd25sb2FkYWJsZUpvYk9y" +
           "ZGVycwEByBcALgBEyBcAAAAF/////wEB/////wAAAAAEYYIKBAAAAAEABQAAAFBhdXNlAQFfGwAvAQFf" +
           "G18bAAABAQIAAAAANQEBAbYTADUBAQHSEwIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQGp" +
           "FwAuAESpFwAAlgIAAAABACoBAbMAAAAKAAAASm9iT3JkZXJJRAAM/////wAAAAAClgAAAENvbnRhaW5z" +
           "IGluZm9ybWF0aW9uIGRlZmluaW5nIHRoZSBqb2Igb3JkZXIgd2l0aCBhbGwgcGFyYW1ldGVycyBhbmQg" +
           "YW55IG1hdGVyaWFsLCBlcXVpcG1lbnQsIG9yIHBoeXNpY2FsIGFzc2V0IHJlcXVpcmVtZW50cyBhc3Nv" +
           "Y2lhdGVkIHdpdGggdGhlIG9yZGVyLgEAKgEB6gAAAAcAAABDb21tZW50ABUBAAAAAQAAAAAAAAACzAAA" +
           "AFRoZSBjb21tZW50IHByb3ZpZGVzIGEgZGVzY3JpcHRpb24gb2Ygd2h5IHRoZSBtZXRob2Qgd2FzIGNh" +
           "bGxlZC4gSW4gb3JkZXIgdG8gcHJvdmlkZSB0aGUgY29tbWVudCBpbiBzZXZlcmFsIGxhbmd1YWdlcywg" +
           "aXQgaXMgYW4gYXJyYXkgb2YgTG9jYWxpemVkVGV4dC4gVGhlIGFycmF5IG1heSBiZSBlbXB0eSwgd2hl" +
           "biBubyBjb21tZW50IGlzIHByb3ZpZGVkLgEAKAEBAAAAAQAAAAIAAAABAf////8AAAAAF2CpCgIAAAAA" +
           "AA8AAABPdXRwdXRBcmd1bWVudHMBAaoXAC4ARKoXAACWAQAAAAEAKgEBSgAAAAwAAABSZXR1cm5TdGF0" +
           "dXMACf////8AAAAAAisAAABSZXR1cm5zIHRoZSBzdGF0dXMgb2YgdGhlIG1ldGhvZCBleGVjdXRpb24u" +
           "AQAoAQEAAAABAAAAAQAAAAEB/////wAAAAA3YIkKAgAAAAEACwAAAFBlcnNvbm5lbElEAQGXFwMAAAAA" +
           "XQAAAERlZmluZXMgYSByZWFkLW9ubHkgc2V0IG9mIFBlcnNvbm5lbCBJRHMgYW5kIFBlcnNvbiBJRHMg" +
           "dGhhdCBtYXkgYmUgc3BlY2lmaWVkIGluIGEgam9iIG9yZGVyLgAvAD+XFwAAAAwBAAAAAQAAAAAAAAAB" +
           "Af////8AAAAAN2CJCgIAAAABAA8AAABQaHlzaWNhbEFzc2V0SUQBAZYXAwAAAABwAAAARGVmaW5lcyBh" +
           "IHJlYWQtb25seSBzZXQgb2YgUGh5c2ljYWwgQXNzZXQgQ2xhc3MgSURzIGFuZCBQaHlzaWNhbCBBc3Nl" +
           "dCBJRHMgdGhhdCBtYXkgYmUgc3BlY2lmaWVkIGluIGEgam9iIG9yZGVyLgAvAD+WFwAAAAwBAAAAAQAA" +
           "AAAAAAABAf////8AAAAABGGCCgQAAAABAAYAAABSZXN1bWUBAWAbAC8BAWAbYBsAAAEBAgAAAAA1AQEB" +
           "uhMANQEBAdYTAgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAasXAC4ARKsXAACWAgAAAAEA" +
           "KgEBswAAAAoAAABKb2JPcmRlcklEAAz/////AAAAAAKWAAAAQ29udGFpbnMgaW5mb3JtYXRpb24gZGVm" +
           "aW5pbmcgdGhlIGpvYiBvcmRlciB3aXRoIGFsbCBwYXJhbWV0ZXJzIGFuZCBhbnkgbWF0ZXJpYWwsIGVx" +
           "dWlwbWVudCwgb3IgcGh5c2ljYWwgYXNzZXQgcmVxdWlyZW1lbnRzIGFzc29jaWF0ZWQgd2l0aCB0aGUg" +
           "b3JkZXIuAQAqAQHqAAAABwAAAENvbW1lbnQAFQEAAAABAAAAAAAAAALMAAAAVGhlIGNvbW1lbnQgcHJv" +
           "dmlkZXMgYSBkZXNjcmlwdGlvbiBvZiB3aHkgdGhlIG1ldGhvZCB3YXMgY2FsbGVkLiBJbiBvcmRlciB0" +
           "byBwcm92aWRlIHRoZSBjb21tZW50IGluIHNldmVyYWwgbGFuZ3VhZ2VzLCBpdCBpcyBhbiBhcnJheSBv" +
           "ZiBMb2NhbGl6ZWRUZXh0LiBUaGUgYXJyYXkgbWF5IGJlIGVtcHR5LCB3aGVuIG5vIGNvbW1lbnQgaXMg" +
           "cHJvdmlkZWQuAQAoAQEAAAABAAAAAgAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3Vt" +
           "ZW50cwEBrBcALgBErBcAAJYBAAAAAQAqAQFKAAAADAAAAFJldHVyblN0YXR1cwAJ/////wAAAAACKwAA" +
           "AFJldHVybnMgdGhlIHN0YXR1cyBvZiB0aGUgbWV0aG9kIGV4ZWN1dGlvbi4BACgBAQAAAAEAAAABAAAA" +
           "AQH/////AAAAAARhggoEAAAAAQALAAAAUmV2b2tlU3RhcnQBAWUbAC8BAWUbZRsAAAEBAgAAAAA1AQEB" +
           "sxMANQEBAc8TAgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAbUXAC4ARLUXAACWAgAAAAEA" +
           "KgEBswAAAAoAAABKb2JPcmRlcklEAAz/////AAAAAAKWAAAAQ29udGFpbnMgaW5mb3JtYXRpb24gZGVm" +
           "aW5pbmcgdGhlIGpvYiBvcmRlciB3aXRoIGFsbCBwYXJhbWV0ZXJzIGFuZCBhbnkgbWF0ZXJpYWwsIGVx" +
           "dWlwbWVudCwgb3IgcGh5c2ljYWwgYXNzZXQgcmVxdWlyZW1lbnRzIGFzc29jaWF0ZWQgd2l0aCB0aGUg" +
           "b3JkZXIuAQAqAQHqAAAABwAAAENvbW1lbnQAFQEAAAABAAAAAAAAAALMAAAAVGhlIGNvbW1lbnQgcHJv" +
           "dmlkZXMgYSBkZXNjcmlwdGlvbiBvZiB3aHkgdGhlIG1ldGhvZCB3YXMgY2FsbGVkLiBJbiBvcmRlciB0" +
           "byBwcm92aWRlIHRoZSBjb21tZW50IGluIHNldmVyYWwgbGFuZ3VhZ2VzLCBpdCBpcyBhbiBhcnJheSBv" +
           "ZiBMb2NhbGl6ZWRUZXh0LiBUaGUgYXJyYXkgbWF5IGJlIGVtcHR5LCB3aGVuIG5vIGNvbW1lbnQgaXMg" +
           "cHJvdmlkZWQuAQAoAQEAAAABAAAAAgAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3Vt" +
           "ZW50cwEBthcALgBEthcAAJYBAAAAAQAqAQFKAAAADAAAAFJldHVyblN0YXR1cwAJ/////wAAAAACKwAA" +
           "AFJldHVybnMgdGhlIHN0YXR1cyBvZiB0aGUgbWV0aG9kIGV4ZWN1dGlvbi4BACgBAQAAAAEAAAABAAAA" +
           "AQH/////AAAAAARhggoEAAAAAQAFAAAAU3RhcnQBAV0bAC8BAV0bXRsAAAEBAgAAAAA1AQEBshMANQEB" +
           "Ac4TAgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAaUXAC4ARKUXAACWAgAAAAEAKgEBswAA" +
           "AAoAAABKb2JPcmRlcklEAAz/////AAAAAAKWAAAAQ29udGFpbnMgaW5mb3JtYXRpb24gZGVmaW5pbmcg" +
           "dGhlIGpvYiBvcmRlciB3aXRoIGFsbCBwYXJhbWV0ZXJzIGFuZCBhbnkgbWF0ZXJpYWwsIGVxdWlwbWVu" +
           "dCwgb3IgcGh5c2ljYWwgYXNzZXQgcmVxdWlyZW1lbnRzIGFzc29jaWF0ZWQgd2l0aCB0aGUgb3JkZXIu" +
           "AQAqAQHqAAAABwAAAENvbW1lbnQAFQEAAAABAAAAAAAAAALMAAAAVGhlIGNvbW1lbnQgcHJvdmlkZXMg" +
           "YSBkZXNjcmlwdGlvbiBvZiB3aHkgdGhlIG1ldGhvZCB3YXMgY2FsbGVkLiBJbiBvcmRlciB0byBwcm92" +
           "aWRlIHRoZSBjb21tZW50IGluIHNldmVyYWwgbGFuZ3VhZ2VzLCBpdCBpcyBhbiBhcnJheSBvZiBMb2Nh" +
           "bGl6ZWRUZXh0LiBUaGUgYXJyYXkgbWF5IGJlIGVtcHR5LCB3aGVuIG5vIGNvbW1lbnQgaXMgcHJvdmlk" +
           "ZWQuAQAoAQEAAAABAAAAAgAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEB" +
           "phcALgBEphcAAJYBAAAAAQAqAQFKAAAADAAAAFJldHVyblN0YXR1cwAJ/////wAAAAACKwAAAFJldHVy" +
           "bnMgdGhlIHN0YXR1cyBvZiB0aGUgbWV0aG9kIGV4ZWN1dGlvbi4BACgBAQAAAAEAAAABAAAAAQH/////" +
           "AAAAAARhggoEAAAAAQAEAAAAU3RvcAEBXhsALwEBXhteGwAAAQEEAAAAADUBAQG7EwA1AQEBtxMANQEB" +
           "AdMTADUBAQHXEwIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQGnFwAuAESnFwAAlgIAAAAB" +
           "ACoBAbMAAAAKAAAASm9iT3JkZXJJRAAM/////wAAAAAClgAAAENvbnRhaW5zIGluZm9ybWF0aW9uIGRl" +
           "ZmluaW5nIHRoZSBqb2Igb3JkZXIgd2l0aCBhbGwgcGFyYW1ldGVycyBhbmQgYW55IG1hdGVyaWFsLCBl" +
           "cXVpcG1lbnQsIG9yIHBoeXNpY2FsIGFzc2V0IHJlcXVpcmVtZW50cyBhc3NvY2lhdGVkIHdpdGggdGhl" +
           "IG9yZGVyLgEAKgEB6gAAAAcAAABDb21tZW50ABUBAAAAAQAAAAAAAAACzAAAAFRoZSBjb21tZW50IHBy" +
           "b3ZpZGVzIGEgZGVzY3JpcHRpb24gb2Ygd2h5IHRoZSBtZXRob2Qgd2FzIGNhbGxlZC4gSW4gb3JkZXIg" +
           "dG8gcHJvdmlkZSB0aGUgY29tbWVudCBpbiBzZXZlcmFsIGxhbmd1YWdlcywgaXQgaXMgYW4gYXJyYXkg" +
           "b2YgTG9jYWxpemVkVGV4dC4gVGhlIGFycmF5IG1heSBiZSBlbXB0eSwgd2hlbiBubyBjb21tZW50IGlz" +
           "IHByb3ZpZGVkLgEAKAEBAAAAAQAAAAIAAAABAf////8AAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1" +
           "bWVudHMBAagXAC4ARKgXAACWAQAAAAEAKgEBSgAAAAwAAABSZXR1cm5TdGF0dXMACf////8AAAAAAisA" +
           "AABSZXR1cm5zIHRoZSBzdGF0dXMgb2YgdGhlIG1ldGhvZCBleGVjdXRpb24uAQAoAQEAAAABAAAAAQAA" +
           "AAEB/////wAAAAAEYYIKBAAAAAEABQAAAFN0b3JlAQFZGwAvAQFZG1kbAAABAf////8CAAAAF2CpCgIA" +
           "AAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBmBcALgBEmBcAAJYCAAAAAQAqAQGzAAAACAAAAEpvYk9yZGVy" +
           "AQHAC/////8AAAAAApYAAABDb250YWlucyBpbmZvcm1hdGlvbiBkZWZpbmluZyB0aGUgam9iIG9yZGVy" +
           "IHdpdGggYWxsIHBhcmFtZXRlcnMgYW5kIGFueSBtYXRlcmlhbCwgZXF1aXBtZW50LCBvciBwaHlzaWNh" +
           "bCBhc3NldCByZXF1aXJlbWVudHMgYXNzb2NpYXRlZCB3aXRoIHRoZSBvcmRlci4BACoBAeoAAAAHAAAA" +
           "Q29tbWVudAAVAQAAAAEAAAAAAAAAAswAAABUaGUgY29tbWVudCBwcm92aWRlcyBhIGRlc2NyaXB0aW9u" +
           "IG9mIHdoeSB0aGUgbWV0aG9kIHdhcyBjYWxsZWQuIEluIG9yZGVyIHRvIHByb3ZpZGUgdGhlIGNvbW1l" +
           "bnQgaW4gc2V2ZXJhbCBsYW5ndWFnZXMsIGl0IGlzIGFuIGFycmF5IG9mIExvY2FsaXplZFRleHQuIFRo" +
           "ZSBhcnJheSBtYXkgYmUgZW1wdHksIHdoZW4gbm8gY29tbWVudCBpcyBwcm92aWRlZC4BACgBAQAAAAEA" +
           "AAACAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGZFwAuAESZFwAAlgEA" +
           "AAABACoBAUoAAAAMAAAAUmV0dXJuU3RhdHVzAAn/////AAAAAAIrAAAAUmV0dXJucyB0aGUgc3RhdHVz" +
           "IG9mIHRoZSBtZXRob2QgZXhlY3V0aW9uLgEAKAEBAAAAAQAAAAEAAAABAf////8AAAAABGGCCgQAAAAB" +
           "AA0AAABTdG9yZUFuZFN0YXJ0AQFcGwAvAQFcG1wbAAABAf////8CAAAAF2CpCgIAAAAAAA4AAABJbnB1" +
           "dEFyZ3VtZW50cwEBoxcALgBEoxcAAJYCAAAAAQAqAQGzAAAACAAAAEpvYk9yZGVyAQHAC/////8AAAAA" +
           "ApYAAABDb250YWlucyBpbmZvcm1hdGlvbiBkZWZpbmluZyB0aGUgam9iIG9yZGVyIHdpdGggYWxsIHBh" +
           "cmFtZXRlcnMgYW5kIGFueSBtYXRlcmlhbCwgZXF1aXBtZW50LCBvciBwaHlzaWNhbCBhc3NldCByZXF1" +
           "aXJlbWVudHMgYXNzb2NpYXRlZCB3aXRoIHRoZSBvcmRlci4BACoBAeoAAAAHAAAAQ29tbWVudAAVAQAA" +
           "AAEAAAAAAAAAAswAAABUaGUgY29tbWVudCBwcm92aWRlcyBhIGRlc2NyaXB0aW9uIG9mIHdoeSB0aGUg" +
           "bWV0aG9kIHdhcyBjYWxsZWQuIEluIG9yZGVyIHRvIHByb3ZpZGUgdGhlIGNvbW1lbnQgaW4gc2V2ZXJh" +
           "bCBsYW5ndWFnZXMsIGl0IGlzIGFuIGFycmF5IG9mIExvY2FsaXplZFRleHQuIFRoZSBhcnJheSBtYXkg" +
           "YmUgZW1wdHksIHdoZW4gbm8gY29tbWVudCBpcyBwcm92aWRlZC4BACgBAQAAAAEAAAACAAAAAQH/////" +
           "AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGkFwAuAESkFwAAlgEAAAABACoBAUoAAAAM" +
           "AAAAUmV0dXJuU3RhdHVzAAn/////AAAAAAIrAAAAUmV0dXJucyB0aGUgc3RhdHVzIG9mIHRoZSBtZXRo" +
           "b2QgZXhlY3V0aW9uLgEAKAEBAAAAAQAAAAEAAAABAf////8AAAAABGGCCgQAAAABAAYAAABVcGRhdGUB" +
           "AWEbAC8BAWEbYRsAAAEBBAAAAAA1AQEBtBMANQEBAbETADUBAQHNEwA1AQEB0BMCAAAAF2CpCgIAAAAA" +
           "AA4AAABJbnB1dEFyZ3VtZW50cwEBrRcALgBErRcAAJYCAAAAAQAqAQGzAAAACAAAAEpvYk9yZGVyAQHA" +
           "C/////8AAAAAApYAAABDb250YWlucyBpbmZvcm1hdGlvbiBkZWZpbmluZyB0aGUgam9iIG9yZGVyIHdp" +
           "dGggYWxsIHBhcmFtZXRlcnMgYW5kIGFueSBtYXRlcmlhbCwgZXF1aXBtZW50LCBvciBwaHlzaWNhbCBh" +
           "c3NldCByZXF1aXJlbWVudHMgYXNzb2NpYXRlZCB3aXRoIHRoZSBvcmRlci4BACoBAeoAAAAHAAAAQ29t" +
           "bWVudAAVAQAAAAEAAAAAAAAAAswAAABUaGUgY29tbWVudCBwcm92aWRlcyBhIGRlc2NyaXB0aW9uIG9m" +
           "IHdoeSB0aGUgbWV0aG9kIHdhcyBjYWxsZWQuIEluIG9yZGVyIHRvIHByb3ZpZGUgdGhlIGNvbW1lbnQg" +
           "aW4gc2V2ZXJhbCBsYW5ndWFnZXMsIGl0IGlzIGFuIGFycmF5IG9mIExvY2FsaXplZFRleHQuIFRoZSBh" +
           "cnJheSBtYXkgYmUgZW1wdHksIHdoZW4gbm8gY29tbWVudCBpcyBwcm92aWRlZC4BACgBAQAAAAEAAAAC" +
           "AAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGuFwAuAESuFwAAlgEAAAAB" +
           "ACoBAUoAAAAMAAAAUmV0dXJuU3RhdHVzAAn/////AAAAAAIrAAAAUmV0dXJucyB0aGUgc3RhdHVzIG9m" +
           "IHRoZSBtZXRob2QgZXhlY3V0aW9uLgEAKAEBAAAAAQAAAAEAAAABAf////8AAAAAN2CJCgIAAAABAAoA" +
           "AABXb3JrTWFzdGVyAQGSFwMAAAAApgAAAERlZmluZXMgYSByZWFkLW9ubHkgc2V0IG9mIHdvcmsgbWFz" +
           "dGVyIElEcyB0aGF0IG1heSBiZSBzcGVjaWZpZWQgaW4gYSBqb2Igb3JkZXIsIGFuZCB0aGUgcmVhZC1v" +
           "bmx5IHNldCBvZiBwYXJhbWV0ZXJzIHRoYXQgbWF5IGJlIHNwZWNpZmllZCBmb3IgYSBzcGVjaWZpYyB3" +
           "b3JrIG1hc3Rlci4ALwA/khcAAAEBvwsBAAAAAQAAAAAAAAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public AbortMethodState Abort
        {
            get
            {
                return m_abortMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_abortMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_abortMethod = value;
            }
        }

        /// <remarks />
        public CancelMethodState Cancel
        {
            get
            {
                return m_cancelMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_cancelMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_cancelMethod = value;
            }
        }

        /// <remarks />
        public ClearMethodState Clear
        {
            get
            {
                return m_clearMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_clearMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_clearMethod = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<string[]> EquipmentID
        {
            get
            {
                return m_equipmentID;
            }

            set
            {
                if (!Object.ReferenceEquals(m_equipmentID, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_equipmentID = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<ISA95JobOrderAndStateDataType[]> JobOrderList
        {
            get
            {
                return m_jobOrderList;
            }

            set
            {
                if (!Object.ReferenceEquals(m_jobOrderList, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_jobOrderList = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<string[]> MaterialClassID
        {
            get
            {
                return m_materialClassID;
            }

            set
            {
                if (!Object.ReferenceEquals(m_materialClassID, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_materialClassID = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<string[]> MaterialDefinitionID
        {
            get
            {
                return m_materialDefinitionID;
            }

            set
            {
                if (!Object.ReferenceEquals(m_materialDefinitionID, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_materialDefinitionID = value;
            }
        }

        /// <remarks />
        public PropertyState<ushort> MaxDownloadableJobOrders
        {
            get
            {
                return m_maxDownloadableJobOrders;
            }

            set
            {
                if (!Object.ReferenceEquals(m_maxDownloadableJobOrders, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_maxDownloadableJobOrders = value;
            }
        }

        /// <remarks />
        public PauseMethodState Pause
        {
            get
            {
                return m_pauseMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_pauseMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_pauseMethod = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<string[]> PersonnelID
        {
            get
            {
                return m_personnelID;
            }

            set
            {
                if (!Object.ReferenceEquals(m_personnelID, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_personnelID = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<string[]> PhysicalAssetID
        {
            get
            {
                return m_physicalAssetID;
            }

            set
            {
                if (!Object.ReferenceEquals(m_physicalAssetID, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_physicalAssetID = value;
            }
        }

        /// <remarks />
        public ResumeMethodState Resume
        {
            get
            {
                return m_resumeMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_resumeMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_resumeMethod = value;
            }
        }

        /// <remarks />
        public RevokeStartMethodState RevokeStart
        {
            get
            {
                return m_revokeStartMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_revokeStartMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_revokeStartMethod = value;
            }
        }

        /// <remarks />
        public StartMethodState Start
        {
            get
            {
                return m_startMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_startMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_startMethod = value;
            }
        }

        /// <remarks />
        public StopMethodState Stop
        {
            get
            {
                return m_stopMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_stopMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_stopMethod = value;
            }
        }

        /// <remarks />
        public StoreMethodState Store
        {
            get
            {
                return m_storeMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_storeMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_storeMethod = value;
            }
        }

        /// <remarks />
        public StoreAndStartMethodState StoreAndStart
        {
            get
            {
                return m_storeAndStartMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_storeAndStartMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_storeAndStartMethod = value;
            }
        }

        /// <remarks />
        public new UpdateMethodState Update
        {
            get
            {
                return m_updateMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_updateMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_updateMethod = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<ISA95WorkMasterDataType[]> WorkMaster
        {
            get
            {
                return m_workMaster;
            }

            set
            {
                if (!Object.ReferenceEquals(m_workMaster, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_workMaster = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_abortMethod != null)
            {
                children.Add(m_abortMethod);
            }

            if (m_cancelMethod != null)
            {
                children.Add(m_cancelMethod);
            }

            if (m_clearMethod != null)
            {
                children.Add(m_clearMethod);
            }

            if (m_equipmentID != null)
            {
                children.Add(m_equipmentID);
            }

            if (m_jobOrderList != null)
            {
                children.Add(m_jobOrderList);
            }

            if (m_materialClassID != null)
            {
                children.Add(m_materialClassID);
            }

            if (m_materialDefinitionID != null)
            {
                children.Add(m_materialDefinitionID);
            }

            if (m_maxDownloadableJobOrders != null)
            {
                children.Add(m_maxDownloadableJobOrders);
            }

            if (m_pauseMethod != null)
            {
                children.Add(m_pauseMethod);
            }

            if (m_personnelID != null)
            {
                children.Add(m_personnelID);
            }

            if (m_physicalAssetID != null)
            {
                children.Add(m_physicalAssetID);
            }

            if (m_resumeMethod != null)
            {
                children.Add(m_resumeMethod);
            }

            if (m_revokeStartMethod != null)
            {
                children.Add(m_revokeStartMethod);
            }

            if (m_startMethod != null)
            {
                children.Add(m_startMethod);
            }

            if (m_stopMethod != null)
            {
                children.Add(m_stopMethod);
            }

            if (m_storeMethod != null)
            {
                children.Add(m_storeMethod);
            }

            if (m_storeAndStartMethod != null)
            {
                children.Add(m_storeAndStartMethod);
            }

            if (m_updateMethod != null)
            {
                children.Add(m_updateMethod);
            }

            if (m_workMaster != null)
            {
                children.Add(m_workMaster);
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
                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.Abort:
                    {
                        if (createOrReplace)
                        {
                            if (Abort == null)
                            {
                                if (replacement == null)
                                {
                                    Abort = new AbortMethodState(this);
                                }
                                else
                                {
                                    Abort = (AbortMethodState)replacement;
                                }
                            }
                        }

                        instance = Abort;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.Cancel:
                    {
                        if (createOrReplace)
                        {
                            if (Cancel == null)
                            {
                                if (replacement == null)
                                {
                                    Cancel = new CancelMethodState(this);
                                }
                                else
                                {
                                    Cancel = (CancelMethodState)replacement;
                                }
                            }
                        }

                        instance = Cancel;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.Clear:
                    {
                        if (createOrReplace)
                        {
                            if (Clear == null)
                            {
                                if (replacement == null)
                                {
                                    Clear = new ClearMethodState(this);
                                }
                                else
                                {
                                    Clear = (ClearMethodState)replacement;
                                }
                            }
                        }

                        instance = Clear;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.EquipmentID:
                    {
                        if (createOrReplace)
                        {
                            if (EquipmentID == null)
                            {
                                if (replacement == null)
                                {
                                    EquipmentID = new BaseDataVariableState<string[]>(this);
                                }
                                else
                                {
                                    EquipmentID = (BaseDataVariableState<string[]>)replacement;
                                }
                            }
                        }

                        instance = EquipmentID;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.JobOrderList:
                    {
                        if (createOrReplace)
                        {
                            if (JobOrderList == null)
                            {
                                if (replacement == null)
                                {
                                    JobOrderList = new BaseDataVariableState<ISA95JobOrderAndStateDataType[]>(this);
                                }
                                else
                                {
                                    JobOrderList = (BaseDataVariableState<ISA95JobOrderAndStateDataType[]>)replacement;
                                }
                            }
                        }

                        instance = JobOrderList;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.MaterialClassID:
                    {
                        if (createOrReplace)
                        {
                            if (MaterialClassID == null)
                            {
                                if (replacement == null)
                                {
                                    MaterialClassID = new BaseDataVariableState<string[]>(this);
                                }
                                else
                                {
                                    MaterialClassID = (BaseDataVariableState<string[]>)replacement;
                                }
                            }
                        }

                        instance = MaterialClassID;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.MaterialDefinitionID:
                    {
                        if (createOrReplace)
                        {
                            if (MaterialDefinitionID == null)
                            {
                                if (replacement == null)
                                {
                                    MaterialDefinitionID = new BaseDataVariableState<string[]>(this);
                                }
                                else
                                {
                                    MaterialDefinitionID = (BaseDataVariableState<string[]>)replacement;
                                }
                            }
                        }

                        instance = MaterialDefinitionID;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.MaxDownloadableJobOrders:
                    {
                        if (createOrReplace)
                        {
                            if (MaxDownloadableJobOrders == null)
                            {
                                if (replacement == null)
                                {
                                    MaxDownloadableJobOrders = new PropertyState<ushort>(this);
                                }
                                else
                                {
                                    MaxDownloadableJobOrders = (PropertyState<ushort>)replacement;
                                }
                            }
                        }

                        instance = MaxDownloadableJobOrders;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.Pause:
                    {
                        if (createOrReplace)
                        {
                            if (Pause == null)
                            {
                                if (replacement == null)
                                {
                                    Pause = new PauseMethodState(this);
                                }
                                else
                                {
                                    Pause = (PauseMethodState)replacement;
                                }
                            }
                        }

                        instance = Pause;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.PersonnelID:
                    {
                        if (createOrReplace)
                        {
                            if (PersonnelID == null)
                            {
                                if (replacement == null)
                                {
                                    PersonnelID = new BaseDataVariableState<string[]>(this);
                                }
                                else
                                {
                                    PersonnelID = (BaseDataVariableState<string[]>)replacement;
                                }
                            }
                        }

                        instance = PersonnelID;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.PhysicalAssetID:
                    {
                        if (createOrReplace)
                        {
                            if (PhysicalAssetID == null)
                            {
                                if (replacement == null)
                                {
                                    PhysicalAssetID = new BaseDataVariableState<string[]>(this);
                                }
                                else
                                {
                                    PhysicalAssetID = (BaseDataVariableState<string[]>)replacement;
                                }
                            }
                        }

                        instance = PhysicalAssetID;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.Resume:
                    {
                        if (createOrReplace)
                        {
                            if (Resume == null)
                            {
                                if (replacement == null)
                                {
                                    Resume = new ResumeMethodState(this);
                                }
                                else
                                {
                                    Resume = (ResumeMethodState)replacement;
                                }
                            }
                        }

                        instance = Resume;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.RevokeStart:
                    {
                        if (createOrReplace)
                        {
                            if (RevokeStart == null)
                            {
                                if (replacement == null)
                                {
                                    RevokeStart = new RevokeStartMethodState(this);
                                }
                                else
                                {
                                    RevokeStart = (RevokeStartMethodState)replacement;
                                }
                            }
                        }

                        instance = RevokeStart;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.Start:
                    {
                        if (createOrReplace)
                        {
                            if (Start == null)
                            {
                                if (replacement == null)
                                {
                                    Start = new StartMethodState(this);
                                }
                                else
                                {
                                    Start = (StartMethodState)replacement;
                                }
                            }
                        }

                        instance = Start;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.Stop:
                    {
                        if (createOrReplace)
                        {
                            if (Stop == null)
                            {
                                if (replacement == null)
                                {
                                    Stop = new StopMethodState(this);
                                }
                                else
                                {
                                    Stop = (StopMethodState)replacement;
                                }
                            }
                        }

                        instance = Stop;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.Store:
                    {
                        if (createOrReplace)
                        {
                            if (Store == null)
                            {
                                if (replacement == null)
                                {
                                    Store = new StoreMethodState(this);
                                }
                                else
                                {
                                    Store = (StoreMethodState)replacement;
                                }
                            }
                        }

                        instance = Store;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.StoreAndStart:
                    {
                        if (createOrReplace)
                        {
                            if (StoreAndStart == null)
                            {
                                if (replacement == null)
                                {
                                    StoreAndStart = new StoreAndStartMethodState(this);
                                }
                                else
                                {
                                    StoreAndStart = (StoreAndStartMethodState)replacement;
                                }
                            }
                        }

                        instance = StoreAndStart;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.Update:
                    {
                        if (createOrReplace)
                        {
                            if (Update == null)
                            {
                                if (replacement == null)
                                {
                                    Update = new UpdateMethodState(this);
                                }
                                else
                                {
                                    Update = (UpdateMethodState)replacement;
                                }
                            }
                        }

                        instance = Update;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.WorkMaster:
                    {
                        if (createOrReplace)
                        {
                            if (WorkMaster == null)
                            {
                                if (replacement == null)
                                {
                                    WorkMaster = new BaseDataVariableState<ISA95WorkMasterDataType[]>(this);
                                }
                                else
                                {
                                    WorkMaster = (BaseDataVariableState<ISA95WorkMasterDataType[]>)replacement;
                                }
                            }
                        }

                        instance = WorkMaster;
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
        private AbortMethodState m_abortMethod;
        private CancelMethodState m_cancelMethod;
        private ClearMethodState m_clearMethod;
        private BaseDataVariableState<string[]> m_equipmentID;
        private BaseDataVariableState<ISA95JobOrderAndStateDataType[]> m_jobOrderList;
        private BaseDataVariableState<string[]> m_materialClassID;
        private BaseDataVariableState<string[]> m_materialDefinitionID;
        private PropertyState<ushort> m_maxDownloadableJobOrders;
        private PauseMethodState m_pauseMethod;
        private BaseDataVariableState<string[]> m_personnelID;
        private BaseDataVariableState<string[]> m_physicalAssetID;
        private ResumeMethodState m_resumeMethod;
        private RevokeStartMethodState m_revokeStartMethod;
        private StartMethodState m_startMethod;
        private StopMethodState m_stopMethod;
        private StoreMethodState m_storeMethod;
        private StoreAndStartMethodState m_storeAndStartMethod;
        private UpdateMethodState m_updateMethod;
        private BaseDataVariableState<ISA95WorkMasterDataType[]> m_workMaster;
        #endregion
    }
#endif
    #endregion

    #region ISA95JobOrderReceiverSubStatesTypeState Class
#if (!OPCUA_EXCLUDE_ISA95JobOrderReceiverSubStatesTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ISA95JobOrderReceiverSubStatesTypeState : ISA95JobOrderReceiverObjectTypeState
    {
        #region Constructors
        /// <remarks />
        public ISA95JobOrderReceiverSubStatesTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95JobOrderReceiverSubStatesType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, namespaceUris);
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

            if (AllowedToStartSubstates != null)
            {
                AllowedToStartSubstates.Initialize(context, AllowedToStartSubstates_InitializationString);
            }

            if (EndedSubstates != null)
            {
                EndedSubstates.Initialize(context, EndedSubstates_InitializationString);
            }

            if (InterruptedSubstates != null)
            {
                InterruptedSubstates.Initialize(context, InterruptedSubstates_InitializationString);
            }

            if (NotAllowedToStartSubstates != null)
            {
                NotAllowedToStartSubstates.Initialize(context, NotAllowedToStartSubstates_InitializationString);
            }
        }

        #region Initialization String
        private const string AllowedToStartSubstates_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "JGCACgEAAAABABcAAABBbGxvd2VkVG9TdGFydFN1YnN0YXRlcwEB2RMDAAAAABsAAABTdWJzdGF0ZXMg" +
           "b2YgQWxsb3dlZFRvU3RhcnQALwEB6QPZEwAAAQAAAAB1AQEByBMBAAAAFWCJCgIAAAAAAAwAAABDdXJy" +
           "ZW50U3RhdGUBAXMXAC8BAMgKcxcAAAAV/////wEB/////wEAAAAVYIkKAgAAAAAAAgAAAElkAQF0FwAu" +
           "AER0FwAAABH/////AQH/////AAAAAA==";

        private const string EndedSubstates_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "JGCACgEAAAABAA4AAABFbmRlZFN1YnN0YXRlcwEB2hMDAAAAABIAAABTdWJzdGF0ZXMgb2YgRW5kZWQA" +
           "LwEB7QPaEwAAAQAAAAB1AQEByxMBAAAAFWCJCgIAAAAAAAwAAABDdXJyZW50U3RhdGUBAXUXAC8BAMgK" +
           "dRcAAAAV/////wEB/////wEAAAAVYIkKAgAAAAAAAgAAAElkAQF2FwAuAER2FwAAABH/////AQH/////" +
           "AAAAAA==";

        private const string InterruptedSubstates_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "JGCACgEAAAABABQAAABJbnRlcnJ1cHRlZFN1YnN0YXRlcwEB2xMDAAAAABgAAABTdWJzdGF0ZXMgb2Yg" +
           "SW50ZXJydXB0ZWQALwEB7wPbEwAAAQAAAAB1AQEByhMBAAAAFWCJCgIAAAAAAAwAAABDdXJyZW50U3Rh" +
           "dGUBAXcXAC8BAMgKdxcAAAAV/////wEB/////wEAAAAVYIkKAgAAAAAAAgAAAElkAQF4FwAuAER4FwAA" +
           "ABH/////AQH/////AAAAAA==";

        private const string NotAllowedToStartSubstates_InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "JGCACgEAAAABABoAAABOb3RBbGxvd2VkVG9TdGFydFN1YnN0YXRlcwEB2BMDAAAAAB4AAABTdWJzdGF0" +
           "ZXMgb2YgTm90QWxsb3dlZFRvU3RhcnQALwEB6QPYEwAAAQAAAAB1AQEBxxMBAAAAFWCJCgIAAAAAAAwA" +
           "AABDdXJyZW50U3RhdGUBAXEXAC8BAMgKcRcAAAAV/////wEB/////wEAAAAVYIkKAgAAAAAAAgAAAElk" +
           "AQFyFwAuAERyFwAAABH/////AQH/////AAAAAA==";

        private const string InitializationString =
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGCAAgEAAAABACoAAABJU0E5NUpvYk9yZGVyUmVjZWl2ZXJTdWJTdGF0ZXNUeXBlSW5zdGFuY2UBAfAD" +
           "AQHwA/ADAAD/////DQAAABVgiQgCAAAAAAAMAAAAQ3VycmVudFN0YXRlAQEAAAAvAQDICgAV/////wEB" +
           "/////wEAAAAVYIkIAgAAAAAAAgAAAElkAQEAAAAuAEQAEf////8BAf////8AAAAAN2CJCgIAAAABAAsA" +
           "AABFcXVpcG1lbnRJRAEBlRcDAAAAAGYAAABEZWZpbmVzIGEgcmVhZC1vbmx5IHNldCBvZiBFcXVpcG1l" +
           "bnQgQ2xhc3MgSURzIGFuZCBFcXVpcG1lbnQgSURzIHRoYXQgbWF5IGJlIHNwZWNpZmllZCBpbiBhIGpv" +
           "YiBvcmRlci4ALwA/lRcAAAAMAQAAAAEAAAAAAAAAAQH/////AAAAADdgiQoCAAAAAQAMAAAASm9iT3Jk" +
           "ZXJMaXN0AQGRFwMAAAAATAAAAERlZmluZXMgYSByZWFkLW9ubHkgbGlzdCBvZiBqb2Igb3JkZXIgaW5m" +
           "b3JtYXRpb24gYXZhaWxhYmxlIGZyb20gdGhlIHNlcnZlci4ALwA/kRcAAAEBxwsBAAAAAQAAAAAAAAAB" +
           "Af////8AAAAAN2CJCgIAAAABAA8AAABNYXRlcmlhbENsYXNzSUQBAZMXAwAAAABVAAAARGVmaW5lcyBh" +
           "IHJlYWQtb25seSBzZXQgb2YgTWF0ZXJpYWwgQ2xhc3NlcyBJRHMgdGhhdCBtYXkgYmUgc3BlY2lmaWVk" +
           "IGluIGEgam9iIG9yZGVyLgAvAD+TFwAAAAwBAAAAAQAAAAAAAAABAf////8AAAAAN2CJCgIAAAABABQA" +
           "AABNYXRlcmlhbERlZmluaXRpb25JRAEBlBcDAAAAAFUAAABEZWZpbmVzIGEgcmVhZC1vbmx5IHNldCBv" +
           "ZiBNYXRlcmlhbCBDbGFzc2VzIElEcyB0aGF0IG1heSBiZSBzcGVjaWZpZWQgaW4gYSBqb2Igb3JkZXIu" +
           "AC8AP5QXAAAADAEAAAABAAAAAAAAAAEB/////wAAAAAVYIkKAgAAAAEAGAAAAE1heERvd25sb2FkYWJs" +
           "ZUpvYk9yZGVycwEByBcALgBEyBcAAAAF/////wEB/////wAAAAA3YIkKAgAAAAEACwAAAFBlcnNvbm5l" +
           "bElEAQGXFwMAAAAAXQAAAERlZmluZXMgYSByZWFkLW9ubHkgc2V0IG9mIFBlcnNvbm5lbCBJRHMgYW5k" +
           "IFBlcnNvbiBJRHMgdGhhdCBtYXkgYmUgc3BlY2lmaWVkIGluIGEgam9iIG9yZGVyLgAvAD+XFwAAAAwB" +
           "AAAAAQAAAAAAAAABAf////8AAAAAN2CJCgIAAAABAA8AAABQaHlzaWNhbEFzc2V0SUQBAZYXAwAAAABw" +
           "AAAARGVmaW5lcyBhIHJlYWQtb25seSBzZXQgb2YgUGh5c2ljYWwgQXNzZXQgQ2xhc3MgSURzIGFuZCBQ" +
           "aHlzaWNhbCBBc3NldCBJRHMgdGhhdCBtYXkgYmUgc3BlY2lmaWVkIGluIGEgam9iIG9yZGVyLgAvAD+W" +
           "FwAAAAwBAAAAAQAAAAAAAAABAf////8AAAAAN2CJCgIAAAABAAoAAABXb3JrTWFzdGVyAQGSFwMAAAAA" +
           "pgAAAERlZmluZXMgYSByZWFkLW9ubHkgc2V0IG9mIHdvcmsgbWFzdGVyIElEcyB0aGF0IG1heSBiZSBz" +
           "cGVjaWZpZWQgaW4gYSBqb2Igb3JkZXIsIGFuZCB0aGUgcmVhZC1vbmx5IHNldCBvZiBwYXJhbWV0ZXJz" +
           "IHRoYXQgbWF5IGJlIHNwZWNpZmllZCBmb3IgYSBzcGVjaWZpYyB3b3JrIG1hc3Rlci4ALwA/khcAAAEB" +
           "vwsBAAAAAQAAAAAAAAABAf////8AAAAAJGCACgEAAAABABcAAABBbGxvd2VkVG9TdGFydFN1YnN0YXRl" +
           "cwEB2RMDAAAAABsAAABTdWJzdGF0ZXMgb2YgQWxsb3dlZFRvU3RhcnQALwEB6QPZEwAAAQAAAAB1AQEB" +
           "yBMBAAAAFWCJCgIAAAAAAAwAAABDdXJyZW50U3RhdGUBAXMXAC8BAMgKcxcAAAAV/////wEB/////wEA" +
           "AAAVYIkKAgAAAAAAAgAAAElkAQF0FwAuAER0FwAAABH/////AQH/////AAAAACRggAoBAAAAAQAOAAAA" +
           "RW5kZWRTdWJzdGF0ZXMBAdoTAwAAAAASAAAAU3Vic3RhdGVzIG9mIEVuZGVkAC8BAe0D2hMAAAEAAAAA" +
           "dQEBAcsTAQAAABVgiQoCAAAAAAAMAAAAQ3VycmVudFN0YXRlAQF1FwAvAQDICnUXAAAAFf////8BAf//" +
           "//8BAAAAFWCJCgIAAAAAAAIAAABJZAEBdhcALgBEdhcAAAAR/////wEB/////wAAAAAkYIAKAQAAAAEA" +
           "FAAAAEludGVycnVwdGVkU3Vic3RhdGVzAQHbEwMAAAAAGAAAAFN1YnN0YXRlcyBvZiBJbnRlcnJ1cHRl" +
           "ZAAvAQHvA9sTAAABAAAAAHUBAQHKEwEAAAAVYIkKAgAAAAAADAAAAEN1cnJlbnRTdGF0ZQEBdxcALwEA" +
           "yAp3FwAAABX/////AQH/////AQAAABVgiQoCAAAAAAACAAAASWQBAXgXAC4ARHgXAAAAEf////8BAf//" +
           "//8AAAAAJGCACgEAAAABABoAAABOb3RBbGxvd2VkVG9TdGFydFN1YnN0YXRlcwEB2BMDAAAAAB4AAABT" +
           "dWJzdGF0ZXMgb2YgTm90QWxsb3dlZFRvU3RhcnQALwEB6QPYEwAAAQAAAAB1AQEBxxMBAAAAFWCJCgIA" +
           "AAAAAAwAAABDdXJyZW50U3RhdGUBAXEXAC8BAMgKcRcAAAAV/////wEB/////wEAAAAVYIkKAgAAAAAA" +
           "AgAAAElkAQFyFwAuAERyFwAAABH/////AQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public ISA95PrepareStateMachineTypeState AllowedToStartSubstates
        {
            get
            {
                return m_allowedToStartSubstates;
            }

            set
            {
                if (!Object.ReferenceEquals(m_allowedToStartSubstates, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_allowedToStartSubstates = value;
            }
        }

        /// <remarks />
        public ISA95EndedStateMachineTypeState EndedSubstates
        {
            get
            {
                return m_endedSubstates;
            }

            set
            {
                if (!Object.ReferenceEquals(m_endedSubstates, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_endedSubstates = value;
            }
        }

        /// <remarks />
        public ISA95InterruptedStateMachineTypeState InterruptedSubstates
        {
            get
            {
                return m_interruptedSubstates;
            }

            set
            {
                if (!Object.ReferenceEquals(m_interruptedSubstates, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_interruptedSubstates = value;
            }
        }

        /// <remarks />
        public ISA95PrepareStateMachineTypeState NotAllowedToStartSubstates
        {
            get
            {
                return m_notAllowedToStartSubstates;
            }

            set
            {
                if (!Object.ReferenceEquals(m_notAllowedToStartSubstates, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_notAllowedToStartSubstates = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_allowedToStartSubstates != null)
            {
                children.Add(m_allowedToStartSubstates);
            }

            if (m_endedSubstates != null)
            {
                children.Add(m_endedSubstates);
            }

            if (m_interruptedSubstates != null)
            {
                children.Add(m_interruptedSubstates);
            }

            if (m_notAllowedToStartSubstates != null)
            {
                children.Add(m_notAllowedToStartSubstates);
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
                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.AllowedToStartSubstates:
                    {
                        if (createOrReplace)
                        {
                            if (AllowedToStartSubstates == null)
                            {
                                if (replacement == null)
                                {
                                    AllowedToStartSubstates = new ISA95PrepareStateMachineTypeState(this);
                                }
                                else
                                {
                                    AllowedToStartSubstates = (ISA95PrepareStateMachineTypeState)replacement;
                                }
                            }
                        }

                        instance = AllowedToStartSubstates;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.EndedSubstates:
                    {
                        if (createOrReplace)
                        {
                            if (EndedSubstates == null)
                            {
                                if (replacement == null)
                                {
                                    EndedSubstates = new ISA95EndedStateMachineTypeState(this);
                                }
                                else
                                {
                                    EndedSubstates = (ISA95EndedStateMachineTypeState)replacement;
                                }
                            }
                        }

                        instance = EndedSubstates;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.InterruptedSubstates:
                    {
                        if (createOrReplace)
                        {
                            if (InterruptedSubstates == null)
                            {
                                if (replacement == null)
                                {
                                    InterruptedSubstates = new ISA95InterruptedStateMachineTypeState(this);
                                }
                                else
                                {
                                    InterruptedSubstates = (ISA95InterruptedStateMachineTypeState)replacement;
                                }
                            }
                        }

                        instance = InterruptedSubstates;
                        break;
                    }

                case UAModel.ISA95_JOBCONTROL_V2.BrowseNames.NotAllowedToStartSubstates:
                    {
                        if (createOrReplace)
                        {
                            if (NotAllowedToStartSubstates == null)
                            {
                                if (replacement == null)
                                {
                                    NotAllowedToStartSubstates = new ISA95PrepareStateMachineTypeState(this);
                                }
                                else
                                {
                                    NotAllowedToStartSubstates = (ISA95PrepareStateMachineTypeState)replacement;
                                }
                            }
                        }

                        instance = NotAllowedToStartSubstates;
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
        private ISA95PrepareStateMachineTypeState m_allowedToStartSubstates;
        private ISA95EndedStateMachineTypeState m_endedSubstates;
        private ISA95InterruptedStateMachineTypeState m_interruptedSubstates;
        private ISA95PrepareStateMachineTypeState m_notAllowedToStartSubstates;
        #endregion
    }
#endif
    #endregion

    #region ISA95PrepareStateMachineTypeState Class
#if (!OPCUA_EXCLUDE_ISA95PrepareStateMachineTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ISA95PrepareStateMachineTypeState : FiniteStateMachineState
    {
        #region Constructors
        /// <remarks />
        public ISA95PrepareStateMachineTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95PrepareStateMachineType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, namespaceUris);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGCAAgEAAAABACQAAABJU0E5NVByZXBhcmVTdGF0ZU1hY2hpbmVUeXBlSW5zdGFuY2UBAekDAQHpA+kD" +
           "AAD/////AQAAABVgiQgCAAAAAAAMAAAAQ3VycmVudFN0YXRlAQEAAAAvAQDICgAV/////wEB/////wEA" +
           "AAAVYIkIAgAAAAAAAgAAAElkAQEAAAAuAEQAEf////8BAf////8AAAAA";
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

    #region RequestJobResponseByJobOrderIDMethodState Class
#if (!OPCUA_EXCLUDE_RequestJobResponseByJobOrderIDMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class RequestJobResponseByJobOrderIDMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public RequestJobResponseByJobOrderIDMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new RequestJobResponseByJobOrderIDMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABACgAAABSZXF1ZXN0Sm9iUmVzcG9uc2VCeUpvYk9yZGVySURNZXRob2RUeXBlAQEAAAEB" +
           "AAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public RequestJobResponseByJobOrderIDMethodStateMethodCallHandler OnCall;
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

            string jobOrderID = (string)_inputArguments[0];

            ISA95JobResponseDataType jobResponse = (ISA95JobResponseDataType)_outputArguments[0];
            ulong returnStatus = (ulong)_outputArguments[1];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrderID,
                    ref jobResponse,
                    ref returnStatus);
            }

            _outputArguments[0] = jobResponse;
            _outputArguments[1] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult RequestJobResponseByJobOrderIDMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string jobOrderID,
        ref ISA95JobResponseDataType jobResponse,
        ref ulong returnStatus);
#endif
    #endregion

    #region RequestJobResponseByJobOrderStateMethodState Class
#if (!OPCUA_EXCLUDE_RequestJobResponseByJobOrderStateMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class RequestJobResponseByJobOrderStateMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public RequestJobResponseByJobOrderStateMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new RequestJobResponseByJobOrderStateMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABACsAAABSZXF1ZXN0Sm9iUmVzcG9uc2VCeUpvYk9yZGVyU3RhdGVNZXRob2RUeXBlAQEA" +
           "AAEBAAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public RequestJobResponseByJobOrderStateMethodStateMethodCallHandler OnCall;
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

            ISA95StateDataType[] jobOrderState = (ISA95StateDataType[])ExtensionObject.ToArray(_inputArguments[0], typeof(ISA95StateDataType));

            ISA95JobResponseDataType[] jobResponses = (ISA95JobResponseDataType[])_outputArguments[0];
            ulong returnStatus = (ulong)_outputArguments[1];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrderState,
                    ref jobResponses,
                    ref returnStatus);
            }

            _outputArguments[0] = jobResponses;
            _outputArguments[1] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult RequestJobResponseByJobOrderStateMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        ISA95StateDataType[] jobOrderState,
        ref ISA95JobResponseDataType[] jobResponses,
        ref ulong returnStatus);
#endif
    #endregion

    #region ReceiveJobResponseMethodState Class
#if (!OPCUA_EXCLUDE_ReceiveJobResponseMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ReceiveJobResponseMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public ReceiveJobResponseMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new ReceiveJobResponseMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABABwAAABSZWNlaXZlSm9iUmVzcG9uc2VNZXRob2RUeXBlAQEAAAEBAAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public ReceiveJobResponseMethodStateMethodCallHandler OnCall;
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

            ISA95JobResponseDataType jobResponse = (ISA95JobResponseDataType)ExtensionObject.ToEncodeable((ExtensionObject)_inputArguments[0]);

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobResponse,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult ReceiveJobResponseMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        ISA95JobResponseDataType jobResponse,
        ref ulong returnStatus);
#endif
    #endregion

    #region AbortMethodState Class
#if (!OPCUA_EXCLUDE_AbortMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class AbortMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public AbortMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new AbortMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABAA8AAABBYm9ydE1ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public AbortMethodStateMethodCallHandler OnCall;
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

            string jobOrderID = (string)_inputArguments[0];
            LocalizedText[] comment = (LocalizedText[])_inputArguments[1];

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrderID,
                    comment,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult AbortMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string jobOrderID,
        LocalizedText[] comment,
        ref ulong returnStatus);
#endif
    #endregion

    #region CancelMethodState Class
#if (!OPCUA_EXCLUDE_CancelMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class CancelMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public CancelMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new CancelMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABABAAAABDYW5jZWxNZXRob2RUeXBlAQEAAAEBAAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public CancelMethodStateMethodCallHandler OnCall;
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

            string jobOrderID = (string)_inputArguments[0];
            LocalizedText[] comment = (LocalizedText[])_inputArguments[1];

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrderID,
                    comment,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult CancelMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string jobOrderID,
        LocalizedText[] comment,
        ref ulong returnStatus);
#endif
    #endregion

    #region ClearMethodState Class
#if (!OPCUA_EXCLUDE_ClearMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ClearMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public ClearMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new ClearMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABAA8AAABDbGVhck1ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public ClearMethodStateMethodCallHandler OnCall;
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

            string jobOrderID = (string)_inputArguments[0];
            LocalizedText[] comment = (LocalizedText[])_inputArguments[1];

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrderID,
                    comment,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult ClearMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string jobOrderID,
        LocalizedText[] comment,
        ref ulong returnStatus);
#endif
    #endregion

    #region PauseMethodState Class
#if (!OPCUA_EXCLUDE_PauseMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class PauseMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public PauseMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new PauseMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABAA8AAABQYXVzZU1ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public PauseMethodStateMethodCallHandler OnCall;
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

            string jobOrderID = (string)_inputArguments[0];
            LocalizedText[] comment = (LocalizedText[])_inputArguments[1];

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrderID,
                    comment,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult PauseMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string jobOrderID,
        LocalizedText[] comment,
        ref ulong returnStatus);
#endif
    #endregion

    #region ResumeMethodState Class
#if (!OPCUA_EXCLUDE_ResumeMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ResumeMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public ResumeMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new ResumeMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABABAAAABSZXN1bWVNZXRob2RUeXBlAQEAAAEBAAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public ResumeMethodStateMethodCallHandler OnCall;
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

            string jobOrderID = (string)_inputArguments[0];
            LocalizedText[] comment = (LocalizedText[])_inputArguments[1];

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrderID,
                    comment,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult ResumeMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string jobOrderID,
        LocalizedText[] comment,
        ref ulong returnStatus);
#endif
    #endregion

    #region RevokeStartMethodState Class
#if (!OPCUA_EXCLUDE_RevokeStartMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class RevokeStartMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public RevokeStartMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new RevokeStartMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABABUAAABSZXZva2VTdGFydE1ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public RevokeStartMethodStateMethodCallHandler OnCall;
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

            string jobOrderID = (string)_inputArguments[0];
            LocalizedText[] comment = (LocalizedText[])_inputArguments[1];

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrderID,
                    comment,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult RevokeStartMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string jobOrderID,
        LocalizedText[] comment,
        ref ulong returnStatus);
#endif
    #endregion

    #region StartMethodState Class
#if (!OPCUA_EXCLUDE_StartMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class StartMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public StartMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new StartMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABAA8AAABTdGFydE1ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public StartMethodStateMethodCallHandler OnCall;
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

            string jobOrderID = (string)_inputArguments[0];
            LocalizedText[] comment = (LocalizedText[])_inputArguments[1];

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrderID,
                    comment,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult StartMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string jobOrderID,
        LocalizedText[] comment,
        ref ulong returnStatus);
#endif
    #endregion

    #region StopMethodState Class
#if (!OPCUA_EXCLUDE_StopMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class StopMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public StopMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new StopMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABAA4AAABTdG9wTWV0aG9kVHlwZQEBAAABAQAAAQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public StopMethodStateMethodCallHandler OnCall;
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

            string jobOrderID = (string)_inputArguments[0];
            LocalizedText[] comment = (LocalizedText[])_inputArguments[1];

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrderID,
                    comment,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult StopMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string jobOrderID,
        LocalizedText[] comment,
        ref ulong returnStatus);
#endif
    #endregion

    #region StoreMethodState Class
#if (!OPCUA_EXCLUDE_StoreMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class StoreMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public StoreMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new StoreMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABAA8AAABTdG9yZU1ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public StoreMethodStateMethodCallHandler OnCall;
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

            ISA95JobOrderDataType jobOrder = (ISA95JobOrderDataType)ExtensionObject.ToEncodeable((ExtensionObject)_inputArguments[0]);
            LocalizedText[] comment = (LocalizedText[])_inputArguments[1];

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrder,
                    comment,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult StoreMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        ISA95JobOrderDataType jobOrder,
        LocalizedText[] comment,
        ref ulong returnStatus);
#endif
    #endregion

    #region StoreAndStartMethodState Class
#if (!OPCUA_EXCLUDE_StoreAndStartMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class StoreAndStartMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public StoreAndStartMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new StoreAndStartMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABABcAAABTdG9yZUFuZFN0YXJ0TWV0aG9kVHlwZQEBAAABAQAAAQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public StoreAndStartMethodStateMethodCallHandler OnCall;
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

            ISA95JobOrderDataType jobOrder = (ISA95JobOrderDataType)ExtensionObject.ToEncodeable((ExtensionObject)_inputArguments[0]);
            LocalizedText[] comment = (LocalizedText[])_inputArguments[1];

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrder,
                    comment,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult StoreAndStartMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        ISA95JobOrderDataType jobOrder,
        LocalizedText[] comment,
        ref ulong returnStatus);
#endif
    #endregion

    #region UpdateMethodState Class
#if (!OPCUA_EXCLUDE_UpdateMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class UpdateMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public UpdateMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new UpdateMethodState(parent);
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
           "AQAAADAAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvSVNBOTUtSk9CQ09OVFJPTF9WMi//////" +
           "BGGCAAQAAAABABAAAABVcGRhdGVNZXRob2RUeXBlAQEAAAEBAAABAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public UpdateMethodStateMethodCallHandler OnCall;
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

            ISA95JobOrderDataType jobOrder = (ISA95JobOrderDataType)ExtensionObject.ToEncodeable((ExtensionObject)_inputArguments[0]);
            LocalizedText[] comment = (LocalizedText[])_inputArguments[1];

            ulong returnStatus = (ulong)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    jobOrder,
                    comment,
                    ref returnStatus);
            }

            _outputArguments[0] = returnStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult UpdateMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        ISA95JobOrderDataType jobOrder,
        LocalizedText[] comment,
        ref ulong returnStatus);
#endif
    #endregion
}
