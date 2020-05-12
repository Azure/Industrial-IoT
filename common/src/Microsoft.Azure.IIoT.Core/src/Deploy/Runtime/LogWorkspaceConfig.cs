// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deploy.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Azure Log Analytics Workspace configuration
    /// </summary>
    public class LogWorkspaceConfig : ConfigBase, ILogWorkspaceConfig {

        private const string kWorkspaceId = "Docker:WorkspaceId";
        private const string kWorkspaceKey = "Docker:WorkspaceKey";
        private const string kWorkBookId = "Docker:WorkbookId";

        /// <inheritdoc/>
        public string LogWorkspaceId => GetStringOrDefault(kWorkspaceId,
            () => GetStringOrDefault(PcsVariable.PCS_WORKSPACE_ID));

        /// <inheritdoc/>
        public string LogWorkspaceKey => GetStringOrDefault(kWorkspaceKey,
            () => GetStringOrDefault(PcsVariable.PCS_WORKSPACE_KEY));

        /// <inheritdoc/>
        public string LogWorkbookId => GetStringOrDefault(kWorkBookId,
            () => GetStringOrDefault(PcsVariable.PCS_WORKBOOK_ID));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public LogWorkspaceConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
