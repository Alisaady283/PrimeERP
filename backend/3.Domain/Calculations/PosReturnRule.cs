using System;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>ما يُرتجَع في نقطة البيع: مفتوح، أو من الوردية الحالية، أو مما باعه الكاشير نفسه، أو خلال ساعاتٍ من البيع</summary>
    public static class PosReturnRule
    {
        public const string Open = "Open", Shift = "Shift", Cashier = "Cashier", Hours = "Hours";

        public static readonly string[] Policies = { Open, Shift, Cashier, Hours };

        public static bool Allows(string policy, int hours, DateTime soldAt, int? soldShift, int? soldBy, int? currentShift, int currentUser, DateTime now) =>
            policy switch
            {
                Shift   => soldShift != null && soldShift == currentShift,
                Cashier => soldBy != null && soldBy == currentUser,
                Hours   => now - soldAt <= TimeSpan.FromHours(hours),
                _       => true,
            };
    }
}
