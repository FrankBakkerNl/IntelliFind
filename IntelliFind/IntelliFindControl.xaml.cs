using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ScriptWrapper;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IntelliFind.ScriptContext;

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
            InitializeComponent();
            LoadGlobals();
        }

        private void LoadGlobals()
        {
            var globalProperties = typeof (ScriptGlobals).GetProperties().Select(p => p.Name);
            GlobalsComboBox.Items.Clear();
            foreach (var property in globalProperties)
            {
                GlobalsComboBox.Items.Add(property);
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSearch();
        }
        private async void IntelliFindControl_KeyUp(object sender, KeyEventArgs e)
        {
            // Handle Ctr+Enter for Search
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                await ExecuteSearch();
            }
        }

        private void GlobalsComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combobox = (ComboBox)sender;
            TextBoxInput.SelectedText = combobox.SelectedItem.ToString();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }

        private CancellationTokenSource _cancellationTokenSource;

        private async Task ExecuteSearch()
        {
            try
            {
                SearchButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Visible;
                ListViewResults.Items.Clear();

                _cancellationTokenSource = new CancellationTokenSource();

                var scriptGlobals = new ScriptGlobals(_cancellationTokenSource.Token);

                var scriptText = CheckBoxSelectMode.IsChecked ?? false ? TextBoxInput.SelectedText : TextBoxInput.Text;
                try
                {
                    await Task.Run(()=>RunScript(scriptGlobals, scriptText), _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    ListViewResults.Items.Add(CreateListViewItem(ex));
                }
            }
            finally
            {
                SearchButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Collapsed;
            }
        }

        private async Task RunScript(ScriptGlobals scriptGlobals, string scriptText)
        {
            var scriptResult = await CSharpScriptWrapper.EvaluateAsync<object>(
                scriptText,
                scriptGlobals,
                ReferencedAssemblies,
                Usings,
                _cancellationTokenSource.Token);

            await DisplayResult(scriptResult);
        }

        // These types are used to set te references and Usings for the script
        private static readonly IEnumerable<Type> NeededTypes = new[]
        {
            typeof (System.Object),
            typeof (System.Linq.Enumerable),
            typeof (Microsoft.CodeAnalysis.Workspace),
            typeof (Microsoft.CodeAnalysis.ModelExtensions),
            typeof (Microsoft.CodeAnalysis.CSharp.CSharpSyntaxNode), 
            typeof (Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax),
        };

        private static readonly string[] Usings = NeededTypes.Select(t => t.Namespace).Distinct().ToArray();
        private static readonly IEnumerable<Assembly> ReferencedAssemblies = NeededTypes.Select(t => t.Assembly).ToArray();
        

        private async Task DisplayResult(object scriptResult)
        {
            // The foreach is done on background thread
            foreach (var item in MakeEnumerable(scriptResult))
            {
                _cancellationTokenSource?.Token.ThrowIfCancellationRequested();
                await Dispatcher.InvokeAsync(() =>
                {
                    var listviewItem = CreateListViewItem(item);
                    ListViewResults.Items.Add(listviewItem);
                });
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

                listviewItem.MouseDoubleClick += (o, args) => { RoslynVisxHelpers.SelectSpanInCodeWindow(lineSpan); };
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
            var enumerable = input as IEnumerable;
            if (enumerable != null && !(input is string))
            {
                // If the item is Enumerable we return it as is
                return enumerable;
            }
            else
            {
                // If not we wrap it in an Enumerable with a single item
                return new[] { input };
            }
        }
    }
}