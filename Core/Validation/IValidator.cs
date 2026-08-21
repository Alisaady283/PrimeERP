namespace PrimeERP.Core.Validation
{
    public interface IValidator<T>
    {
        ValidationResult Validate(T item);
    }
}
