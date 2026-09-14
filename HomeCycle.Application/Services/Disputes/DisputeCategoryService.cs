using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Disputes
{
    public class DisputeCategoryService : IDisputeCategoryService
    {
        private readonly IDisputeCategoryRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateDisputeCategoryRequest> _createValidator;
        private readonly IValidator<UpdateDisputeCategoryRequest> _updateValidator;

        public DisputeCategoryService(
            IDisputeCategoryRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateDisputeCategoryRequest> createValidator,
            IValidator<UpdateDisputeCategoryRequest> updateValidator)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<Result<IReadOnlyList<DisputeCategoryResponseDto>>> GetAllAsync(
            bool? isActive, CancellationToken ct = default)
        {
            var categories = await _repository.GetAllAsync(isActive, ct);
            var response = categories.Select(x => _mapper.Map<DisputeCategoryResponseDto>(x)).ToArray();

            return Result<IReadOnlyList<DisputeCategoryResponseDto>>.Success(response);
        }

        public async Task<Result<IReadOnlyList<DisputeCategoryOptionDto>>> GetActiveOptionsAsync(
            DisputeTargetType? targetType, CancellationToken ct = default)
        {
            var categories = targetType.HasValue
                ? await _repository.GetActiveByTargetTypeAsync(targetType.Value, ct)
                : await _repository.GetAllAsync(true, ct);

            var response = categories.Select(x => _mapper.Map<DisputeCategoryOptionDto>(x)).ToArray();
            return Result<IReadOnlyList<DisputeCategoryOptionDto>>.Success(response);
        }

        public async Task<Result<DisputeCategoryResponseDto>> CreateAsync(
            Guid adminId, CreateDisputeCategoryRequest request, CancellationToken ct = default)
        {
            var validation = await _createValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Result<DisputeCategoryResponseDto>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(x => x.ErrorMessage))));

            var code = request.Code.Trim().ToUpperInvariant();

            if (await _repository.ExistsCodeAsync(code, ct))
                return Result<DisputeCategoryResponseDto>.Fail(DisputeCategoryErrors.CodeAlreadyExists);

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var now = DateTime.UtcNow;
                var category = _mapper.Map<dispute_category>(request);

                category.Code = code;
                category.Name = request.Name.Trim();
                category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
                category.IsActive = true;
                category.CreatedAt = now;
                category.UpdatedAt = now;
                category.CreatedBy = adminId;
                category.TargetTypes = request.TargetTypes.Distinct().ToList();

                await _repository.AddAsync(category, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                var created = await _repository.GetByCodeAsync(code, ct);
                if (created == null)
                    throw new InvalidOperationException("Không thể đọc lại Dispute Category vừa tạo.");

                await _unitOfWork.CommitTransactionAsync(ct);
                return Result<DisputeCategoryResponseDto>.Success(_mapper.Map<DisputeCategoryResponseDto>(created));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }

        public async Task<Result<DisputeCategoryResponseDto>> UpdateAsync(
            int categoryId, UpdateDisputeCategoryRequest request, CancellationToken ct = default)
        {
            var validation = await _updateValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Result<DisputeCategoryResponseDto>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(x => x.ErrorMessage))));

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var category = await _repository.GetByIdForUpdateAsync(categoryId, ct);

                if (category == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<DisputeCategoryResponseDto>.Fail(DisputeCategoryErrors.NotFound);
                }

                _mapper.Map(request, category);

                if (request.Name != null)
                    category.Name = request.Name.Trim();

                if (request.Description != null)
                    category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

                if (request.TargetTypes != null)
                    category.TargetTypes = request.TargetTypes.Distinct().ToList();

                category.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(category, ct);

                if (request.TargetTypes != null)
                    await _repository.ReplaceTargetsAsync(categoryId, category.TargetTypes, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                var updated = await _repository.GetByIdAsync(categoryId, ct);
                return Result<DisputeCategoryResponseDto>.Success(_mapper.Map<DisputeCategoryResponseDto>(updated!));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }

        public async Task<Result<DisputeCategoryResponseDto>> UpdateStatusAsync(
            int categoryId, bool isActive, CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var category = await _repository.GetByIdForUpdateAsync(categoryId, ct);

                if (category == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<DisputeCategoryResponseDto>.Fail(DisputeCategoryErrors.NotFound);
                }

                category.IsActive = isActive;
                category.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(category, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                var updated = await _repository.GetByIdAsync(categoryId, ct);
                return Result<DisputeCategoryResponseDto>.Success(_mapper.Map<DisputeCategoryResponseDto>(updated!));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }
    }
}
