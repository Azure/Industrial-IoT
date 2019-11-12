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
                Task.Run(() => Common.Configuration.Program.Main(args)),
                Task.Run(() => Common.Identity.Program.Main(args)),
                Task.Run(() => Common.Jobs.Program.Main(args)),
                Task.Run(() => Common.Jobs.Edge.Program.Main(args)),
                Task.Run(() => Common.Hub.Fileupload.Program.Main(args)),
                Task.Run(() => Processor.Telemetry.Program.Main(args)),
                Task.Run(() => OpcUa.Registry.Discovery.Program.Main(args)),
                Task.Run(() => OpcUa.Registry.Onboarding.Program.Main(args)),
                Task.Run(() => OpcUa.Registry.Events.Program.Main(args)),
                Task.Run(() => OpcUa.Registry.Security.Program.Main(args)),
                Task.Run(() => OpcUa.Registry.Program.Main(args)),
                Task.Run(() => OpcUa.Twin.Program.Main(args)),
                Task.Run(() => OpcUa.Twin.Import.Program.Main(args)),
                Task.Run(() => OpcUa.Twin.Gateway.Program.Main(args)),
                Task.Run(() => OpcUa.Twin.History.Program.Main(args)),
                Task.Run(() => OpcUa.Publisher.Program.Main(args)),
                Task.Run(() => OpcUa.Vault.Program.Main(args)),
            });
        }
    }
}
