using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using NuGet.Common;

namespace Nuvers
{
    public class MyConsole : Logger, IConsole
    {
        private readonly static object _writerLock = new object();

        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "isatty")]
        extern static int _isatty(int fd);
        public MyConsole()
        {
            var originalForegroundColor = Console.ForegroundColor;
            var originalBackgroundColor = Console.BackgroundColor;
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.ForegroundColor = originalForegroundColor;
                Console.BackgroundColor = originalBackgroundColor;
            };
        }

        public int CursorLeft
        {
            get
            {
                try
                {
                    return Console.CursorLeft;
                }
                catch (IOException)
                {
                    return 0;
                }
            }
            set => Console.CursorLeft = value;
        }

        public int WindowWidth
        {
            get
            {
                try
                {
                    var width = Console.WindowWidth;
                    return width > 0 ? width : 80;
                    // This happens when redirecting output to a file, on
                    // Linux and OS X (running with Mono).
                }
                catch (IOException)
                {
                    // probably means redirected to file
                    return int.MaxValue;
                }
            }
            set => Console.WindowWidth = value;
        }

        private Verbosity _verbosity;

        public Verbosity Verbosity
        {
            get => _verbosity;

            set
            {
                _verbosity = value;
                VerbosityLevel = GetVerbosityLevel(_verbosity);
            }
        }

        public bool IsNonInteractive
        {
            get; set;
        }

        private TextWriter Out => Verbosity == Verbosity.Quiet ? TextWriter.Null : Console.Out;

        public void Write(object value)
        {
            lock (_writerLock)
            {
                Out.Write(value);
            }
        }

        public void Write(string value)
        {
            lock (_writerLock)
            {
                Out.Write(value);
            }
        }

        public void Write(string format, params object[] args)
        {
            lock (_writerLock)
            {
                if (args == null || !args.Any())
                {
                    // Don't try to format strings that do not have arguments. We end up throwing if the original string was not meant to be a format token 
                    // and contained braces (for instance html)
                    Out.Write(format);
                }
                else
                {
                    Out.Write(format, args);
                }
            }
        }

        public void WriteLine()
        {
            lock (_writerLock)
            {
                Out.WriteLine();
            }
        }

        public void WriteLine(object value)
        {
            lock (_writerLock)
            {
                Out.WriteLine(value);
            }
        }

        public void WriteLine(string value)
        {
            lock (_writerLock)
            {
                Out.WriteLine(value);
            }
        }

        public void WriteLine(string format, params object[] args)
        {
            lock (_writerLock)
            {
                Out.WriteLine(format, args);
            }
        }

        public void WriteError(object value)
        {
            WriteError(value.ToString());
        }

        public void WriteError(string value)
        {
            WriteError(value, new object[0]);
        }

        public void WriteError(string format, params object[] args) => 
            WriteColor(Console.Error, ConsoleColor.Red, format, args);

        void IConsole.WriteWarning(string value) => 
            WriteWarning(prependWarningText: true, value: value, args: new object[0]);

        public void WriteWarning(bool prependWarningText, string value) => 
            WriteWarning(prependWarningText, value, new object[0]);

        public void WriteWarning(string value, params object[] args) => 
            WriteWarning(prependWarningText: true, value: value, args: args);

        public void WriteWarning(bool prependWarningText, string value, params object[] args)
        {
            string message = prependWarningText
                ? String.Format(CultureInfo.CurrentCulture, LocalizedResourceManager.GetString("CommandLine_Warning"), value)
                : value;

            WriteColor(Console.Out, ConsoleColor.Yellow, message, args);
        }

        public void WriteLine(ConsoleColor color, string value, params object[] args) => WriteColor(Out, color, value, args);

        private static void WriteColor(TextWriter writer, ConsoleColor color, string value, params object[] args)
        {
            lock (_writerLock)
            {
                var currentColor = Console.ForegroundColor;
                try
                {
                    currentColor = Console.ForegroundColor;
                    Console.ForegroundColor = color;
                    if (args == null || !args.Any())
                    {
                        // If it doesn't look like something that needs to be formatted, don't format it.
                        writer.WriteLine(value);
                    }
                    else
                    {
                        writer.WriteLine(value, args);
                    }
                }
                finally
                {
                    Console.ForegroundColor = currentColor;
                }
            }
        }

        public void PrintJustified(int startIndex, string text) => PrintJustified(startIndex, text, WindowWidth);

        public void PrintJustified(int startIndex, string text, int maxWidth)
        {
            if (maxWidth > startIndex)
            {
                maxWidth = maxWidth - startIndex - 1;
            }

            lock (_writerLock)
            {
                while (text.Length > 0)
                {
                    // Trim whitespace at the beginning
                    text = text.TrimStart();
                    // Calculate the number of chars to print based on the width of the System.Console
                    int length = Math.Min(text.Length, maxWidth);

                    // Text we can print without overflowing the System.Console, excluding new line characters.
                    int newLineIndex = text.IndexOf(Environment.NewLine, 0, length, StringComparison.OrdinalIgnoreCase);
                    var content = text.Substring(0, newLineIndex > -1 ? newLineIndex : length);

                    int leftPadding = startIndex + content.Length - CursorLeft;

                    // Print it with the correct padding
                    Out.WriteLine((leftPadding > 0) ? content.PadLeft(leftPadding) : content);

                    // Get the next substring to be printed
                    text = text.Substring(content.Length);
                }
            }
        }

        public bool Confirm(string description)
        {
            if (IsNonInteractive)
            {
                return true;
            }

            var currentColor = ConsoleColor.Gray;
            try
            {
                currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(String.Format(CultureInfo.CurrentCulture, LocalizedResourceManager.GetString("ConsoleConfirmMessage"), description));
                var result = Console.ReadLine();
                return result.StartsWith(LocalizedResourceManager.GetString("ConsoleConfirmMessageAccept"), StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Console.ForegroundColor = currentColor;
            }
        }

        public ConsoleKeyInfo ReadKey()
        {
            EnsureInteractive();
            return Console.ReadKey(intercept: true);
        }

        public string ReadLine()
        {
            EnsureInteractive();
            return Console.ReadLine();
        }

        public void ReadSecureString(SecureString secureString)
        {
            EnsureInteractive();
            try
            {
                ReadSecureStringFromConsole(secureString);
            }
            catch (InvalidOperationException)
            {
                // This can happen when you redirect nuget.exe input, either from the shell with "<" or 
                // from code with ProcessStartInfo. 
                // In this case, just read data from Console.ReadLine()
                foreach (var c in ReadLine())
                {
                    secureString.AppendChar(c);
                }
            }
            secureString.MakeReadOnly();
        }

        private static void ReadSecureStringFromConsole(SecureString secureString)
        {
            // When you redirect nuget.exe input, either from the shell with "<" or
            // from code with ProcessStartInfo, throw exception on mono.
            if (!RuntimeEnvironmentHelper.IsWindows && RuntimeEnvironmentHelper.IsMono && _isatty(1) != 1)
            {
                throw new InvalidOperationException();
            }
            ConsoleKeyInfo keyInfo;
            while ((keyInfo = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
            {
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (secureString.Length < 1)
                    {
                        continue;
                    }
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    Console.Write(' ');
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    secureString.RemoveAt(secureString.Length - 1);
                }
                else
                {
                    secureString.AppendChar(keyInfo.KeyChar);
                    Console.Write('*');
                }
            }
            Console.WriteLine();
        }

        private void EnsureInteractive()
        {
            //if (IsNonInteractive)
            //{
            //    throw new InvalidOperationException(LocalizedResourceManager.GetString("Error_CannotPromptForInput"));
            //}
        }

        //public void Log(ILogMessage message)
        //{
        //    if (DisplayMessage(message.Level))
        //    {
        //        if (message.Level == LogLevel.Debug)
        //        {
        //            WriteColor(Out, ConsoleColor.Gray, message.Message);
        //        }
        //        else if (message.Level == LogLevel.Warning)
        //        {
        //            WriteWarning(message.FormatWithCode());
        //        }
        //        else if (message.Level == LogLevel.Error)
        //        {
        //            WriteError(message.FormatWithCode());
        //        }
        //        else
        //        {
        //            // Verbose, Information
        //            WriteLine(message.Message);
        //        }
        //    }
        //}

        //public override Task LogAsync(ILogMessage message)
        //{
        //    Log(message);

        //    return Task.FromResult(0);
        //}

        private static LogLevel GetVerbosityLevel(Verbosity level)
        {
            switch (level)
            {
                case Verbosity.Detailed:
                    return LogLevel.Debug;
                case Verbosity.Normal:
                    return LogLevel.Information;
                case Verbosity.Quiet:
                    return LogLevel.Warning;
            }

            return LogLevel.Information;
        }
    }
}