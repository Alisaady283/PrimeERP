using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.ViewModels;

namespace PrimeERP.Modules
{
    /// <summary>يسجّل كل وحدة عمل فعلية في IModuleRegistry — إثبات شرط إغلاق R8: وحدتان بالتكوين، الثانية
    /// (Suppliers) أقصر من الأولى (Customers) لأنها تعيد استخدام نفس التعريف بالكامل.</summary>
    public static class ModuleRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            registry.Register(new ModuleDefinition
            {
                Key = "Customers",
                TitleKey = "Str.Module.Customers",
                PermissionPrefix = "Customers",
                ViewModelType = typeof(CustomersViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(CustomerDto.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(CustomerDto.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Phone"), Binding = nameof(CustomerDto.Phone), Width = 130 },
                    new() { Header = LocalizationService.Get("Str.Balance"), Binding = nameof(CustomerDto.Balance), Width = 120, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Suppliers",
                TitleKey = "Str.Module.Suppliers",
                PermissionPrefix = "Suppliers",
                ViewModelType = typeof(SuppliersViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(SupplierDto.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(SupplierDto.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Phone"), Binding = nameof(SupplierDto.Phone), Width = 130 },
                }
            });
        }
    }
}
