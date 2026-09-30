using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Externals
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string originalFileName, string folderName, bool overwrite = false);
        Task<byte[]?> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);
        string GetFileUrl(string storedFileName, string folderName);
        Task DeleteFileAsync(string fileUrlOrPath);
    }
}
