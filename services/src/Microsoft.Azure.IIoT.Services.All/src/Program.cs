// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.All {
    using System.Threading.Tasks;

    /// <summary>
    /// All in one services host
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point for all in one services host
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            Task.WaitAll(new[] {
                Task.Run(() => Hub.Router.Program.Main(args)),
                Task.Run(() => OpcUa.Alerting.Program.Main(args)),
                Task.Run(() => OpcUa.Jobs.Program.Main(args)),
                Task.Run(() => OpcUa.Gateway.Program.Main(args)),
                Task.Run(() => OpcUa.History.Program.Main(args)),
                Task.Run(() => OpcUa.Onboarding.Program.Main(args)),
                Task.Run(() => OpcUa.Processor.Program.Main(args)),
                Task.Run(() => OpcUa.Registry.Program.Main(args)),
                Task.Run(() => OpcUa.Twin.Program.Main(args)),
                Task.Run(() => OpcUa.Vault.Program.Main(args)),
            });
        }
    }
}
