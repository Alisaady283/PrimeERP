using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Assets;
using PrimeERP.Application.Services.Treasury;
using PrimeERP.Data.Repositories;
using PrimeERP.Application.Services.Common;
using PrimeERP.Domain.Rules;
using PrimeERP.Platform.Settings;

namespace PrimeERP.App.Bootstrap
{
    /// <summary>
    /// تسويةٌ لمرّة واحدة لأصولٍ سبقت وجود قيد الاقتناء ومواضع حسابات الأصول:
    /// يُنشئ الحسابات الخمسة في مواضعها المعيارية ويربطها، ويُكمل العمر الإنتاجي والمموّل الناقصَين،
    /// ثم يُرحّل قيد اقتناءٍ لكل أصلٍ بلا قيد. كلها عبر الخدمات والمستودعات — صفر SQL مكتوب هنا.
    ///
    /// كل خطوة مشروطة بغياب قيمتها، فتُعاد في كل إقلاع بلا أثر على قاعدةٍ مضبوطة — ولو تعذّرت مرّةً
    /// أُعيدت في التالية. والمستخدم يُعدّل أي رقم من شاشاته بعدها: العمر من حوار الأصل، والحسابات من
    /// الإعدادات، والمموّل بتعديل الأصل.
    /// </summary>
    public static class AssetBackfill
    {
        private static IServiceProvider ServicesRef;

        public static void Apply(IServiceProvider services)
        {
            ServicesRef = services;

            LinkAccounts(services);
            LinkTree(services);
            CompleteAssets(services);
            HealDepreciation(services);
        }

        /// <summary>
        /// تسوية الإهلاك: قيدُ إهلاكٍ لا يملكه قسطٌ قائم يتيمٌ يُضاعف المصروف — يُحذف. ثم يُشتقّ مجمّع
        /// كل أصلٍ ودفتريّته وآخر شهرٍ أُهلك من أقساطه، فلا يبقى رقمٌ محفوظ يخالف سجلّاته.
        /// </summary>
        private static void HealDepreciation(IServiceProvider services)
        {
            var charges = services.GetRequiredService<IAssetDepreciationRepository>();
            var journals = services.GetRequiredService<IJournalRepository>();
            var journalService = services.GetRequiredService<PrimeERP.Application.Services.Accounting.IJournalService>();
            var assets = services.GetRequiredService<IAssetRepository>();

            var linked = charges.LinkedEntryIds().ToHashSet();

            var orphans = journals.GetPaged(1, 100000, source: "AssetDepreciation").Items
                .Where(entry => !linked.Contains(entry.Id))
                .Select(entry => entry.Id)
                .ToList();

            // مسار المالك (conn, tx) لا Delete(id) العامّة: الأخيرة ترفض القيد المرحَّل بحكم قاعدتها،
            // وهذه قيودُ إهلاكٍ مرحَّلة فقدت أقساطها — تُعكَس كما تعكس كل خدمةٍ قيدها هي.
            foreach (var id in orphans)
                PrimeERP.Data.Core.DbHelper.RunTransaction((conn, tx) => journalService.Delete(conn, tx, id));

            foreach (var asset in assets.GetPaged(1, 100000).Items)
            {
                var own = charges.OfAsset(asset.Id);
                var accumulated = own.Sum(c => c.Amount);
                var last = own.Count == 0 ? (DateTime?)null : own.Max(c => c.PeriodDate);
                var book = DepreciationRules.BookValue(
                    asset.RevaluedValue > 0 ? asset.RevaluedValue : asset.PurchaseCost, accumulated);

                if (asset.AccumulatedDepreciation == accumulated
                    && asset.LastDepreciationDate == last
                    && asset.CurrentValue == book) continue;

                asset.AccumulatedDepreciation = accumulated;
                asset.LastDepreciationDate = last;
                asset.CurrentValue = book;
                assets.Update(asset);
            }
        }

        /// <summary>الحسابات الخمسة في مواضعها من الشجرة الرسمية — تُنشأ إن غابت، وتُربَط إن لم تُربَط.</summary>
        private static void LinkAccounts(IServiceProvider services)
        {
            var settings = services.GetRequiredService<ISettingsProvider>();
            var accounts = services.GetRequiredService<IAccountService>();

            void Link(string key, string parentCode, string name)
            {
                if (!string.IsNullOrWhiteSpace(settings.Get<string>(key, ""))) return;

                var parent = accounts.GetByCode(parentCode);
                if (parent.IsFailure) return;

                // حسابٌ بنفس الاسم تحت الأب لا يُكرَّر — التسوية تُعاد بلا أثر.
                var existing = accounts.GetLeaves().Value?
                    .FirstOrDefault(leaf => leaf.Name == name && (leaf.Code ?? "").StartsWith(parentCode));

                var code = existing?.Code ?? accounts.Create(new CreateAccountDto
                { ParentId = parent.Value.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value?.Code;

                if (!string.IsNullOrWhiteSpace(code)) settings.SetRaw(key, code);
            }

            // جذر الأصول الثابتة مُعلَنٌ في LinkedRoots، فيتولّاه RepairLinkedRoots عند الإقلاع:
            // القائمة تعرض التجميعية فقط، والحفظ يرفض الورقة، وقيمةٌ ورقيّة قديمة تُوجَّه إلى أبيها.
            Seeded(SettingKeys.Accounts.FixedAssets, "1101001");
            Seeded(SettingKeys.Accounts.AccumulatedDepreciation, "1101002");
            Link(SettingKeys.Accounts.DepreciationExpense,     "51",   "مصروف إهلاك الأصول الثابتة");
            Link(SettingKeys.Accounts.CapitalGains,            "42",   "أرباح رأسمالية");
            Link(SettingKeys.Accounts.CapitalLosses,           "52",   "خسائر رأسمالية");
        }

        /// <summary>حسابٌ مبذورٌ في الشجرة يُربَط بكوده — لا يُنشأ شيء، البذرة كفَت.</summary>
        private static void Seeded(string key, string code)
        {
            var settings = ServicesRef.GetRequiredService<ISettingsProvider>();
            if (!string.IsNullOrWhiteSpace(settings.Get<string>(key, ""))) return;

            var accounts = ServicesRef.GetRequiredService<IAccountService>();
            if (accounts.GetByCode(code).IsSuccess) settings.SetRaw(key, code);
        }

        /// <summary>
        /// ما سبق الربط يأخذ حسابه: كل فئة أصولٍ تصير تجميعياً تحت الجذر، وكل أصلٍ ورقةً تحت فئته.
        /// مشروطٌ بغياب الكود، فيُعاد بلا أثر.
        /// </summary>
        private static void LinkTree(IServiceProvider services)
        {
            var categories = services.GetRequiredService<ICategoryRepository>();
            var assets = services.GetRequiredService<IAssetRepository>();
            var accounts = services.GetRequiredService<IAccountService>();
            var settings = services.GetRequiredService<ISettingsProvider>();

            var root = settings.Get<string>(SettingKeys.Accounts.FixedAssets, "");
            if (string.IsNullOrWhiteSpace(root)) return;

            var rootAccount = accounts.GetByCode(root);
            if (rootAccount.IsFailure) return;

            foreach (var category in categories.GetAll("AssetCategories", includeInactive: true)
                         .Where(c => string.IsNullOrWhiteSpace(c.AccountCode)))
            {
                var created = accounts.Create(new CreateAccountDto
                { ParentId = rootAccount.Value.Id, Name = category.Name, IsLeaf = false, SkipAutoLink = true });

                if (created.IsFailure) continue;

                category.AccountCode = created.Value.Code;
                categories.Update(category);
            }

            // الأصل حسابان: واحدٌ تحت فئته وآخر تحت مجمّعها. أيٌّ منهما ناقص يُستكمَل — وبلا المجمّع
            // يتخطّى الاحتساب الأصل فلا يُنتج قيداً.
            foreach (var asset in assets.GetPaged(1, 100000).Items
                         .Where(a => a.CategoryId != null &&
                                     (string.IsNullOrWhiteSpace(a.AccountCode) ||
                                      string.IsNullOrWhiteSpace(a.DepreciationAccountCode))))
            {
                var category = categories.GetById(asset.CategoryId.Value);
                if (category == null) continue;

                var changed = false;

                if (string.IsNullOrWhiteSpace(asset.AccountCode))
                {
                    var code = Leaf(accounts, category.AccountCode, asset.Name);
                    if (code != null) { asset.AccountCode = code; changed = true; }
                }

                if (string.IsNullOrWhiteSpace(asset.DepreciationAccountCode))
                {
                    var code = Leaf(accounts, category.DepreciationAccountCode, PrimeERP.Application.Services.Common.CategoryService.Mirror(asset.Name));
                    if (code != null) { asset.DepreciationAccountCode = code; changed = true; }
                }

                if (changed) assets.Update(asset);
            }
        }

        /// <summary>ورقةٌ تحت حسابٍ أبٍ — null لو غاب الأب أو فشل الإنشاء، فتُعاد التسوية في الإقلاع التالي.</summary>
        private static string Leaf(IAccountService accounts, string parentCode, string name)
        {
            if (string.IsNullOrWhiteSpace(parentCode)) return null;

            var parent = accounts.GetByCode(parentCode);
            if (parent.IsFailure) return null;

            var created = accounts.Create(new CreateAccountDto
            { ParentId = parent.Value.Id, Name = name, IsLeaf = true, SkipAutoLink = true });

            return created.IsSuccess ? created.Value.Code : null;
        }

        /// <summary>العمر والتاريخ والمموّل الناقصون، ثم قيد اقتناءٍ لكل أصلٍ بلا قيد.</summary>
        private static void CompleteAssets(IServiceProvider services)
        {
            var repository = services.GetRequiredService<IAssetRepository>();
            var assetService = services.GetRequiredService<IAssetService>();

            var funding = CashAccount(services);
            var pending = repository.GetPaged(1, 100000).Items.Where(a => a.IsActive).ToList();

            foreach (var asset in pending)
            {
                var changed = false;

                if (asset.RevaluedValue <= 0)   { asset.RevaluedValue = asset.PurchaseCost; changed = true; }

                // بلا تاريخ شراء لا يبدأ الإهلاك — تاريخ الإنشاء أقرب تقديرٍ صادق.
                if (asset.PurchaseDate == null)
                {
                    asset.PurchaseDate = asset.CreatedAt == DateTime.MinValue ? DateTime.Today : asset.CreatedAt.Date;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(asset.FundingAccountCode) && funding != null)
                {
                    asset.FundingAccountCode = funding;
                    changed = true;
                }

                // دفتريّةٌ مخزَّنة تخالف مصدرها (قيمةٌ أُدخلت يدوياً قبل أن تصير محسوبة) — تُصحَّح.
                var book = DepreciationRules.BookValue(asset.RevaluedValue, asset.AccumulatedDepreciation);
                if (asset.CurrentValue != book) { asset.CurrentValue = book; changed = true; }

                if (changed) repository.Update(asset);
                if (asset.JournalEntryId == null) assetService.PostMissingAcquisition(asset);
            }
        }

        /// <summary>أول خزينة نقدية نشطة مربوطة بحساب — المموّل الافتراضي، يُغيّره المستخدم بالتعديل.</summary>
        private static string CashAccount(IServiceProvider services)
        {
            var treasuries = services.GetRequiredService<ITreasuryService>().GetAll();
            if (treasuries.IsFailure) return null;

            return treasuries.Value
                .Where(t => t.IsActive && !string.IsNullOrWhiteSpace(t.AccountCode))
                .OrderBy(t => (int)t.Kind)
                .Select(t => t.AccountCode)
                .FirstOrDefault();
        }

    }
}
