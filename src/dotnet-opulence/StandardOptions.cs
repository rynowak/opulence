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

        public static Option Project
        {
            get
            {
                // This dance is necessary to try and put the intialization and validation
                // of the project file in a single code path, and have the command line system
                // be responsible for reporting the errors.

                var argument = new Argument<FileInfo>(TryConvert)
                {
                    Arity = ArgumentArity.ZeroOrOne,
                    Name = "project-file or directory",
                };

                argument.SetDefaultValue(() =>
                {
                    var directoryPath = Path.GetFullPath(".");
                    if (TryFindProjectFile(directoryPath, out var projectFilePath, out var errorMessage))
                    {
                        return new FileInfo(projectFilePath);
                    }
                    else
                    {
                        // This might be called when we're not going to find anything. 
                        // Just return null for now, and the validator will catch it.
                        return null;
                    }
                });

                argument.AddValidator(r =>
                {
                    var directoryPath = Path.GetFullPath(".");
                    if (TryFindProjectFile(directoryPath, out var projectFilePath, out var errorMessage))
                    {
                        return null;
                    }
                    else
                    {
                        return errorMessage;
                    }
                });

                return new Option(new [] { "-p", "--project" }, "application project file")
                {
                    Argument = argument,
                };

                static bool TryFindProjectFile(string directoryPath, out string? projectFilePath, out string? errorMessage)
                {
                    var matches = new List<string>();
                    foreach (var candidate in Directory.EnumerateFiles(directoryPath))
                    {
                        if (Path.GetExtension(candidate).EndsWith("proj"))
                        {
                            matches.Add(candidate);
                        }
                    }

                    if (matches.Count == 0)
                    {
                        errorMessage = $"no project file was found in directory '{directoryPath}'.";
                        projectFilePath = default;
                        return false;
                    }
                    else if (matches.Count == 1)
                    {
                        errorMessage = null;
                        projectFilePath = matches[0];
                        return true;
                    }
                    else
                    {
                        errorMessage = $"more than one project file was found in directory '{directoryPath}'.";
                        projectFilePath = default;
                        return false;
                    }
                }

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
                        if (TryFindProjectFile(token, out var filePath, out var errorMessage))
                        {
                            file = new FileInfo(filePath);
                            return true;
                        }
                        else
                        {
                            symbol.ErrorMessage = errorMessage;
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