using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ScriptWrapper;
using TestVisx;
using Task = System.Threading.Tasks.Task;

namespace IntelliFind
{

    /// <summary>
    /// Interaction logic for IntelliFindControl.
    /// </summary>
    public partial class IntelliFindControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntelliFindControl"/> class.
        /// </summary>
        public IntelliFindControl()
        {
            this.InitializeComponent();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSearch();
        }
        private async void TextBoxInput_KeyUp(object sender, KeyEventArgs e)
        {
            // Handle Ctr+Enter for Search
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                await ExecuteSearch();
            }
        }

        private async Task ExecuteSearch()
        {
            object scriptResult;
            try
            {
                scriptResult = await CSharpScript.EvaluateAsync<object>(TextBoxInput.Text, new ScriptGlobals());
            }
            catch (Exception ex)
            {
                scriptResult = ex;
            }

            DisplayResult(scriptResult);
        }

        private void DisplayResult(object scriptResult)
        {
            ListViewResults.Items.Clear();

            foreach (var item in MakeEnumerable(scriptResult))
            {
                var listviewItem = CreateListViewItem(item);
                ListViewResults.Items.Add(listviewItem);
            }
        }

        private ListViewItem CreateListViewItem(object item)
        {

            var syntaxNodeorToken = AsSyntaxNodeOrToken(item);
            if (syntaxNodeorToken != null)
            {
                var lineSpan = syntaxNodeorToken.Value.GetLocation().GetLineSpan();
                var listviewItem = new ListViewItem
                {
                    Content = $"{lineSpan}: {syntaxNodeorToken.Value.Kind()} {Truncate(syntaxNodeorToken.ToString())}",
                    ToolTip = syntaxNodeorToken.ToString(),
                };

                listviewItem.MouseDoubleClick += (o, args) => { SelectSpanInCodeWindow(lineSpan); };
                return listviewItem;
            }
            else
            {
                return new ListViewItem() { Content = item?.ToString() ?? "<null>" };
            }
        }

        private SyntaxNodeOrToken? AsSyntaxNodeOrToken(object item)
        {
            if (item is SyntaxNode)
                return (SyntaxNode)item;

            if (item is SyntaxToken)
                return (SyntaxToken)item;

            return item as SyntaxNodeOrToken?;
        }


        public static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value)) { return value; }

            return value.Substring(0, Math.Min(value.Length, 30)).Replace("\n", string.Empty).Replace("\r", string.Empty);
        }


        private IEnumerable MakeEnumerable(object input)
        {
            // If the result is Enumerable we return it, o
            var enumerable = input as IEnumerable;
            if (enumerable != null && !(input is string))
            {
                return enumerable;
            }
            else
            {
                return new[] { input };
            }
        }

        private void SelectSpanInCodeWindow(FileLinePositionSpan span)
        {
            // If the path is not avalable we cannot jump to it
            if (string.IsNullOrEmpty(span.Path)) return;

            // Check if the document is opened, if not open it.
            IVsUIHierarchy hierarchy;
            uint itemId;
            IVsWindowFrame windowFrame;
            if (!VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, span.Path, VSConstants.LOGVIEWID_Any, out hierarchy, out itemId, out windowFrame))
            {
                VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, span.Path, VSConstants.LOGVIEWID_Primary, out hierarchy, out itemId, out windowFrame);
            }

            var window = VsShellUtilities.GetWindowObject(windowFrame);
            window.SetFocus();

            var textView = VsShellUtilities.GetTextView(windowFrame);
            textView.SetSelection(span.StartLinePosition.Line, span.StartLinePosition.Character, span.EndLinePosition.Line, span.EndLinePosition.Character);
        }
    }
}