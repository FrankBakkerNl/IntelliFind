using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IntelliFind.ScriptContext;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace IntelliFind
{
    /// <summary>
    /// Interaction logic for IntelliFindControl.
    /// </summary>
    public partial class IntelliFindControl : UserControl
    {
        private CancellationTokenSource _cancellationTokenSource;

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
        private void GlobalsComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combobox = (ComboBox)sender;
            TextBoxInput.SelectedText = combobox.SelectedItem.ToString();
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

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }

        private async void TextBoxInput_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) return;

            // If Mode is Auto we execute the script after each change, otherwise we just validate it.
            if (ModeComboBox.SelectedItem == AutoMode)
                await ExecuteSearch();
            else
                await ValidateScript();
        }

        private async Task ValidateScript()
        {
            var cancellationToken = ResetCancellationToken();

            var code = TextBoxInput.Text;
            try
            {
                var diagnostics = await Task.Run(() => ScriptRunner.ValidateScript(code, typeof(ScriptGlobals)), cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;

                DisplayDiagnostics(diagnostics);
            }
            catch (OperationCanceledException) {}
        }

        private async Task ExecuteSearch()
        {
            try
            {
                SearchButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Visible;
                ListViewResults.Items.Clear();

                var cancellationToken = ResetCancellationToken();

                var searchExpression = SearchExpression;
                try
                {
                    await Task.Run(() => RunScript(searchExpression, cancellationToken), cancellationToken);
                }
                catch (CompilationErrorException cex)
                {
                    DisplayDiagnostics(cex.Diagnostics);
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


        private string SearchExpression => 
            (ModeComboBox.SelectedItem == ManualSelectedMode) ? TextBoxInput.SelectedText : TextBoxInput.Text;

        private async Task RunScript(string scriptText, CancellationToken cancellationToken)
        {
            var scriptResult  = await ScriptRunner.RunScriptAsync(scriptText, new ScriptGlobals(cancellationToken), cancellationToken);
            await DisplayResult(scriptResult);
        }
 
        private async Task DisplayResult(object scriptResult)
        {
            await Dispatcher.InvokeAsync(()
                => ListViewResults.Items.Clear());

            // The foreach is done on the Thread from the ThreadPool, because this could call back into 
            // the enumeration that is returned by the script.
            // Creating and adding the ListViewItems is done on the UI Thread
            foreach (var item in MakeEnumerable(scriptResult))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    _cancellationTokenSource?.Token.ThrowIfCancellationRequested();
                    var listviewItem = CreateListViewItem(item);
                    ListViewResults.Items.Add(listviewItem);
                });
            }
        }

        /// <summary>
        /// Cancells any pending operation and creates a new CancellationToken for a new Cancellable operation
        /// </summary>
        private CancellationToken ResetCancellationToken()
        {
            // If there was a previous action we cancel it
            _cancellationTokenSource?.Cancel();

            // Then we create a new CancallationTokenSource
            var newSource = new CancellationTokenSource();
            _cancellationTokenSource = newSource;
            return newSource.Token;
        }

        private ListViewItem CreateListViewItem(object item)
        {
            var syntaxNodeorToken = RoslynHelpers.AsSyntaxNodeOrToken(item);
            if (syntaxNodeorToken != null)
            {
                return new SyntaxNodeOrTokenListItem(syntaxNodeorToken.Value);
            }

            return new ListViewItem() {Content = item?.ToString() ?? "<null>"};
        }

        private static IEnumerable MakeEnumerable(object input)
        {
            var enumerable = input as IEnumerable;
            if (enumerable != null && !(input is string))
            {
                // If the item is Enumerable we return it as is
                return enumerable;
            }

            // If not we wrap it in an Enumerable with a single item
            return new[] { input };
        }

        private async void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cancellationToken = ResetCancellationToken();

                var replaceExpression = TextBoxReplace.Text;
                var searchExpression = SearchExpression;

                // Create new solution in Background Task
                var solution = await Task.Run(() => Replace(searchExpression, replaceExpression, cancellationToken), cancellationToken);

                // Updating the Visual Studio Workspace shoud be done back on the UI Thread
                var success = RoslynVisxHelpers.GetWorkspace().TryApplyChanges(solution);
                if (!success)
                {
                    ListViewResults.Items.Add(CreateListViewItem("Unable to update solution, maybe it was updated during replace action?"));
                }
            }
            catch (CompilationErrorException cex)
            {
                DisplayDiagnostics(cex.Diagnostics);
            }
            catch (Exception ex)
            {
                ListViewResults.Items.Add(CreateListViewItem(ex));
            }
        }

        private static async Task<Solution> Replace(string findExpression, string replaceExpression, CancellationToken cancellationToken)
        {
            var scriptResult = await ScriptRunner.RunScriptAsync(findExpression, new ScriptGlobals(cancellationToken),  cancellationToken);
            var nodesAndTokens = RoslynHelpers.GetSyntaxNodesAndTokens(MakeEnumerable(scriptResult).Cast<object>());
            var currentSolution = RoslynVisxHelpers.GetWorkspace().CurrentSolution;
            return await currentSolution.ReplaceNodesWithLiteralAsync(nodesAndTokens, replaceExpression, cancellationToken);
        }

        private void DisplayDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            ListViewResults.Items.Clear();
            foreach (var diagnostic in diagnostics)
            {
                var item = new DiagnosticListItem(diagnostic, TextBoxInput);
                ListViewResults.Items.Add(item);
            }
        }

        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var replace = ModeComboBox.SelectedItem == ReplaceMode;
            if (replace)
            {
                MainGrid.RowDefinitions[2].Height = new GridLength(45);
                MainGrid.RowDefinitions[3].Height = new GridLength(3);
            }
            else
            {
                MainGrid.RowDefinitions[2].Height = new GridLength(0);
                MainGrid.RowDefinitions[3].Height = new GridLength(0);
            }
        }
    }
}