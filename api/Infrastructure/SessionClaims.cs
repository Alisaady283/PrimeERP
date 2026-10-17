namespace PrimeERP.Api.Infrastructure
{
    /// <summary>أسماء مطالبات رمز الدخول — هويةٌ فقط، والصلاحيات تُقرأ من القاعدة في كل طلب</summary>
    public static class SessionClaims
    {
        public const string UserId = "sub";
        public const string Username = "name";
        public const string DisplayName = "display";
        public const string RoleId = "role_id";
        public const string RoleName = "role";
        public const string BranchId = "branch";

        /// <summary>هوية المستخدم مطالباتٍ — رمز الدخول ومفتاح الربط من هنا وحده</summary>
        public static Dictionary<string, object> Of(PrimeERP.Application.PageServices.Security.SignedInUser user) => new()
        {
            [UserId] = user.Id.ToString(),
            [Username] = user.Username ?? "",
            [DisplayName] = user.DisplayName ?? "",
            [RoleId] = user.RoleId.ToString(),
            [RoleName] = user.RoleName ?? "",
            [BranchId] = user.BranchId?.ToString() ?? "",
        };
    }
}
