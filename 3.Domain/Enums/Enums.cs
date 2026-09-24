namespace PrimeERP.Domain.Enums
{
    /// <summary>تعدادات النظام</summary>
    public enum AccountType
    {
        Asset     = 1,
        Liability = 2,
        Equity    = 3,
        Revenue   = 4,
        Expense   = 5
    }

    public enum InvoiceStatus
    {
        Draft     = 1,
        Confirmed = 2,
        Cancelled = 3
    }

    public enum InvoiceType
    {
        Sales    = 1,
        Purchase = 2,
        Return   = 3
    }

    public enum VoucherType
    {
        StockIn   = 1,
        StockOut  = 2,
        Transfer  = 3
    }

    public enum PaymentMethod
    {
        Cash   = 1,
        Bank   = 2,
        Credit = 3,
        Cheque = 4
    }

    public enum JournalSource
    {
        Manual    = 1,
        Sales     = 2,
        Purchase  = 3,
        Payroll   = 4,
        Other     = 5
    }

    public enum UserRole
    {
        Admin      = 1,
        Manager    = 2,
        Accountant = 3,
        Cashier    = 4,
        Warehouse  = 5
    }

    public enum EmployeeStatus
    {
        Active   = 1,
        Inactive = 2,
        OnLeave  = 3
    }

    public enum AuditAction
    {
        Insert = 1,
        Update = 2,
        Delete = 3
    }

    public enum PageType
    {
        Dashboard  = 1,
        Accounts   = 2,
        Customers  = 3,
        Suppliers  = 4,
        Products   = 5,
        Warehouse  = 6,
        Sales      = 7,
        Purchases  = 8,
        Journal    = 9,
        HR         = 10,
        Reports    = 11,
        Settings   = 12
    }

    /// <summary>طريقة التكلفة</summary>
    public enum CostMethod
    {
        WeightedAverage = 2
    }

    public enum TreasuryKind
    {
        Cash = 1,
        Bank = 2
    }

    public enum VoucherKind
    {
        Receipt = 1,
        Payment = 2
    }

    public enum PartyKind
    {
        Customer = 1,
        Supplier = 2,
        Other    = 3
    }

    public enum ChequeDirection
    {
        Incoming = 1,
        Outgoing = 2
    }

    /// <summary>حالات الشيك</summary>
    public enum ChequeStatus
    {
        InHand    = 1,
        Deposited = 2,
        Collected = 3,
        Bounced   = 4,
        Returned  = 5,
        Issued    = 6,
        Paid      = 7
    }

    public enum MovementType
    {
        In         = 1,
        Out        = 2,
        Transfer   = 3,
        Adjustment = 4
    }

    public enum SupplierType
    {
        Local  = 1,
        Foreign = 2
    }

    public enum BackupType
    {
        Manual     = 1,
        Auto       = 2,
        PreRestore = 3
    }

    /// <summary>نوع الوحدة المبنيّة</summary>
    public enum BuilderKind
    {
        Report   = 0,
        Record   = 1,
        Movement = 2
    }

    /// <summary>نوع بيانات عمود مبنيّ</summary>
    public enum BuilderDataType
    {
        Text     = 0,
        Number   = 1,
        Money    = 2,
        Date     = 3,
        Bool     = 4,
        LongText = 5,
        Reference = 6
    }

    /// <summary>تجميع العمود المحسوب من جدول</summary>
    public enum BuilderAggregate
    {
        None  = 0,
        Sum   = 1,
        Count = 2,
        Avg   = 3,
        Min   = 4,
        Max   = 5
    }

    /// <summary>طريقة اقتناء الأصل</summary>
    public enum AssetAcquisition
    {
        Cash = 1,
        Bank = 2,
        Supplier = 3
    }

    public enum LinkedEntityType { None, Customer, Supplier }
}
