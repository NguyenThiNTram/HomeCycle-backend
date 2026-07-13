using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Externals
{
    public interface IFileStorageService
    {
        // Hàm nhận vào một Stream file và tên file, trả về URL
        Task<string> UploadFileAsync(Stream fileStream, string fileName);
        string GetFileUrl(string storedFileName);
    }
}
