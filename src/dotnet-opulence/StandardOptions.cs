using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace Opulence
{
    internal static class StandardOptions
    {
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
    }
}