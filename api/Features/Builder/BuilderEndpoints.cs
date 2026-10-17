using System.IO.Compression;
using PrimeERP.Api.Infrastructure;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Net;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Api.Features.Builder
{
    /// <summary>نافذة البناء</summary>
    public static class BuilderEndpoints
    {
        public static RouteGroupBuilder MapBuilder(this RouteGroupBuilder api)
        {
            var builder = api.MapGroup("/builder").WithTags("Builder").RequireAuthorization();
            var customers = builder.MapGroup("/customers");

            customers.MapGet("/", (ILicenseService licenses) => licenses.GetAll().ToHttp(list => list));
            customers.MapPost("/", (CreateLicenseDto dto, ILicenseService licenses) => licenses.Create(dto).ToHttp(license => license));
            customers.MapPut("/{id:int}/manifest", (int id, List<string> keys, ILicenseService licenses) => licenses.SaveManifest(id, keys).ToHttp());
            customers.MapPut("/{id:int}/version", (int id, VersionBody body, ILicenseService licenses) =>
                licenses.SetVersion(new EditionVersionDto { LicenseId = id, Version = body.Version }).ToHttp());
            customers.MapPut("/{id:int}/simplified", (int id, FlagBody body, ILicenseService licenses) => licenses.SetSimplified(id, body.Value).ToHttp());
            customers.MapPut("/{id:int}/english", (int id, FlagBody body, ILicenseService licenses) => licenses.SetEnglish(id, body.Value).ToHttp());
            customers.MapPost("/{id:int}/toggle", (int id, ILicenseService licenses) => licenses.ToggleActive(id).ToHttp());
            customers.MapDelete("/{id:int}", (int id, ILicenseService licenses) => licenses.Delete(id).ToHttp());

            customers.MapPost("/{id:int}/edition", (int id, EditionBody body, ILicenseService licenses, IProgramEditionService editions) =>
            {
                var license = licenses.GetById(id);
                if (license.IsFailure) return ResultHttp.Problem(license);

                var saved = licenses.SaveManifest(id, body.Keys);
                if (saved.IsFailure) return ResultHttp.Problem(saved);

                var folder = Scratch();
                var created = editions.Create(new CreateEditionDto
                {
                    ModuleKeys = body.Keys, TargetFolder = folder,
                    Simplified = license.Value.Simplified, English = license.Value.English,
                    Serial = license.Value.Serial, Customer = license.Value.CustomerName, Version = license.Value.Version,
                });
                if (created.IsFailure) { Directory.Delete(folder, true); return ResultHttp.Problem(created); }

                return Archive(folder, "PrimeERP.zip");
            });

            customers.MapGet("/{id:int}/installer", async (int id, ILicenseService licenses, ISettingsProvider settings, IHttpGateway http) =>
            {
                var license = licenses.GetById(id);
                if (license.IsFailure) return ResultHttp.Problem(license);

                var server = settings.Get(SettingKeys.Developer.ServerUrl, "").TrimEnd('/');
                if (string.IsNullOrWhiteSpace(server))
                    return ResultHttp.Problem(Result.Fail(LocalizationService.Get("Str.Builder.ServerUrlMissing"), ErrorCode.ValidationFailed));

                var folder = Scratch();
                var (ok, error) = await http.DownloadAsync($"{server}/package/PrimeERP.Setup.exe", Path.Combine(folder, "PrimeERP.Setup.exe"));
                if (!ok)
                {
                    Directory.Delete(folder, true);
                    return ResultHttp.Problem(Result.Fail(LocalizationService.Get("Str.Builder.SetupDownloadFailed", error), ErrorCode.Unexpected));
                }

                await File.WriteAllTextAsync(Path.Combine(folder, "serial.txt"), license.Value.Serial);
                return Archive(folder, "PrimeERP.Setup.zip");
            });

            return api;
        }

        private static string Scratch() =>
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "PrimeERP.Edition." + Guid.NewGuid().ToString("N"))).FullName;

        /// <summary>المجلد ملفاً مضغوطاً</summary>
        private static IResult Archive(string folder, string name)
        {
            var zip = folder + ".zip";
            ZipFile.CreateFromDirectory(folder, zip, CompressionLevel.NoCompression, includeBaseDirectory: false);
            Directory.Delete(folder, true);

            var stream = new FileStream(zip, FileMode.Open, FileAccess.Read, FileShare.None, 81920, FileOptions.DeleteOnClose);
            return Results.File(stream, "application/zip", name);
        }
    }
}
