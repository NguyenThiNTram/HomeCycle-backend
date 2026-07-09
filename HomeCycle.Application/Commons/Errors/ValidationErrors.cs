using HomeCycle.Application.Commons.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Errors
{
    public static class ValidationErrors
    {
        public static Error InvalidRequest(
            string message)
        {
            return new Error(
                "VALIDATION_ERROR",
                message
            );
        }
    }
}
