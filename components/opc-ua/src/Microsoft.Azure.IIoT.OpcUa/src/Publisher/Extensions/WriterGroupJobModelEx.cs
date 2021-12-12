// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Linq;

    /// <summary>
    /// Writer Group Model extensions
    /// </summary>
    public static class WriterGroupJobModelEx {

        /// <summary>
        /// Returns the job Id
        /// </summary>
        public static string GetJobId(this WriterGroupJobModel model) {
            return model?.WriterGroup?.WriterGroupId;
        }
    }
}