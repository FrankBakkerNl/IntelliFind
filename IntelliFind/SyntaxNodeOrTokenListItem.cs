using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IntelliFind
{
    class SyntaxNodeOrTokenListItem : ListViewItem
    {
        readonly FileLinePositionSpan _lineSpan;

        public SyntaxNodeOrTokenListItem(SyntaxNodeOrToken syntaxNodeOrToken)
        {
            _lineSpan = syntaxNodeOrToken.GetLocation().GetLineSpan();

            Content = $"{_lineSpan}: {syntaxNodeOrToken.Kind()} {Truncate(syntaxNodeOrToken.ToString())}";
            ToolTip = syntaxNodeOrToken.ToString();
        }

        private static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value)) { return value; }

            return value.Substring(0, Math.Min(value.Length, 30)).Replace("\n", String.Empty).Replace("\r", String.Empty);
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            RoslynVisxHelpers.SelectSpanInCodeWindow(_lineSpan);
            e.Handled = true;
            base.OnMouseDoubleClick(e);
        }
    }
}
