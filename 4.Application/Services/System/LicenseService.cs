using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Net;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services
{
    public interface ILicenseService
    {
        Result<List<LicenseDto>> GetAll();
        Result<LicenseDto> GetById(int id);
        Result<LicenseDto> Create(CreateLicenseDto dto);
        Result SaveManifest(int id, List<string> moduleKeys);
        Result Delete(int id);
    }

    /// <summary>
    /// تراخيص العملاء: لكل عميلٍ سريالٌ عشوائيّ يُخزَّن هنا ومعه ما يفتحه من صفحات. التحقق الحقيقي عند
    /// التفعيل على الخادم — فلا توقيع داخل السريال ولا مفتاحٌ يُدار، والسريال يبقى قصيراً يُقرأ ويُنسخ.
    /// </summary>
    public class LicenseService : ServiceBase, ILicenseService
    {
        // بلا حروفٍ تلتبس بأرقام (I O U) — السريال يُملى بالهاتف أحياناً.
        private const string Alphabet = "0123456789ABCDEFGHJKLMNPQRSTVWXYZ";

        private readonly ILicenseRepository _repo;
        private readonly IHttpGateway _http;

        public LicenseService(ILicenseRepository repo, IHttpGateway http, IPermissionService permissions,
            ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo; _http = http;
        }

        protected override string PermissionPrefix => "BuilderExport";
        protected override string StringPrefix => "Str.Builder";
        protected override string EntityName => "License";

        public Result<List<LicenseDto>> GetAll()
        {
            if (!Can("View")) return FailDenied<List<LicenseDto>>();

            return Result.Ok(_repo.GetAll().Select(ToDto).ToList());
        }

        public Result<LicenseDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<LicenseDto>();

            var license = _repo.GetById(id);
            return license == null
                ? Result.Fail<LicenseDto>("الترخيص غير موجود", ErrorCode.NotFound)
                : Result.Ok(ToDto(license));
        }

        public Result<LicenseDto> Create(CreateLicenseDto dto)
        {
            if (!Can("Create")) return FailDenied<LicenseDto>();

            if (string.IsNullOrWhiteSpace(dto.CustomerName))
                return Result.Fail<LicenseDto>("اسم العميل مطلوب", ErrorCode.ValidationFailed);

            var license = new License
            {
                CustomerName = dto.CustomerName.Trim(),
                Location = dto.Location?.Trim(),
                Serial = NewSerial(),
                Manifest = string.Join(",", dto.ModuleKeys ?? new()),
                Simplified = dto.Simplified,
                CreatedBy = AppSession.Username
            };

            license.Id = _repo.Insert(license);

            // السريال يوجد عند الطرفين أو لا يوجد: لو لم يبلغ الخادم فلا قيمة له، فيُلغى محلياً كذلك.
            var published = Publish(license);
            if (published.IsFailure)
            {
                _repo.Delete(license.Id);
                return Result.Fail<LicenseDto>(published.ErrorMessage, published.ErrorCode);
            }

            Audit.Log(EntityName, license.Id, AuditAction.Insert, newValue: new { license.CustomerName, license.Location });

            return Result.Ok(ToDto(license));
        }

        /// <summary>ما اختاره المستخدم للعميل من صفحات — يُخزَّن على ترخيصه فيُسلَّم به ويُحدَّث به.</summary>
        public Result SaveManifest(int id, List<string> moduleKeys)
        {
            if (!Can("Edit")) return FailDenied();

            var license = _repo.GetById(id);
            if (license == null) return Result.Fail("الترخيص غير موجود", ErrorCode.NotFound);

            license.Manifest = string.Join(",", moduleKeys ?? new List<string>());
            _repo.Update(license);

            var published = Publish(license);
            if (published.IsFailure) return published;

            Audit.Log(EntityName, id, AuditAction.Update, newValue: new { pages = moduleKeys?.Count ?? 0 });

            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            _repo.Delete(id);
            Audit.Log(EntityName, id, AuditAction.Delete);

            return Result.Ok();
        }

        /// <summary>يُسجَّل السريال على الخادم — هو من يتحقق منه عند التفعيل، فلا معنى لسريالٍ لا يعرفه.</summary>
        private Result Publish(License license)
        {
            var server = Setting(SettingKeys.Developer.ServerUrl, "").TrimEnd('/');
            var token = Setting(SettingKeys.Developer.AdminToken, "");

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(token))
                return Result.Fail("اضبط عنوان خادم التراخيص وتوكن المطوّر من الإعدادات أولاً", ErrorCode.ValidationFailed);

            var payload = new
            {
                serial = license.Serial,
                customer = license.CustomerName,
                location = license.Location ?? "",
                manifest = license.Manifest ?? "",
                simplified = license.Simplified
            };

            // خارج خيط الواجهة: الطلب من داخله حاجزاً يُعلّق النافذة — نفس قاعدة العمليات الطويلة.
            var (ok, error, _) = System.Threading.Tasks.Task
                .Run(() => _http.PostAsync($"{server}/licenses", payload, token))
                .GetAwaiter().GetResult();
            return ok ? Result.Ok() : Result.Fail($"تعذّر تسجيل السريال على الخادم: {error}", ErrorCode.Unexpected);
        }

        /// <summary>عشرون حرفاً من مولّدٍ آمن في أربع مجموعات — لا تسلسل يُخمَّن ولا معنى يُقرأ منه.</summary>
        private static string NewSerial()
        {
            var bytes = RandomNumberGenerator.GetBytes(20);
            var text = new string(bytes.Select(b => Alphabet[b % Alphabet.Length]).ToArray());

            return string.Join("-", Enumerable.Range(0, 4).Select(i => text.Substring(i * 5, 5)));
        }

        private static LicenseDto ToDto(License l) => new()
        {
            Id = l.Id, CustomerName = l.CustomerName, Location = l.Location, Serial = l.Serial,
            ModuleKeys = string.IsNullOrWhiteSpace(l.Manifest)
                ? new List<string>()
                : l.Manifest.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            Simplified = l.Simplified,
            IsActivated = !string.IsNullOrWhiteSpace(l.MachineHash),
            CreatedAt = l.CreatedAt
        };
    }
}
