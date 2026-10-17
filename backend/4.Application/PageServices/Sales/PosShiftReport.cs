using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.PageServices.Builder;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.Sales
{
    public class PosShiftReport : ServiceBase, IRowService
    {
        protected override string PermissionPrefix => "Pos";
        protected override string StringPrefix => "Str.Pos";
        protected override string EntityName => "PosShifts";

        private readonly IPosShiftRepository _shifts;

        public PosShiftReport(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            IPosShiftRepository shifts) : base(permissions, settings, localization, audit) => _shifts = shifts;

        public Result<PagedResult<IDictionary<string, object>>> GetPaged(int page, int pageSize, DynamicFilter filter)
        {
            if (!Can("Close")) return FailDenied<PagedResult<IDictionary<string, object>>>();
            var (items, total) = _shifts.GetPaged(page, pageSize);
            return Ok(new PagedResult<IDictionary<string, object>>
            {
                Items = items.Select(Rows.Of).ToList(), TotalCount = total, Page = page, PageSize = pageSize
            });
        }

        public Result<IDictionary<string, object>> GetById(int id) =>
            _shifts.GetById(id) is { } shift ? Ok(Rows.Of(shift)) : Fail<IDictionary<string, object>>("NotFound", ErrorCode.NotFound);

        public Result<IDictionary<string, object>> Create(IDictionary<string, object> values) => FailDenied<IDictionary<string, object>>();
        public Result Update(IDictionary<string, object> values) => FailDenied();
        public Result Delete(int id) => FailDenied();
    }
}
