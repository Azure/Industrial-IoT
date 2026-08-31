// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller
{
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class LegacyControllerTests
    {
        [Theory]
        [InlineData(nameof(LegacyController.GetInfoAsync), "GetInfo not supported")]
        [InlineData(nameof(LegacyController.GetDiagnosticLogAsync),
            "GetDiagnosticLog not supported")]
        [InlineData(nameof(LegacyController.GetDiagnosticStartupLogAsync),
            "GetDiagnosticStartupLog not supported")]
        public async Task UnsupportedMethodsReturnFaultedTasksAsync(
            string method, string message)
        {
            var controller = new LegacyController();

            var exception = await Assert.ThrowsAsync<NotSupportedException>(() => method switch
            {
                nameof(LegacyController.GetInfoAsync) => controller.GetInfoAsync(),
                nameof(LegacyController.GetDiagnosticLogAsync) =>
                    controller.GetDiagnosticLogAsync(),
                _ => controller.GetDiagnosticStartupLogAsync()
            });

            Assert.Equal(message, exception.Message);
        }
    }
}
