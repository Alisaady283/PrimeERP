using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>
    /// كل حقل يُدخله المستخدم في نموذج المستند لا بدّ أن يصل الورق. عمود الملاحظات سقط بصمت من فواتير
    /// البيع والشراء ومرتجعاتهما لأن أعمدة الورق تُعدّ يدوياً هناك، وهذا يمنع تكرارها.
    /// </summary>
    public class DocumentColumnParityTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public DocumentColumnParityTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void EveryDocument_PrintsEveryFieldItsFormCollects()
        {
            var registry = _db.Services.GetRequiredService<IModuleRegistry>();
            var missing = new List<string>();

            foreach (var module in registry.All().Where(m => m.DocumentDialog is { PrintColumns.Count: > 0 }))
            {
                var definition = module.DocumentDialog;
                var printed = definition.PrintColumns.Select(c => c.Key).ToHashSet();

                foreach (var field in definition.LineFields)
                {
                    // الكود يُطبع عمودين (كود واسم)، والنسبة يمثّلها مبلغها المحسوب.
                    if (printed.Contains(field.Key)) continue;
                    if (field.Key.EndsWith("Code") && printed.Contains(field.Key[..^4] + "Name")) continue;
                    if (field.Key.EndsWith("Percent") && printed.Contains(field.Key[..^7] + "Amount")) continue;

                    missing.Add($"{definition.PrintTitle}: {field.Key}");
                }
            }

            Assert.True(missing.Count == 0, "حقول تُدخَل ولا تُطبع — " + string.Join("، ", missing));
        }
    }
}
