using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrimeERP.Application.Legacy.Print
{
    /// <summary>ترميز Code128-B</summary>
    public static class Code128
    {
        private const int StartB = 104, Stop = 106;

        private static readonly string[] Patterns =
        {
            "212222","222122","222221","121223","121322","131222","122213","122312","132212","221213",
            "221312","231212","112232","122132","122231","113222","123122","123221","223211","221132",
            "221231","213212","223112","312131","311222","321122","321221","312212","322112","322211",
            "212123","212321","232121","111323","131123","131321","112313","132113","132311","211313",
            "231113","231311","112133","112331","132131","113123","113321","133121","313121","211331",
            "231131","213113","213311","213131","311123","311321","331121","312113","312311","332111",
            "314111","221411","431111","111224","111422","121124","121421","141122","141221","112214",
            "112412","122114","122411","142112","142211","241211","221114","413111","241112","134111",
            "111242","121142","121241","114212","124112","124211","411212","421112","421211","212141",
            "214121","412121","111143","111341","131141","114113","114311","411113","411311","113141",
            "114131","311141","411131","211412","211214","211232","2331112"
        };

        public static IReadOnlyList<int> Encode(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Any(c => c < 32 || c > 126)) return Array.Empty<int>();

            var codes = new List<int> { StartB };
            codes.AddRange(text.Select(c => c - 32));

            var checksum = StartB;
            for (var i = 0; i < text.Length; i++) checksum += (text[i] - 32) * (i + 1);

            codes.Add(checksum % 103);
            codes.Add(Stop);

            var widths = new List<int>();
            foreach (var code in codes) widths.AddRange(Patterns[code].Select(d => d - '0'));

            return widths;
        }
    }
}
