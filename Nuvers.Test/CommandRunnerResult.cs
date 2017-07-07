using System;
using System.Diagnostics;

namespace Nuvers.Test
{
    public class CommandRunnerResult
    {
        public Process Process { get; }
        public int Item1 { get; }
        public string Item2 { get; }
        public string Item3 { get; }

        public int ExitCode => Item1;

        public bool Success => Item1 == 0;

        /// <summary>
        /// All output messages including errors
        /// </summary>
        public string AllOutput => Item2 + Environment.NewLine + Item3;

        public string Output => Item2;

        public string Errors => Item3;

        internal CommandRunnerResult(Process process, int exitCode, string output, string error)
        {
            Process = process;
            Item1 = exitCode;
            Item2 = output;
            Item3 = error;
        }
    }
}