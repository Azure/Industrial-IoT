// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Helper to manage request and responses
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    internal class ServiceResponse<TRequest, TResult> :
        IReadOnlyList<ServiceResponse<TRequest, TResult>.Operation>
    {
        /// <summary>
        /// Error info
        /// </summary>
        public ServiceResultModel? ErrorInfo
        {
            get
            {
                if (StatusCode == StatusCodes.Good)
                {
                    return null;
                }
                return ResultInfo;
            }
        }

        /// <summary>
        /// Result info
        /// </summary>
        public ServiceResultModel ResultInfo
        {
            get
            {
                var diagnostics = _response.ResponseHeader.ServiceDiagnostics;
                var stringTable = _response.ResponseHeader.StringTable;
                return StatusCode.CreateResultModel(diagnostics, stringTable);
            }
        }

        /// <summary>
        /// Result info
        /// </summary>
        public StatusCode StatusCode => _response.ResponseHeader.ServiceResult;

        /// <inheritdoc/>
        public int Count => _operations.Length;

        /// <inheritdoc/>
        public Operation this[int index] => _operations[index];

        /// <summary>
        /// Validates responses against requests
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="statusCode"></param>
        /// <param name="diagnostics"></param>
        /// <param name="requested"></param>
        internal ServiceResponse(IServiceResponse response,
            IEnumerable<TResult>? results, Func<TResult, StatusCode> statusCode,
            DiagnosticInfoCollection? diagnostics = null,
            IEnumerable<TRequest>? requested = null)
        {
            _response = response;
            Debug.Assert(_response.ResponseHeader != null,
                "Response header should have been checked by ValidateResponse.");
            _statusCode = statusCode;
            if (results == null)
            {
                if (!StatusCode.IsBad(response.ResponseHeader.ServiceResult))
                {
                    response.ResponseHeader.ServiceResult = StatusCodes.BadUnexpectedError;
                    response.ResponseHeader.ServiceDiagnostics = new DiagnosticInfo
                    {
                        AdditionalInfo = "Response was good, but results were missing."
                    };
                }
                _results = [];
            }
            else
            {
                _results = results.ToArray();
            }
            if (requested == null)
            {
                _requests = _results.Length == 0 ?
                    [] :
                    new TRequest[_results.Length];
            }
            else
            {
                _requests = requested.ToArray();
            }
            if (_results.Length != _requests.Length)
            {
                if (!StatusCode.IsBad(response.ResponseHeader.ServiceResult))
                {
                    response.ResponseHeader.ServiceResult = StatusCodes.BadUnexpectedError;
                    response.ResponseHeader.ServiceDiagnostics = new DiagnosticInfo
                    {
                        AdditionalInfo = $"The server returned {_results.Length} results" +
                            $" but {_requests.Length} elements were expected."
                    };
                }
                if (_results.Length > _requests.Length)
                {
                    // Limit the results
                    _results = _results[0.._requests.Length];
                }
                else
                {
                    _results = [];
                }
            }
            if (diagnostics == null || diagnostics.Count == 0)
            {
                _diagnostics = _results.Length == 0 ?
                    [] :
                    new DiagnosticInfo[_results.Length];
            }
            else
            {
                _diagnostics = [.. diagnostics];
            }
            if (_diagnostics.Length != _results.Length)
            {
                if (!StatusCode.IsBad(response.ResponseHeader.ServiceResult))
                {
                    response.ResponseHeader.ServiceResult = StatusCodes.BadUnexpectedError;
                    response.ResponseHeader.ServiceDiagnostics = new DiagnosticInfo
                    {
                        AdditionalInfo = $"The server returned {_results.Length} diagnostic" +
                            $" infos but {_requests.Length} were expected."
                    };
                }
                _diagnostics = new DiagnosticInfo[_results.Length];
            }
            Activity.Current?.AddTag("Response", ErrorInfo);
            if (_results.Length > 0)
            {
                _operations = Enumerable.Range(0, _results.Length)
                    .Select(i => new Operation(this, i))
                    .ToArray();
            }
            else
            {
                _operations = [];
            }
        }

        /// <summary>
        /// Throw if error response
        /// </summary>
        /// <exception cref="ServiceResultException"></exception>
        public void ThrowIfError()
        {
            if (StatusCode.IsBad(StatusCode))
            {
                throw new ServiceResultException(new ServiceResult(
                    StatusCode,
                    _response.ResponseHeader.ServiceDiagnostics,
                    _response.ResponseHeader.StringTable));
            }
        }

        /// <inheritdoc/>
        public IEnumerator<Operation> GetEnumerator()
        {
            return ((IEnumerable<Operation>)_operations).GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _operations.GetEnumerator();
        }

        /// <summary>
        /// Service operation
        /// </summary>
        internal class Operation
        {
            /// <summary>
            /// Index
            /// </summary>
            public int Index { get; }

            /// <summary>
            /// Get result
            /// </summary>
            public DiagnosticInfo DiagnosticInfo => _outer._diagnostics[Index];

            /// <summary>
            /// Get result
            /// </summary>
            public TRequest Request => _outer._requests[Index];

            /// <summary>
            /// Get result
            /// </summary>
            public TResult Result => _outer._results[Index];

            /// <summary>
            /// Get status code
            /// </summary>
            public StatusCode StatusCode
            {
                get
                {
                    try
                    {
                        return _outer._statusCode(Result);
                    }
                    catch
                    {
                        return StatusCodes.BadUnknownResponse;
                    }
                }
            }

            /// <summary>
            /// Error info
            /// </summary>
            public ServiceResultModel? ErrorInfo
            {
                get
                {
                    if (StatusCode == StatusCodes.Good)
                    {
                        return null;
                    }
                    return ResultInfo;
                }
            }

            /// <summary>
            /// Result info
            /// </summary>
            public ServiceResultModel ResultInfo
            {
                get
                {
                    var stringTable = _outer._response.ResponseHeader.StringTable;
                    return StatusCode.CreateResultModel(DiagnosticInfo, stringTable);
                }
            }

            internal Operation(ServiceResponse<TRequest, TResult> outer, int i)
            {
                _outer = outer;
                Index = i;

                Activity.Current?.AddTag("Result_" + i, ErrorInfo);
            }

            private readonly ServiceResponse<TRequest, TResult> _outer;
        }

        private readonly TRequest[] _requests;
        private readonly TResult[] _results;
        private readonly DiagnosticInfo[] _diagnostics;
        private readonly Operation[] _operations;
        private readonly IServiceResponse _response;
        private readonly Func<TResult, StatusCode> _statusCode;
    }
}
