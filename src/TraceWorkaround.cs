
using System;
using static System.Console;

namespace Opc.Ua.Workarounds
{
    /// <summary>
    /// Class to enable output to the console.
    /// </summary>
    public static class TraceWorkaround
    {
        private static int _opcStackTraceMask;
        private static bool _verboseConsole;

        public static void Init(int opcStackTraceMask, bool verboseConsole)
        {
            _opcStackTraceMask = opcStackTraceMask;
            _verboseConsole = verboseConsole;
        }
        /// <summary>
        /// Trace message helper
        /// </summary>
        public static void Trace(string message)
        {
            Utils.Trace(Utils.TraceMasks.Error, message);
            if (_verboseConsole)
            {
                WriteLine(DateTime.Now.ToString() + ": " + message);
            }
        }

        public static void Trace(string message, params object[] args)
        {
            Utils.Trace(Utils.TraceMasks.Error, message, args);
            if (_verboseConsole)
            {
                WriteLine(DateTime.Now.ToString() + ": " + message, args);
            }
        }

        public static void Trace(int traceMask, string format, params object[] args)
        {
            Utils.Trace(traceMask, format, args);
            if (_verboseConsole && (_opcStackTraceMask & traceMask) != 0)
            {
                WriteLine(DateTime.Now.ToString() + ": " + format, args);
            }
        }

        public static void Trace(Exception e, string format, params object[] args)
        {
            Utils.Trace(e, format, args);
            WriteLine(DateTime.Now.ToString() + ": " + e.Message.ToString());
            WriteLine(DateTime.Now.ToString() + ": " + format, args);
        }
    }
}
