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

namespace UAModel.ISA95_JOBCONTROL_V2
{
    #region DataType Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypes
    {
        /// <remarks />
        public const uint ISA95EquipmentDataType = 3005;

        /// <remarks />
        public const uint ISA95JobOrderAndStateDataType = 3015;

        /// <remarks />
        public const uint ISA95JobOrderDataType = 3008;

        /// <remarks />
        public const uint ISA95JobResponseDataType = 3013;

        /// <remarks />
        public const uint ISA95MaterialDataType = 3010;

        /// <remarks />
        public const uint ISA95ParameterDataType = 3003;

        /// <remarks />
        public const uint ISA95PersonnelDataType = 3011;

        /// <remarks />
        public const uint ISA95PhysicalAssetDataType = 3012;

        /// <remarks />
        public const uint ISA95PropertyDataType = 3002;

        /// <remarks />
        public const uint ISA95StateDataType = 3006;

        /// <remarks />
        public const uint ISA95WorkMasterDataType = 3007;
    }
    #endregion

    #region Method Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Methods
    {
        /// <remarks />
        public const uint ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderID = 7002;

        /// <remarks />
        public const uint ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderState = 7014;

        /// <remarks />
        public const uint ISA95JobResponseReceiverObjectType_ReceiveJobResponse = 7003;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Abort = 7010;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Cancel = 7011;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Clear = 7012;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Pause = 7007;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Resume = 7008;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_RevokeStart = 7013;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Start = 7005;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Stop = 7006;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Store = 7001;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_StoreAndStart = 7004;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Update = 7009;

        /// <remarks />
        public const string ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderIDMethodType = "";

        /// <remarks />
        public const string ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderStateMethodType = "";

        /// <remarks />
        public const string ISA95JobResponseReceiverObjectType_ReceiveJobResponseMethodType = "";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType_AbortMethodType = "";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType_CancelMethodType = "";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType_ClearMethodType = "";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType_PauseMethodType = "";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType_ResumeMethodType = "";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType_RevokeStartMethodType = "";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType_StartMethodType = "";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType_StopMethodType = "";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType_StoreMethodType = "";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType_StoreAndStartMethodType = "";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType_UpdateMethodType = "";
    }
    #endregion

    #region Object Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Objects
    {
        /// <remarks />
        public const uint ISA95EndedStateMachineType_Closed = 5057;

        /// <remarks />
        public const uint ISA95EndedStateMachineType_Completed = 5056;

        /// <remarks />
        public const uint ISA95EndedStateMachineType_FromCompletedToClosed = 5058;

        /// <remarks />
        public const uint ISA95InterruptedStateMachineType_FromHeldToSuspended = 5061;

        /// <remarks />
        public const uint ISA95InterruptedStateMachineType_FromSuspendedToHeld = 5062;

        /// <remarks />
        public const uint ISA95InterruptedStateMachineType_Held = 5059;

        /// <remarks />
        public const uint ISA95InterruptedStateMachineType_Suspended = 5060;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Aborted = 5040;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_AllowedToStart = 5036;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Ended = 5039;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromAllowedToStartToAborted = 5085;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromAllowedToStartToAllowedToStart = 5044;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromAllowedToStartToNotAllowedToStart = 5043;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromAllowedToStartToRunning = 5045;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromInterruptedToAborted = 5049;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromInterruptedToEnded = 5051;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromInterruptedToRunning = 5050;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAborted = 5084;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAllowedToStart = 5042;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToNotAllowedToStart = 5041;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromRunningToAborted = 5048;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromRunningToEnded = 5047;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromRunningToInterrupted = 5046;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Interrupted = 5038;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_NotAllowedToStart = 5035;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Running = 5037;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Aborted = 5068;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_AllowedToStart = 5064;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Ended = 5067;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAborted = 5086;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAllowedToStart = 5072;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToNotAllowedToStart = 5071;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToRunning = 5073;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromInterruptedToAborted = 5077;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromInterruptedToEnded = 5079;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromInterruptedToRunning = 5078;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAborted = 5087;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAllowedToStart = 5070;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToNotAllowedToStart = 5069;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromRunningToAborted = 5076;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromRunningToEnded = 5075;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromRunningToInterrupted = 5074;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Interrupted = 5066;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_NotAllowedToStart = 5063;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Running = 5065;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_AllowedToStartSubstates = 5081;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_EndedSubstates = 5082;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_InterruptedSubstates = 5083;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_NotAllowedToStartSubstates = 5080;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_FromLoadedToReady = 5089;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_FromLoadedToWaiting = 5090;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_FromReadyToLoaded = 5055;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_FromReadyToWaiting = 5088;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_FromWaitingToReady = 5054;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_Loaded = 5053;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_Ready = 5052;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_Waiting = 5000;

        /// <remarks />
        public const uint http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2_ = 5001;

        /// <remarks />
        public const uint ISA95PropertyDataType_Encoding_DefaultBinary = 5002;

        /// <remarks />
        public const uint ISA95PropertyDataType_Encoding_DefaultXml = 5003;

        /// <remarks />
        public const uint ISA95PropertyDataType_Encoding_DefaultJson = 5004;

        /// <remarks />
        public const uint ISA95ParameterDataType_Encoding_DefaultBinary = 5005;

        /// <remarks />
        public const uint ISA95ParameterDataType_Encoding_DefaultXml = 5006;

        /// <remarks />
        public const uint ISA95ParameterDataType_Encoding_DefaultJson = 5007;

        /// <remarks />
        public const uint ISA95EquipmentDataType_Encoding_DefaultBinary = 5008;

        /// <remarks />
        public const uint ISA95EquipmentDataType_Encoding_DefaultXml = 5009;

        /// <remarks />
        public const uint ISA95EquipmentDataType_Encoding_DefaultJson = 5010;

        /// <remarks />
        public const uint ISA95WorkMasterDataType_Encoding_DefaultBinary = 5011;

        /// <remarks />
        public const uint ISA95WorkMasterDataType_Encoding_DefaultXml = 5012;

        /// <remarks />
        public const uint ISA95WorkMasterDataType_Encoding_DefaultJson = 5013;

        /// <remarks />
        public const uint ISA95JobOrderDataType_Encoding_DefaultBinary = 5014;

        /// <remarks />
        public const uint ISA95JobOrderDataType_Encoding_DefaultXml = 5015;

        /// <remarks />
        public const uint ISA95JobOrderDataType_Encoding_DefaultJson = 5016;

        /// <remarks />
        public const uint ISA95MaterialDataType_Encoding_DefaultBinary = 5017;

        /// <remarks />
        public const uint ISA95MaterialDataType_Encoding_DefaultXml = 5018;

        /// <remarks />
        public const uint ISA95MaterialDataType_Encoding_DefaultJson = 5019;

        /// <remarks />
        public const uint ISA95PersonnelDataType_Encoding_DefaultBinary = 5020;

        /// <remarks />
        public const uint ISA95PersonnelDataType_Encoding_DefaultXml = 5021;

        /// <remarks />
        public const uint ISA95PersonnelDataType_Encoding_DefaultJson = 5022;

        /// <remarks />
        public const uint ISA95PhysicalAssetDataType_Encoding_DefaultBinary = 5023;

        /// <remarks />
        public const uint ISA95PhysicalAssetDataType_Encoding_DefaultXml = 5024;

        /// <remarks />
        public const uint ISA95PhysicalAssetDataType_Encoding_DefaultJson = 5025;

        /// <remarks />
        public const uint ISA95JobResponseDataType_Encoding_DefaultBinary = 5026;

        /// <remarks />
        public const uint ISA95JobResponseDataType_Encoding_DefaultXml = 5027;

        /// <remarks />
        public const uint ISA95JobResponseDataType_Encoding_DefaultJson = 5028;

        /// <remarks />
        public const uint ISA95StateDataType_Encoding_DefaultBinary = 5029;

        /// <remarks />
        public const uint ISA95StateDataType_Encoding_DefaultXml = 5030;

        /// <remarks />
        public const uint ISA95StateDataType_Encoding_DefaultJson = 5031;

        /// <remarks />
        public const uint ISA95JobOrderAndStateDataType_Encoding_DefaultBinary = 5032;

        /// <remarks />
        public const uint ISA95JobOrderAndStateDataType_Encoding_DefaultXml = 5033;

        /// <remarks />
        public const uint ISA95JobOrderAndStateDataType_Encoding_DefaultJson = 5034;
    }
    #endregion

    #region ObjectType Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypes
    {
        /// <remarks />
        public const uint ISA95JobOrderStatusEventType = 1006;

        /// <remarks />
        public const uint ISA95JobResponseProviderObjectType = 1003;

        /// <remarks />
        public const uint ISA95JobResponseReceiverObjectType = 1004;

        /// <remarks />
        public const uint ISA95EndedStateMachineType = 1005;

        /// <remarks />
        public const uint ISA95InterruptedStateMachineType = 1007;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType = 1002;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType = 1008;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType = 1001;
    }
    #endregion

    #region Variable Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Variables
    {
        /// <remarks />
        public const uint ISA95JobOrderStatusEventType_JobOrder = 6047;

        /// <remarks />
        public const uint ISA95JobOrderStatusEventType_JobResponse = 6049;

        /// <remarks />
        public const uint ISA95JobOrderStatusEventType_JobState = 6048;

        /// <remarks />
        public const uint ISA95JobResponseProviderObjectType_JobOrderResponseList = 6050;

        /// <remarks />
        public const uint ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderID_InputArguments = 6042;

        /// <remarks />
        public const uint ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderID_OutputArguments = 6043;

        /// <remarks />
        public const uint ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderState_InputArguments = 6016;

        /// <remarks />
        public const uint ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderState_OutputArguments = 6017;

        /// <remarks />
        public const uint ISA95JobResponseReceiverObjectType_ReceiveJobResponse_InputArguments = 6044;

        /// <remarks />
        public const uint ISA95JobResponseReceiverObjectType_ReceiveJobResponse_OutputArguments = 6045;

        /// <remarks />
        public const uint ISA95EndedStateMachineType_Closed_StateNumber = 6094;

        /// <remarks />
        public const uint ISA95EndedStateMachineType_Completed_StateNumber = 6093;

        /// <remarks />
        public const uint ISA95EndedStateMachineType_FromCompletedToClosed_TransitionNumber = 6095;

        /// <remarks />
        public const uint ISA95InterruptedStateMachineType_FromHeldToSuspended_TransitionNumber = 6098;

        /// <remarks />
        public const uint ISA95InterruptedStateMachineType_FromSuspendedToHeld_TransitionNumber = 6099;

        /// <remarks />
        public const uint ISA95InterruptedStateMachineType_Held_StateNumber = 6096;

        /// <remarks />
        public const uint ISA95InterruptedStateMachineType_Suspended_StateNumber = 6097;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Abort_InputArguments = 6063;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Abort_OutputArguments = 6064;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Aborted_StateNumber = 6076;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_AllowedToStart_StateNumber = 6072;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Cancel_InputArguments = 6065;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Cancel_OutputArguments = 6066;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Clear_InputArguments = 6067;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Clear_OutputArguments = 6068;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Ended_StateNumber = 6075;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_EquipmentID = 6037;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromAllowedToStartToAborted_TransitionNumber = 6010;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromAllowedToStartToAllowedToStart_TransitionNumber = 6080;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromAllowedToStartToNotAllowedToStart_TransitionNumber = 6079;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromAllowedToStartToRunning_TransitionNumber = 6081;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromInterruptedToAborted_TransitionNumber = 6085;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromInterruptedToEnded_TransitionNumber = 6087;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromInterruptedToRunning_TransitionNumber = 6086;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAborted_TransitionNumber = 6009;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAllowedToStart_TransitionNumber = 6078;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToNotAllowedToStart_TransitionNumber = 6077;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromRunningToAborted_TransitionNumber = 6084;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromRunningToEnded_TransitionNumber = 6083;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_FromRunningToInterrupted_TransitionNumber = 6082;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Interrupted_StateNumber = 6074;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_JobOrderList = 6033;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_MaterialClassID = 6035;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_MaterialDefinitionID = 6036;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_MaxDownloadableJobOrders = 6088;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_NotAllowedToStart_StateNumber = 6071;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Pause_InputArguments = 6057;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Pause_OutputArguments = 6058;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_PersonnelID = 6039;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_PhysicalAssetID = 6038;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Resume_InputArguments = 6059;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Resume_OutputArguments = 6060;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_RevokeStart_InputArguments = 6069;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_RevokeStart_OutputArguments = 6070;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Running_StateNumber = 6073;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Start_InputArguments = 6053;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Start_OutputArguments = 6054;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Stop_InputArguments = 6055;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Stop_OutputArguments = 6056;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Store_InputArguments = 6040;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Store_OutputArguments = 6041;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_StoreAndStart_InputArguments = 6051;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_StoreAndStart_OutputArguments = 6052;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Update_InputArguments = 6061;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_Update_OutputArguments = 6062;

        /// <remarks />
        public const uint ISA95JobOrderReceiverObjectType_WorkMaster = 6034;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Abort_InputArguments = 6063;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Abort_OutputArguments = 6064;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Aborted_StateNumber = 6105;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_AllowedToStart_StateNumber = 6101;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Cancel_InputArguments = 6065;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Cancel_OutputArguments = 6066;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Clear_InputArguments = 6067;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Clear_OutputArguments = 6068;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Ended_StateNumber = 6104;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAborted_TransitionNumber = 6011;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAllowedToStart_TransitionNumber = 6109;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToNotAllowedToStart_TransitionNumber = 6108;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToRunning_TransitionNumber = 6110;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromInterruptedToAborted_TransitionNumber = 6114;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromInterruptedToEnded_TransitionNumber = 6116;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromInterruptedToRunning_TransitionNumber = 6115;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAborted_TransitionNumber = 6012;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAllowedToStart_TransitionNumber = 6107;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToNotAllowedToStart_TransitionNumber = 6106;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromRunningToAborted_TransitionNumber = 6113;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromRunningToEnded_TransitionNumber = 6112;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_FromRunningToInterrupted_TransitionNumber = 6111;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Interrupted_StateNumber = 6103;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_NotAllowedToStart_StateNumber = 6100;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Pause_InputArguments = 6057;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Pause_OutputArguments = 6058;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Resume_InputArguments = 6059;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Resume_OutputArguments = 6060;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_RevokeStart_InputArguments = 6069;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_RevokeStart_OutputArguments = 6070;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Running_StateNumber = 6102;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Start_InputArguments = 6053;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Start_OutputArguments = 6054;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Stop_InputArguments = 6055;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Stop_OutputArguments = 6056;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Store_InputArguments = 6040;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Store_OutputArguments = 6041;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_StoreAndStart_InputArguments = 6051;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_StoreAndStart_OutputArguments = 6052;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Update_InputArguments = 6061;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_Update_OutputArguments = 6062;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_AllowedToStartSubstates_CurrentState = 6003;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_AllowedToStartSubstates_CurrentState_Id = 6004;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_EndedSubstates_CurrentState = 6005;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_EndedSubstates_CurrentState_Id = 6006;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_InterruptedSubstates_CurrentState = 6007;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_InterruptedSubstates_CurrentState_Id = 6008;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_NotAllowedToStartSubstates_CurrentState = 6001;

        /// <remarks />
        public const uint ISA95JobOrderReceiverSubStatesType_NotAllowedToStartSubstates_CurrentState_Id = 6002;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_FromLoadedToReady_TransitionNumber = 6014;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_FromLoadedToWaiting_TransitionNumber = 6015;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_FromReadyToLoaded_TransitionNumber = 6092;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_FromReadyToWaiting_TransitionNumber = 6013;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_FromWaitingToReady_TransitionNumber = 6091;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_Loaded_StateNumber = 6090;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_Ready_StateNumber = 6089;

        /// <remarks />
        public const uint ISA95PrepareStateMachineType_Waiting_StateNumber = 6000;

        /// <remarks />
        public const uint http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__NamespaceUri = 6025;

        /// <remarks />
        public const uint http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__NamespaceVersion = 6026;

        /// <remarks />
        public const uint http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__NamespacePublicationDate = 6024;

        /// <remarks />
        public const uint http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__IsNamespaceSubset = 6023;

        /// <remarks />
        public const uint http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__StaticNodeIdTypes = 6027;

        /// <remarks />
        public const uint http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__StaticNumericNodeIdRange = 6028;

        /// <remarks />
        public const uint http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__StaticStringNodeIdPattern = 6029;
    }
    #endregion

    #region DataType Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypeIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId ISA95EquipmentDataType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.DataTypes.ISA95EquipmentDataType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderAndStateDataType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.DataTypes.ISA95JobOrderAndStateDataType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderDataType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.DataTypes.ISA95JobOrderDataType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseDataType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.DataTypes.ISA95JobResponseDataType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95MaterialDataType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.DataTypes.ISA95MaterialDataType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95ParameterDataType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.DataTypes.ISA95ParameterDataType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PersonnelDataType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.DataTypes.ISA95PersonnelDataType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PhysicalAssetDataType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.DataTypes.ISA95PhysicalAssetDataType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PropertyDataType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.DataTypes.ISA95PropertyDataType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95StateDataType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.DataTypes.ISA95StateDataType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95WorkMasterDataType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.DataTypes.ISA95WorkMasterDataType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
    }
    #endregion

    #region Method Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class MethodIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderID = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderID, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderState = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderState, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseReceiverObjectType_ReceiveJobResponse = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobResponseReceiverObjectType_ReceiveJobResponse, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Abort = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_Abort, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Cancel = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_Cancel, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Clear = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_Clear, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Pause = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_Pause, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Resume = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_Resume, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_RevokeStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_RevokeStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Start = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_Start, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Stop = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_Stop, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Store = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_Store, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_StoreAndStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_StoreAndStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Update = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_Update, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderIDMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderIDMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderStateMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderStateMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseReceiverObjectType_ReceiveJobResponseMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobResponseReceiverObjectType_ReceiveJobResponseMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_AbortMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_AbortMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_CancelMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_CancelMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_ClearMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_ClearMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_PauseMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_PauseMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_ResumeMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_ResumeMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_RevokeStartMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_RevokeStartMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_StartMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_StartMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_StopMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_StopMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_StoreMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_StoreMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_StoreAndStartMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_StoreAndStartMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_UpdateMethodType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Methods.ISA95JobOrderReceiverObjectType_UpdateMethodType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
    }
    #endregion

    #region Object Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId ISA95EndedStateMachineType_Closed = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95EndedStateMachineType_Closed, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95EndedStateMachineType_Completed = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95EndedStateMachineType_Completed, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95EndedStateMachineType_FromCompletedToClosed = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95EndedStateMachineType_FromCompletedToClosed, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95InterruptedStateMachineType_FromHeldToSuspended = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95InterruptedStateMachineType_FromHeldToSuspended, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95InterruptedStateMachineType_FromSuspendedToHeld = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95InterruptedStateMachineType_FromSuspendedToHeld, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95InterruptedStateMachineType_Held = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95InterruptedStateMachineType_Held, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95InterruptedStateMachineType_Suspended = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95InterruptedStateMachineType_Suspended, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Aborted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_Aborted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_AllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_AllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Ended = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_Ended, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromAllowedToStartToAborted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromAllowedToStartToAborted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromAllowedToStartToAllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromAllowedToStartToAllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromAllowedToStartToNotAllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromAllowedToStartToNotAllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromAllowedToStartToRunning = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromAllowedToStartToRunning, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromInterruptedToAborted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromInterruptedToAborted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromInterruptedToEnded = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromInterruptedToEnded, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromInterruptedToRunning = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromInterruptedToRunning, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAborted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAborted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToNotAllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToNotAllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromRunningToAborted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromRunningToAborted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromRunningToEnded = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromRunningToEnded, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromRunningToInterrupted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_FromRunningToInterrupted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Interrupted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_Interrupted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_NotAllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_NotAllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Running = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverObjectType_Running, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Aborted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_Aborted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_AllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_AllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Ended = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_Ended, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAborted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAborted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToNotAllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToNotAllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToRunning = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToRunning, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromInterruptedToAborted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromInterruptedToAborted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromInterruptedToEnded = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromInterruptedToEnded, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromInterruptedToRunning = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromInterruptedToRunning, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAborted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAborted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToNotAllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToNotAllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromRunningToAborted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromRunningToAborted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromRunningToEnded = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromRunningToEnded, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromRunningToInterrupted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_FromRunningToInterrupted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Interrupted = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_Interrupted, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_NotAllowedToStart = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_NotAllowedToStart, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Running = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_Running, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_AllowedToStartSubstates = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_AllowedToStartSubstates, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_EndedSubstates = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_EndedSubstates, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_InterruptedSubstates = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_InterruptedSubstates, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_NotAllowedToStartSubstates = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderReceiverSubStatesType_NotAllowedToStartSubstates, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_FromLoadedToReady = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PrepareStateMachineType_FromLoadedToReady, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_FromLoadedToWaiting = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PrepareStateMachineType_FromLoadedToWaiting, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_FromReadyToLoaded = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PrepareStateMachineType_FromReadyToLoaded, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_FromReadyToWaiting = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PrepareStateMachineType_FromReadyToWaiting, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_FromWaitingToReady = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PrepareStateMachineType_FromWaitingToReady, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_Loaded = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PrepareStateMachineType_Loaded, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_Ready = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PrepareStateMachineType_Ready, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_Waiting = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PrepareStateMachineType_Waiting, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2_ = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2_, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PropertyDataType_Encoding_DefaultBinary = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PropertyDataType_Encoding_DefaultBinary, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PropertyDataType_Encoding_DefaultXml = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PropertyDataType_Encoding_DefaultXml, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PropertyDataType_Encoding_DefaultJson = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PropertyDataType_Encoding_DefaultJson, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95ParameterDataType_Encoding_DefaultBinary = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95ParameterDataType_Encoding_DefaultBinary, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95ParameterDataType_Encoding_DefaultXml = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95ParameterDataType_Encoding_DefaultXml, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95ParameterDataType_Encoding_DefaultJson = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95ParameterDataType_Encoding_DefaultJson, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95EquipmentDataType_Encoding_DefaultBinary = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95EquipmentDataType_Encoding_DefaultBinary, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95EquipmentDataType_Encoding_DefaultXml = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95EquipmentDataType_Encoding_DefaultXml, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95EquipmentDataType_Encoding_DefaultJson = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95EquipmentDataType_Encoding_DefaultJson, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95WorkMasterDataType_Encoding_DefaultBinary = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95WorkMasterDataType_Encoding_DefaultBinary, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95WorkMasterDataType_Encoding_DefaultXml = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95WorkMasterDataType_Encoding_DefaultXml, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95WorkMasterDataType_Encoding_DefaultJson = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95WorkMasterDataType_Encoding_DefaultJson, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderDataType_Encoding_DefaultBinary = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderDataType_Encoding_DefaultBinary, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderDataType_Encoding_DefaultXml = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderDataType_Encoding_DefaultXml, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderDataType_Encoding_DefaultJson = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderDataType_Encoding_DefaultJson, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95MaterialDataType_Encoding_DefaultBinary = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95MaterialDataType_Encoding_DefaultBinary, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95MaterialDataType_Encoding_DefaultXml = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95MaterialDataType_Encoding_DefaultXml, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95MaterialDataType_Encoding_DefaultJson = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95MaterialDataType_Encoding_DefaultJson, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PersonnelDataType_Encoding_DefaultBinary = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PersonnelDataType_Encoding_DefaultBinary, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PersonnelDataType_Encoding_DefaultXml = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PersonnelDataType_Encoding_DefaultXml, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PersonnelDataType_Encoding_DefaultJson = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PersonnelDataType_Encoding_DefaultJson, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PhysicalAssetDataType_Encoding_DefaultBinary = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PhysicalAssetDataType_Encoding_DefaultBinary, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PhysicalAssetDataType_Encoding_DefaultXml = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PhysicalAssetDataType_Encoding_DefaultXml, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PhysicalAssetDataType_Encoding_DefaultJson = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95PhysicalAssetDataType_Encoding_DefaultJson, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseDataType_Encoding_DefaultBinary = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobResponseDataType_Encoding_DefaultBinary, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseDataType_Encoding_DefaultXml = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobResponseDataType_Encoding_DefaultXml, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseDataType_Encoding_DefaultJson = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobResponseDataType_Encoding_DefaultJson, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95StateDataType_Encoding_DefaultBinary = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95StateDataType_Encoding_DefaultBinary, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95StateDataType_Encoding_DefaultXml = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95StateDataType_Encoding_DefaultXml, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95StateDataType_Encoding_DefaultJson = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95StateDataType_Encoding_DefaultJson, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderAndStateDataType_Encoding_DefaultBinary = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderAndStateDataType_Encoding_DefaultBinary, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderAndStateDataType_Encoding_DefaultXml = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderAndStateDataType_Encoding_DefaultXml, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderAndStateDataType_Encoding_DefaultJson = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Objects.ISA95JobOrderAndStateDataType_Encoding_DefaultJson, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
    }
    #endregion

    #region ObjectType Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypeIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderStatusEventType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95JobOrderStatusEventType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseProviderObjectType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95JobResponseProviderObjectType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseReceiverObjectType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95JobResponseReceiverObjectType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95EndedStateMachineType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95EndedStateMachineType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95InterruptedStateMachineType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95InterruptedStateMachineType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95JobOrderReceiverObjectType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95JobOrderReceiverSubStatesType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.ObjectTypes.ISA95PrepareStateMachineType, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
    }
    #endregion

    #region Variable Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class VariableIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderStatusEventType_JobOrder = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderStatusEventType_JobOrder, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderStatusEventType_JobResponse = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderStatusEventType_JobResponse, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderStatusEventType_JobState = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderStatusEventType_JobState, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseProviderObjectType_JobOrderResponseList = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobResponseProviderObjectType_JobOrderResponseList, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderID_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderID_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderID_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderID_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderState_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderState_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderState_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobResponseProviderObjectType_RequestJobResponseByJobOrderState_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseReceiverObjectType_ReceiveJobResponse_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobResponseReceiverObjectType_ReceiveJobResponse_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobResponseReceiverObjectType_ReceiveJobResponse_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobResponseReceiverObjectType_ReceiveJobResponse_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95EndedStateMachineType_Closed_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95EndedStateMachineType_Closed_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95EndedStateMachineType_Completed_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95EndedStateMachineType_Completed_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95EndedStateMachineType_FromCompletedToClosed_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95EndedStateMachineType_FromCompletedToClosed_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95InterruptedStateMachineType_FromHeldToSuspended_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95InterruptedStateMachineType_FromHeldToSuspended_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95InterruptedStateMachineType_FromSuspendedToHeld_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95InterruptedStateMachineType_FromSuspendedToHeld_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95InterruptedStateMachineType_Held_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95InterruptedStateMachineType_Held_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95InterruptedStateMachineType_Suspended_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95InterruptedStateMachineType_Suspended_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Abort_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Abort_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Abort_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Abort_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Aborted_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Aborted_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_AllowedToStart_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_AllowedToStart_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Cancel_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Cancel_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Cancel_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Cancel_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Clear_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Clear_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Clear_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Clear_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Ended_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Ended_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_EquipmentID = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_EquipmentID, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromAllowedToStartToAborted_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromAllowedToStartToAborted_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromAllowedToStartToAllowedToStart_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromAllowedToStartToAllowedToStart_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromAllowedToStartToNotAllowedToStart_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromAllowedToStartToNotAllowedToStart_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromAllowedToStartToRunning_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromAllowedToStartToRunning_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromInterruptedToAborted_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromInterruptedToAborted_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromInterruptedToEnded_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromInterruptedToEnded_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromInterruptedToRunning_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromInterruptedToRunning_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAborted_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAborted_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAllowedToStart_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToAllowedToStart_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToNotAllowedToStart_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromNotAllowedToStartToNotAllowedToStart_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromRunningToAborted_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromRunningToAborted_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromRunningToEnded_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromRunningToEnded_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_FromRunningToInterrupted_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_FromRunningToInterrupted_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Interrupted_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Interrupted_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_JobOrderList = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_JobOrderList, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_MaterialClassID = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_MaterialClassID, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_MaterialDefinitionID = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_MaterialDefinitionID, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_MaxDownloadableJobOrders = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_MaxDownloadableJobOrders, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_NotAllowedToStart_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_NotAllowedToStart_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Pause_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Pause_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Pause_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Pause_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_PersonnelID = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_PersonnelID, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_PhysicalAssetID = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_PhysicalAssetID, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Resume_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Resume_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Resume_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Resume_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_RevokeStart_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_RevokeStart_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_RevokeStart_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_RevokeStart_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Running_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Running_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Start_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Start_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Start_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Start_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Stop_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Stop_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Stop_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Stop_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Store_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Store_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Store_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Store_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_StoreAndStart_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_StoreAndStart_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_StoreAndStart_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_StoreAndStart_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Update_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Update_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_Update_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_Update_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverObjectType_WorkMaster = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverObjectType_WorkMaster, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Abort_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Abort_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Abort_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Abort_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Aborted_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Aborted_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_AllowedToStart_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_AllowedToStart_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Cancel_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Cancel_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Cancel_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Cancel_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Clear_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Clear_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Clear_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Clear_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Ended_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Ended_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAborted_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAborted_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAllowedToStart_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToAllowedToStart_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToNotAllowedToStart_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToNotAllowedToStart_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToRunning_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromAllowedToStartToRunning_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromInterruptedToAborted_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromInterruptedToAborted_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromInterruptedToEnded_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromInterruptedToEnded_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromInterruptedToRunning_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromInterruptedToRunning_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAborted_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAborted_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAllowedToStart_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToAllowedToStart_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToNotAllowedToStart_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromNotAllowedToStartToNotAllowedToStart_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromRunningToAborted_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromRunningToAborted_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromRunningToEnded_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromRunningToEnded_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_FromRunningToInterrupted_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_FromRunningToInterrupted_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Interrupted_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Interrupted_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_NotAllowedToStart_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_NotAllowedToStart_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Pause_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Pause_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Pause_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Pause_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Resume_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Resume_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Resume_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Resume_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_RevokeStart_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_RevokeStart_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_RevokeStart_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_RevokeStart_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Running_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Running_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Start_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Start_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Start_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Start_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Stop_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Stop_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Stop_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Stop_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Store_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Store_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Store_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Store_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_StoreAndStart_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_StoreAndStart_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_StoreAndStart_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_StoreAndStart_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Update_InputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Update_InputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_Update_OutputArguments = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_Update_OutputArguments, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_AllowedToStartSubstates_CurrentState = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_AllowedToStartSubstates_CurrentState, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_AllowedToStartSubstates_CurrentState_Id = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_AllowedToStartSubstates_CurrentState_Id, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_EndedSubstates_CurrentState = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_EndedSubstates_CurrentState, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_EndedSubstates_CurrentState_Id = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_EndedSubstates_CurrentState_Id, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_InterruptedSubstates_CurrentState = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_InterruptedSubstates_CurrentState, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_InterruptedSubstates_CurrentState_Id = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_InterruptedSubstates_CurrentState_Id, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_NotAllowedToStartSubstates_CurrentState = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_NotAllowedToStartSubstates_CurrentState, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95JobOrderReceiverSubStatesType_NotAllowedToStartSubstates_CurrentState_Id = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95JobOrderReceiverSubStatesType_NotAllowedToStartSubstates_CurrentState_Id, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_FromLoadedToReady_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95PrepareStateMachineType_FromLoadedToReady_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_FromLoadedToWaiting_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95PrepareStateMachineType_FromLoadedToWaiting_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_FromReadyToLoaded_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95PrepareStateMachineType_FromReadyToLoaded_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_FromReadyToWaiting_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95PrepareStateMachineType_FromReadyToWaiting_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_FromWaitingToReady_TransitionNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95PrepareStateMachineType_FromWaitingToReady_TransitionNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_Loaded_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95PrepareStateMachineType_Loaded_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_Ready_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95PrepareStateMachineType_Ready_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId ISA95PrepareStateMachineType_Waiting_StateNumber = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.ISA95PrepareStateMachineType_Waiting_StateNumber, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__NamespaceUri = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__NamespaceUri, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__NamespaceVersion = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__NamespaceVersion, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__NamespacePublicationDate = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__NamespacePublicationDate, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__IsNamespaceSubset = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__IsNamespaceSubset, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__StaticNodeIdTypes = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__StaticNodeIdTypes, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__StaticNumericNodeIdRange = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__StaticNumericNodeIdRange, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

        /// <remarks />
        public static readonly ExpandedNodeId http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__StaticStringNodeIdPattern = new ExpandedNodeId(UAModel.ISA95_JOBCONTROL_V2.Variables.http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2__StaticStringNodeIdPattern, UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
    }
    #endregion

    #region BrowseName Declarations
    /// <remarks />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class BrowseNames
    {
        /// <remarks />
        public const string Abort = "Abort";

        /// <remarks />
        public const string Aborted = "Aborted";

        /// <remarks />
        public const string AbortMethodType = "AbortMethodType";

        /// <remarks />
        public const string AllowedToStart = "AllowedToStart";

        /// <remarks />
        public const string AllowedToStartSubstates = "AllowedToStartSubstates";

        /// <remarks />
        public const string Cancel = "Cancel";

        /// <remarks />
        public const string CancelMethodType = "CancelMethodType";

        /// <remarks />
        public const string Clear = "Clear";

        /// <remarks />
        public const string ClearMethodType = "ClearMethodType";

        /// <remarks />
        public const string Closed = "Closed";

        /// <remarks />
        public const string Completed = "Completed";

        /// <remarks />
        public const string Ended = "Ended";

        /// <remarks />
        public const string EndedSubstates = "EndedSubstates";

        /// <remarks />
        public const string EquipmentID = "EquipmentID";

        /// <remarks />
        public const string FromAllowedToStartToAborted = "FromAllowedToStartToAborted";

        /// <remarks />
        public const string FromAllowedToStartToAllowedToStart = "FromAllowedToStartToAllowedToStart";

        /// <remarks />
        public const string FromAllowedToStartToNotAllowedToStart = "FromAllowedToStartToNotAllowedToStart";

        /// <remarks />
        public const string FromAllowedToStartToRunning = "FromAllowedToStartToRunning";

        /// <remarks />
        public const string FromCompletedToClosed = "FromCompletedToClosed";

        /// <remarks />
        public const string FromHeldToSuspended = "FromHeldToSuspended";

        /// <remarks />
        public const string FromInterruptedToAborted = "FromInterruptedToAborted";

        /// <remarks />
        public const string FromInterruptedToEnded = "FromInterruptedToEnded";

        /// <remarks />
        public const string FromInterruptedToRunning = "FromInterruptedToRunning";

        /// <remarks />
        public const string FromLoadedToReady = "FromLoadedToReady";

        /// <remarks />
        public const string FromLoadedToWaiting = "FromLoadedToWaiting";

        /// <remarks />
        public const string FromNotAllowedToStartToAborted = "FromNotAllowedToStartToAborted";

        /// <remarks />
        public const string FromNotAllowedToStartToAllowedToStart = "FromNotAllowedToStartToAllowedToStart";

        /// <remarks />
        public const string FromNotAllowedToStartToNotAllowedToStart = "FromNotAllowedToStartToNotAllowedToStart";

        /// <remarks />
        public const string FromReadyToLoaded = "FromReadyToLoaded";

        /// <remarks />
        public const string FromReadyToWaiting = "FromReadyToWaiting";

        /// <remarks />
        public const string FromRunningToAborted = "FromRunningToAborted";

        /// <remarks />
        public const string FromRunningToEnded = "FromRunningToEnded";

        /// <remarks />
        public const string FromRunningToInterrupted = "FromRunningToInterrupted";

        /// <remarks />
        public const string FromSuspendedToHeld = "FromSuspendedToHeld";

        /// <remarks />
        public const string FromWaitingToReady = "FromWaitingToReady";

        /// <remarks />
        public const string Held = "Held";

        /// <remarks />
        public const string http___opcfoundation_org_UA_ISA95_JOBCONTROL_V2_ = "http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/";

        /// <remarks />
        public const string Interrupted = "Interrupted";

        /// <remarks />
        public const string InterruptedSubstates = "InterruptedSubstates";

        /// <remarks />
        public const string ISA95EndedStateMachineType = "ISA95EndedStateMachineType";

        /// <remarks />
        public const string ISA95EquipmentDataType = "ISA95EquipmentDataType";

        /// <remarks />
        public const string ISA95InterruptedStateMachineType = "ISA95InterruptedStateMachineType";

        /// <remarks />
        public const string ISA95JobOrderAndStateDataType = "ISA95JobOrderAndStateDataType";

        /// <remarks />
        public const string ISA95JobOrderDataType = "ISA95JobOrderDataType";

        /// <remarks />
        public const string ISA95JobOrderReceiverObjectType = "ISA95JobOrderReceiverObjectType";

        /// <remarks />
        public const string ISA95JobOrderReceiverSubStatesType = "ISA95JobOrderReceiverSubStatesType";

        /// <remarks />
        public const string ISA95JobOrderStatusEventType = "ISA95JobOrderStatusEventType";

        /// <remarks />
        public const string ISA95JobResponseDataType = "ISA95JobResponseDataType";

        /// <remarks />
        public const string ISA95JobResponseProviderObjectType = "ISA95JobResponseProviderObjectType";

        /// <remarks />
        public const string ISA95JobResponseReceiverObjectType = "ISA95JobResponseReceiverObjectType";

        /// <remarks />
        public const string ISA95MaterialDataType = "ISA95MaterialDataType";

        /// <remarks />
        public const string ISA95ParameterDataType = "ISA95ParameterDataType";

        /// <remarks />
        public const string ISA95PersonnelDataType = "ISA95PersonnelDataType";

        /// <remarks />
        public const string ISA95PhysicalAssetDataType = "ISA95PhysicalAssetDataType";

        /// <remarks />
        public const string ISA95PrepareStateMachineType = "ISA95PrepareStateMachineType";

        /// <remarks />
        public const string ISA95PropertyDataType = "ISA95PropertyDataType";

        /// <remarks />
        public const string ISA95StateDataType = "ISA95StateDataType";

        /// <remarks />
        public const string ISA95WorkMasterDataType = "ISA95WorkMasterDataType";

        /// <remarks />
        public const string JobOrder = "JobOrder";

        /// <remarks />
        public const string JobOrderList = "JobOrderList";

        /// <remarks />
        public const string JobOrderResponseList = "JobOrderResponseList";

        /// <remarks />
        public const string JobResponse = "JobResponse";

        /// <remarks />
        public const string JobState = "JobState";

        /// <remarks />
        public const string Loaded = "Loaded";

        /// <remarks />
        public const string MaterialClassID = "MaterialClassID";

        /// <remarks />
        public const string MaterialDefinitionID = "MaterialDefinitionID";

        /// <remarks />
        public const string MaxDownloadableJobOrders = "MaxDownloadableJobOrders";

        /// <remarks />
        public const string NotAllowedToStart = "NotAllowedToStart";

        /// <remarks />
        public const string NotAllowedToStartSubstates = "NotAllowedToStartSubstates";

        /// <remarks />
        public const string Pause = "Pause";

        /// <remarks />
        public const string PauseMethodType = "PauseMethodType";

        /// <remarks />
        public const string PersonnelID = "PersonnelID";

        /// <remarks />
        public const string PhysicalAssetID = "PhysicalAssetID";

        /// <remarks />
        public const string Ready = "Ready";

        /// <remarks />
        public const string ReceiveJobResponse = "ReceiveJobResponse";

        /// <remarks />
        public const string ReceiveJobResponseMethodType = "ReceiveJobResponseMethodType";

        /// <remarks />
        public const string RequestJobResponseByJobOrderID = "RequestJobResponseByJobOrderID";

        /// <remarks />
        public const string RequestJobResponseByJobOrderIDMethodType = "RequestJobResponseByJobOrderIDMethodType";

        /// <remarks />
        public const string RequestJobResponseByJobOrderState = "RequestJobResponseByJobOrderState";

        /// <remarks />
        public const string RequestJobResponseByJobOrderStateMethodType = "RequestJobResponseByJobOrderStateMethodType";

        /// <remarks />
        public const string Resume = "Resume";

        /// <remarks />
        public const string ResumeMethodType = "ResumeMethodType";

        /// <remarks />
        public const string RevokeStart = "RevokeStart";

        /// <remarks />
        public const string RevokeStartMethodType = "RevokeStartMethodType";

        /// <remarks />
        public const string Running = "Running";

        /// <remarks />
        public const string Start = "Start";

        /// <remarks />
        public const string StartMethodType = "StartMethodType";

        /// <remarks />
        public const string Stop = "Stop";

        /// <remarks />
        public const string StopMethodType = "StopMethodType";

        /// <remarks />
        public const string Store = "Store";

        /// <remarks />
        public const string StoreAndStart = "StoreAndStart";

        /// <remarks />
        public const string StoreAndStartMethodType = "StoreAndStartMethodType";

        /// <remarks />
        public const string StoreMethodType = "StoreMethodType";

        /// <remarks />
        public const string Suspended = "Suspended";

        /// <remarks />
        public const string Update = "Update";

        /// <remarks />
        public const string UpdateMethodType = "UpdateMethodType";

        /// <remarks />
        public const string Waiting = "Waiting";

        /// <remarks />
        public const string WorkMaster = "WorkMaster";
    }
    #endregion

    #region Namespace Declarations
    /// <remarks />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Namespaces
    {
        /// <summary>
        /// The URI for the ISA95_JOBCONTROL_V2 namespace (.NET code namespace is 'UAModel.ISA95_JOBCONTROL_V2').
        /// </summary>
        public const string ISA95_JOBCONTROL_V2 = "http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/";

        /// <summary>
        /// The URI for the OpcUa namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUa = "http://opcfoundation.org/UA/";
    }
    #endregion
}