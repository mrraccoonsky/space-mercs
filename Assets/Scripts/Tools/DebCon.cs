using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Tools
{
    public enum ConsoleVerbosity
    {
        Verbose = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Exception = 4,
        None = 5
    }

    public static class DebCon
    {
        public static ConsoleVerbosity Verbosity;

        private readonly struct RgbColor
        {
            private readonly byte _r, _g, _b;

            public RgbColor(byte r, byte g, byte b)
            {
                _r = r;
                _g = g;
                _b = b;
            }

            public string ToHex() => $"#{_r:X2}{_g:X2}{_b:X2}FF";
        }

        private static readonly RgbColor ColorDefault = new(200, 200, 200);  // lt gray
        private static readonly RgbColor ColorInfo = new(100, 181, 246);     // lt blue
        private static readonly RgbColor ColorWarning = new(255, 213, 79);   // yellow
        private static readonly RgbColor ColorError = new(239, 83, 80);      // red
        
        private static string FormatMessage(RgbColor color, object message)
        {
            return $"<color={color.ToHex()}>{message}</color>";
        }

        private static string FormatMessageWithCategory(RgbColor color, string category, object message)
        {
            var catColor = GetCatColor(category);
            return $"<color={catColor.ToHex()}><b>[{category}]</b></color> <color={color.ToHex()}>{message}</color>";
        }
        
        private static RgbColor GetCatColor(string cat)
        {
            if (string.IsNullOrEmpty(cat))
                return ColorDefault;
                
            var hash = cat.GetHashCode();
            byte r = (byte)Math.Max(127, Math.Abs(hash % 256));
            byte g = (byte)Math.Max(127, Math.Abs((hash >> 8) % 256));
            byte b = (byte)Math.Max(127, Math.Abs((hash >> 16) % 256)); 
            
            return new RgbColor(r, g, b);
        }

        private static void LogInternal(Action<string, Object> logAction, RgbColor color, string category, object message, Object context)
        {
            if (message == null) return;

            var msg = string.IsNullOrEmpty(category)
                ? FormatMessage(color, message)
                : FormatMessageWithCategory(color, category, message);

            logAction(msg, context);
        }
        
// Verbose

        [Conditional("DEBUG")]
        public static void Log(object msg, string cat = null, Object ctx = null)
        {
            if (Verbosity > ConsoleVerbosity.Verbose) return;
            LogInternal(Debug.Log, ColorDefault, cat, msg, ctx);
        }

        [Conditional("DEBUG")]
        public static void LogFormat(string format, string cat = null, Object ctx = null, params object[] args)
        {
            if (Verbosity > ConsoleVerbosity.Verbose) return;
            LogInternal(Debug.Log, ColorDefault, cat, string.Format(format, args), ctx);
        }
        
// Info

        [Conditional("DEBUG")]
        public static void Info(object msg, string cat = null, Object ctx = null)
        {
            if (Verbosity > ConsoleVerbosity.Info) return;
            LogInternal(Debug.Log, ColorInfo, cat, msg, ctx);
        }

        [Conditional("DEBUG")]
        public static void InfoFormat(string format, string cat = null, Object ctx = null, params object[] args)
        {
            if (Verbosity > ConsoleVerbosity.Info) return;
            LogInternal(Debug.Log, ColorInfo, cat, string.Format(format, args), ctx);
        }
        
// Warn
        
        [Conditional("DEBUG")]
        public static void Warn(object msg, string cat = null, Object ctx = null)
        {
            if (Verbosity > ConsoleVerbosity.Warning) return;
            LogInternal(Debug.LogWarning, ColorWarning, cat, msg, ctx);
        }

        [Conditional("DEBUG")]
        public static void WarnFormat(string format, string cat = null, Object ctx = null, params object[] args)
        {
            if (Verbosity > ConsoleVerbosity.Warning) return;
            LogInternal(Debug.LogWarning, ColorWarning, cat, string.Format(format, args), ctx);
        }
        
// Err
        
        [Conditional("DEBUG")]
        public static void Err(object msg, string cat = null, Object ctx = null)
        {
            if (Verbosity > ConsoleVerbosity.Error) return;
            LogInternal(Debug.LogError, ColorError, cat, msg, ctx);
        }

        [Conditional("DEBUG")]
        public static void ErrFormat(string format, string cat = null, Object ctx = null, params object[] args)
        {
            if (Verbosity > ConsoleVerbosity.Error) return;
            LogInternal(Debug.LogError, ColorError, cat, string.Format(format, args), ctx);
        }

// Exception
        
        [Conditional("DEBUG")]
        public static void Exception(Exception exception, string cat = null, Object ctx = null)
        {
            if (Verbosity > ConsoleVerbosity.Exception) return;
            if (exception == null) return;

            var msg = $"{exception.Message}\n{exception.StackTrace}";
            LogInternal(Debug.LogError, ColorError, cat, msg, ctx);
        }
    }
}