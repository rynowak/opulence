using System;
using System.CommandLine;

namespace Opulence
{
    internal sealed class OutputContext
    {
        public OutputContext(IConsole console, Verbosity verbosity)
        {
            if (console is null)
            {
                throw new ArgumentNullException(nameof(console));
            }

            Console = console;
            Verbosity = verbosity;
        }

        private IConsole Console { get; }

        public Verbosity Verbosity { get; }

        private void Write(Verbosity verbosity, string message)
        {
            if (Verbosity >= verbosity)
            {
                Console.Out.Write(message);
            }
        }

        private void WriteLine(Verbosity verbosity, string message)
        {
            if (Verbosity >= verbosity)
            {
                Console.Out.WriteLine(message);
            }
        }

        public void WriteAlways(string message)
        {
            Write(Verbosity.Info, message);
        }

        public void WriteAlwaysLine(string message)
        {
            WriteLine(Verbosity.Info, message);
        }

        public void WriteInfo(string message)
        {
            Write(Verbosity.Info, message);
        }

        public void WriteInfoLine(string message)
        {
            WriteLine(Verbosity.Info, message);
        }

        public void WriteDebug(string message)
        {
            Write(Verbosity.Debug, message);
        }

        public void WriteDebugLine(string message)
        {
            WriteLine(Verbosity.Debug, message);
        }

        public CapturedCommandOutput Capture()
        {
            return new CapturedCommandOutput(this);
        }

        public sealed class CapturedCommandOutput
        {
            private readonly OutputContext output;
            public CapturedCommandOutput(OutputContext output)
            {
                this.output = output;
            }

            public void StdOut(string line)
            {
                if (output.Verbosity >= Verbosity.Debug)
                {
                    output.Console.SetTerminalForegroundColor(ConsoleColor.Gray);
                    output.Console.Out.WriteLine("\t" + line);
                    output.Console.ResetTerminalForegroundColor();
                }
            }

            public void StdErr(string line)
            {
                if (output.Verbosity >= Verbosity.Info)
                {
                    output.Console.SetTerminalForegroundColor(ConsoleColor.Red);
                    output.Console.Out.WriteLine("\t" + line);
                    output.Console.ResetTerminalForegroundColor();
                }
            }
        }
    }
}