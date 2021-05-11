// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {

    /// <summary>
    /// PubSub writer group job
    /// </summary>
    public class WriterGroupJobModel {

        /// <summary>
        /// Writer group configuration
        /// </summary>
        public WriterGroupModel WriterGroup { get; set; }

        /// <summary>
        /// Injected connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Messaging mode to use
        /// </summary>
        public MessagingMode? MessagingMode { get; set; }

        /// <summary>
        /// Engine configuration
        /// </summary>
        public EngineConfigurationModel Engine { get; set; }
    }
}