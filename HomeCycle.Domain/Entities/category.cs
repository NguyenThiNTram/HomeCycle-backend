using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class category
{
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public category()
    {
    }

    public category(Guid CategoryId)
    {
        this.CategoryId = CategoryId;
    }
}
