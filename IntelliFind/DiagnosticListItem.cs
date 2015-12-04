using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis;

namespace IntelliFind
{
    /// <summary>
    /// Displays a Diagnostic in a ListView and enables jumping to the source location
    /// </summary>
    class DiagnosticListItem : ListViewItem
    {
        private readonly Diagnostic _diagnostic;
        private readonly TextBox _codeTextBox;

        public DiagnosticListItem(Diagnostic diagnostic, TextBox codeTextBox)
        {
            _diagnostic = diagnostic;
            _codeTextBox = codeTextBox;
            Content = diagnostic.ToString();
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            // set the caret to the position of the diagnostic
            _codeTextBox.CaretIndex = _diagnostic.Location.SourceSpan.Start;
            _codeTextBox.ScrollToLine(_diagnostic.Location.GetLineSpan().StartLinePosition.Line);
            _codeTextBox.Focus();
            e.Handled = true;

            base.OnMouseDoubleClick(e);
        }
    }
}