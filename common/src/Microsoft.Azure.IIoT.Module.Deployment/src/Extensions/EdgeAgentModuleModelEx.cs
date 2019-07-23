// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// IotEdge Agent model extension
    /// </summary>
    public static class EdgeAgentModuleModelEx {

        /// <summary>
        /// Default system modules
        /// </summary>
        public static Dictionary<string, EdgeAgentModuleModel> DefaultSystemModules =>
            JsonConvertEx.DeserializeObject<Dictionary<string, EdgeAgentModuleModel>>(@"{
                ""edgeAgent"": {
                    ""type"": ""docker"",
                    ""settings"": {
                        ""image"": ""mcr.microsoft.com/azureiotedge-agent:latest"",
                        ""createOptions"": """"
                    }
                },
                ""edgeHub"": {
                    ""type"": ""docker"",
                    ""status"": ""running"",
                    ""restartPolicy"": ""always"",
                    ""settings"": {
                        ""image"": ""mcr.microsoft.com/azureiotedge-hub:latest"",
                        ""createOptions"": ""{\""HostConfig\"":{\""PortBindings\"":{\""8883/tcp\"":[{\""HostPort\"":\""8883\""}],\""443/tcp\"":[{\""HostPort\"":\""443\""}]}},\""Env\"":[\""SSL_CERTIFICATE_PATH=/mnt/edgehub\"",\""SSL_CERTIFICATE_NAME=edge-hub-server.cert.pfx\""]}""
                    }
                }
            }");

        // TODO: Remove Env (SSL_CERTIFICATE_PATH and SSL_CERTIFICATE_NAME) from
        //       modulesContent.$edgeAgent.systemModules.edgeHub.settings.createOptions
        //       once Azure/iot-edge-v1#632 is fixed and available on mcr.microsoft.com
    }
}
