using System;
using System.Collections.Generic;

namespace PrimeERP.Views.Controls.Tree
{
    /// <summary>
    /// فلترة إخفاء حقيقية على شجرة TreeNodeViewModel (لا مجرد توسيع).
    /// القواعد:
    ///   - العقدة تظهر لو مطابقة، أو لو أي ابن لها مطابق (مباشر أو غير مباشر)
    ///   - الآباء المؤدية لمطابقة تظهر وتتوسع تلقائياً أثناء البحث
    ///   - غير المطابقة وبلا أبناء مطابقين تختفي تماماً (لا تدخل VisibleChildren لأبيها)
    ///   - المطابقة تُميّز عبر IsMatch (تلوين في القالب)
    ///   - بحث فارغ: كل العقد تظهر، وحالة التوسّع تبقى كما كانت (لا تُفرض ولا تُلغى)
    /// </summary>
    public static class TreeFilterEngine
    {
        public static void Apply(IEnumerable<TreeNodeViewModel> roots, string search,
                                 Func<TreeNodeViewModel, bool> extraFilter = null)
        {
            var term = (search ?? "").Trim();
            var hasSearch = term.Length > 0;

            foreach (var root in roots)
                ApplyNode(root, term, hasSearch, extraFilter);
        }

        private static bool ApplyNode(TreeNodeViewModel node, string term, bool hasSearch,
                                      Func<TreeNodeViewModel, bool> extraFilter)
        {
            bool passesExtra   = extraFilter == null || extraFilter(node);
            bool selfTextMatch = !hasSearch || MatchesText(node, term);

            bool anyChildVisible = false;
            node.VisibleChildren.Clear();

            foreach (var child in node.Children)
            {
                if (ApplyNode(child, term, hasSearch, extraFilter))
                {
                    node.VisibleChildren.Add(child);
                    anyChildVisible = true;
                }
            }

            node.IsMatch   = hasSearch && selfTextMatch && passesExtra;
            node.IsVisible = (passesExtra && selfTextMatch) || anyChildVisible;

            if (hasSearch && anyChildVisible)
                node.IsExpanded = true;
            // بحث فارغ: لا نلمس IsExpanded — يبقى بحالته السابقة كما هو (الحالة الأصلية)

            return node.IsVisible;
        }

        private static bool MatchesText(TreeNodeViewModel node, string term) =>
            Contains(node.Name, term) || Contains(node.Code, term) || Contains(node.DisplayText, term);

        private static bool Contains(string source, string term) =>
            !string.IsNullOrEmpty(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
