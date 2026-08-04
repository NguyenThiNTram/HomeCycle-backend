using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HomeCycle.Application.DTOs.Responses.Media
{
    //public class MediaResponse
    //{
    //    public Guid MediaId { get; set; }

    //    public Guid? TargetId { get; set; }

    //    public string? TargetType { get; set; }

    //    public string? Url { get; set; }

    //    public string? FileName { get; set; }

    //    public long? FileSize { get; set; }

    //    public int? DisplayOrder { get; set; }

    //    public DateTime CreatedAt { get; set; }

    //    public DateTime UpdatedAt { get; set; }
    //}

    public class MediaResponse
    {
        public Guid MediaId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int DisplayOrder { get; set; }
    }
}

