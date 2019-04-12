// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{
    using Autofac;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    public class WarmStartDatabase : IStartable
    {
        private readonly IDocumentDBRepository _repository;
        private readonly ICertificateRequest _certificateRequest;
        private readonly IApplicationsDatabase _applicationDatabase;
        private readonly ILogger _logger;

        public WarmStartDatabase(
            IDocumentDBRepository repository,
            ICertificateRequest certificateRequest,
            IApplicationsDatabase applicationDatabase,
            ILogger logger
            )
        {
            _repository = repository;
            _certificateRequest = certificateRequest;
            _applicationDatabase = applicationDatabase;
            _logger = logger;
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                try
                {
                    await _repository.CreateRepositoryIfNotExistsAsync();
                    await _applicationDatabase.Initialize();
                    await _certificateRequest.Initialize();
                    _logger.Information("Database warm start successful.");
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to warm start databases.", ex);
                }
            });
        }
    }
}
