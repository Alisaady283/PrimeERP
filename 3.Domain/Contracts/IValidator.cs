namespace PrimeERP.Domain.Contracts
{
    /// <summary>عقد Validator</summary>
    public interface IValidator<T>
    {
        ValidationResult Validate(T item);
    }
}
