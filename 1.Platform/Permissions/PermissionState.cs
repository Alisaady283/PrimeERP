namespace PrimeERP.Platform.Permissions
{
    /// <summary>حالة مفتاح صلاحية لمستخدم بعينه — الحجب الصريح يتفوّق دائماً على منح الدور.</summary>
    public enum PermissionState
    {
        Inherited,
        Granted,
        Revoked
    }
}
