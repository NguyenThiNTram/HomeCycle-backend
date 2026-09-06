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

        public PlatformPolicyService(
            IPlatformPolicyRepository policyRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<UpdateDisputePolicyRequest> disputeValidator,
            IValidator<UpdateAppointmentPolicyRequest> appointmentValidator)
        {
            _policyRepository = policyRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _disputeValidator = disputeValidator;
            _appointmentValidator = appointmentValidator;
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
                    PolicyType = (int)PlatformPolicyType.Dispute,
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
                    PolicyType = (int)PlatformPolicyType.Appointment,
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
            var policy = await _policyRepository.GetByVersionAsync(
                policyType,
                version,
                cancellationToken);

            if (policy == null)
            {
                return Result<PlatformPolicyVersionDetailDto>
                    .Fail(PlatformPolicyErrors.VersionNotFound(policyType, version));
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

                if (!IsValidPolicyContent(policyType, source.Content))
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
                    PolicyType = (int)policyType,
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

        private static bool IsValidPolicyContent(
            PlatformPolicyType policyType,
            string content)
        {
            return policyType switch
            {
                PlatformPolicyType.Dispute =>
                    TryDeserialize(content, out DisputePolicyConfigDto? disputeConfig)
                    && IsValidDisputeConfig(disputeConfig!),

                PlatformPolicyType.Appointment =>
                    TryDeserialize(content, out AppointmentPolicyConfigDto? appointmentConfig)
                    && IsValidAppointmentConfig(appointmentConfig!),

                _ => false
            };
        }

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
    }
}
