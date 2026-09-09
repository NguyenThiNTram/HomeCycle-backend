using System.ComponentModel.DataAnnotations;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Requests.Dashboard;

public class BusinessOverviewRequest
{
    [EnumDataType(typeof(BusinessModel))] public BusinessModel? BusinessModel { get; set; }
    [EnumDataType(typeof(BusinessProfileStatus))] public BusinessProfileStatus? ProfileStatus { get; set; }
    [EnumDataType(typeof(UserStatus))] public UserStatus? UserStatus { get; set; }
}
public sealed class BusinessDemandRequest : BusinessOverviewRequest
{
    [StringLength(100)] public string? TargetCity { get; set; }
    [StringLength(100)] public string? ServiceCity { get; set; }
    public Guid? ProductTypeId { get; set; }
}
