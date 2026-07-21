using AutoMapper;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Media;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Media;
using HomeCycle.Application.Interfaces.Services.Externals;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Posts
{
    public class MediaService : IMediaService
    {
        private readonly IMediaRepository _mediaRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MediaService> _logger;

        public MediaService(
            IMediaRepository mediaRepository,
            IFileStorageService fileStorageService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MediaService> logger)
        {
            _mediaRepository = mediaRepository;
            _fileStorageService = fileStorageService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<Result<IReadOnlyList<MediaResponse>>> UploadAndSaveMediaAsync(
            Guid targetId,
            string targetType,
            string folderName,
             IEnumerable<IFormFile> files,
            CancellationToken cancellationToken = default)
        {
            var items = files?.Where(x => x != null && x.Length > 0).ToList();
            if (items == null || items.Count == 0)
            {
                return Result<IReadOnlyList<MediaResponse>>.Success(new List<MediaResponse>());
            }

            var entities = new List<media>();
            var now = DateTime.UtcNow;

            try
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var file = items[i];

                    // 1. Tải file lên Firebase Storage theo folderName được chỉ định
                    await using var stream = file.OpenReadStream();
                    var firebaseUrl = await _fileStorageService.UploadFileAsync(
                        stream,
                        file.FileName,
                        folderName,
                        overwrite: false);

                    entities.Add(new media
                    {
                        MediaId = Guid.NewGuid(),
                        TargetId = targetId,
                        TargetType = targetType,
                        FileName = file.FileName,
                        FileSize = file.Length,
                        DisplayOrder = i + 1, // Tự động đánh số thứ tự hiển thị
                        Url = firebaseUrl,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                await _mediaRepository.AddRangeAsync(entities, cancellationToken);

                var response = _mapper.Map<List<MediaResponse>>(entities).AsReadOnly();
                return Result<IReadOnlyList<MediaResponse>>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload media cho TargetId {TargetId} ({TargetType})", targetId, targetType);
                return Result<IReadOnlyList<MediaResponse>>.Fail(
                    ValidationErrors.InvalidRequest("Tải hình ảnh/tệp tin lên thất bại."));
            }
        }

        public async Task<Result<IReadOnlyList<MediaResponse>>> ReplaceMediaAsync(
            Guid targetId,
            string targetType,
            string folderName,
            IEnumerable<IFormFile> files,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Xóa các bản ghi Media cũ trong DB
                await _mediaRepository.RemoveByTargetAsync(targetId, targetType, cancellationToken);

                // 2. Upload và chèn danh sách Media mới
                return await UploadAndSaveMediaAsync(targetId, targetType, folderName, files, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thay thế (Replace) media cho TargetId {TargetId} ({TargetType})", targetId, targetType);
                return Result<IReadOnlyList<MediaResponse>>.Fail(
                    ValidationErrors.InvalidRequest("Cập nhật hình ảnh/tệp tin thất bại."));
            }
        }

        public async Task<Result<IReadOnlyList<MediaResponse>>> GetByTargetAsync(
            Guid targetId,
            string targetType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _mediaRepository.GetByTargetAsync(targetId, targetType, cancellationToken);
                var response = _mapper.Map<List<MediaResponse>>(entities).AsReadOnly();

                return Result<IReadOnlyList<MediaResponse>>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy media cho TargetId {TargetId} ({TargetType})", targetId, targetType);
                return Result<IReadOnlyList<MediaResponse>>.Fail(
                    ValidationErrors.InvalidRequest("Không thể tải danh sách hình ảnh/tệp tin."));
            }
        }

        public async Task<Result<bool>> DeleteByTargetAsync(
            Guid targetId,
            string targetType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _mediaRepository.RemoveByTargetAsync(targetId, targetType, cancellationToken);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa media cho TargetId {TargetId} ({TargetType})", targetId, targetType);
                return Result<bool>.Fail(
                    ValidationErrors.InvalidRequest("Xóa hình ảnh/tệp tin thất bại."));
            }
        }

    }
}
