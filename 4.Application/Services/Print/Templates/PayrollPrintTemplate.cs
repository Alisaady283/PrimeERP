using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Domain.Contracts;

namespace PrimeERP.Application.Services.Print.Templates
{
    /// <summary>
    /// كشف رواتب رسميّ من PayrollDetailDto. لا يستدعي خدمةً هنا: المستدعي يجلب المسير ويمرّره،
    /// نفس نمط AccountStatementPrintTemplate.
    ///
    /// عرضيّ لا طوليّ: تسعة أعمدة لا تسعها الورقة الطولية، فتنضغط أرقامها حتى تُقرأ بصعوبة.
    /// </summary>
    public static class PayrollPrintTemplate
    {
        public static IPrintable Build(PayrollDetailDto payroll) => new PayrollPrintable(payroll);

        private class PayrollPrintable : IPrintable
        {
            private readonly PayrollDetailDto _payroll;

            public PayrollPrintable(PayrollDetailDto payroll) => _payroll = payroll;

            public string DocumentTitle    => "كشف رواتب";
            public string DocumentSubtitle => $"{_payroll.PayrollNo} — {_payroll.PeriodStart:yyyy-MM-dd} إلى {_payroll.PeriodEnd:yyyy-MM-dd}";

            public PrintOrientation Orientation => PrintOrientation.Landscape;

            public Dictionary<string, string> HeaderFields => new()
            {
                ["رقم المسير"]   = _payroll.PayrollNo,
                ["من"]           = _payroll.PeriodStart.ToString("yyyy-MM-dd"),
                ["إلى"]          = _payroll.PeriodEnd.ToString("yyyy-MM-dd"),
                ["تاريخ الصرف"]  = _payroll.PaymentDate.ToString("yyyy-MM-dd"),
                ["عدد الموظفين"] = _payroll.Lines.Count.ToString(),
                ["الحالة"]       = _payroll.StatusText,
            };

            public Dictionary<string, string> FooterFields => null;

            public bool ShowCompanyHeader => true;
            public bool ShowPageNumbers   => true;
            public bool ShowSignatures    => true;

            // ثلاثة توقيعات لا اثنان: المسير يمرّ بالموارد البشرية قبل المالية.
            public List<string> SignatureLabels => new() { "شؤون العاملين", "المحاسب", "المدير المالي" };

            public List<PrintSection> BuildSections()
            {
                var sections = new List<PrintSection>
                {
                    new()
                    {
                        Type = PrintSectionType.Table,
                        Columns = new List<PrintColumn>
                        {
                            new() { Key = "EmployeeName", Header = "الموظف",      Width = 2.4, Align = "Right" },
                            new() { Key = "BasicSalary",  Header = "الأساسي",     Width = 1.1, Format = "N2" },
                            new() { Key = "Allowances",   Header = "البدلات",     Width = 1.0, Format = "N2" },
                            new() { Key = "Overtime",     Header = "الإضافي",     Width = 1.0, Format = "N2" },
                            new() { Key = "Deductions",   Header = "الخصومات",    Width = 1.0, Format = "N2" },
                            new() { Key = "Advances",     Header = "السلف",       Width = 1.0, Format = "N2" },
                            new() { Key = "Insurance",    Header = "التأمينات",   Width = 1.0, Format = "N2" },
                            new() { Key = "Tax",          Header = "الضرائب",     Width = 1.0, Format = "N2" },
                            new() { Key = "NetSalary",    Header = "صافي الراتب", Width = 1.3, Format = "N2" },
                        },
                        Rows = _payroll.Lines.Select(Row).ToList(),
                        TotalsRow = TotalsRow(),
                    },

                    new()
                    {
                        Type = PrintSectionType.KeyValues,
                        Title = "إجماليات المسير",
                        KeyValues = new Dictionary<string, string>
                        {
                            ["إجمالي الاستحقاقات"] = Gross().ToString("N2"),
                            ["إجمالي الاستقطاعات"] = Withheld().ToString("N2"),
                            ["صافي الرواتب"]       = _payroll.NetTotal.ToString("N2"),
                        }
                    },

                    new()
                    {
                        Type = PrintSectionType.AmountInWords,
                        Amount = _payroll.NetTotal,
                    },
                };

                if (!string.IsNullOrWhiteSpace(_payroll.Notes))
                    sections.Add(new PrintSection { Type = PrintSectionType.Text, Title = "ملاحظات", Text = _payroll.Notes });

                return sections;
            }

            private decimal Gross() => _payroll.Lines.Sum(l => l.BasicSalary + l.Allowances + l.Overtime);

            private decimal Withheld() =>
                _payroll.Lines.Sum(l => l.Deductions + l.Advances + l.Insurance + l.Tax);

            private static Dictionary<string, object> Row(PayrollLineDto line) => new()
            {
                ["EmployeeName"] = line.EmployeeName,
                ["BasicSalary"]  = line.BasicSalary,
                ["Allowances"]   = line.Allowances,
                ["Overtime"]     = line.Overtime,
                ["Deductions"]   = line.Deductions,
                ["Advances"]     = line.Advances,
                ["Insurance"]    = line.Insurance,
                ["Tax"]          = line.Tax,
                ["NetSalary"]    = line.NetSalary,
            };

            private Dictionary<string, object> TotalsRow() => new()
            {
                ["EmployeeName"] = "الإجمالي",
                ["BasicSalary"]  = _payroll.Lines.Sum(l => l.BasicSalary),
                ["Allowances"]   = _payroll.Lines.Sum(l => l.Allowances),
                ["Overtime"]     = _payroll.Lines.Sum(l => l.Overtime),
                ["Deductions"]   = _payroll.Lines.Sum(l => l.Deductions),
                ["Advances"]     = _payroll.Lines.Sum(l => l.Advances),
                ["Insurance"]    = _payroll.Lines.Sum(l => l.Insurance),
                ["Tax"]          = _payroll.Lines.Sum(l => l.Tax),
                ["NetSalary"]    = _payroll.NetTotal,
            };
        }
    }
}
