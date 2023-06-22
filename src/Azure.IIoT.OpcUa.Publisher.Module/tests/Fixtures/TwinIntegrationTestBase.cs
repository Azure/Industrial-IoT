// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using System;
    using System.Threading;
    using Xunit.Abstractions;

    public abstract class TwinIntegrationTestBase : IDisposable
    {
        protected CancellationToken Ct => _cts.Token;

        protected TwinIntegrationTestBase(ITestOutputHelper testOutputHelper = null,
            TimeSpan? timeout = null)
        {
            _cts = new CancellationTokenSource(timeout ?? kTotalTestTimeout);
            _testOutputHelper = testOutputHelper;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_cts.IsCancellationRequested)
                    {
                        _testOutputHelper.WriteLine(
                            "OperationCanceledException thrown due to test time out.");
                    }
                    _cts.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private static readonly TimeSpan kTotalTestTimeout =
#if DEBUG
            TimeSpan.FromMinutes(10)
#else
            TimeSpan.FromMinutes(2)
#endif
            ;
        private readonly CancellationTokenSource _cts;
        private readonly ITestOutputHelper _testOutputHelper;
        private bool _disposedValue;
    }
}
