using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Tree;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    public class CustomerSelectionTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public CustomerSelectionTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private Func<IServiceProvider, int, List<TreeNodeViewModel>> Tree =>
            _db.Services.GetRequiredService<IModuleRegistry>().Get("BuilderExport").TreeCheckList.BuildTree;

        private static List<string> Selected(List<TreeNodeViewModel> roots) =>
            roots.SelectMany(section => section.Children)
                .Where(page => page.CheckState == NodeCheckState.Checked)
                .SelectMany(page => page.Children.Where(tab => tab.CheckState == NodeCheckState.Checked).Select(tab => tab.Id).Prepend(page.Id))
                .ToList();

        [Fact]
        public void ACustomersSelection_ComesBackAsSaved()
        {
            var all = Tree(_db.Services, 0);
            var saved = Selected(all.Where(section => section.Id != "Sales").ToList());
            Assert.Contains("HrLists", saved);
            Assert.Contains("Departments", saved);

            var id = _db.Services.GetRequiredService<ILicenseRepository>()
                .Insert(new License { CustomerName = "اختبار", Serial = "TEST-SERIAL", Manifest = string.Join(",", saved) });

            var reloaded = Tree(_db.Services, id);

            Assert.Equal(saved.OrderBy(k => k), Selected(reloaded).OrderBy(k => k));
            Assert.DoesNotContain(Selected(reloaded), key => all.Single(s => s.Id == "Sales").Children.Any(page => page.Id == key));
        }

        [Fact]
        public void TheTree_HasNoDuplicateNodes()
        {
            var ids = Tree(_db.Services, 0).SelectMany(section => section.Children)
                .SelectMany(page => page.Children.Select(tab => tab.Id).Prepend(page.Id)).ToList();

            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
    }
}
