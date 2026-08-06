// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class ServiceResultExTests
    {
        [Fact]
        public void ToServiceResultModelFromGoodResult()
        {
            var sr = new ServiceResult(StatusCodes.Good);
            var model = sr.ToServiceResultModel();

            Assert.Equal(StatusCodes.Good.Code, model.StatusCode);
        }

        [Fact]
        public void ToServiceResultModelFromBadResult()
        {
            var sr = new ServiceResult(StatusCodes.Bad);
            var model = sr.ToServiceResultModel();

            Assert.Equal(StatusCodes.Bad.Code, model.StatusCode);
            Assert.NotNull(model.SymbolicId);
        }

        [Fact]
        public void ToServiceResultModelFromResultWithInnerBadResult()
        {
            var inner = new ServiceResult(StatusCodes.BadNotFound);
            var outer = new ServiceResult(StatusCodes.Bad, inner);
            var model = outer.ToServiceResultModel();

            Assert.Equal(StatusCodes.Bad.Code, model.StatusCode);
            Assert.NotNull(model.Inner);
            Assert.Equal(StatusCodes.BadNotFound.Code, model.Inner!.StatusCode);
        }

        [Fact]
        public void ToServiceResultModelFromResultWithGoodInnerOmitsInner()
        {
            var inner = new ServiceResult(StatusCodes.Good);
            var outer = new ServiceResult(StatusCodes.Bad, inner);
            var model = outer.ToServiceResultModel();

            Assert.Null(model.Inner);
        }

        [Fact]
        public void ToServiceResultModelFromExceptionTimeoutException()
        {
            var ex = new TimeoutException("timed out");
            var model = ex.ToServiceResultModel();

            Assert.Equal(StatusCodes.BadTimeout.Code, model.StatusCode);
            Assert.Contains("timed out", model.ErrorMessage);
        }

        [Fact]
        public void ToServiceResultModelFromExceptionOperationCanceledException()
        {
            var ex = new OperationCanceledException("cancelled");
            var model = ex.ToServiceResultModel();

            Assert.Equal(StatusCodes.BadRequestCancelledByClient.Code, model.StatusCode);
            Assert.Contains("cancelled", model.ErrorMessage);
        }

        [Fact]
        public void ToServiceResultModelFromExceptionResourceInvalidState()
        {
            var ex = new ResourceInvalidStateException("bad state");
            var model = ex.ToServiceResultModel();

            Assert.Equal(StatusCodes.BadInvalidState.Code, model.StatusCode);
        }

        [Fact]
        public void ToServiceResultModelFromExceptionResourceNotFoundException()
        {
            var ex = new ResourceNotFoundException("not found");
            var model = ex.ToServiceResultModel();

            Assert.Equal(StatusCodes.BadNotFound.Code, model.StatusCode);
        }

        [Fact]
        public void ToServiceResultModelFromExceptionResourceConflictException()
        {
            var ex = new ResourceConflictException("conflict");
            var model = ex.ToServiceResultModel();

            Assert.Equal(StatusCodes.BadEntryExists.Code, model.StatusCode);
        }

        [Fact]
        public void ToServiceResultModelFromExceptionArgumentNullException()
        {
            var ex = new ArgumentNullException("param");
            var model = ex.ToServiceResultModel();

            Assert.Equal(StatusCodes.BadArgumentsMissing.Code, model.StatusCode);
        }

        [Fact]
        public void ToServiceResultModelFromExceptionArgumentException()
        {
            var ex = new ArgumentException("invalid arg");
            var model = ex.ToServiceResultModel();

            Assert.Equal(StatusCodes.BadInvalidArgument.Code, model.StatusCode);
        }

        [Fact]
        public void ToServiceResultModelFromExceptionGeneric()
        {
            var ex = new InvalidOperationException("generic error");
            var model = ex.ToServiceResultModel();

            Assert.Equal(StatusCodes.Bad.Code, model.StatusCode);
            Assert.Contains("generic error", model.ErrorMessage);
        }

        [Fact]
        public void ToServiceResultModelFromServiceResultException()
        {
            var sre = new ServiceResultException(StatusCodes.BadCertificateInvalid);
            var model = sre.ToServiceResultModel();

            Assert.Equal(StatusCodes.BadCertificateInvalid.Code, model.StatusCode);
        }

        [Fact]
        public void CreateResultModelFromGoodStatusCodeReturnsModel()
        {
            var code = StatusCodes.Good;
            var model = code.CreateResultModel();

            Assert.Equal(StatusCodes.Good.Code, model.StatusCode);
        }

        [Fact]
        public void CreateResultModelFromBadStatusCodeWithNullDiagnosticsUsesSymbolicId()
        {
            var code = StatusCodes.Bad;
            var model = code.CreateResultModel(null, null);

            Assert.Equal(StatusCodes.Bad.Code, model.StatusCode);
            Assert.NotNull(model.SymbolicId);
        }

        [Fact]
        public void CreateResultModelWithDiagnosticsAndStringTable()
        {
            var code = StatusCodes.BadNotFound;
            var stringTable = new List<string> { "my.namespace", "SymBadNotFound", "Not found message", "en-US" };
            var diagnostics = new DiagnosticInfo
            {
                NamespaceUri = 0,
                SymbolicId = 1,
                LocalizedText = 2,
                Locale = 3
            };

            var model = code.CreateResultModel(diagnostics, stringTable);

            Assert.Equal(StatusCodes.BadNotFound.Code, model.StatusCode);
            Assert.Equal("Not found message", model.ErrorMessage);
            Assert.Equal("en-US", model.Locale);
            Assert.Equal("SymBadNotFound", model.SymbolicId);
            Assert.Equal("my.namespace", model.NamespaceUri);
        }

        [Fact]
        public void CreateResultModelWithInnerDiagnosticInfo()
        {
            var code = StatusCodes.Bad;
            var innerCode = StatusCodes.BadNotFound;
            var diagnostics = new DiagnosticInfo
            {
                InnerStatusCode = innerCode.Code,
                InnerDiagnosticInfo = new DiagnosticInfo()
            };

            var model = code.CreateResultModel(diagnostics, null);

            Assert.NotNull(model.Inner);
            Assert.Equal(StatusCodes.BadNotFound.Code, model.Inner!.StatusCode);
        }

        [Fact]
        public void CreateResultModelWithGoodInnerOmitsInner()
        {
            var code = StatusCodes.Bad;
            var diagnostics = new DiagnosticInfo
            {
                InnerStatusCode = StatusCodes.Good.Code
            };

            var model = code.CreateResultModel(diagnostics, null);

            Assert.Null(model.Inner);
        }

        [Fact]
        public void CreateResultModelFromBadStatusCodeNoStringTableFallsBackToSymbolicId()
        {
            var code = StatusCodes.BadTimeout;
            var diagnostics = new DiagnosticInfo();

            var model = code.CreateResultModel(diagnostics, null);

            Assert.Equal(StatusCodes.BadTimeout.Code, model.StatusCode);
            Assert.NotNull(model.SymbolicId);
            Assert.Equal("en-US", model.Locale); // Historical default preserved
        }

        [Fact]
        public void CreateResultModelWithNullDiagnosticsHasNoLocale()
        {
            var code = StatusCodes.Good;
            var model = code.CreateResultModel(null, null);

            // Good result with no diagnostics and no string table → no error message → no locale
            Assert.Null(model.ErrorMessage);
        }
    }
}
