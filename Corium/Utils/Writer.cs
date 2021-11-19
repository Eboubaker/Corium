using System;

namespace Corium.Utils
{
    public static class Writer
    {
        public static void FeedBack(string v)
        {
            W(v, Console.ForegroundColor);
        }

        public static void Error(string v)
        {
            W("[ERROR] " + v, ConsoleColor.Red);
        }

        public static void Warning(string v)
        {
            W("[WARNING] " + v, ConsoleColor.Yellow);
        }

        public static void Exception(string v)
        {
            W("[Exception] " + v, ConsoleColor.Red);
        }

        public static void VerboseWarning(string s)
        {
            if (Context.Verbose)
                Warning($"[Verbose] {s}");
        }

        public static void VerboseFeedBack(string s)
        {
            if (Context.Verbose)
                FeedBack($"[Verbose] {s}");
        }

        public static void VerboseException(string s)
        {
            if (Context.Verbose)
                Exception($"[Verbose] {s}");
        }

        /// <summary>
        /// [W] -> Write to the console with the given color
        /// </summary>
        private static void W(string s, ConsoleColor color)
        {
            if (Context.Silent) return;
            var original = Console.ForegroundColor;
            Console.ForegroundColor = color; // push
            Console.WriteLine(s);
            Console.ForegroundColor = original; // pop
        }

        public static void Suggestion(string s)
        {
            W($"[TIP] {s}", ConsoleColor.DarkMagenta);
        }
    }
}