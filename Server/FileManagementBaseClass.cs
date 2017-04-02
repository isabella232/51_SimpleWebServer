using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Server
{
    sealed class FileManagement
    {
        public async Task<string> GetFile(string filePath)
        {
            var txt = string.Empty;
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            // acquire file
            var file = await folder.GetFileAsync(filePath.Replace(@"/", "\\"));
            var readFile = await Windows.Storage.FileIO.ReadTextAsync(file);
            return readFile;
        }

        public async Task<byte[]> GetBinaryFile(string filePath)
        {
            var txt = string.Empty;
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            // acquire file
            var file = await folder.GetFileAsync(filePath.Replace(@"/", "\\"));
            IBuffer buffer = await FileIO.ReadBufferAsync(file);
            byte[] bytes = buffer.ToArray();
            return bytes;
        }
    }
}
