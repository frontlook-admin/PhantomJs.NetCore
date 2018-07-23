using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace PhantomJs.NetCore
{
    public interface IPdfGenerator
    {
        /// <summary>
        /// Render the specified HTML code to a PDF.
        /// Save that PDF to the specified directory.
        /// </summary>
        /// <param name="html">The HTML to convert to PDF.</param>
        /// <param name="outputFolder">The directory to save the PDF to.</param>
        /// <returns>The full file path of the generated PDF.</returns>
        string GeneratePdf(string html, string outputFolder);
    }

    public class PdfGenerator : IPdfGenerator
    {
        private readonly OS _platform;
        private readonly PdfGeneratorOptions _options;

        public PdfGenerator(PdfGeneratorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _platform = GetOsPlatform();
        }

        /// <summary>
        /// Render the specified HTML code to a PDF.
        /// Save that PDF to the specified directory.
        /// </summary>
        /// <param name="html">The HTML to convert to PDF.</param>
        /// <param name="outputFolder">The directory to save the PDF to.</param>
        /// <returns>The file name of the generated PDF.</returns>
        public string GeneratePdf(string html, string outputFolder)
        {
            // Write the passed html in a file.
            var htmlFileName = WriteHtmlToTempFile(html);
            try
            {
                if (!Directory.Exists(outputFolder))
                {
                    throw new ArgumentException("The output folder is not a valid directory!");
                }

                var phantomExeToUse =
                    (_platform == OS.LINUX) ? "linux64_phantomjs.exe" :
                    (_platform == OS.WINDOWS) ? "windows_phantomjs.exe" :
                    "osx_phantomjs.exe";

                return ExecutePhantomJs(phantomExeToUse, htmlFileName, outputFolder);
            }
            finally
            {
                File.Delete(Path.Combine(_options.PhantomRootFolder, htmlFileName));
            }
        }

        private OS GetOsPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OS.WINDOWS;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OS.LINUX;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OS.OSX;
            }

            throw new Exception("PdfGenerator: OS Environment could not be probed, halting!");
        }

        private string ExecutePhantomJs(string phantomJsExeToUse, string inputFileName, string outputFolder)
        {
            // The output file must be located in the output folder.
            var outputFilePath = Path.Combine(outputFolder, $"{inputFileName}.pdf");

            var phantomJsAbsolutePath = Path.Combine(_options.PhantomRootFolder, phantomJsExeToUse);
            var startInfo = new ProcessStartInfo(phantomJsAbsolutePath)
            {
                WorkingDirectory = _options.PhantomRootFolder,
                Arguments = $"rasterize.js \"{inputFileName}\" \"{outputFilePath}\" \"{_options.PaperSize}\"",
                UseShellExecute = false
            };

            var proc = new Process() { StartInfo = startInfo };
            proc.Start();
            proc.WaitForExit();
            return outputFilePath;
        }

        private string WriteHtmlToTempFile(string html)
        {
            var filename = Path.GetRandomFileName() + ".html";
            var absolutePath = Path.Combine(_options.PhantomRootFolder, filename);
            File.WriteAllText(absolutePath, html);
            return filename;
        }

        private enum OS
        {
            LINUX,
            WINDOWS,
            OSX
        }
    }

    public class PdfGeneratorOptions
    {
        public const string DefaultPaperSize = "Letter";

        public PdfGeneratorOptions(string phantomRootFolder)
        {
            if (string.IsNullOrWhiteSpace(phantomRootFolder)) { throw new ArgumentNullException(nameof(phantomRootFolder)); }
            if (!Directory.Exists(phantomRootFolder)) { throw new ArgumentException($"Invalid Path: No such folder exists: {phantomRootFolder}"); }
            PhantomRootFolder = phantomRootFolder;
        }

        public string PhantomRootFolder { get; set; }
        public string PaperSize { get; set; } = DefaultPaperSize;
    }
}