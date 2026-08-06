// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="ServiceResponseEx"/> and the <see cref="ServiceResponse{TRequest, TResult}"/>
    /// class it wraps. All tests are pure logic — no OPC UA server needed.
    /// </summary>
    public sealed class ServiceResponseExTests
    {
        // ── ServiceResponse: good response with matching requests ─────────────

        [Fact]
        public void Validate_GoodResponseWithMatchingRequestsAndResults_HasNoErrorInfo()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1", "req-2" };
            var results = new[] { StatusCodes.Good, StatusCodes.Good };

            var sr = response.Validate(results, s => s, null, requests);

            Assert.Null(sr.ErrorInfo);
            Assert.Equal(2, sr.Count);
        }

        [Fact]
        public void Validate_GoodResponseWithMatchingRequestsAndResults_OperationsAreIterable()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1", "req-2" };
            var results = new[] { StatusCodes.Good, StatusCodes.Good };

            var sr = response.Validate(results, s => s, null, requests);

            var count = 0;
            foreach (var op in sr)
            {
                Assert.NotNull(op);
                count++;
            }
            Assert.Equal(2, count);
        }

        // ── ServiceResponse: bad response status ──────────────────────────────

        [Fact]
        public void Validate_BadResponseStatus_HasErrorInfo()
        {
            var response = CreateBadResponse(StatusCodes.BadServiceUnsupported);
            var requests = new[] { "req-1" };
            var results = new[] { StatusCodes.Good };

            var sr = response.Validate(results, s => s, null, requests);

            Assert.NotNull(sr.ErrorInfo);
            Assert.Equal(StatusCodes.BadServiceUnsupported.Code, sr.ErrorInfo!.StatusCode);
        }

        // ── ServiceResponse: null results ─────────────────────────────────────

        [Fact]
        public void Validate_NullResults_SetsResponseToBadUnexpectedError()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1" };

            var sr = response.Validate(
                (IEnumerable<StatusCode>?)null, s => s, null, requests);

            Assert.NotNull(sr.ErrorInfo);
            Assert.Equal(0, sr.Count);
        }

        // ── ServiceResponse: mismatched result count ──────────────────────────

        [Fact]
        public void Validate_MoreResultsThanRequests_TruncatesResults()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1" };
            var results = new[] { StatusCodes.Good, StatusCodes.Good, StatusCodes.Good };

            var sr = response.Validate(results, s => s, null, requests);

            Assert.Equal(1, sr.Count);
        }

        [Fact]
        public void Validate_FewerResultsThanRequests_ReturnsEmptyResults()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1", "req-2", "req-3" };
            var results = new[] { StatusCodes.Good };

            var sr = response.Validate(results, s => s, null, requests);

            Assert.Equal(0, sr.Count);
        }

        // ── ServiceResponse: per-operation error info ─────────────────────────

        [Fact]
        public void Validate_GoodResultOperation_HasNoErrorInfo()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1" };
            var results = new[] { StatusCodes.Good };

            var sr = response.Validate(results, s => s, null, requests);

            Assert.Null(sr[0].ErrorInfo);
        }

        [Fact]
        public void Validate_BadResultOperation_HasErrorInfo()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1" };
            var results = new[] { StatusCodes.BadNotFound };

            var sr = response.Validate(results, s => s, null, requests);

            Assert.NotNull(sr[0].ErrorInfo);
            Assert.Equal(StatusCodes.BadNotFound.Code, sr[0].ErrorInfo!.StatusCode);
        }

        // ── ServiceResponse: ArrayOf overload ─────────────────────────────────

        [Fact]
        public void Validate_ArrayOfResultsOverload_WorksLikeEnumerableOverload()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1", "req-2" };
            var results = new ArrayOf<StatusCode>(new[] { StatusCodes.Good, StatusCodes.Good });
            var diagnostics = new ArrayOf<DiagnosticInfo>(Array.Empty<DiagnosticInfo>());

            var sr = response.Validate(
                results,
                s => s,
                diagnostics,
                requests);

            Assert.Null(sr.ErrorInfo);
            Assert.Equal(2, sr.Count);
        }

        [Fact]
        public void Validate_ArrayOfWithNoRequest_WorksWithoutRequestList()
        {
            var response = CreateGoodResponse();
            var results = new ArrayOf<StatusCode>(new[] { StatusCodes.Good });
            var diagnostics = new ArrayOf<DiagnosticInfo>(Array.Empty<DiagnosticInfo>());

            var sr = response.Validate(
                results,
                s => s,
                diagnostics);

            Assert.Equal(1, sr.Count);
        }

        // ── ServiceResponse: no-request IEnumerable overload ─────────────────

        [Fact]
        public void Validate_NoRequestOverload_CountMatchesResults()
        {
            var response = CreateGoodResponse();
            var results = new[] { StatusCodes.Good, StatusCodes.Good, StatusCodes.Good };

            var sr = response.Validate(results, s => s, null);

            Assert.Equal(3, sr.Count);
        }

        // ── ServiceResponse: ThrowIfError ─────────────────────────────────────

        [Fact]
        public void Validate_GoodResponse_ThrowIfErrorDoesNotThrow()
        {
            var response = CreateGoodResponse();
            var sr = response.Validate(
                new[] { StatusCodes.Good },
                s => s,
                null);

            var ex = Record.Exception(() => sr.ThrowIfError());
            Assert.Null(ex);
        }

        [Fact]
        public void Validate_BadResponse_ThrowIfErrorThrowsServiceResultException()
        {
            var response = CreateBadResponse(StatusCodes.BadServiceUnsupported);
            var sr = response.Validate(
                new[] { StatusCodes.Good },
                s => s,
                null);

            Assert.Throws<ServiceResultException>(() => sr.ThrowIfError());
        }

        // ── AsLookupTable ─────────────────────────────────────────────────────

        [Fact]
        public void AsLookupTable_GoodResponseWithStructRequests_BuildsDictionary()
        {
            var response = CreateGoodResponse();
            var requests = new[] { (uint)1, (uint)2 };
            var results = new[] { StatusCodes.Good, StatusCodes.BadNotFound };

            var sr = response.Validate(results, s => s, null, requests);
            var lookup = sr.AsLookupTable();

            Assert.Equal(2, lookup.Count);
            Assert.True(lookup.ContainsKey(1u));
            Assert.True(lookup.ContainsKey(2u));
            Assert.Null(lookup[1u].Item2);
            Assert.NotNull(lookup[2u].Item2);
        }

        [Fact]
        public void AsLookupTable_EmptyResponse_ReturnsEmptyDictionary()
        {
            var response = CreateGoodResponse();
            var requests = Array.Empty<uint>();
            var results = Array.Empty<StatusCode>();

            var sr = response.Validate(results, s => s, null, requests);
            var lookup = sr.AsLookupTable();

            Assert.Empty(lookup);
        }

        // ── ResultInfo even on a Good response ────────────────────────────────

        [Fact]
        public void Validate_ResultInfo_ReturnsServiceResultModel()
        {
            var response = CreateGoodResponse();
            var sr = response.Validate(
                new[] { StatusCodes.Good },
                s => s,
                null);

            var info = sr.ResultInfo;
            Assert.NotNull(info);
            Assert.Equal(StatusCodes.Good.Code, info.StatusCode);
        }

        // ── Per-operation ResultInfo ──────────────────────────────────────────

        [Fact]
        public void Validate_BadOperationResult_ResultInfoContainsStatusCode()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1" };
            var results = new[] { StatusCodes.BadNotFound };

            var sr = response.Validate(results, s => s, null, requests);

            var info = sr[0].ResultInfo;
            Assert.NotNull(info);
            Assert.Equal(StatusCodes.BadNotFound.Code, info.StatusCode);
        }

        [Fact]
        public void Validate_GoodOperationResult_ResultInfoStatusIsGood()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1" };
            var results = new[] { StatusCodes.Good };

            var sr = response.Validate(results, s => s, null, requests);

            var info = sr[0].ResultInfo;
            Assert.NotNull(info);
            Assert.Equal(StatusCodes.Good.Code, info.StatusCode);
        }

        // ── statusCode lambda throws exception ────────────────────────────────

        [Fact]
        public void Validate_StatusCodeLambdaThrows_OperationReturnsUnknownResponse()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1" };
            var results = new[] { StatusCodes.Good };

            // statusCode lambda always throws
            var sr = response.Validate(results,
                _ => throw new InvalidOperationException("bad extractor"), null, requests);

            Assert.Equal(StatusCodes.BadUnknownResponse.Code, sr[0].StatusCode.Code);
        }

        // ── Diagnostics count mismatch ────────────────────────────────────────

        [Fact]
        public void Validate_DiagnosticsCountMismatch_SetsResponseToBadUnexpectedError()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1", "req-2" };
            var results = new[] { StatusCodes.Good, StatusCodes.Good };
            var diagnostics = new List<DiagnosticInfo>
            {
                new DiagnosticInfo()
                // Only 1 diagnostic for 2 results → mismatch
            };

            var sr = response.Validate(results, s => s, diagnostics, requests);

            // The mismatch resets diagnostics to empty arrays, but ErrorInfo comes from service header
            Assert.Equal(2, sr.Count);
            // The service-level error is set to BadUnexpectedError
            Assert.NotNull(sr.ErrorInfo);
        }

        // ── ArrayOf overload: null diagnostics (IsNull) ───────────────────────

        [Fact]
        public void Validate_ArrayOfWithNullDiagnostics_WorksLikeNullList()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-1" };
            var results = new ArrayOf<StatusCode>(new[] { StatusCodes.Good });
            // Default ArrayOf<DiagnosticInfo>() is null (IsNull == true)
            var diagnostics = default(ArrayOf<DiagnosticInfo>);

            var sr = response.Validate(results, s => s, diagnostics, requests);

            Assert.Equal(1, sr.Count);
            Assert.Null(sr.ErrorInfo);
        }

        // ── Operation Index ───────────────────────────────────────────────────

        [Fact]
        public void Validate_MultipleOperations_IndexesAreCorrect()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-0", "req-1", "req-2" };
            var results = new[] { StatusCodes.Good, StatusCodes.BadNotFound, StatusCodes.Good };

            var sr = response.Validate(results, s => s, null, requests);

            Assert.Equal(0, sr[0].Index);
            Assert.Equal(1, sr[1].Index);
            Assert.Equal(2, sr[2].Index);
        }

        // ── Operation Request/Result ──────────────────────────────────────────

        [Fact]
        public void Validate_Operations_RequestAndResultAreMapped()
        {
            var response = CreateGoodResponse();
            var requests = new[] { "req-alpha", "req-beta" };
            var results = new[] { StatusCodes.Good, StatusCodes.BadTimeout };

            var sr = response.Validate(results, s => s, null, requests);

            Assert.Equal("req-alpha", sr[0].Request);
            Assert.Equal("req-beta", sr[1].Request);
            Assert.Equal(StatusCodes.Good.Code, sr[0].Result.Code);
            Assert.Equal(StatusCodes.BadTimeout.Code, sr[1].Result.Code);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static IServiceResponse CreateGoodResponse()
        {
            var mock = new Mock<IServiceResponse>();
            mock.SetupGet(r => r.ResponseHeader).Returns(new ResponseHeader
            {
                ServiceResult = StatusCodes.Good,
                StringTable = new ArrayOf<string>(Array.Empty<string>()),
                ServiceDiagnostics = new DiagnosticInfo()
            });
            return mock.Object;
        }

        private static IServiceResponse CreateBadResponse(StatusCode statusCode)
        {
            var mock = new Mock<IServiceResponse>();
            mock.SetupGet(r => r.ResponseHeader).Returns(new ResponseHeader
            {
                ServiceResult = statusCode,
                StringTable = new ArrayOf<string>(Array.Empty<string>()),
                ServiceDiagnostics = new DiagnosticInfo()
            });
            return mock.Object;
        }
    }
}
