
using System;
using static System.Console;

namespace Opc.Ua.Workarounds
{
    using static Opc.Ua.Publisher.Program;

    public static class TraceWorkaround
    {
        /// <summary>
        /// Trace message helper
        /// </summary>
        public static void Trace(string message, params object[] args)
        {
            Utils.Trace(Utils.TraceMasks.Information, message, args);
            WriteLine(DateTime.Now.ToString() + ": " + message, args);
        }

        public static void Trace(int traceMask, string format, params object[] args)
        {
            Utils.Trace(traceMask, format, args);
            if ((OpcStackTraceMask & traceMask) != 0)
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
