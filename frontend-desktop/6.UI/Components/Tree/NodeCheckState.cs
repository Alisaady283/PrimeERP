namespace PrimeERP.UI.Components.Tree
{
    /// <summary>حالة تحديد عقدة في شجرة</summary>
    public enum NodeCheckState
    {
        Unchecked,
        Checked,
        Inherited,
        Granted,
        Revoked
    }

    public enum TreeCheckMode
    {
        None,
        TwoState,
        ThreeState
    }
}
