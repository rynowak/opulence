using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;

namespace Opulence
{
    internal static class StandardOptions
    {
        private static readonly string[] AllOutputs = new string[] { "container", "chart", };

        public static Option Outputs
        {
            get
            {
                var argument = new Argument<List<string>>(TryConvert)
                {
                    Arity = ArgumentArity.ZeroOrMore,
                };
                argument.AddSuggestions(AllOutputs);
                argument.SetDefaultValue(new List<string>(AllOutputs));

                return new Option(new[]{ "-o", "--outputs" }, "outputs to generate")
                {
                    Argument = argument,
                };

                static bool TryConvert(SymbolResult symbol, out List<string> outputs)
                {
                    outputs = new List<string>();

                    foreach (var token in symbol.Tokens)
                    {
                        if (!AllOutputs.Any(item => string.Equals(item, token.Value, StringComparison.OrdinalIgnoreCase)))
                        {
                            symbol.ErrorMessage = $"output '{token.Value}' is not recognized";
                            outputs = default!;
                            return false;
                        }

                        outputs.Add(token.Value.ToLowerInvariant());
                    }

                    return true;
                }
            }
        }

        public static Option ProjectFile
        {
            get
            {
                return new Option(new [] { "-p", "--project-file" }, "application project file")
                {
                    Argument = new Argument<FileInfo>(TryConvert)
                    {
                        Arity = ArgumentArity.ExactlyOne,
                        Name = "project-file or directory",
                    },
                    Required = true,
                };

                static bool TryConvert(SymbolResult symbol, out FileInfo file)
                {
                    var token = symbol.Token.Value;
                    if (File.Exists(token))
                    {
                        file = new FileInfo(token);
                        return true;
                    }

                    if (Directory.Exists(token))
                    {
                        var matches = new List<string>();
                        foreach (var candidate in Directory.EnumerateFiles(token))
                        {
                            if (Path.GetExtension(candidate).EndsWith("proj"))
                            {
                                matches.Add(candidate);
                            }
                        }

                        if (matches.Count == 0)
                        {
                            symbol.ErrorMessage = $"no project file was found in directory '{token}'.";
                            file = default!;
                            return false;
                        }
                        else if (matches.Count == 1)
                        {
                            file = new FileInfo(matches[0]);
                            return true;
                        }
                        else
                        {
                            symbol.ErrorMessage = $"more than one project file was found in directory '{token}'.";
                            file = default!;
                            return false;
                        }
                    }

                    symbol.ErrorMessage = $"the project file '{token}' could not be found.";
                    file = default!;
                    return false;
                }
            }
        }
    
        public static Option Verbosity
        {
            get
            {
                return new Option(new [] { "-v", "--verbosity" }, "output verbostiy")
                {
                    Argument = new Argument<Verbosity>("one of: quiet|info|debug", Opulence.Verbosity.Info)
                    {
                        Arity = ArgumentArity.ExactlyOne,
                    },
                    Required = false,
                };
            }
        }
    }
}