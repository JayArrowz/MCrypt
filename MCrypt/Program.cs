using MCrypt.Stub;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MCrypt
{
    public class Program
    {
        private const string DecryptionGuidVar = "Fields.DECRYPTION_GUID";
        private const string ResourceNameVar = "Fields.RESOURCE_NAME";
        private const string DefaultStubFileVar = "blank.json";
        private const string TempFolder = "TempCrypt";
        private static string StubPath => TempDir;
        private static string StubFilePath => Path.Combine(StubPath, nameof(StubFile) + ".cs");
        private static string ExePath => StubPath;
        private static string ProjectFilePath => Path.Combine(StubPath , "MCrypt.Stub.csproj");
        public static string TempDir => Path.Combine(Path.GetTempPath(), TempFolder);

        private static Guid TempDirGuid { get; set; } = Guid.NewGuid();

        //TODO: arg[1]? = Runtime https://docs.microsoft.com/en-us/dotnet/core/rid-catalog If self contained
        public static void Main(string[] args)
        {
            args = new string[1];
            args[0] = @"C:\Users\J\Desktop\Videos\Login.mp4";

            if (args.Length <= 0)
            {
                Console.WriteLine("Please specify a file.");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("File does not exist.");
                return;
            }

            var asm = Assembly.GetEntryAssembly();
            var stubAsmName = typeof(StubFile).Assembly.GetName().Name;
            var asmName = asm.GetName().Name;
            var tempDir = TempDir;

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }

            Directory.CreateDirectory(tempDir);

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
                    resourceStream[i] = resourceName.Replace(stubAsmName + ".", string.Empty);
                    resourceStream[i] = resourceStream[i] + ".cs";
                }

                var resourceBytes = ReadFully(resource);
                var filePath = Path.Combine(tempDir, resourceStream[i]);
                File.WriteAllBytes(filePath, resourceBytes);
            }

            var extension = Path.GetExtension(args[0]);
            var seed = Guid.NewGuid().ToString();
            var cipher = new IsaacRandom(seed);
            var outputFileName = Guid.NewGuid().ToString() + extension;
            var encryptionOutput = Path.Combine(ExePath, outputFileName);

            byte[] inputFile = File.ReadAllBytes(args[0]);

            for (int i = 0; i < inputFile.Length; i++)
            {
                inputFile[i] += cipher.NextByte();
            }

            string csProjText = File.ReadAllText(ProjectFilePath);
            File.WriteAllText(ProjectFilePath, csProjText.Replace(DefaultStubFileVar, outputFileName));
            File.WriteAllBytes(encryptionOutput, inputFile);

            string text = File.ReadAllText(StubFilePath);
            text = text.Replace(DecryptionGuidVar, "\"" + seed + "\"");
            text = text.Replace(ResourceNameVar, "\"" + outputFileName + "\"");
            File.WriteAllText(StubFilePath, text);
            
            Process p = new Process();            
            p.StartInfo.FileName = "dotnet.exe";
            p.StartInfo.WorkingDirectory = TempDir;

            //TODO: Extend capabilities for multiple OS'S
            p.StartInfo.Arguments = "publish -r win-x86 -c Release /p:PublishSingleFile=true;PublishTrimmed=true";
            p.Start();
            p.WaitForExit();
            p.Dispose();
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
