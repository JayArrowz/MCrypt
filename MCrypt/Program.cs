using MCrypt.Stub;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MCrypt
{
    public class Program
    {
        private const string DecryptionGuidVar = "Fields.DECRYPTION_GUID";
        private const string ResourceNameVar = "Fields.RESOURCE_NAME";
        private const string DefaultStubFileVar = "stubFile.json";

        private const string StubPath = "Stub/";
        private const string StubFilePath = StubPath + nameof(StubFile) + ".cs";
        private const string ExePath = StubPath + "Data/";
        private const string ProjectFilePath = StubPath + "MCrypt.Stub.csproj";

        public static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine("Please provide a file name.");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("File does not exist.");
                return;
            }

            var extension = Path.GetExtension(args[0]);
            var seed = Guid.NewGuid().ToString();
            var cipher = new IsaacRandom(seed);
            var outputFileName = Guid.NewGuid().ToString() + extension;
            var encryptionOutput = ExePath + outputFileName;

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
            Process.Start($"dotnet publish {ProjectFilePath} --self-contained").WaitForExit();
            Console.ReadLine();
        }
    }
}
