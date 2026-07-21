using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class BankAccountMapper
    {
        public static bank_account ToDomain(this Bank_Account entity)
        {
            return new bank_account
            {
                UserBankId = entity.UserBankId,
                UserId = entity.UserId,
                BankCode = entity.BankCode,
                BankName = entity.BankName,
                AccountNumber = entity.AccountNumber,
                AccountName = entity.AccountName,
                VerifyStatus = (VerifyStatus?)entity.VerifyStatus,
                CreatedAt = entity.CreatedAt
            };
        }
        public static Bank_Account ToInfrastructure(this bank_account entity)
        {
            return new Bank_Account
            {
                UserBankId = entity.UserBankId,
                UserId = entity.UserId,
                BankCode = entity.BankCode,
                BankName = entity.BankName,
                AccountNumber = entity.AccountNumber,
                AccountName = entity.AccountName,
                VerifyStatus = (int?)entity.VerifyStatus,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
