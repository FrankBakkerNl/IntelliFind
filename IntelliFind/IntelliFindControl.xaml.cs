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
            if (AutoMode.IsSelected)
                await ExecuteSearch();
            else
                await ValidateScript();
        }

        private async void TextBoxInput_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ManualSelectedMode.IsSelected)
            {
                await ValidateScript();
            }
        }

        private async Task ValidateScript()
        {
            var cancellationToken = ResetCancellationToken();

            var code = SearchExpression;
            try
            {
                var diagnostics = await ScriptRunner.ValidateScriptAsync(code, typeof(ScriptGlobals), cancellationToken);

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
                    var scriptResult = await ScriptRunner.RunScriptAsync(searchExpression, new ScriptGlobals(cancellationToken), cancellationToken);
                    await DisplayResultAsync(scriptResult, cancellationToken);
                }
                catch (CompilationErrorException cex)
                {
                    DisplayDiagnostics(cex.Diagnostics);
                }
                catch (Exception ex)
                {
                    AddToListView(ex);
                }
            }
            finally
            {
                SearchButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Collapsed;
            }
        }

        private string SearchExpression => ManualSelectedMode.IsSelected ? TextBoxInput.SelectedText : TextBoxInput.Text;

        private async Task DisplayResultAsync(object scriptResult, CancellationToken cancellationToken)
        {
            ListViewResults.Items.Clear();

            var count = 0;
            var pageLimit = 1000;

            var items = MakeEnumerable(scriptResult);
            // Use an ThreadPoolAsyncEnumerator because the MoveNext calls into the Enumerator returned by the script
            // and we do not want to run the script code on the UI thread
            using (var enumerator = new ThreadPoolAsyncEnumerator<object>(items))
            {
                var moveNextTask = enumerator.MoveNextAsync();
                while (await moveNextTask)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    count++;
                    if (count > pageLimit)
                    {
                        // We reached the PageLimit, stop enumerating until the user requests more items
                        await WaitForNextPageRequest(pageLimit);
                        pageLimit += 1000;
                    }

                    var item = enumerator.Current;
                    // Start getting the next item while we process the current
                    moveNextTask = enumerator.MoveNextAsync();
                    AddToListView(item);
                }
            }
            AddToListView($"{count} items found.");
        }

        private Task WaitForNextPageRequest(int pageLimit)
        {
            var moreItemsTaskCompletionSource = new TaskCompletionSource<bool>();
            var moreItemsListViewItem = new ListViewItem()
            {
                Content = $"More than {pageLimit } items found, click to fetch more"
            };
            moreItemsListViewItem.MouseUp += (sender, args) =>
            {
                ListViewResults.Items.Remove(moreItemsListViewItem);
                moreItemsTaskCompletionSource.SetResult(true);
            };
            ListViewResults.Items.Add(moreItemsListViewItem);

            // Return the Task that will complete when the ListViewItem is clicked
            return moreItemsTaskCompletionSource.Task;
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

        private void AddToListView(object item)
        {
            var listviewItem = CreateListViewItem(item);
            ListViewResults.Items.Add(listviewItem);
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

        private static IEnumerable<object> MakeEnumerable(object input)
        {
            var enumerable = input as IEnumerable;
            if (enumerable != null && !(input is string))
            {
                // If the item is Enumerable we return it as is
                return enumerable.Cast<object>();
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
                    AddToListView("Unable to update solution, maybe it was updated during replace action?");
                }
            }
            catch (CompilationErrorException cex)
            {
                DisplayDiagnostics(cex.Diagnostics);
            }
            catch (Exception ex)
            {
                AddToListView(ex);
            }
        }

        private static async Task<Solution> Replace(string findExpression, string replaceExpression, CancellationToken cancellationToken)
        {
            var scriptResult = await ScriptRunner.RunScriptAsync(findExpression, new ScriptGlobals(cancellationToken),  cancellationToken);
            var nodesAndTokens = RoslynHelpers.GetSyntaxNodesAndTokens(MakeEnumerable(scriptResult));
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
            if (!IsInitialized) return;

            if (ReplaceMode.IsSelected)
            {
                // Show the replace pane
                MainGrid.RowDefinitions[2].Height = new GridLength(45);
                MainGrid.RowDefinitions[3].Height = new GridLength(3);
            }
            else
            {
                // Hide the replace pane
                MainGrid.RowDefinitions[2].Height = new GridLength(0);
                MainGrid.RowDefinitions[3].Height = new GridLength(0);
            }
        }
    }
}