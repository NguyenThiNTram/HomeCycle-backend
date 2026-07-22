using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Profiles
{
    public class UpdateBusinessDocumentRequest
    {
        public int DocumentType { get; set; }
        public string DocumentUrl { get; set; } = null!;
    }
}
