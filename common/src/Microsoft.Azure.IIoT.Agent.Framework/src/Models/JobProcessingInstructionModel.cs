// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {

    /// <summary>
    /// Processing instructions
    /// </summary>
    public class JobProcessingInstructionModel {

        /// <summary>
        /// Processing mode
        /// </summary>
        public ProcessMode? ProcessMode { get; set; }

        /// <summary>
        /// Job to process
        /// </summary>
        public JobInfoModel Job { get; set; }
    }
}