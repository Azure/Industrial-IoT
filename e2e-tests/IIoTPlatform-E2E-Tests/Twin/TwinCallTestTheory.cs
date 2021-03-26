// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Twin {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using Microsoft.CSharp.RuntimeBinder;
    using System;
    using System.Collections.Generic;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection(TwinTestCollection.CollectionName)]
    [Trait(TestConstants.TraitConstants.TwinModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    public class TwinCallTestTheory {
        private readonly TwinTestContext _context;

        public TwinCallTestTheory(TwinTestContext context, ITestOutputHelper output) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact, PriorityOrder(1)]
        public void GetMethodMetadata() {
            var methodId = "i=13358"; // CreateFile
            var methodMetadata = TestHelper.Twin.GetMethodMetadataAsync(_context, _context.OpcUaEndpointId, methodId).GetAwaiter().GetResult();

            Assert.Equal(methodMetadata.objectId, "i=13354");
            Assert.Equal(methodMetadata.inputArguments.Count, 2);
            Assert.Equal(methodMetadata.outputArguments.Count, 2);

            Assert.Equal(methodMetadata.inputArguments[0].name, "FileName");
            Assert.Equal(methodMetadata.inputArguments[0].type.displayName, "String");
            Assert.Equal(methodMetadata.inputArguments[1].name, "RequestFileOpen");
            Assert.Equal(methodMetadata.inputArguments[1].type.displayName, "Boolean");

            Assert.Equal(methodMetadata.outputArguments[0].name, "FileNodeId");
            Assert.Equal(methodMetadata.outputArguments[0].type.displayName, "NodeId");
            Assert.Equal(methodMetadata.outputArguments[1].name, "FileHandle");
            Assert.Equal(methodMetadata.outputArguments[1].type.displayName, "UInt32");
        }


        [Fact, PriorityOrder(2)]
        public void CallMethod() {
            // CreateFile method - not implemented
            var methodId = "i=13358";
            var arguments = new List<object> {
                new {dataType = "String", value = "TestFile"},
                new {dataType = "Boolean", value = "false"}
            };
            var methodMetadata = TestHelper.Twin.GetMethodMetadataAsync(_context, _context.OpcUaEndpointId, methodId).GetAwaiter().GetResult();
            var response = TestHelper.Twin.CallMethodAsync(_context, _context.OpcUaEndpointId, methodId, methodMetadata.objectId, arguments).GetAwaiter().GetResult();
            Assert.Equal("BadNotImplemented", GetErrorMessage(response));

            // ConditionRefresh method - wrong arguments
            methodId = "i=3875";
            arguments = new List<object> {
                new {dataType = "IntegerId", value = "0"}
            };

            methodMetadata = TestHelper.Twin.GetMethodMetadataAsync(_context, _context.OpcUaEndpointId, methodId).GetAwaiter().GetResult();
            response = TestHelper.Twin.CallMethodAsync(_context, _context.OpcUaEndpointId, methodId, methodMetadata.objectId, arguments).GetAwaiter().GetResult();
            Assert.True(GetErrorMessage(response).Contains("Cannot refresh conditions for a subscription that does not exist"));

            // HeaterOn method - no arguments expected
            methodId = "http://microsoft.com/Opc/OpcPlc/Boiler#s=HeaterOn";
            methodMetadata = TestHelper.Twin.GetMethodMetadataAsync(_context, _context.OpcUaEndpointId, methodId).GetAwaiter().GetResult();
            response = TestHelper.Twin.CallMethodAsync(_context, _context.OpcUaEndpointId, methodId, methodMetadata.objectId, new List<object>()).GetAwaiter().GetResult();
            Assert.Null(GetErrorMessage(response));
        }

        private string GetErrorMessage(dynamic content) {
            try {
                return content.errorInfo.errorMessage.ToString();
            }
            catch (RuntimeBinderException) {
                return null;
            }
        }
    }
}
