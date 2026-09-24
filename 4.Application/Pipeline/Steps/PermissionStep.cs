using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Application.Pipeline.Steps
{
    /// <summary>خطوة فحص الصلاحية</summary>
    public class PermissionStep : IStep
    {
        private readonly IPermissionService _permissions;
        private readonly string _key;
        private readonly string _deniedMessage;

        public string Name => $"Permission:{_key}";

        public PermissionStep(IPermissionService permissions, string key, string deniedMessage)
        {
            _permissions = permissions;
            _key = key;
            _deniedMessage = deniedMessage;
        }

        public Result Execute(PipelineContext ctx) =>
            _permissions.Can(_key) ? Result.Ok() : Result.Fail(_deniedMessage, ErrorCode.Unauthorized);
    }
}
