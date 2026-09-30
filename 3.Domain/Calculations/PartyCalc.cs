using System.Collections.Generic;
using System.Linq;
using System;
using System.Linq.Expressions;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>قواعد الطرف النقيّة</summary>
    public static class PartyCalc
    {
        /// <summary>تجاوز الحدّ الائتماني</summary>
        public static bool IsOverCreditLimit(decimal balance, decimal creditLimit) =>
            creditLimit > 0 && balance > creditLimit;

        /// <summary>ما يتجاوز الحدّ بعد مبلغ</summary>
        public static decimal Exceeding(decimal balance, decimal additional, decimal creditLimit) =>
            creditLimit <= 0 ? 0 : Math.Max(balance + additional - creditLimit, 0);

        /// <summary>المتاح من الحدّ</summary>
        public static decimal Available(decimal balance, decimal creditLimit) =>
            creditLimit <= 0 ? decimal.MaxValue : creditLimit - balance;

        /// <summary>القاعدة شرطَ استعلام</summary>
        public static Expression<Func<T, bool>> OverCreditLimit<T>(bool over) where T : PartyBase =>
            over ? p => p.CreditLimit > 0 && p.Balance > p.CreditLimit
                 : p => !(p.CreditLimit > 0 && p.Balance > p.CreditLimit);
    }
}
