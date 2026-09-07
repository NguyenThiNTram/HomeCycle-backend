using HomeCycle.Application.Commons.Results;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Configs
{
    public interface IFileValidationService
    {
        Task<Result> ValidateAsync(IFormFile? file, FileUploadContext context, CancellationToken cancellationToken = default);

        Task<Result> ValidateManyAsync(IEnumerable<IFormFile>? files, FileUploadContext context, CancellationToken cancellationToken = default);
    }
}
