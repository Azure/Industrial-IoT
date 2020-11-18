namespace TestEventProcessor.BusinessLogic
{
    /// <summary>
    /// Represents the result of the Stop-Command of the TelemetryValidator.
    /// </summary>
    public class StopResult : IResult
    {
        /// <summary>
        /// Flag whether the monitoring was successful (without errors) or not.
        /// </summary>
        public bool IsSuccess { get; set; }
    }
}