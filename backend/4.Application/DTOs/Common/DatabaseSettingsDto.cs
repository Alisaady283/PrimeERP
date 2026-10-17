namespace PrimeERP.Application.DTOs.Common
{
    /// <summary>قاعدة البيانات التي يعمل عليها البرنامج: نوعها، ومجلد ملفها واسمه، أو خادمها</summary>
    public class DatabaseSettingsDto
    {
        public int    Provider { get; set; }
        public string Folder   { get; set; }
        public string FileName { get; set; }
        public string Host     { get; set; }
        public int    Port     { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool   UseIntegratedSecurity { get; set; } = true;
    }
}
