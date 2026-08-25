using PrimeERP.Domain.Results;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Pipeline.Steps
{
    /// <summary>يقرأ إعداداً ويحفظه في ctx.Items[contextKey] — تلتقطه خطوات لاحقة (RuleStep/FuncStep) بلا وصول مباشر لـ ISettingsService.</summary>
    public class SettingStep<T> : IStep
    {
        private readonly ISettingsService _settings;
        private readonly string _key;
        private readonly string _contextKey;
        private readonly T _default;

        public string Name => $"Setting:{_key}";

        public SettingStep(ISettingsService settings, string key, string contextKey, T defaultValue = default)
        {
            _settings = settings;
            _key = key;
            _contextKey = contextKey;
            _default = defaultValue;
        }

        public Result Execute(PipelineContext ctx)
        {
            ctx.Items[_contextKey] = _settings.Get(_key, _default);
            return Result.Ok();
        }
    }
}
