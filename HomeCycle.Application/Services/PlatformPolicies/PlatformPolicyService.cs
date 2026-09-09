using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Configs;
using HomeCycle.Application.DTOs.Requests.PlatformPolicies;
using HomeCycle.Application.DTOs.Responses.PlatformPolicies;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.PlatformPolicies;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.PlatformPolicies
{
    public class PlatformPolicyService : IPlatformPolicyService, IPlatformPolicyProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IPlatformPolicyRepository _policyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateDisputePolicyRequest> _disputeValidator;
        private readonly IValidator<UpdateAppointmentPolicyRequest> _appointmentValidator;
        private readonly IValidator<UpdateFileUploadPolicyRequest> _fileUploadRequestValidator;
        private readonly IValidator<FileUploadPolicyConfigDto> _fileUploadPolicyValidator;
        private readonly IValidator<UpdatePaymentPolicyRequest> _paymentValidator;
        private readonly IValidator<UpdateOrderPolicyRequest> _orderValidator;

        public PlatformPolicyService(
            IPlatformPolicyRepository policyRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<UpdateDisputePolicyRequest> disputeValidator,
            IValidator<UpdateAppointmentPolicyRequest> appointmentValidator,
            IValidator<UpdateFileUploadPolicyRequest> fileUploadRequestValidator,
            IValidator<FileUploadPolicyConfigDto> fileUploadPolicyValidator,
            IValidator<UpdatePaymentPolicyRequest> paymentValidator,
            IValidator<UpdateOrderPolicyRequest> orderValidator)
        {
            _policyRepository = policyRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _disputeValidator = disputeValidator;
            _appointmentValidator = appointmentValidator;
            _fileUploadRequestValidator = fileUploadRequestValidator;
            _fileUploadPolicyValidator = fileUploadPolicyValidator;
            _paymentValidator = paymentValidator;
            _orderValidator = orderValidator;
        }

        public async Task<Result<IReadOnlyList<PlatformPolicySummaryResponseDto>>> GetAllActiveAsync(
            CancellationToken cancellationToken = default)
        {
            var policies = await _policyRepository.GetAllActiveAsync(cancellationToken);

            var response = _mapper.Map<List<PlatformPolicySummaryResponseDto>>(policies);

            return Result<IReadOnlyList<PlatformPolicySummaryResponseDto>>.Success(response);
        }

        public async Task<Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>> GetDisputePolicyAsync(
            CancellationToken cancellationToken = default)
        {
            var policy = await _policyRepository.GetActiveAsync(
                PlatformPolicyType.Dispute,
                cancellationToken);

            if (policy == null)
                return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>
                    .Fail(PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.Dispute));

            if (!TryDeserialize(policy.Content, out DisputePolicyConfigDto? config)
                || !IsValidDisputeConfig(config!))
            {
                return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>
                    .Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.Dispute));
            }

            var response = _mapper.Map<PlatformPolicyResponseDto<DisputePolicyConfigDto>>(policy);
            response.Config = config!;

            return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>.Success(response);
        }

        public async Task<Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>> GetAppointmentPolicyAsync(
            CancellationToken cancellationToken = default)
        {
            var policy = await _policyRepository.GetActiveAsync(
                PlatformPolicyType.Appointment,
                cancellationToken);

            if (policy == null)
                return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>
                    .Fail(PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.Appointment));

            if (!TryDeserialize(policy.Content, out AppointmentPolicyConfigDto? config)
                || !IsValidAppointmentConfig(config!))
            {
                return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>
                    .Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.Appointment));
            }

            var response = _mapper.Map<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>(policy);
            response.Config = config!;

            return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>.Success(response);
        }

        public async Task<Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>> UpdateDisputePolicyAsync(
            Guid adminId,
            UpdateDisputePolicyRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _disputeValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                var message = string.Join("\n", validation.Errors.Select(x => x.ErrorMessage));

                return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>
                    .Fail(ValidationErrors.InvalidRequest(message));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var current = await _policyRepository.GetActiveForUpdateAsync(
                    PlatformPolicyType.Dispute,
                    cancellationToken);

                if (current == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.Dispute));
                }

                if (!TryDeserialize(current.Content, out DisputePolicyConfigDto? currentConfig)
                    || !IsValidDisputeConfig(currentConfig!))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.Dispute));
                }

                var config = _mapper.Map<DisputePolicyConfigDto>(currentConfig);
                _mapper.Map(request, config);

                if (!IsValidDisputeConfig(config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.InvalidDisputePolicy);
                }

                if (SameDisputeConfig(currentConfig!, config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    var currentResponse =
                        _mapper.Map<PlatformPolicyResponseDto<DisputePolicyConfigDto>>(current);

                    currentResponse.Config = currentConfig;

                    return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>
                        .Success(currentResponse);
                }

                var now = DateTime.UtcNow;

                var nextVersion = await _policyRepository.GetNextVersionAsync(
                    PlatformPolicyType.Dispute,
                    cancellationToken);

                current.IsActive = false;
                current.UpdatedAt = now;

                await _policyRepository.UpdateAsync(current, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var newPolicy = new platform_policy
                {
                    PolicyId = Guid.NewGuid(),
                    PolicyType = PlatformPolicyType.Dispute,
                    Title = string.IsNullOrWhiteSpace(current.Title)
                        ? "Dispute Policy"
                        : current.Title,
                    Content = JsonSerializer.Serialize(config, JsonOptions),
                    Version = nextVersion,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = adminId,
                    UpdatedAt = now
                };

                await _policyRepository.AddAsync(newPolicy, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response =
                    _mapper.Map<PlatformPolicyResponseDto<DisputePolicyConfigDto>>(newPolicy);

                response.Config = config;

                return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>
                    .Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>> UpdateAppointmentPolicyAsync(
            Guid adminId,
            UpdateAppointmentPolicyRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _appointmentValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                var message = string.Join("\n", validation.Errors.Select(x => x.ErrorMessage));

                return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>
                    .Fail(ValidationErrors.InvalidRequest(message));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var current = await _policyRepository.GetActiveForUpdateAsync(
                    PlatformPolicyType.Appointment,
                    cancellationToken);

                if (current == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.Appointment));
                }

                if (!TryDeserialize(current.Content, out AppointmentPolicyConfigDto? currentConfig)
                    || !IsValidAppointmentConfig(currentConfig!))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.Appointment));
                }

                var config = _mapper.Map<AppointmentPolicyConfigDto>(currentConfig);
                _mapper.Map(request, config);

                if (!IsValidAppointmentConfig(config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.InvalidAppointmentPolicy);
                }

                if (SameAppointmentConfig(currentConfig!, config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    var currentResponse =
                        _mapper.Map<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>(current);

                    currentResponse.Config = currentConfig;

                    return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>
                        .Success(currentResponse);
                }

                var now = DateTime.UtcNow;

                var nextVersion = await _policyRepository.GetNextVersionAsync(
                    PlatformPolicyType.Appointment,
                    cancellationToken);

                current.IsActive = false;
                current.UpdatedAt = now;

                await _policyRepository.UpdateAsync(current, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var newPolicy = new platform_policy
                {
                    PolicyId = Guid.NewGuid(),
                    PolicyType = PlatformPolicyType.Appointment,
                    Title = string.IsNullOrWhiteSpace(current.Title)
                        ? "Appointment Policy"
                        : current.Title,
                    Content = JsonSerializer.Serialize(config, JsonOptions),
                    Version = nextVersion,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = adminId,
                    UpdatedAt = now
                };

                await _policyRepository.AddAsync(newPolicy, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response =
                    _mapper.Map<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>(newPolicy);

                response.Config = config;

                return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>
                    .Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Result<IReadOnlyList<PlatformPolicyVersionListItemDto>>> GetVersionsAsync(
            PlatformPolicyType policyType,
            CancellationToken cancellationToken = default)
        {
            if (!IsSupportedPolicyType(policyType))
            {
                return Result<IReadOnlyList<PlatformPolicyVersionListItemDto>>.Fail(
                    PlatformPolicyErrors.UnsupportedType(
                        policyType.ToString()));
            }

            var policies = await _policyRepository.GetVersionsAsync(
                policyType,
                cancellationToken);

            var response =
                _mapper.Map<List<PlatformPolicyVersionListItemDto>>(policies);

            return Result<IReadOnlyList<PlatformPolicyVersionListItemDto>>
                .Success(response);
        }

        public async Task<Result<PlatformPolicyVersionDetailDto>> GetVersionAsync(
            PlatformPolicyType policyType,
            int version,
            CancellationToken cancellationToken = default)
        {
            if (!IsSupportedPolicyType(policyType))
            {
                return Result<PlatformPolicyVersionDetailDto>.Fail(
                    PlatformPolicyErrors.UnsupportedType(
                        policyType.ToString()));
            }

            var policy = await _policyRepository.GetByVersionAsync(
                policyType,
                version,
                cancellationToken);

            if (policy == null)
            {
                return Result<PlatformPolicyVersionDetailDto>
                    .Fail(PlatformPolicyErrors.VersionNotFound(policyType, version));
            }

            if (!IsValidJsonObject(policy.Content))
            {
                return Result<PlatformPolicyVersionDetailDto>
                    .Fail(PlatformPolicyErrors.InvalidContent(policyType));
            }

            var response =
                _mapper.Map<PlatformPolicyVersionDetailDto>(policy);

            return Result<PlatformPolicyVersionDetailDto>.Success(response);
        }

        public async Task<Result<PlatformPolicyVersionDetailDto>> RestoreVersionAsync(
            Guid adminId,
            PlatformPolicyType policyType,
            int version,
            CancellationToken cancellationToken = default)
        {
            //// Block FileUpload restore until full validation is implemented
            //if (policyType == PlatformPolicyType.FileUpload)
            //{
            //    return Result<PlatformPolicyVersionDetailDto>
            //        .Fail(PlatformPolicyErrors.UnsupportedType(policyType.ToString()));
            //}

            if (!IsSupportedPolicyType(policyType))
            {
                return Result<PlatformPolicyVersionDetailDto>.Fail(
                    PlatformPolicyErrors.UnsupportedType(
                        policyType.ToString()));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var current = await _policyRepository.GetActiveForUpdateAsync(
                    policyType,
                    cancellationToken);

                if (current == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyVersionDetailDto>
                        .Fail(PlatformPolicyErrors.ActiveNotFound(policyType));
                }

                var source = await _policyRepository.GetByVersionAsync(
                    policyType,
                    version,
                    cancellationToken);

                if (source == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyVersionDetailDto>
                        .Fail(PlatformPolicyErrors.VersionNotFound(policyType, version));
                }

                if (source.IsActive)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyVersionDetailDto>
                        .Fail(PlatformPolicyErrors.VersionAlreadyActive);
                }

                //if (!IsValidPolicyContent(policyType, source.Content))
                if (!await IsValidPolicyContentAsync(policyType, source.Content, cancellationToken))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyVersionDetailDto>
                        .Fail(PlatformPolicyErrors.InvalidContent(policyType));
                }

                var now = DateTime.UtcNow;

                var nextVersion = await _policyRepository.GetNextVersionAsync(
                    policyType,
                    cancellationToken);

                current.IsActive = false;
                current.UpdatedAt = now;

                await _policyRepository.UpdateAsync(current, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var restoredPolicy = new platform_policy
                {
                    PolicyId = Guid.NewGuid(),
                    PolicyType = policyType,
                    Title = source.Title,
                    Content = source.Content,
                    Version = nextVersion,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = adminId,
                    UpdatedAt = now
                };

                await _policyRepository.AddAsync(restoredPolicy, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response =
                    _mapper.Map<PlatformPolicyVersionDetailDto>(restoredPolicy);

                return Result<PlatformPolicyVersionDetailDto>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<DisputePolicyConfigDto> GetDisputeConfigAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await GetDisputePolicyAsync(cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                throw new InvalidOperationException(
                    result.Error?.Message
                    ?? "Dispute policy configuration is unavailable.");
            }

            return result.Data.Config;
        }

        public async Task<AppointmentPolicyConfigDto> GetAppointmentConfigAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await GetAppointmentPolicyAsync(cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                throw new InvalidOperationException(
                    result.Error?.Message
                    ?? "Appointment policy configuration is unavailable.");
            }

            return result.Data.Config;
        }

        public async Task< Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>> GetFileUploadPolicyAsync(
        CancellationToken cancellationToken = default)
        {
            var policy = await _policyRepository.GetActiveAsync(PlatformPolicyType.FileUpload, cancellationToken);

            if (policy == null)
            {
                return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                    .Fail(PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.FileUpload));
            }

            if (!TryDeserialize(policy.Content, out FileUploadPolicyConfigDto? config))
            {
                return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                    .Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.FileUpload));
            }

            var validation = await _fileUploadPolicyValidator.ValidateAsync(config!, cancellationToken);

            if (!validation.IsValid)
            {
                return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                    .Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.FileUpload));
            }

            var response = _mapper.Map<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>(policy);

            response.Config = config!;

            return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                .Success(response);
        }

        public async Task<Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>> UpdateFileUploadPolicyAsync(Guid adminId, UpdateFileUploadPolicyRequest request, CancellationToken cancellationToken = default)
        {
            var requestValidation = await _fileUploadRequestValidator.ValidateAsync(request, cancellationToken);

            if (!requestValidation.IsValid)
            {
                var message = string.Join("\n", requestValidation.Errors.Select(x => x.ErrorMessage));

                return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                    .Fail(ValidationErrors.InvalidRequest(message));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var current = await _policyRepository.GetActiveForUpdateAsync(PlatformPolicyType.FileUpload, cancellationToken);

                if (current == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.FileUpload));
                }

                if (!TryDeserialize(current.Content, out FileUploadPolicyConfigDto? currentConfig))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.FileUpload));
                }

                var currentValidation =
                    await _fileUploadPolicyValidator.ValidateAsync(currentConfig!, cancellationToken);

                if (!currentValidation.IsValid)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.FileUpload));
                }

                var updatedConfig = CloneFileUploadConfig(currentConfig!);

                var rule = updatedConfig.Rules.SingleOrDefault(
                    x => x.Context == request.Context);

                if (rule == null)
                {
                    // Muốn tạo context mới phải cung cấp đủ cấu hình.
                    if (!request.MaxFileSizeBytes.HasValue || request.AllowedExtensions == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                        return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                            .Fail(
                                ValidationErrors.InvalidRequest(
                                    "Context chưa có rule. " +
                                    "Phải cung cấp MaxFileSizeBytes và " +
                                    "AllowedExtensions để tạo rule mới."));
                    }

                    rule = new FileUploadRuleConfigDto
                    {
                        Context = request.Context,
                        MaxFileSizeBytes = request.MaxFileSizeBytes.Value,
                        AllowedExtensions = FileTypeCatalog.NormalizeExtensions(request.AllowedExtensions)
                    };

                    updatedConfig.Rules.Add(rule);
                }
                else
                {
                    if (request.MaxFileSizeBytes.HasValue)
                    {
                        rule.MaxFileSizeBytes = request.MaxFileSizeBytes.Value;
                    }

                    if (request.AllowedExtensions != null)
                    {
                        rule.AllowedExtensions = FileTypeCatalog.NormalizeExtensions(request.AllowedExtensions);
                    }
                }

                NormalizeFileUploadConfig(updatedConfig);

                var updatedValidation = await _fileUploadPolicyValidator.ValidateAsync(updatedConfig, cancellationToken);

                if (!updatedValidation.IsValid)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    var message = string.Join("\n", updatedValidation.Errors.Select(x => x.ErrorMessage));

                    return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                        .Fail(ValidationErrors.InvalidRequest(message));
                }

                if (SameFileUploadConfig(currentConfig!, updatedConfig))
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        cancellationToken);

                    var currentResponse = _mapper.Map<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>(current);

                    currentResponse.Config = currentConfig!;

                    return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                        .Success(currentResponse);
                }

                var now = DateTime.UtcNow;

                var nextVersion = await _policyRepository.GetNextVersionAsync(PlatformPolicyType.FileUpload, cancellationToken);

                current.IsActive = false;
                current.UpdatedAt = now;

                await _policyRepository.UpdateAsync(current, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var newPolicy = new platform_policy
                {
                    PolicyId = Guid.NewGuid(),
                    PolicyType = PlatformPolicyType.FileUpload,
                    Title = string.IsNullOrWhiteSpace(current.Title)
                        ? "File Upload Policy"
                        : current.Title,
                    Content = JsonSerializer.Serialize(
                        updatedConfig,
                        JsonOptions),
                    Version = nextVersion,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = adminId,
                    UpdatedAt = now
                };

                await _policyRepository.AddAsync(newPolicy, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>(newPolicy);

                response.Config = updatedConfig;

                return Result<PlatformPolicyResponseDto<FileUploadPolicyConfigDto>>
                    .Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<FileUploadPolicyConfigDto> GetFileUploadConfigAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetFileUploadPolicyAsync(cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                throw new InvalidOperationException(
                    result.Error?.Message ??
                    "File upload policy configuration is unavailable.");
            }

            return result.Data.Config;
        }

        public async Task<Result<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>> GetPaymentPolicyAsync(CancellationToken cancellationToken = default)
        {
            var policy = await _policyRepository.GetActiveAsync(
                PlatformPolicyType.Payment,
                cancellationToken);

            if (policy == null)
                return Result<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>
                    .Fail(PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.Payment));

            if (!TryDeserialize(policy.Content, out PaymentPolicyConfigDto? config)
                || !IsValidPaymentConfig(config!))
            {
                return Result<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>
                    .Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.Payment));
            }

            var response =
                _mapper.Map<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>(policy);

            response.Config = config!;

            return Result<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>
                .Success(response);
        }


        public async Task<Result<PlatformPolicyResponseDto<OrderPolicyConfigDto>>> GetOrderPolicyAsync(CancellationToken cancellationToken = default)
        {
            var policy = await _policyRepository.GetActiveAsync(
                PlatformPolicyType.Order,
                cancellationToken);

            if (policy == null)
                return Result<PlatformPolicyResponseDto<OrderPolicyConfigDto>>
                    .Fail(PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.Order));

            if (!TryDeserialize(policy.Content, out OrderPolicyConfigDto? config)
                || !IsValidOrderConfig(config!))
            {
                return Result<PlatformPolicyResponseDto<OrderPolicyConfigDto>>
                    .Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.Order));
            }

            var response =
                _mapper.Map<PlatformPolicyResponseDto<OrderPolicyConfigDto>>(policy);

            response.Config = config!;

            return Result<PlatformPolicyResponseDto<OrderPolicyConfigDto>>
                .Success(response);
        }


        public async Task<Result<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>> UpdatePaymentPolicyAsync(
            Guid adminId,
            UpdatePaymentPolicyRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _paymentValidator.ValidateAsync(
                request,
                cancellationToken);

            if (!validation.IsValid)
            {
                var message = string.Join(
                    "\n",
                    validation.Errors.Select(x => x.ErrorMessage));

                return Result<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>
                    .Fail(ValidationErrors.InvalidRequest(message));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var current = await _policyRepository.GetActiveForUpdateAsync(
                    PlatformPolicyType.Payment,
                    cancellationToken);

                if (current == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.ActiveNotFound(
                            PlatformPolicyType.Payment));
                }

                if (!TryDeserialize(
                        current.Content,
                        out PaymentPolicyConfigDto? currentConfig)
                    || !IsValidPaymentConfig(currentConfig!))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.InvalidContent(
                            PlatformPolicyType.Payment));
                }

                var config = _mapper.Map<PaymentPolicyConfigDto>(currentConfig);
                _mapper.Map(request, config);

                if (!IsValidPaymentConfig(config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.InvalidPaymentPolicy);
                }

                if (SamePaymentConfig(currentConfig!, config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    var currentResponse =
                        _mapper.Map<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>(
                            current);

                    currentResponse.Config = currentConfig;

                    return Result<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>
                        .Success(currentResponse);
                }

                var now = DateTime.UtcNow;

                var nextVersion = await _policyRepository.GetNextVersionAsync(
                    PlatformPolicyType.Payment,
                    cancellationToken);

                current.IsActive = false;
                current.UpdatedAt = now;

                await _policyRepository.UpdateAsync(current, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var newPolicy = new platform_policy
                {
                    PolicyId = Guid.NewGuid(),
                    PolicyType = PlatformPolicyType.Payment,
                    Title = string.IsNullOrWhiteSpace(current.Title)
                        ? "Payment Policy"
                        : current.Title,
                    Content = JsonSerializer.Serialize(config, JsonOptions),
                    Version = nextVersion,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = adminId,
                    UpdatedAt = now
                };

                await _policyRepository.AddAsync(newPolicy, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response =
                    _mapper.Map<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>(
                        newPolicy);

                response.Config = config;

                return Result<PlatformPolicyResponseDto<PaymentPolicyConfigDto>>
                    .Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }


        public async Task<Result<PlatformPolicyResponseDto<OrderPolicyConfigDto>>> UpdateOrderPolicyAsync(
            Guid adminId,
            UpdateOrderPolicyRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _orderValidator.ValidateAsync(
                request,
                cancellationToken);

            if (!validation.IsValid)
            {
                var message = string.Join(
                    "\n",
                    validation.Errors.Select(x => x.ErrorMessage));

                return Result<PlatformPolicyResponseDto<OrderPolicyConfigDto>>
                    .Fail(ValidationErrors.InvalidRequest(message));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var current = await _policyRepository.GetActiveForUpdateAsync(
                    PlatformPolicyType.Order,
                    cancellationToken);

                if (current == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<OrderPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.ActiveNotFound(
                            PlatformPolicyType.Order));
                }

                if (!TryDeserialize(
                        current.Content,
                        out OrderPolicyConfigDto? currentConfig)
                    || !IsValidOrderConfig(currentConfig!))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<OrderPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.InvalidContent(
                            PlatformPolicyType.Order));
                }

                var config = _mapper.Map<OrderPolicyConfigDto>(currentConfig);
                _mapper.Map(request, config);

                if (!IsValidOrderConfig(config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<PlatformPolicyResponseDto<OrderPolicyConfigDto>>
                        .Fail(PlatformPolicyErrors.InvalidOrderPolicy);
                }

                if (SameOrderConfig(currentConfig!, config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    var currentResponse =
                        _mapper.Map<PlatformPolicyResponseDto<OrderPolicyConfigDto>>(
                            current);

                    currentResponse.Config = currentConfig;

                    return Result<PlatformPolicyResponseDto<OrderPolicyConfigDto>>
                        .Success(currentResponse);
                }

                var now = DateTime.UtcNow;

                var nextVersion = await _policyRepository.GetNextVersionAsync(
                    PlatformPolicyType.Order,
                    cancellationToken);

                current.IsActive = false;
                current.UpdatedAt = now;

                await _policyRepository.UpdateAsync(current, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var newPolicy = new platform_policy
                {
                    PolicyId = Guid.NewGuid(),
                    PolicyType = PlatformPolicyType.Order,
                    Title = string.IsNullOrWhiteSpace(current.Title)
                        ? "Order Policy"
                        : current.Title,
                    Content = JsonSerializer.Serialize(config, JsonOptions),
                    Version = nextVersion,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = adminId,
                    UpdatedAt = now
                };

                await _policyRepository.AddAsync(newPolicy, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response =
                    _mapper.Map<PlatformPolicyResponseDto<OrderPolicyConfigDto>>(
                        newPolicy);

                response.Config = config;

                return Result<PlatformPolicyResponseDto<OrderPolicyConfigDto>>
                    .Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<PaymentPolicyConfigDto> GetPaymentConfigAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetPaymentPolicyAsync(cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                throw new InvalidOperationException(
                    result.Error?.Message
                    ?? "Payment policy configuration is unavailable.");
            }

            return result.Data.Config;
        }

        public async Task<OrderPolicyConfigDto> GetOrderConfigAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await GetOrderPolicyAsync(cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                throw new InvalidOperationException(
                    result.Error?.Message
                    ?? "Order policy configuration is unavailable.");
            }

            return result.Data.Config;
        }


        // ================= HELPER ====================

        private static bool TryDeserialize<T>(
            string? content,
            out T? config) where T : class
        {
            config = null;

            if (string.IsNullOrWhiteSpace(content))
                return false;

            try
            {
                config = JsonSerializer.Deserialize<T>(content, JsonOptions);
                return config != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        //private static bool IsValidPolicyContent(
        //    PlatformPolicyType policyType,
        //    string content)
        //{
        //    return policyType switch
        //    {
        //        PlatformPolicyType.Dispute =>
        //            TryDeserialize(content, out DisputePolicyConfigDto? disputeConfig)
        //            && IsValidDisputeConfig(disputeConfig!),

        //        PlatformPolicyType.Appointment =>
        //            TryDeserialize(content, out AppointmentPolicyConfigDto? appointmentConfig)
        //            && IsValidAppointmentConfig(appointmentConfig!),

        //        _ => false
        //    };
        //}

        private static bool IsValidDisputeConfig(
            DisputePolicyConfigDto config)
        {
            return config.NormalDisputeWindowDays is >= 1 and <= 365
                && config.LowReputationDisputeWindowDays is >= 1 and <= 365
                && config.LowReputationDisputeWindowDays >= config.NormalDisputeWindowDays
                && config.LowReputationThreshold is >= 0 and <= 100
                && config.ReturnWindowDays is >= 1 and <= 30
                && config.DisputeLossPenaltyPoints is >= 1 and <= 100;
        }

        private static bool IsValidAppointmentConfig(
            AppointmentPolicyConfigDto config)
        {
            return config.CheckInOpenBeforeMinutes is >= 0 and <= 1440
                && config.LateThresholdMinutes is >= 1 and <= 10080
                && config.RescheduleCutoffHours is >= 1 and <= 720
                && config.CancellationCutoffHours is >= 1 and <= 720
                && config.RescheduleCutoffHours >= config.CancellationCutoffHours;
        }

        private static bool IsValidPaymentConfig(
            PaymentPolicyConfigDto config)
        {
            return config.DepositRatePercent > 0
                && config.DepositRatePercent <= 100
                && config.PaymentExpiryMinutes is >= 1 and <= 1440;
        }

        private static bool IsValidOrderConfig(
            OrderPolicyConfigDto config)
        {
            return config.BuyerReceiveConfirmationTimeoutHours
                is >= 1 and <= 720;
        }

        private static bool SamePaymentConfig(
            PaymentPolicyConfigDto current,
            PaymentPolicyConfigDto updated)
        {
            return current.DepositRatePercent == updated.DepositRatePercent
                && current.PaymentExpiryMinutes == updated.PaymentExpiryMinutes;
        }

        private static bool SameOrderConfig(
            OrderPolicyConfigDto current,
            OrderPolicyConfigDto updated)
        {
            return current.BuyerReceiveConfirmationTimeoutHours
                == updated.BuyerReceiveConfirmationTimeoutHours;
        }

        private static bool SameDisputeConfig(
            DisputePolicyConfigDto current,
            DisputePolicyConfigDto updated)
        {
            return current.NormalDisputeWindowDays == updated.NormalDisputeWindowDays
                && current.LowReputationDisputeWindowDays == updated.LowReputationDisputeWindowDays
                && current.LowReputationThreshold == updated.LowReputationThreshold
                && current.ReturnWindowDays == updated.ReturnWindowDays
                && current.DisputeLossPenaltyPoints == updated.DisputeLossPenaltyPoints;

        }

        private static bool SameAppointmentConfig(
            AppointmentPolicyConfigDto current,
            AppointmentPolicyConfigDto updated)
        {
            return current.CheckInOpenBeforeMinutes == updated.CheckInOpenBeforeMinutes
                && current.LateThresholdMinutes == updated.LateThresholdMinutes
                && current.RescheduleCutoffHours == updated.RescheduleCutoffHours
                && current.CancellationCutoffHours == updated.CancellationCutoffHours;
        }

        private static FileUploadPolicyConfigDto CloneFileUploadConfig(FileUploadPolicyConfigDto source)
        {
            return new FileUploadPolicyConfigDto
            {
                Rules = source.Rules
                    .Select(rule => new FileUploadRuleConfigDto
                    {
                        Context = rule.Context,
                        MaxFileSizeBytes = rule.MaxFileSizeBytes,
                        AllowedExtensions =
                            rule.AllowedExtensions.ToList()
                    })
                    .ToList()
            };
        }

        private static void NormalizeFileUploadConfig(FileUploadPolicyConfigDto config)
        {
            foreach (var rule in config.Rules)
            {
                rule.AllowedExtensions = FileTypeCatalog.NormalizeExtensions(rule.AllowedExtensions);
            }

            config.Rules = config.Rules
                .OrderBy(x => (int)x.Context)
                .ToList();
        }

        private static bool SameFileUploadConfig(FileUploadPolicyConfigDto current, FileUploadPolicyConfigDto updated)
        {
            if (current.Rules.Count != updated.Rules.Count)
                return false;

            foreach (var currentRule in current.Rules)
            {
                var updatedRule = updated.Rules.SingleOrDefault(
                    x => x.Context == currentRule.Context);

                if (updatedRule == null)
                    return false;

                if (currentRule.MaxFileSizeBytes != updatedRule.MaxFileSizeBytes)
                    return false;

                var currentExtensions = FileTypeCatalog.NormalizeExtensions(currentRule.AllowedExtensions);

                var updatedExtensions = FileTypeCatalog.NormalizeExtensions(updatedRule.AllowedExtensions);
                if (!currentExtensions.SequenceEqual(updatedExtensions, StringComparer.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private async Task<bool> IsValidPolicyContentAsync(PlatformPolicyType policyType, string content, CancellationToken cancellationToken)
        {
            switch (policyType)
            {
                case PlatformPolicyType.Payment:
                    return TryDeserialize(
                               content,
                               out PaymentPolicyConfigDto? paymentConfig)
                           && IsValidPaymentConfig(paymentConfig!);

                case PlatformPolicyType.Order:
                    return TryDeserialize(
                               content,
                               out OrderPolicyConfigDto? orderConfig)
                           && IsValidOrderConfig(orderConfig!);

                case PlatformPolicyType.Dispute:
                    return TryDeserialize(content, out DisputePolicyConfigDto? disputeConfig)
                           && IsValidDisputeConfig(disputeConfig!);

                case PlatformPolicyType.Appointment:
                    return TryDeserialize(content, out AppointmentPolicyConfigDto? appointmentConfig)
                           && IsValidAppointmentConfig(appointmentConfig!);

                case PlatformPolicyType.FileUpload:
                    if (!TryDeserialize(content, out FileUploadPolicyConfigDto? fileUploadConfig))
                    {
                        return false;
                    }
                    var validation = await _fileUploadPolicyValidator.ValidateAsync(fileUploadConfig!, cancellationToken);

                    return validation.IsValid;

                default:
                    return false;
            }
        }

        //Chặn PlatformPolicyType không hỗ trợ trong service
        private static bool IsSupportedPolicyType(PlatformPolicyType policyType)
        {
            return policyType is
                PlatformPolicyType.Dispute or
                PlatformPolicyType.Appointment or
                PlatformPolicyType.FileUpload or
                PlatformPolicyType.Payment or
                PlatformPolicyType.Order;
        }

        private static bool IsValidJsonObject(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            try
            {
                using var document = JsonDocument.Parse(content);

                return document.RootElement.ValueKind ==
                       JsonValueKind.Object;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
