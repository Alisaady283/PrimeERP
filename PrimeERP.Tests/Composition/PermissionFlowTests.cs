using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Tree;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>
    /// دورة الصلاحيات كما يعيشها المستخدم: يفتح الشاشة فيرى ما هو ممنوح، يمنح، يحفظ، يدخل المستخدم
    /// فيجد أقسامه. كل حلقة مكسورة هنا تظهر كسايد بار فارغ أو شجرة بلا تحديد.
    /// </summary>
    public class PermissionFlowTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public PermissionFlowTests() => AppSession.DevMode = false;

        public void Dispose()
        {
            AppSession.DevMode = true;
            _db.Dispose();
        }

        private TreeCheckListDefinition RoleScreen() =>
            _db.Services.GetRequiredService<IModuleRegistry>().Get("RolePermissions").TreeCheckList;

        /// <summary>
        /// أذون الدورة الأربعة بدورٍ حقيقي لا بوضع التطوير. كانت بوّابتها تسأل عن مفاتيح غير مُعرَّفة في
        /// PermissionKeys، فتُرفض عند كل دور مهما مُنح — وهو ما ظهر للمستخدم «ليس لديك الصلاحية».
        /// </summary>
        [Theory]
        [InlineData("GoodsReceipt")]
        [InlineData("GoodsIssue")]
        [InlineData("DeliveryNote")]
        [InlineData("SalesReceipt")]
        public void AStockVoucher_OpensForARoleGrantedIt(string moduleKey)
        {
            var roleId = PermissionDb.InsertRole($"Keeper{moduleKey}", "أمين مخزن");
            var userId = PermissionDb.InsertUser($"keeper{moduleKey}", "p", "أمين", roleId, true);

            // يُمنَح ما تعرضه شاشة الأدوار فعلاً لا ما نكتبه نحن: مفتاحٌ غائب عن PermissionKeys لا يظهر
            // فيها فلا يستطيع أحدٌ منحه — وهو جوهر العطب. منحُه نصّاً هنا كان سيُخفيه.
            PermissionDb.ReplaceRolePermissions(roleId,
                PermissionKeys.All().Where(key => key.StartsWith("Inventory.")).ToList());
            _db.Services.GetRequiredService<IPermissionService>().LoadForUser(userId);

            var serviceType = _db.Services.GetRequiredService<IModuleRegistry>()
                .Get(moduleKey).DocumentDialog.ServiceType;
            var service = (dynamic)_db.Services.GetRequiredService(serviceType);

            var result = service.GetPaged(1, 20, null);

            Assert.True(result.IsSuccess, $"{moduleKey}: {result.ErrorMessage}");
            Assert.NotEqual(PrimeERP.Domain.Results.ErrorCode.Unauthorized, result.ErrorCode);
        }

        /// <summary>المفتاح يظهر في شجرة الأدوار — بلا ظهوره لا سبيل لمنحه من الشاشة.</summary>
        [Theory]
        [InlineData("Inventory.GoodsReceipt")]
        [InlineData("Inventory.GoodsIssue")]
        [InlineData("Inventory.DeliveryNote")]
        [InlineData("Inventory.SalesReceipt")]
        public void AStockVoucherKey_AppearsInTheRoleScreen(string key)
        {
            var roleId = PermissionDb.InsertRole($"Empty{key}", "فارغ");

            var ids = PermissionTreeFactory.KeyNodes(RoleScreen().BuildTree(_db.Services, roleId))
                .Select(node => node.Id).ToList();

            Assert.Contains(key, ids);
        }

        [Fact]
        public void ARoleWithPermissions_OpensWithThemChecked()
        {
            var roleId = PermissionDb.InsertRole("Full", "كامل");
            PermissionDb.ReplaceRolePermissions(roleId, PermissionKeys.All());

            var keys = PermissionTreeFactory.KeyNodes(RoleScreen().BuildTree(_db.Services, roleId)).ToList();

            Assert.NotEmpty(keys);
            Assert.All(keys, node => Assert.Equal(NodeCheckState.Checked, node.CheckState));
        }

        [Fact]
        public void CheckingAnActionWithoutView_StillOpensTheSection()
        {
            var roleId = PermissionDb.InsertRole("Printer", "طابع");
            var userId = PermissionDb.InsertUser("printer", "p", "طابع", roleId, true);

            var screen = RoleScreen();
            var nodes = screen.BuildTree(_db.Services, roleId);

            // طباعة بلا عرض: القسم محجوب فلا شيء يُطبع — العرض بوّابة لباقي الإجراءات.
            foreach (var node in PermissionTreeFactory.KeyNodes(nodes))
                if (node.Id == "Sales.Print") node.CheckState = NodeCheckState.Checked;

            Assert.True(screen.Save(_db.Services, roleId, nodes).Result.IsSuccess);

            var permissions = _db.Services.GetRequiredService<IPermissionService>();
            permissions.LoadForUser(userId);

            Assert.True(permissions.Can("Sales.Print"));
            Assert.True(permissions.Can("Sales.View"), "الطباعة مُنحت والعرض لم يلحق بها");

            // ولا تتسرّب لوحدة أخرى.
            Assert.False(permissions.Can("Purchases.View"));
        }

        [Fact]
        public void GrantingARole_Persists_AndReachesTheUsersSession()
        {
            var roleId = PermissionDb.InsertRole("Assistant", "مساعد");
            var userId = PermissionDb.InsertUser("rehab", "rehab", "رحاب", roleId, true);

            var screen = RoleScreen();
            var nodes = screen.BuildTree(_db.Services, roleId);

            // ما فعله المستخدم بالضبط: عرض قيود اليومية + قسم المبيعات كاملاً.
            foreach (var node in PermissionTreeFactory.KeyNodes(nodes))
                if (node.Id == "Journal.View" || node.Id.StartsWith("Sales."))
                    node.CheckState = NodeCheckState.Checked;

            Assert.True(screen.Save(_db.Services, roleId, nodes).Result.IsSuccess);

            // ١) تُقرأ عند إعادة فتح الشاشة
            var reopened = PermissionTreeFactory.KeyNodes(screen.BuildTree(_db.Services, roleId))
                .Where(n => n.CheckState == NodeCheckState.Checked)
                .Select(n => n.Id).ToHashSet();

            Assert.Contains("Journal.View", reopened);
            Assert.Contains("Sales.View", reopened);

            // ٢) تصل جلسة المستخدم بعد تسجيل الدخول
            var permissions = _db.Services.GetRequiredService<IPermissionService>();
            permissions.LoadForUser(userId);

            Assert.True(permissions.Can("Journal.View"), "قيود اليومية محجوبة رغم منحها");
            Assert.True(permissions.Can("Sales.View"), "المبيعات محجوبة رغم منحها");
            Assert.False(permissions.Can("Settings.System"), "صلاحية غير ممنوحة صارت مسموحة");
        }
    }
}
