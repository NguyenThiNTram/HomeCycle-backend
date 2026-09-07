using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Configs;
using HomeCycle.Application.Interfaces.Services.Configs;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Configs
{
    public class FileValidationService : IFileValidationService
    {
        private readonly IPlatformPolicyProvider _policyProvider;

        public FileValidationService(IPlatformPolicyProvider policyProvider)
        {
            _policyProvider = policyProvider;
        }

        public async Task<Result> ValidateAsync(IFormFile? file, FileUploadContext context, CancellationToken cancellationToken = default)
        {
            var config = await _policyProvider.GetFileUploadConfigAsync(cancellationToken);
            var rule = GetRequiredRule(config, context);

            return ValidateAgainstRule(file, rule);
        }

        public async Task<Result> ValidateManyAsync(IEnumerable<IFormFile>? files, FileUploadContext context, CancellationToken cancellationToken = default)
        {
            var items = files?.ToList() ?? [];

            // Danh sách file có bắt buộc hay không do request validator hoặc service nghiệp vụ quyết định.
            if (items.Count == 0)
                return Result.Success();

            var config = await _policyProvider.GetFileUploadConfigAsync(cancellationToken);

            var rule = GetRequiredRule(config, context);

            foreach (var file in items)
            {
                var validation = ValidateAgainstRule(file, rule);

                if (!validation.IsSuccess)
                    return validation;
            }

            return Result.Success();
        }

        private static FileUploadRuleConfigDto GetRequiredRule(FileUploadPolicyConfigDto config, FileUploadContext context)
        {
            var rule = config.Rules.SingleOrDefault(x => x.Context == context);

            if (rule == null)
            {
                // Đây là lỗi cấu hình hệ thống, không phải lỗi input client.
                throw new InvalidOperationException($"Chưa cấu hình upload rule cho context {context}.");
            }

            return rule;
        }

        private static Result ValidateAgainstRule(IFormFile? file, FileUploadRuleConfigDto rule)
        {
            if (file == null)
            {
                return Result.Fail(ValidationErrors.InvalidRequest("Tệp tin không được để trống."));
            }

            if (file.Length <= 0)
            {
                return Result.Fail(ValidationErrors.InvalidRequest($"Tệp {file.FileName} không có dữ liệu."));
            }

            if (file.Length > rule.MaxFileSizeBytes)
            {
                var maxSizeMb = rule.MaxFileSizeBytes / 1024d / 1024d;

                return Result.Fail(
                    ValidationErrors.InvalidRequest(
                        $"Tệp {file.FileName} vượt quá giới hạn " +
                        $"{maxSizeMb:0.##} MB."));
            }

            if (string.IsNullOrWhiteSpace(file.FileName))
            {
                return Result.Fail(ValidationErrors.InvalidRequest("Tên tệp không hợp lệ."));
            }

            var extension = FileTypeCatalog.NormalizeExtension(
                Path.GetExtension(file.FileName));

            var allowed = rule.AllowedExtensions.Any(
                configuredExtension =>
                    string.Equals(
                        FileTypeCatalog.NormalizeExtension(
                            configuredExtension),
                        extension,
                        StringComparison.OrdinalIgnoreCase));

            if (!allowed)
            {
                return Result.Fail(
                    ValidationErrors.InvalidRequest(
                        $"Định dạng {extension} không được phép cho " +
                        $"{rule.Context}. Các định dạng hỗ trợ: " +
                        $"{string.Join(", ", rule.AllowedExtensions)}."));
            }

            if (!FileTypeCatalog.TryGetMimeType(extension, out var expectedMimeType))
            {
                return Result.Fail(
                    ValidationErrors.InvalidRequest(
                        $"Không xác định được MIME type của {extension}."));
            }

            var suppliedMimeType = file.ContentType?
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?
                .Trim();

            if (string.IsNullOrWhiteSpace(suppliedMimeType) || !string.Equals(suppliedMimeType, expectedMimeType, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Fail(
                    ValidationErrors.InvalidRequest(
                        $"Content-Type của tệp {file.FileName} " +
                        $"không phù hợp với định dạng {extension}."));
            }

            return Result.Success();
        }
    }
}
