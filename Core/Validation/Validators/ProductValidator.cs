using PrimeERP.Models;

namespace PrimeERP.Core.Validation.Validators
{
    public class ProductValidator : ValidatorBase, IValidator<Product>
    {
        public ValidationResult Validate(Product product)
        {
            var result = new ValidationResult();

            Required(result, "Code", product.Code, "كود المنتج");
            Required(result, "Name", product.Name, "اسم المنتج");
            MaxLength(result, "Name", product.Name, 200, "اسم المنتج");
            Positive(result, "CostPrice", product.CostPrice, "سعر التكلفة");
            Positive(result, "SalePrice", product.SalePrice, "سعر البيع");

            Custom(result, "SalePrice",
                product.MinPrice == 0 || product.SalePrice >= product.MinPrice,
                "سعر البيع لا يمكن أن يقل عن الحد الأدنى المسموح به");

            return result;
        }
    }
}
