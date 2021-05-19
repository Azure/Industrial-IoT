// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestExtensions {
    using IIoTPlatform_E2E_Tests.TestModels;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Context to pass between test, for test that handle multiple OPC UA nodes
    /// </summary>
    public class IIoTMultipleNodesTestContext : IIoTPlatformTestContext  {
        public IIoTMultipleNodesTestContext() {
            ConsumedOpcUaNodes = new Dictionary<string, PublishedNodesEntryModel>();
        }

        /// <summary>
        /// Save to simulated OPC UA Nodes, key is the discovery url of opc server
        /// </summary>
        public IReadOnlyDictionary<string, PublishedNodesEntryModel> SimulatedPublishedNodes { get; private set; }

        /// <summary>
        /// Dictionary that can be used to save the nodes, that are currently published
        /// </summary>
        public IDictionary<string, PublishedNodesEntryModel> ConsumedOpcUaNodes { get; }

        /// <summary>
        /// Uses the Testhelper to load the simulated OPC UA Nodes and transform them
        /// </summary>
        /// <returns></returns>
        public async Task LoadSimulatedPublishedNodes(CancellationToken token) {
            var simulatedPlcs = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(this, token);

            SimulatedPublishedNodes = new ReadOnlyDictionary<string, PublishedNodesEntryModel>(
                simulatedPlcs.ToDictionary(kvp => kvp.Value.EndpointUrl, kvp => kvp.Value));
        }

        /// <summary>
        /// Create a Copy except the OpcNodes array
        /// </summary>
        /// <param name="testPlc">Source object</param>
        /// <returns>Copy</returns>
        public PublishedNodesEntryModel GetEntryModelWithoutNodes(PublishedNodesEntryModel testPlc) {
            return new PublishedNodesEntryModel {
                EncryptedAuthPassword = testPlc.EncryptedAuthPassword,
                EncryptedAuthUsername = testPlc.EncryptedAuthUsername,
                EndpointUrl = testPlc.EndpointUrl,
                OpcAuthenticationPassword = testPlc.OpcAuthenticationPassword,
                OpcAuthenticationUsername = testPlc.OpcAuthenticationUsername,
                UseSecurity = testPlc.UseSecurity,
                OpcNodes = null
            };
        }

        /// <summary>
        /// Reset the consumed nodes
        /// </summary>
        public void Reset() {
            ConsumedOpcUaNodes?.Clear();
        }
    }
}
