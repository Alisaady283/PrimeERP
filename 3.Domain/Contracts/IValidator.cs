namespace PrimeERP.Domain.Contracts
{
    public interface IValidator<T>
    {
        ValidationResult Validate(T item);
    }
}
