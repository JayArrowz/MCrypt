using CommandLine;
using System.Collections.Generic;

namespace MCrypt
{
    [Verb("crypt", HelpText = "Crypt a file.")]
    public class CryptCommand
    {
        [Option('i', "input", Required = true, HelpText = "Input files to be processed (allows multiple).")]
        public IEnumerable<string> InputFiles { get; set; }

        [Option('d', "output-dir", Required = false, HelpText = "Output Directory", Default = "crypt-output/")]
        public string OutputDir { get; set; }

        [Option('o', "output-file", Required = false, HelpText = "Output Filename", Default = "MCry.exe")]
        public string OutputFileName { get; set; }
        
        [Option('r', "runtime", Required = false, HelpText = "Runtime type (https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)", Default = "win-x86")]
        public string Runtime { get; set; }

        [Option("stub-output-dir", Required = false, HelpText = "Stub source file output", Default = "stub-temp-src-out/")]
        public string SrcStubOutput { get; set; }

        [Option("randomise-out-resources", Required = false, HelpText = "Randomises the names of files inside exe", Default = true)]
        public bool RandomiseOutputFileNames { get; set; }

        [Option("publish-config", Required = false, HelpText = "Publish configuration", Default = "Release")]
        public string PublishConfiguration { get; set; }

        [Option("delay-execute", Required = false, HelpText = "Delay execution in seconds", Default = 10)]
        public int DelayExecution { get; set; }
    }
}
