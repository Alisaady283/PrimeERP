using System.Collections.Generic;
using System.ComponentModel;
using PrimeERP.UI.ViewModels;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>سطر مستند مرن</summary>
    public class DocumentLine : BaseViewModel, IEditableObject
    {
        public Dictionary<string, object> Values { get; } = new();

        public object this[string key]
        {
            get => Values.TryGetValue(key, out var v) ? v : null;
            set => SetValue(key, value);
        }

        public DocumentLine Self => this;

        public int LineNo
        {
            get => GetInt(nameof(LineNo));
            set => SetValue(nameof(LineNo), value);
        }

        public int?    ItemId   { get => GetIntNullable(nameof(ItemId)); set => SetValue(nameof(ItemId), value); }
        public string  ItemCode { get => GetString(nameof(ItemCode));    set => SetValue(nameof(ItemCode), value); }
        public string  ItemName { get => GetString(nameof(ItemName));    set => SetValue(nameof(ItemName), value); }
        public decimal Qty      { get => GetDecimal(nameof(Qty));        set => SetValue(nameof(Qty), value); }
        public int?    UnitId   { get => GetIntNullable(nameof(UnitId)); set => SetValue(nameof(UnitId), value); }
        public string  UnitName { get => GetString(nameof(UnitName));    set => SetValue(nameof(UnitName), value); }

        public decimal Price           { get => GetDecimal(nameof(Price));           set => SetValue(nameof(Price), value); }
        public decimal DiscountPercent { get => GetDecimal(nameof(DiscountPercent)); set => SetValue(nameof(DiscountPercent), value); }
        public decimal DiscountAmount  { get => GetDecimal(nameof(DiscountAmount));  set => SetValue(nameof(DiscountAmount), value); }
        public decimal VatPercent      { get => GetDecimal(nameof(VatPercent));      set => SetValue(nameof(VatPercent), value); }
        public decimal VatAmount       { get => GetDecimal(nameof(VatAmount));       set => SetValue(nameof(VatAmount), value); }

        public decimal Debit     { get => GetDecimal(nameof(Debit));     set => SetValue(nameof(Debit), value); }
        public decimal Credit    { get => GetDecimal(nameof(Credit));    set => SetValue(nameof(Credit), value); }
        public decimal LineTotal { get => GetDecimal(nameof(LineTotal)); set => SetValue(nameof(LineTotal), value); }
        public string  Notes     { get => GetString(nameof(Notes));      set => SetValue(nameof(Notes), value); }

        public bool IsEmpty =>
            string.IsNullOrEmpty(ItemCode) && Qty == 0 && Debit == 0 && Credit == 0 && string.IsNullOrEmpty(Notes);

        public bool IsValid => Errors.Count == 0;

        public Dictionary<string, string> Errors { get; } = new();

        public void NotifyErrorsChanged() => OnPropertyChanged(nameof(Errors));

        public DocumentLine Clone()
        {
            var clone = new DocumentLine();
            foreach (var kv in Values)
                clone.Values[kv.Key] = kv.Value;
            return clone;
        }


        private Dictionary<string, object> _snapshot;

        void IEditableObject.BeginEdit() => _snapshot ??= new Dictionary<string, object>(Values);

        void IEditableObject.CancelEdit()
        {
            if (_snapshot == null) return;
            Values.Clear();
            foreach (var kv in _snapshot) Values[kv.Key] = kv.Value;
            _snapshot = null;
            OnPropertyChanged(string.Empty);
            NotifyErrorsChanged();
        }

        void IEditableObject.EndEdit() => _snapshot = null;

        private void SetValue(string key, object value)
        {
            Values[key] = value;
            OnPropertyChanged(key);
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(string.Empty);
        }

        private string GetString(string key) => this[key] as string ?? "";

        private int GetInt(string key) => this[key] switch
        {
            int i => i,
            decimal d => (int)d,
            null => 0,
            _ => 0
        };

        private int? GetIntNullable(string key) => this[key] switch
        {
            int i => i,
            decimal d => (int)d,
            _ => null
        };

        private decimal GetDecimal(string key) => this[key] switch
        {
            decimal d => d,
            int i => i,
            double db => (decimal)db,
            _ => 0m
        };
    }
}
