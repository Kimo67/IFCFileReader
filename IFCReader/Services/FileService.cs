using System.IO;

namespace IFCFileReader.Services
{
    public static class FileService
    {
        public static bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}
