// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MqttTestValidator.Models {
    public class MqttVerificationDetailedResponse {
        /// <summary>
        /// Indicates if the verification task has finished
        /// </summary>
        public bool IsFinished { get; set; }
        /// <summary>
        /// Indicates if the verification task has faulted
        /// </summary>
        public bool HasFailed { get; set; }
        /// <summary>
        /// Error message in case of <see cref="HasFailed"/> is true
        /// </summary>
        public string Error { get; set; } = string.Empty;
        /// <summary>
        /// Total number if messages received on the topic
        /// </summary>
        public ulong NumberOfMessages { get; set; }
        /// <summary>
        /// Minimal message id received 
        /// </summary>
        public uint LowestMessageId { get; set; }
        /// <summary>
        /// Maximal message id received 
        /// </summary>
        public uint HighestMessageId { get; set; }
    }
}
