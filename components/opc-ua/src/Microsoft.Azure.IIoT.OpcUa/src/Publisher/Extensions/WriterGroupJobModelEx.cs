// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Linq;

    /// <summary>
    /// Writer Group Model extensions
    /// </summary>
    public static class WriterGroupJobModelEx {

        /// <summary>
        /// Returns the job Id
        /// </summary>
        public static string GetJobId(this WriterGroupJobModel model) {
            var connection = model?.WriterGroup?.DataSetWriters?.First()?.DataSet?.DataSetSource?.Connection;
            if (connection == null) {
                return null;
            }

            return connection.CreateConnectionId();
        }
    }
}