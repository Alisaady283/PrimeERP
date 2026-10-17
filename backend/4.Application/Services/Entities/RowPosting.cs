using PrimeERP.Data.Core;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Entities
{
    /// <summary>
    /// صفُّ قائمةٍ يحمل قيده (ساعات مشروع، مستخلص): يُحسب ويُقيَّد عند الحفظ في معاملته، ويُعكس قيده عند التعديل والحذف.
    /// Lookup يستدعيه إن سُجّل لكيانه، فلا خدمة صفحةٍ خاصة لكل حركة.
    /// </summary>
    public interface IRowPosting<in TEntity>
    {
        /// <summary>يُكمل الصفّ بما يُحسب (التكلفة، الضمان، الصافي) ويتحقّق قبل الحفظ</summary>
        Result Prepare(TEntity entity);
        /// <summary>قيد الصفّ قابلٌ للعكس</summary>
        Result CanReverse(TEntity entity);
        /// <summary>يُعكس قيد الصفّ المحفوظ (stored) إن وُجد، ويُقيَّد الجديد ويُكتب رقمه على الصفّ</summary>
        Result Post(PrimeDbContext db, TEntity entity, TEntity stored);
        void Reverse(PrimeDbContext db, TEntity entity);
    }
}
