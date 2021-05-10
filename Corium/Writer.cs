using System;
using System.Collections.Generic;
using System.Text;

namespace Corium
{
    class Writer
    {

        internal static void Error(string v)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ERROR] " + v);
            Console.ForegroundColor = original;
        }
        internal static void Error(string format, params object[] parameters)
        {
            Error(String.Format(format, parameters));
        }
        internal static void Debug(string v)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[DEBUG] " + v);
            Console.ForegroundColor = original;
        }
        internal static void Debug(string format, params object[] parameters)
        {
            Debug(String.Format(format, parameters));
        }
        internal static void Warning(string v)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[WARNING] " + v);
            Console.ForegroundColor = original;
        }
        internal static void Warning(string format, params object[] parameters)
        {
            Warning(String.Format(format, parameters));
        }
        internal static void Info(string v)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("[INFO] " + v);
            Console.ForegroundColor = original;
        }
        internal static void Exception(string v)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("[Exception] " + v);
            Console.ForegroundColor = original;
        }
        internal static void Info(string format, params object[] parameters)
        {
            Info(String.Format(format, parameters));
        }
        internal static void VerboseWarning(string s)
        {
            if (ProgramOptions.Verbose)
                Warning("[Verbose] " + s);
        }
        internal static void VerboseWarning(string format, params object[] parameters)
        {
            if (ProgramOptions.Verbose)
                VerboseWarning(String.Format(format, parameters));
        }

        internal static void VerboseError(string s)
        {
            if (ProgramOptions.Verbose)
                Error("[Verbose] " + s);
        }
        internal static void VerboseError(string format, params object[] parameters)
        {
            if (ProgramOptions.Verbose)
                VerboseError(String.Format(format, parameters));
        }

        internal static void VerboseInfo(string s)
        {
            if (ProgramOptions.Verbose)
                Info("[Verbose] " + s);
        }
        internal static void VerboseInfo(string format, params object[] parameters)
        {
            if (ProgramOptions.Verbose)
                VerboseInfo(String.Format(format, parameters));
        }

        internal static void VerboseException(string s)
        {
            if (ProgramOptions.Verbose)
                Exception("[Verbose] " + s);
        }

        internal static void VerboseDebug(string s)
        {
            if (ProgramOptions.Verbose)
                Debug("[Verbose] " + s);
        }
        internal static void VerboseDebug(string format, params object[] parameters)
        {
            if (ProgramOptions.Verbose)
                VerboseDebug(String.Format(format, parameters));
        }
    }
}
