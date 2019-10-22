# MCrypt
Encrypt file and load

This project was made for educational purposes.

It can bind & encrypt multiple files together and execute with a delay. The default delay is 10 seconds.

Command Line Usage
MCrypt.exe -i InputFile.exe InputFile.txt

  -i, --input                  Required. Input files to be processed (allows multiple).

  -d, --output-dir             (Default: crypt-output/) Output Directory

  -o, --output-file            (Default: MCry.exe) Output Filename

  -r, --runtime                (Default: win-x86) Runtime type
                               (https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)

  --stub-output-dir            (Default: stub-temp-src-out/) Stub source file output

  --randomise-out-resources    (Default: true) Randomises the names of files inside exe

  --publish-config             (Default: Release) Publish configuration

  --delay-execute              (Default: 10) Delay execution in seconds

  --help                       Display this help screen.

  --version                    Display version information.
