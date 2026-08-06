using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Profiles
{
    public class UpdateBusinessDocumentsRequest
    {
        public List<BusinessDocumentDto> Documents { get; set; } = new();
    }
}
