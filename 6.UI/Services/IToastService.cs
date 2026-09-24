namespace PrimeERP.UI.Services
{
    /// <summary>خدمة واجهة Toast</summary>
    public interface IToastService
    {
        void Success(string message, int durationMs = 3000);

        void Error(string message);

        void Warning(string message, int durationMs = 4000);
        void Info(string message, int durationMs = 3000);
    }
}
