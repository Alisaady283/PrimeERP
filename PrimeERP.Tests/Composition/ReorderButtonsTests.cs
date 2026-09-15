using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Design;
using PrimeERP.UI.Components.Actions;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>زرّ الشريط يُبنى بأيقونته من موارد التطبيق — أيقونةٌ لا تُحلّ تعني زرّاً بلا رسم.</summary>
    public class ReorderButtonsTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void EveryCatalogueAction_ResolvesItsIcon()
        {
            WpfApplicationFixture.Run(() =>
            {
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var missing = ToolbarAction.Catalogue
                    .Where(entry => System.Windows.Application.Current.TryFindResource(entry.Value.IconKey) == null)
                    .Select(entry => $"{entry.Key} -> {entry.Value.IconKey}")
                    .ToList();

                Assert.True(missing.Count == 0, "أيقونات لا يجدها التطبيق: " + string.Join(", ", missing));
            });
        }

        [Theory]
        [InlineData("moveUp")]
        [InlineData("moveDown")]
        public void TheArrow_CarriesTextAndGeometry(string key)
        {
            WpfApplicationFixture.Run(() =>
            {
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                var action = key == "moveUp"
                    ? ToolbarAction.MoveUp(null, "BuilderColumns.Edit")
                    : ToolbarAction.MoveDown(null, "BuilderColumns.Edit");

                Assert.False(string.IsNullOrWhiteSpace(action.Text), "الزرّ بلا نص");
                Assert.NotNull(action.Icon);
            });
        }
    }
}
