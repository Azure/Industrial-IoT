// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The integration tests host the OPC Publisher module in-process inside the test
    /// host. <c>PublisherModule.StopAsync</c> schedules <c>Process.GetCurrentProcess().Kill()</c>
    /// five minutes after a graceful stop when <c>DOTNET_RUNNING_IN_CONTAINER=true</c> - a
    /// production container failsafe that guarantees the container exits. The official
    /// Windows Server 2022/2025 (and Linux) .NET base images set that variable, so the very
    /// first publisher shutdown in a test arms a timer that hard-kills the shared test host
    /// roughly five minutes later, surfacing as "Test host process crashed" after ~1000
    /// tests. The Windows Server 2019 image did not set the variable, which is why the crash
    /// only appeared on the newer CI images. Clearing the variable for the test process
    /// neutralises the failsafe so it can never target the test host; production behaviour is
    /// unaffected because this only runs from the test assembly.
    /// </summary>
    internal static class TestProcessGuard
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
        }
    }
}
