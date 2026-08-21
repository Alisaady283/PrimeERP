namespace PrimeERP.Services
{
    public interface IToastService
    {
        void Success(string message, int durationMs = 3000);

        /// <summary>يبقى ظاهراً حتى يُغلق المستخدم يدوياً — لا يختفي تلقائياً.</summary>
        void Error(string message);

        void Warning(string message, int durationMs = 4000);
        void Info(string message, int durationMs = 3000);
    }
}
