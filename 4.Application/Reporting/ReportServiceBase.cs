using PrimeERP.Application.Services.Core;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Reporting
{
    /// <summary>ما يتقاسمه كل تقرير</summary>
    public abstract class ReportServiceBase : ServiceBase
    {
        protected ReportServiceBase(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) { }

        protected override string PermissionPrefix => "Reports";
        protected override string StringPrefix => "Str.Reports";
        protected override string EntityName => "Report";

        /// <summary>يُنادى أول كل تقرير</summary>
        protected Result<ReportData> Gate() => Can("View") ? null : FailDenied<ReportData>();
    }
}
