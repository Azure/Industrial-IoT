// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MqttTestValidator.Controllers {
    using Microsoft.AspNetCore.Mvc;
    using MqttTestValidator.Interfaces;
    using MqttTestValidator.Models;

    [ApiController]
    [Route("/Mqtt")]
    public class MqttVerificationController : ControllerBase {
        private readonly ILogger<MqttVerificationController> _logger;
        private readonly ITaskRepository _repostiory;
        private readonly IVerificationTaskFactory _factory;

        public MqttVerificationController(ILogger<MqttVerificationController> logger, ITaskRepository repository, IVerificationTaskFactory factory) {
            _logger = logger;
            _repostiory = repository;
            _factory = factory;
        }

        [HttpPost("/StartVerification")]
        [Produces(typeof(MqttVerificationResponse))]
        public IActionResult Verify([FromBody] MqttVerificationRequest request) {
            if (request == null) {
                _logger.LogDebug("Request object is null");
                return BadRequest();
            }

            if (string.IsNullOrEmpty(request.MqttBroker)) {
                _logger.LogDebug("MqttBroker is null or Empty");
                return BadRequest();
            }

            if (string.IsNullOrEmpty(request.MqttTopic)) {
                _logger.LogDebug("Mqtt Topic is null or Empty");
                return BadRequest();
            }

            var verificationTask = _factory.CreateVerificationTask(request);
            _repostiory.Add(verificationTask);
            verificationTask.Start();

            _logger.LogInformation("Started new verification Task with id: {Id}", verificationTask.Id);
            return Ok(new MqttVerificationResponse { ValidationTaskId = verificationTask.Id });
        }

        [HttpGet("/GetVerificationResult/{id}")]
        [Produces(typeof(MqttVerificationDetailedResponse))]
        public IActionResult GetResult([FromRoute] ulong id) {
            if (!_repostiory.Contains(id)) {
                _logger.LogDebug("Verification task id unkown");
                return BadRequest();
            }

            var verificationTask = _repostiory.GetById(id);
            return Ok(verificationTask.GetResult());
        }
    }
}