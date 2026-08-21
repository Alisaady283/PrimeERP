using Xunit;

// الخدمات كلها Singletons ثابتة (AppSession.Permissions، ServiceLocator، PrintService._theme...) — تعطيل
// التوازي بين فئات الاختبار يمنع تسابقاً حقيقياً حين يُعدِّل اختبار حالة مشتركة مؤقتاً (مثال: AppSession.DevMode).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
