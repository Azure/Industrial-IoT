using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestEventProcessor.BusinessLogic;
using TestEventProcessor.Service.Authentication;
using TestEventProcessor.Service.Enums;
using TestEventProcessor.Service.Models;

namespace TestEventProcessor.Service.Controllers
{
    /// <summary>
    /// Controller that provides access to the runtime of the validator to start and stop monitoring as well as requesting a status.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [BasicAuthentication]
    public class RuntimeController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ITelemetryValidator _validator;

        /// <summary>
        /// Creates a new instance of the RuntimeController.
        /// </summary>
        /// <param name="logger">The logger to use to log messages.</param>
        /// <param name="validator">The validator to use to validate telemetry.</param>
        public RuntimeController(ILogger<RuntimeController> logger, ITelemetryValidator validator)
        {
            _logger = logger;
            _validator = validator;
        }

        /// <summary>
        /// Start/Stop monitoring of messages that are being received by the IotHub. If the command is "Start", the configuration in
        /// the model needs to be passed. For "Stop", no additional information is required.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <returns>Result of the command.</returns>
        [HttpPut]
        public async Task<IResult> Command(CommandModel command)
        {
            IResult result;

            switch (command.CommandType)
            {
                case CommandEnum.Start:
                    result= await _validator.StartAsync(command.Configuration);
                    break;
                case CommandEnum.Stop:
                    result = await _validator.StopAsync();
                    break;
                default: throw new NotImplementedException($"Unknown command: {command.CommandType}");
            }

            return result;
        }
    }
}
