using System;
using System.IO;
using System.Text;
using CLASSIC.Core.Logging;

namespace CLASSIC.Core.IO
{
    public static class FileUtils
    {
        public static string DetectEncoding(string filePath)
        {
            // Read file as bytes
            var bytes = File.ReadAllBytes(filePath);

            // Check for BOM markers
            if (bytes is [0xEF, 0xBB, 0xBF, ..])
                return "utf-8";
            switch (bytes.Length)
            {
                case >= 2 when bytes[0] == 0xFE && bytes[1] == 0xFF:
                    return "utf-16BE";
                case >= 2 when bytes[0] == 0xFF && bytes[1] == 0xFE:
                    return "utf-16LE";
                default:
                    // Default to UTF-8
                    try
                    {
                        using var reader = new StreamReader(filePath, Encoding.UTF8, true);
                        reader.ReadToEnd();
                        return "utf-8";
                    }
                    catch
                    {
                        // Fallback to ASCII if UTF-8 fails
                        return "ascii";
                    }

/*
                    break;
*/
            }
        }

        public static void RemoveReadOnly(string filePath)
        {
            try
            {
                // FileInfo.IsReadOnly works on both Windows and Unix
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                    Logger.Debug($"'{filePath}' is no longer Read-Only.");
                }
                else
                {
                    Logger.Debug($"'{filePath}' is not set to Read-Only.");
                }
            }
            catch (FileNotFoundException)
            {
                Logger.Error($"ERROR (remove_readonly) : '{filePath}' not found.");
            }
            catch (Exception ex)
            {
                Logger.Error($"ERROR (remove_readonly) : {ex.Message}");
            }
        }
    }
}