using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

/// <summary>
/// Giá bán mới ngoài thị trường cho một model/biến thể đã được kiểm duyệt.
/// Mỗi bản ghi là một quan sát giá theo VND trên một sản phẩm từ một nguồn.
/// </summary>
public class market_price_reference
{
    public Guid Id { get; set; }
    public Guid ProductTypeId { get; set; }
    public Guid? BrandId { get; set; }
    public string ModelNumber { get; set; } = string.Empty;
    public string NormalizedModelNumber { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string KeySpecifications { get; set; } = "{}";
    public bool HasVariants { get; set; }
    public decimal PriceVndPerUnit { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime ObservedAt { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
