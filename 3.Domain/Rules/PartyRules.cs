using System;
using System.Linq.Expressions;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Rules
{
    /// <summary>قواعد الطرف النقيّة</summary>
    public static class PartyRules
    {
        /// <summary>تجاوز الحدّ الائتماني</summary>
        public static bool IsOverCreditLimit(decimal balance, decimal creditLimit) =>
            creditLimit > 0 && balance > creditLimit;

        /// <summary>القاعدة شرطَ استعلام</summary>
        public static Expression<Func<T, bool>> OverCreditLimit<T>(bool over) where T : PartyBase =>
            over ? p => p.CreditLimit > 0 && p.Balance > p.CreditLimit
                 : p => !(p.CreditLimit > 0 && p.Balance > p.CreditLimit);
    }
}
