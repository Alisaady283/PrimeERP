using System.Linq;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    public class FlowScopeTests
    {
        private static ModuleDefinition Module(string key, FlowScope scope) =>
            new() { Key = key, TitleKey = key, PermissionPrefix = "X", FlowScope = scope };

        private static IModuleRegistry Registry()
        {
            var registry = new ModuleRegistry();
            registry.Register(Module("SalesInvoices", FlowScope.Both));
            registry.Register(Module("StockIn", FlowScope.SimplifiedOnly));
            registry.Register(Module("PurchaseOrder", FlowScope.FullCycleOnly));
            return registry;
        }

        [Fact]
        public void SimplifiedMode_HidesCycleDocuments_KeepsStandaloneVouchers()
        {
            var keys = Registry().VisibleFor(simplifiedFlow: true).Select(m => m.Key).ToList();

            Assert.Contains("SalesInvoices", keys);
            Assert.Contains("StockIn", keys);
            Assert.DoesNotContain("PurchaseOrder", keys);
        }

        [Fact]
        public void FullCycleMode_ShowsCycleDocuments_HidesStandaloneVouchers()
        {
            var keys = Registry().VisibleFor(simplifiedFlow: false).Select(m => m.Key).ToList();

            Assert.Contains("SalesInvoices", keys);
            Assert.Contains("PurchaseOrder", keys);
            Assert.DoesNotContain("StockIn", keys);
        }

        [Fact]
        public void ModulesDefaultToBothModes()
        {
            var registry = new ModuleRegistry();
            registry.Register(new ModuleDefinition { Key = "Customers", TitleKey = "x", PermissionPrefix = "Customers" });

            Assert.Contains("Customers", registry.VisibleFor(true).Select(m => m.Key));
            Assert.Contains("Customers", registry.VisibleFor(false).Select(m => m.Key));
        }
    }
}
