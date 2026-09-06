using System;
using System.Collections.Generic;
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Print
{
    /// <summary>
    /// المصدر الوحيد لمحتوى الترويسة — الطباعة والتصدير كلاهما يقرأ من هنا فلا تفترق ورقتان.
    /// يأخذ دالة قراءة لا واجهة إعدادات بعينها: المستهلكان يحملان عقدين مختلفين لنفس العملية.
    /// </summary>
    public static class CompanyHeaderReader
    {
        private static readonly (string Key, string Label)[] Fields =
        {
            (SettingKeys.Company.CommercialRegNo, "س.ت"),
            (SettingKeys.Company.TaxNumber, "الرقم الضريبي"),
            (SettingKeys.Company.Phone, "هاتف"),
            (SettingKeys.Company.Address, ""),
        };

        public static CompanyHeader From(Func<string, string> read)
        {
            var details = new List<string>();
            foreach (var (key, label) in Fields)
            {
                var value = read(key);
                if (!string.IsNullOrWhiteSpace(value))
                    details.Add(string.IsNullOrEmpty(label) ? value : label + ": " + value);
            }

            return new CompanyHeader
            {
                Name = read(SettingKeys.Company.Name),
                Details = details,
                LogoData = read(SettingKeys.Company.LogoData)
            };
        }
    }
}
