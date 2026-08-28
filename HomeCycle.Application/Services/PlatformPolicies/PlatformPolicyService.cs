using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
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
        private readonly IValidator<UpdateDisputePolicyRequest> _disputeValidator;
        private readonly IValidator<UpdateAppointmentPolicyRequest> _appointmentValidator;

        public PlatformPolicyService(
            IPlatformPolicyRepository policyRepository,
            IUnitOfWork unitOfWork,
            IValidator<UpdateDisputePolicyRequest> disputeValidator,
            IValidator<UpdateAppointmentPolicyRequest> appointmentValidator)
        {
            _policyRepository = policyRepository;
            _unitOfWork = unitOfWork;
            _disputeValidator = disputeValidator;
            _appointmentValidator = appointmentValidator;
        }

        public async Task<Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>> GetDisputePolicyAsync(CancellationToken cancellationToken = default)
        {
            var policy = await _policyRepository.GetActiveAsync(PlatformPolicyType.Dispute, cancellationToken);

            if (policy == null)
                return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>.Fail(PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.Dispute));

            if (!TryDeserialize(policy.Content, out DisputePolicyConfigDto? config) || !IsValidDisputeConfig(config!))
                return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>.Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.Dispute));

            return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>.Success(
                ToResponse(policy, PlatformPolicyType.Dispute, config!));
        }

        public async Task<Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>> GetAppointmentPolicyAsync(CancellationToken cancellationToken = default)
        {
            var policy = await _policyRepository.GetActiveAsync(PlatformPolicyType.Appointment, cancellationToken);

            if (policy == null)
                return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>.Fail(PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.Appointment));

            if (!TryDeserialize(policy.Content, out AppointmentPolicyConfigDto? config) || !IsValidAppointmentConfig(config!))
                return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>.Fail(PlatformPolicyErrors.InvalidContent(PlatformPolicyType.Appointment));

            return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>.Success(
                ToResponse(policy, PlatformPolicyType.Appointment, config!));
        }

        public async Task<Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>> UpdateDisputePolicyAsync(
            Guid adminId,
            UpdateDisputePolicyRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _disputeValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
                return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(x => x.ErrorMessage))));

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var current = await _policyRepository.GetActiveForUpdateAsync(PlatformPolicyType.Dispute, cancellationToken);

                if (current == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>.Fail(
                        PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.Dispute));
                }

                if (!TryDeserialize(current.Content, out DisputePolicyConfigDto? currentConfig) || !IsValidDisputeConfig(currentConfig!))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>.Fail(
                        PlatformPolicyErrors.InvalidContent(PlatformPolicyType.Dispute));
                }

                var config = new DisputePolicyConfigDto
                {
                    NormalDisputeWindowDays = request.NormalDisputeWindowDays ?? currentConfig!.NormalDisputeWindowDays,
                    LowReputationDisputeWindowDays = request.LowReputationDisputeWindowDays ?? currentConfig.LowReputationDisputeWindowDays,
                    LowReputationThreshold = request.LowReputationThreshold ?? currentConfig.LowReputationThreshold
                };

                if (!IsValidDisputeConfig(config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>.Fail(
                        PlatformPolicyErrors.InvalidDisputePolicy);
                }

                if (SameDisputeConfig(currentConfig, config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>.Success(
                        ToResponse(current, PlatformPolicyType.Dispute, currentConfig));
                }

                var now = DateTime.UtcNow;
                var nextVersion = await _policyRepository.GetNextVersionAsync(PlatformPolicyType.Dispute, cancellationToken);

                current.IsActive = false;
                current.UpdatedAt = now;

                await _policyRepository.UpdateAsync(current, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var newPolicy = new platform_policy
                {
                    PolicyId = Guid.NewGuid(),
                    PolicyType = PlatformPolicyType.Dispute.ToString(),
                    Title = string.IsNullOrWhiteSpace(current.Title) ? "Dispute Policy" : current.Title,
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

                return Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>.Success(
                    ToResponse(newPolicy, PlatformPolicyType.Dispute, config));
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
                return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(x => x.ErrorMessage))));

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var current = await _policyRepository.GetActiveForUpdateAsync(PlatformPolicyType.Appointment, cancellationToken);

                if (current == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>.Fail(
                        PlatformPolicyErrors.ActiveNotFound(PlatformPolicyType.Appointment));
                }

                if (!TryDeserialize(current.Content, out AppointmentPolicyConfigDto? currentConfig) || !IsValidAppointmentConfig(currentConfig!))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>.Fail(
                        PlatformPolicyErrors.InvalidContent(PlatformPolicyType.Appointment));
                }

                var config = new AppointmentPolicyConfigDto
                {
                    CheckInOpenBeforeMinutes = request.CheckInOpenBeforeMinutes ?? currentConfig!.CheckInOpenBeforeMinutes,
                    NoInteractionExpiryMinutes = request.NoInteractionExpiryMinutes ?? currentConfig.NoInteractionExpiryMinutes,
                    RescheduleCutoffHours = request.RescheduleCutoffHours ?? currentConfig.RescheduleCutoffHours,
                    CancellationCutoffHours = request.CancellationCutoffHours ?? currentConfig.CancellationCutoffHours
                };

                if (!IsValidAppointmentConfig(config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>.Fail(
                        PlatformPolicyErrors.InvalidAppointmentPolicy);
                }

                if (SameAppointmentConfig(currentConfig, config))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>.Success(
                        ToResponse(current, PlatformPolicyType.Appointment, currentConfig));
                }

                var now = DateTime.UtcNow;
                var nextVersion = await _policyRepository.GetNextVersionAsync(PlatformPolicyType.Appointment, cancellationToken);

                current.IsActive = false;
                current.UpdatedAt = now;

                await _policyRepository.UpdateAsync(current, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var newPolicy = new platform_policy
                {
                    PolicyId = Guid.NewGuid(),
                    PolicyType = PlatformPolicyType.Appointment.ToString(),
                    Title = string.IsNullOrWhiteSpace(current.Title) ? "Appointment Policy" : current.Title,
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

                return Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>.Success(
                    ToResponse(newPolicy, PlatformPolicyType.Appointment, config));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<DisputePolicyConfigDto> GetDisputeConfigAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetDisputePolicyAsync(cancellationToken);

            if (!result.IsSuccess || result.Data == null)
                throw new InvalidOperationException(result.Error?.Message ?? "Dispute policy configuration is unavailable.");

            return result.Data.Config;
        }

        public async Task<AppointmentPolicyConfigDto> GetAppointmentConfigAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAppointmentPolicyAsync(cancellationToken);

            if (!result.IsSuccess || result.Data == null)
                throw new InvalidOperationException(result.Error?.Message ?? "Appointment policy configuration is unavailable.");

            return result.Data.Config;
        }

        private static bool TryDeserialize<T>(string? content, out T? config) where T : class
        {
            config = null;

            if (string.IsNullOrWhiteSpace(content)) return false;

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

        private static bool IsValidDisputeConfig(DisputePolicyConfigDto config)
        {
            return config.NormalDisputeWindowDays is >= 1 and <= 365
                && config.LowReputationDisputeWindowDays is >= 1 and <= 365
                && config.LowReputationDisputeWindowDays >= config.NormalDisputeWindowDays
                && config.LowReputationThreshold is >= 0 and <= 100;
        }

        private static bool IsValidAppointmentConfig(AppointmentPolicyConfigDto config)
        {
            return config.CheckInOpenBeforeMinutes is >= 0 and <= 1440
                && config.NoInteractionExpiryMinutes is >= 1 and <= 10080
                && config.RescheduleCutoffHours is >= 1 and <= 720
                && config.CancellationCutoffHours is >= 1 and <= 720
                && config.RescheduleCutoffHours >= config.CancellationCutoffHours;
        }

        private static bool SameDisputeConfig(DisputePolicyConfigDto current, DisputePolicyConfigDto updated)
        {
            return current.NormalDisputeWindowDays == updated.NormalDisputeWindowDays
                && current.LowReputationDisputeWindowDays == updated.LowReputationDisputeWindowDays
                && current.LowReputationThreshold == updated.LowReputationThreshold;
        }

        private static bool SameAppointmentConfig(AppointmentPolicyConfigDto current, AppointmentPolicyConfigDto updated)
        {
            return current.CheckInOpenBeforeMinutes == updated.CheckInOpenBeforeMinutes
                && current.NoInteractionExpiryMinutes == updated.NoInteractionExpiryMinutes
                && current.RescheduleCutoffHours == updated.RescheduleCutoffHours
                && current.CancellationCutoffHours == updated.CancellationCutoffHours;
        }

        private static PlatformPolicyResponseDto<TConfig> ToResponse<TConfig>(
            platform_policy policy,
            PlatformPolicyType policyType,
            TConfig config)
        {
            return new PlatformPolicyResponseDto<TConfig>
            {
                PolicyId = policy.PolicyId,
                PolicyType = policyType,
                Title = policy.Title ?? string.Empty,
                Version = policy.Version,
                IsActive = policy.IsActive,
                Config = config,
                CreatedAt = policy.CreatedAt,
                CreatedBy = policy.CreatedBy,
                UpdatedAt = policy.UpdatedAt
            };
        }
    }
}
