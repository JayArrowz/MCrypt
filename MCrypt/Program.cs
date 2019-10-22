using CommandLine;
using MCrypt.Stub;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MCrypt
{
    public class Program
    {
        private const string DecryptionGuidVar = "Fields.DECRYPTION_GUID";
        private const string ResourceNameVar = "Fields.RESOURCE_NAME";
        private const string SecondsDelayVar = "Fields.SECONDS_DELAY";
        private static string StubFileName => nameof(StubFile) + ".cs";
        private static string ProjectFileName => "MCrypt.Stub.csproj";
        private const string ResourceText = "<ItemGroup>\r\n    <EmbeddedResource Include=\"{0}\">\r\n      <CopyToOutputDirectory>Always</CopyToOutputDirectory>\r\n    </EmbeddedResource>\r\n  </ItemGroup>";
        private const string ProjectEndText = "</Project>";

        public static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                Parse(args);
                Console.WriteLine("Press any key to continue.");
                Console.ReadLine();
                return;
            }
            else
            {
                //Interactive
                while (true)
                {
                    try
                    {
                        Console.Write("Enter Command (.e.g. help, crypt): ");
                        var line = Console.ReadLine().Trim();
                        var commandArgs = Regex.Split(line, "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                        for (var i = 0; i < commandArgs.Length; i++)
                        {
                            commandArgs[i] = commandArgs[i].Replace("\"", string.Empty);
                        }
                        Parse(commandArgs);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        private static void Parse(string[] args)
        {

            var parseResult = Parser.Default.ParseArguments(args, typeof(CryptCommand));
            _ = parseResult.MapResult((opts) => ExecuteCommand(opts), (error) => HandleParseError(error));

        }

        private static bool ExecuteCommand(object opts)
        {
            var command = opts as CryptCommand;
            try
            {
                RemoveAndRecreate(command.SrcStubOutput);
                RemoveAndRecreate(command.OutputDir);
                WriteStub(command);
                WriteInputFilesToStub(command);
                Publish(command);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            RemoveAndRecreate(command.SrcStubOutput);
            return true;
        }

        private static void RemoveAndRecreate(string srcStubOutput)
        {
            if (Directory.Exists(srcStubOutput))
            {
                Directory.Delete(srcStubOutput, true);
            }

            Directory.CreateDirectory(srcStubOutput);
        }

        private static bool HandleParseError(IEnumerable<Error> errors)
        {
            return false;
        }

        private static void WriteStub(CryptCommand command)
        {
            var asm = Assembly.GetEntryAssembly();
            var asmName = asm.GetName().Name;


            //Bit of ugly logic to resolve the project files
            var resourceStream = asm.GetManifestResourceNames();
            for (int i = 0; i < resourceStream.Length; i++)
            {
                var resourceName = resourceStream[i];
                var resource = Assembly.GetEntryAssembly().GetManifestResourceStream(resourceName);
                if (resourceStream[i].StartsWith(asmName + "." + asmName))
                {
                    resourceStream[i] = resourceName.Replace(asmName + "." + asmName, asmName);
                }
                else
                {
                    resourceStream[i] = resourceName.Replace(asmName + ".", string.Empty);
                }

                var resourceBytes = ReadFully(resource);
                var filePath = Path.Combine(command.SrcStubOutput, resourceStream[i]);
                File.WriteAllBytes(filePath, resourceBytes);
            }
        }

        public static void WriteInputFilesToStub(CryptCommand command)
        {
            var seed = Guid.NewGuid().ToString();
            var projectFilePath = Path.Combine(command.SrcStubOutput, ProjectFileName);
            var stubSrcFilePath = Path.Combine(command.SrcStubOutput, StubFileName);
            var csProjLines = File.ReadAllLines(projectFilePath).ToList();
            csProjLines[csProjLines.Count - 1] = "";
            string stubSourceText = File.ReadAllText(stubSrcFilePath);
            string inFilesStubString = "";
            stubSourceText = stubSourceText.Replace(DecryptionGuidVar, "\"" + seed + "\"");

            foreach (var inFile in command.InputFiles)
            {
                var cipher = new IsaacRandom(seed);
                var extension = Path.GetExtension(inFile);
                var outputFileName = command.RandomiseOutputFileNames ? Guid.NewGuid().ToString() + extension : Path.GetFileName(inFile);
                var encryptionOutput = Path.Combine(command.SrcStubOutput, outputFileName);

                byte[] inputFile = File.ReadAllBytes(inFile);

                for (int i = 0; i < inputFile.Length; i++)
                {
                    inputFile[i] += cipher.NextByte();
                }
                File.WriteAllBytes(encryptionOutput, inputFile);
                csProjLines.Add(string.Format(ResourceText, outputFileName));

                inFilesStubString += "\"" + outputFileName + "\"";
                if (!command.InputFiles.Last().Equals(inFile))
                {
                    inFilesStubString += ", ";
                }
            }

            stubSourceText = stubSourceText.Replace(ResourceNameVar, inFilesStubString);
            stubSourceText = stubSourceText.Replace(SecondsDelayVar, command.DelayExecution.ToString());
            csProjLines.Add(ProjectEndText);

            File.WriteAllText(stubSrcFilePath, stubSourceText);
            File.WriteAllText(projectFilePath, string.Join('\n', csProjLines));
        }

        public static void Publish(CryptCommand command)
        {
            using Process p = new Process();
            p.StartInfo.FileName = "dotnet.exe";
            p.StartInfo.WorkingDirectory = command.SrcStubOutput;
            p.StartInfo.Arguments = "publish -r " + command.Runtime + " -c " + command.PublishConfiguration;
            p.Start();
            p.WaitForExit();
            if (p.ExitCode == 0)
            {
                var outputExe =
                    Path.Combine(command.SrcStubOutput, "bin", command.PublishConfiguration, "netcoreapp3.0", command.Runtime, "publish", "MCrypt.Stub.Exe");
                var outputPath = Path.Combine(command.OutputDir, command.OutputFileName);
                Console.WriteLine("Copying files to " + outputPath);
                File.Move(outputExe, outputPath);
            }
            else
            {
                Console.WriteLine("Error publishing application");
            }
        }

        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
