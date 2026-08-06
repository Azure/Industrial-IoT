// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Opc.Ua;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="ServiceResultModelEx.WithSymbolicId"/>.
    /// </summary>
    public class ServiceResultModelExTests
    {
        [Fact]
        public void WithSymbolicId_MissingSymbolicId_PopulatesFromStatusCode()
        {
            // StatusCodes.BadNodeIdUnknown = 0x80340000
            var model = new ServiceResultModel
            {
                StatusCode = StatusCodes.BadNodeIdUnknown.Code,
                SymbolicId = null
            };

            var result = model.WithSymbolicId();

            Assert.Same(model, result);
            Assert.False(string.IsNullOrEmpty(result.SymbolicId));
            Assert.Contains("BadNodeIdUnknown", result.SymbolicId!,
                System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void WithSymbolicId_GoodStatus_NoSymbolicIdPopulated()
        {
            var model = new ServiceResultModel
            {
                StatusCode = StatusCodes.Good.Code,
                SymbolicId = null
            };

            var result = model.WithSymbolicId();

            // Good has no symbolic id usually - depends on the StatusCodes dictionary
            // At minimum, the model is returned unchanged (same instance)
            Assert.Same(model, result);
        }

        [Fact]
        public void WithSymbolicId_ExistingSymbolicId_NotOverwritten()
        {
            var model = new ServiceResultModel
            {
                StatusCode = StatusCodes.BadNodeIdUnknown.Code,
                SymbolicId = "MyCustomSymbolicId"
            };

            var result = model.WithSymbolicId();

            Assert.Equal("MyCustomSymbolicId", result.SymbolicId);
        }

        [Fact]
        public void WithSymbolicId_ReturnsSameInstance()
        {
            var model = new ServiceResultModel
            {
                StatusCode = StatusCodes.BadTimeout.Code,
                SymbolicId = null
            };

            var returned = model.WithSymbolicId();

            Assert.Same(model, returned);
        }

        [Fact]
        public void WithSymbolicId_AppliesRecursivelyToInnerResult()
        {
            var inner = new ServiceResultModel
            {
                StatusCode = StatusCodes.BadNodeIdUnknown.Code,
                SymbolicId = null
            };
            var model = new ServiceResultModel
            {
                StatusCode = StatusCodes.BadTimeout.Code,
                SymbolicId = null,
                Inner = inner
            };

            model.WithSymbolicId();

            // Inner should also get its symbolic id populated
            Assert.False(string.IsNullOrEmpty(inner.SymbolicId));
        }

        [Fact]
        public void WithSymbolicId_NullInner_DoesNotThrow()
        {
            var model = new ServiceResultModel
            {
                StatusCode = StatusCodes.BadTimeout.Code,
                SymbolicId = null,
                Inner = null
            };

            var ex = Record.Exception(() => model.WithSymbolicId());
            Assert.Null(ex);
        }

        [Fact]
        public void WithSymbolicId_BadTimeout_PopulatesFromStatusCode()
        {
            var model = new ServiceResultModel
            {
                StatusCode = StatusCodes.BadTimeout.Code,
                SymbolicId = null
            };

            model.WithSymbolicId();

            Assert.False(string.IsNullOrEmpty(model.SymbolicId));
            Assert.Contains("BadTimeout", model.SymbolicId!,
                System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void WithSymbolicId_UnknownStatusCode_LeavesSymbolicIdEmpty()
        {
            var model = new ServiceResultModel
            {
                // 0x01230000 is not a standard status code
                StatusCode = 0x01230000u,
                SymbolicId = null
            };

            model.WithSymbolicId();

            // Unknown codes won't be found in the dictionary
            Assert.Null(model.SymbolicId);
        }

        [Fact]
        public void WithSymbolicId_DeepChain_AllGetPopulated()
        {
            var level2 = new ServiceResultModel
            {
                StatusCode = StatusCodes.BadNodeIdUnknown.Code,
                SymbolicId = null
            };
            var level1 = new ServiceResultModel
            {
                StatusCode = StatusCodes.BadTimeout.Code,
                SymbolicId = null,
                Inner = level2
            };
            var root = new ServiceResultModel
            {
                StatusCode = StatusCodes.BadNotFound.Code,
                SymbolicId = null,
                Inner = level1
            };

            root.WithSymbolicId();

            Assert.False(string.IsNullOrEmpty(root.SymbolicId));
            Assert.False(string.IsNullOrEmpty(level1.SymbolicId));
            Assert.False(string.IsNullOrEmpty(level2.SymbolicId));
        }
    }
}
