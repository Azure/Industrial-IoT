// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{
    using System;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB;

    public class WarmStartDatabase : IStartable
    {
        IDocumentDBRepository _repository;
        ICertificateRequest _certificateRequest;
        IApplicationsDatabase _applicationDatabase;
        ILogger _logger;

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
                    _logger.Info("Database warm start successful.");
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to warm start databases.", ex);
                }
            });
        }
    }
}
