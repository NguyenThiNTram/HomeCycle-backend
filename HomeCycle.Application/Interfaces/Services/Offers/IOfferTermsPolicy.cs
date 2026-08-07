using HomeCycle.Application.Commons.Results;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Offers
{
    public interface IOfferTermsPolicy
    {
        Error? Validate(post post, decimal offerPrice, int offerQuantity);
    }
}
