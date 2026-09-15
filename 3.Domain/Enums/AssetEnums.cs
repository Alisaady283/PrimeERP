namespace PrimeERP.Domain.Enums
{
    /// <summary>
    /// طريقة اقتناء الأصل — تحكم أي قائمةٍ تُعرَض للمموّل وأي حسابٍ يُدان به. قيمتا الخزينة والبنك
    /// تطابقان <see cref="TreasuryKind"/> عمداً، فتصلح الطريقةُ مرشِّحاً مباشراً لقائمة الخزائن.
    /// </summary>
    public enum AssetAcquisition
    {
        Cash = 1,
        Bank = 2,
        Supplier = 3
    }
}
