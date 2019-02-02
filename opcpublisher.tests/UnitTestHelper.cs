using System.Threading;

namespace OpcPublisher
{
    using static Program;

    /// <summary>
    /// Class with unit test helper methods.
    /// </summary>
    public static class UnitTestHelper
    {
        public static string GetMethodName([System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            return memberName;
        }

        public static int WaitTilItemsAreMonitored()
        {
            int _maxIterations = 30;
            // wait till monitoring starts
            int iter = 0;
            while (NodeConfiguration.NumberOfOpcMonitoredItemsMonitored == 0 && iter < _maxIterations)
            {
                Thread.Sleep(1000);
                iter++;
            }
            return iter < _maxIterations ? iter : -1;
        }
    }
}
