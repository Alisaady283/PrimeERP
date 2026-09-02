namespace PrimeERP.UI.Components.Tree
{
    /// <summary>حالة تحديد عقدة في شجرة قابلة للتأشير. TwoState يستخدم Unchecked/Checked فقط،
    /// وThreeState يستخدم Inherited/Granted/Revoked.</summary>
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
