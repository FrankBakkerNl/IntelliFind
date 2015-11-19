﻿using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ScriptWrapper;
using Task = System.Threading.Tasks.Task;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        private async void IntelliFindControl_KeyUp(object sender, KeyEventArgs e)
        {
            // Handle Ctr+Enter for Search
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                await ExecuteSearch();
            }
        }

        private CancellationTokenSource _cancellationTokenSource;

        private async Task ExecuteSearch()
        {
            try
            {
                SearchButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Visible;

                _cancellationTokenSource = new CancellationTokenSource();

                object scriptResult;
                try
                {
                    var scriptGlobals = new ScriptGlobals(_cancellationTokenSource.Token);
                    scriptResult = await CSharpScript.EvaluateAsync<object>(TextBoxInput.Text, scriptGlobals, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    scriptResult = ex;
                }

                DisplayResult(scriptResult);
            }
            finally
            {
                SearchButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Collapsed;
            }
        }

        // This static field forces the required assemblies to be loaded
        // That way we are sure they will be available to the script
        private static readonly IEnumerable NeededTypes = new[]
        {
            typeof (CSharpSyntaxNode),
            typeof (ClassDeclarationSyntax),
            typeof (Location),
            typeof (Workspace),
        };

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

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combobox = (ComboBox) sender;
            TextBoxInput.Text = ((ListViewItem)combobox.SelectedItem).Content.ToString();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }
    }
}