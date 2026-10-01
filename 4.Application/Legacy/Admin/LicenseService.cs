using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Core;
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

namespace PrimeERP.Application.Legacy.Admin
{
    /// <summary>تراخيص العملاء</summary>
    public interface ILicenseService
    {
        Result<List<LicenseDto>> GetAll();
        Result<LicenseDto> GetById(int id);
        Result<LicenseDto> Create(CreateLicenseDto dto);
        Result SaveManifest(int id, List<string> moduleKeys);
        Result Delete(int id);
    }

    public class LicenseService : ServiceBase, ILicenseService
    {
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
                ? Result.Fail<LicenseDto>(Msg("LicenseNotFound"), ErrorCode.NotFound)
                : Result.Ok(ToDto(license));
        }

        public Result<LicenseDto> Create(CreateLicenseDto dto)
        {
            if (!Can("Create")) return FailDenied<LicenseDto>();

            var input = Check.Valid(dto, new Field<CreateLicenseDto>(x => x.CustomerName, "", Required: true, Message: "Str.Builder.CustomerNameRequired"));
            if (input.IsFailure) return input.As<LicenseDto>();

            var license = Rows.Copy(dto, new License(), l =>
            {
                l.CustomerName = dto.CustomerName.Trim();
                l.Location = dto.Location?.Trim();
                l.Serial = NewSerial();
                l.Manifest = string.Join(",", dto.ModuleKeys ?? new());
            });

            license.Id = _repo.Insert(license);

            var published = Publish(license);
            if (published.IsFailure)
            {
                _repo.Delete(license.Id);
                return Result.Fail<LicenseDto>(published.ErrorMessage, published.ErrorCode);
            }

            Audit.Log(EntityName, license.Id, AuditAction.Insert, newValue: new { license.CustomerName, license.Location });

            return Result.Ok(ToDto(license));
        }

        public Result SaveManifest(int id, List<string> moduleKeys)
        {
            if (!Can("Edit")) return FailDenied();

            var license = _repo.GetById(id);
            if (license == null) return Result.Fail(Msg("LicenseNotFound"), ErrorCode.NotFound);

            license.Manifest = string.Join(",", moduleKeys ?? new List<string>());

            var published = Publish(license);
            if (published.IsFailure) return published;

            _repo.Update(license);

            Audit.Log(EntityName, id, AuditAction.Update, newValue: new { pages = moduleKeys?.Count ?? 0 });

            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var license = _repo.GetById(id);
            if (license == null) return Result.Fail(Msg("LicenseNotFound"), ErrorCode.NotFound);

            var revoked = Send("licenses/revoke", new { serial = license.Serial }, "SerialRevokeFailed");
            if (revoked.IsFailure) return revoked;

            _repo.Delete(id);
            Audit.Log(EntityName, id, AuditAction.Delete);

            return Result.Ok();
        }

        private Result Publish(License license) => Send("licenses", new
        {
            serial = license.Serial,
            customer = license.CustomerName,
            location = license.Location ?? "",
            manifest = license.Manifest ?? "",
            simplified = license.Simplified
        }, "SerialRegisterFailed");

        /// <summary>طلبٌ بتوكن المطوّر</summary>
        private Result Send(string path, object payload, string failKey)
        {
            var server = Setting(SettingKeys.Developer.ServerUrl, "").TrimEnd('/');
            var token = Setting(SettingKeys.Developer.AdminToken, "");

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(token))
                return Result.Fail(Msg("LicenseServerMissing"), ErrorCode.ValidationFailed);

            var (ok, error, _) = System.Threading.Tasks.Task
                .Run(() => _http.PostAsync($"{server}/{path}", payload, token))
                .GetAwaiter().GetResult();
            return ok ? Result.Ok() : Result.Fail(Msg(failKey, error), ErrorCode.Unexpected);
        }

        private static string NewSerial()
        {
            var bytes = RandomNumberGenerator.GetBytes(20);
            var text = new string(bytes.Select(b => Alphabet[b % Alphabet.Length]).ToArray());

            return string.Join("-", Enumerable.Range(0, 4).Select(i => text.Substring(i * 5, 5)));
        }

        private static LicenseDto ToDto(License l) => Rows.Copy<LicenseDto>(l, new(), to =>
        {
            to.ModuleKeys = string.IsNullOrWhiteSpace(l.Manifest) ? new List<string>() : l.Manifest.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            to.IsActivated = !string.IsNullOrWhiteSpace(l.MachineHash);
        });
    }
}
