using System;
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Print
{
    /// <summary>ترويسة الشركة كقطعة واحدة تُستدعى</summary>
    public static class CompanyHeaderComponent
    {
        private static readonly (string Key, string Label)[] Fields =
        {
            (SettingKeys.Company.CommercialRegNo, "س.ت"),
            (SettingKeys.Company.TaxNumber, "الرقم الضريبي"),
            (SettingKeys.Company.Phone, "هاتف"),
            (SettingKeys.Company.Address, ""),
        };

        public static PaperNode Build(Func<string, string> read)
        {
            var info = new PaperStack();
            info.Children.Add(new PaperText { Text = read(SettingKeys.Company.Name), Role = PaperTextRole.Name });

            foreach (var (key, label) in Fields)
            {
                var value = read(key);
                if (string.IsNullOrWhiteSpace(value)) continue;

                info.Children.Add(new PaperText
                {
                    Text = string.IsNullOrEmpty(label) ? value : label + ": " + value,
                    Role = PaperTextRole.Detail
                });
            }

            var row = new PaperRow();
            row.Children.Add(info);

            var logo = read(SettingKeys.Company.LogoData);
            if (!string.IsNullOrWhiteSpace(logo))
                row.Children.Add(new PaperImage
                {
                    Data = logo,
                    MaxWidth = PaperTheme.Value<double>("LogoMaxWidth"),
                    MaxHeight = PaperTheme.Value<double>("LogoMaxHeight")
                });

            return new PaperStack
            {
                Children =
                {
                    row,
                    new PaperRule
                    {
                        Thickness = PaperTheme.Value<double>("HeaderSeparator"),
                        GapAbove = PaperTheme.Value<double>("HeaderGap"),
                        GapBelow = PaperTheme.Value<double>("HeaderGap")
                    }
                }
            };
        }
    }
}
