using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    /// <summary>
    /// Interface to enable output to the console.
    /// </summary>
    public interface IPublisherDiagnostics
    {
        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Fetch diagnostic data.
        /// </summary>
        DiagnosticInfoMethodResponseModel GetDiagnosticInfo();

        /// <summary>
        /// Fetch diagnostic log data.
        /// </summary>
        Task<DiagnosticLogMethodResponseModel> GetDiagnosticLogAsync();

        /// <summary>
        /// Fetch diagnostic startup log data.
        /// </summary>
        Task<DiagnosticLogMethodResponseModel> GetDiagnosticStartupLogAsync();

        /// <summary>
        /// Kicks of the task to show diagnostic information each 30 seconds.
        /// </summary>
        Task ShowDiagnosticsInfoAsync(CancellationToken ct);

        /// <summary>
        /// Writes a line to the diagnostic log.
        /// </summary>
        void WriteLog(string message);
    }
}
