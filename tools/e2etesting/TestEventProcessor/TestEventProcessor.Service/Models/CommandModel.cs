using TestEventProcessor.BusinessLogic;
using TestEventProcessor.Service.Enums;

namespace TestEventProcessor.Service.Models
{
    /// <summary>
    /// The model that contains the command to execute along with its configuration (only required with "Start"-Command).
    /// </summary>
    public class CommandModel
    {
        public CommandEnum CommandType { get; set; }

        public ValidatorConfiguration Configuration { get; set; }
    }
}