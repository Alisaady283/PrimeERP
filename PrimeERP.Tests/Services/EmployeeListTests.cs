using PrimeERP.Application.Services.Entities;
using PrimeERP.Tests.Helpers;
using PrimeERP.Domain.Entities;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Legacy.HR;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>القائمة تحمل اسمَي القسم والوظيفة</summary>
    public class EmployeeListTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public EmployeeListTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void ThePage_CarriesDepartmentAndJobTitleNames()
        {
            var deptId = _db.Services.GetRequiredService<Lookup<Department>>().Add("المبيعات");
            var jobId = _db.Services.GetRequiredService<Lookup<JobTitle>>().Add("محاسب");
            var employees = _db.Services.GetRequiredService<IEmployeeService>();
            employees.Create(new Employee { Name = "بقسم", DepartmentId = deptId, JobTitleId = jobId, HireDate = DateTime.Today });
            employees.Create(new Employee { Name = "بلا قسم", HireDate = DateTime.Today });

            var page = employees.GetPaged(1, 50, new EmployeeFilter());

            Assert.True(page.IsSuccess, page.ErrorMessage);
            var placed = page.Value.Items.Single(e => e.Name == "بقسم");
            Assert.Equal("المبيعات", placed.DepartmentName);
            Assert.Equal("محاسب", placed.JobTitleName);
            var loose = page.Value.Items.Single(e => e.Name == "بلا قسم");
            Assert.Null(loose.DepartmentName);
            Assert.Null(loose.JobTitleName);
        }
    }
}
