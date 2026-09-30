using PrimeERP.Application.Services.Entities;
using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;
using Xunit;

namespace PrimeERP.Tests.Helpers
{
    /// <summary>صفٌّ في صفحة قائمة</summary>
    public static class LookupRows
    {
        public static int Add<T>(this Lookup<T> page, string name) where T : BaseModel, new()
        {
            var created = page.Create(new Dictionary<string, object> { ["Name"] = name, ["IsActive"] = true });
            Assert.True(created.IsSuccess, created.ErrorMessage);
            return Rows.Id(created.Value);
        }
    }
}
