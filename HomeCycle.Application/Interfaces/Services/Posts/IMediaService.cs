using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Media;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Posts
{
    public interface IMediaService
    {

        //Upload và lưu danh sách Media chung
        Task<Result<IReadOnlyList<MediaResponse>>> UploadAndSaveMediaAsync(
            Guid targetId,
            string targetType,
            string folderName,
            IEnumerable<IFormFile> files,
            CancellationToken cancellationToken = default);

        // Xóa media cũ và thay thế bằng danh sách media mới
        Task<Result<IReadOnlyList<MediaResponse>>> ReplaceMediaAsync(
            Guid targetId,
            string targetType,
            string folderName,
             IEnumerable<IFormFile> files,
            CancellationToken cancellationToken = default);

        // Lấy danh sách Media theo TargetId và TargetType
        Task<Result<IReadOnlyList<MediaResponse>>> GetByTargetAsync(
            Guid targetId,
            string targetType,
            CancellationToken cancellationToken = default);

        // Xóa toàn bộ Media của một Target
        Task<Result<bool>> DeleteByTargetAsync(
            Guid targetId,
            string targetType,
            CancellationToken cancellationToken = default);
    }
}
