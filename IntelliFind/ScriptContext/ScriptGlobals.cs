using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Document = Microsoft.CodeAnalysis.Document;
using Task = System.Threading.Tasks.Task;

namespace IntelliFind.ScriptContext
{
    public class ScriptGlobals
    {
        public ScriptGlobals(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }



        /// <summary>
        /// Provides access to the Workspace
        /// </summary>
        public Workspace Workspace => RoslynVisxHelpers.GetWorkspace();

        /// <summary>
        /// Returns the SyntaxToken that is currently selected in the active code window
        /// </summary>
        public SyntaxToken SelectedToken
        {
            get
            {
                var token = RoslynVisxHelpers.GetSelectedToken(Workspace);
                if (!token.HasValue) throw new InvalidOperationException("No SyntaxToken is currently selected");
                    
                return token.Value;
            }
        }

        public Document ActiveDocument
        {
            get
            {
                var activeDocument = RoslynVisxHelpers.GetActiveDteDocument();
                if (activeDocument == null) return null;
                return RoslynVisxHelpers.GetCodeAnalysisDocumentFromDteDocument(activeDocument, Workspace);
            }
        }

        /// <summary>
        /// Returns all SyntaxNodes for all the Documents for all Projects in the current Solution
        /// </summary>
        public IEnumerable<SyntaxNode> AllNodes =>
            Workspace.CurrentSolution.Projects
            .SelectMany(p => p.Documents)
            .SelectMany(d => d.GetSyntaxRootAsync(CancellationToken).Result.DescendantNodesAndSelf());

        public CancellationToken CancellationToken { get; }


        public bool Replace(IEnumerable<SyntaxToken> tokens, Func<SyntaxToken, SyntaxToken> computeReplacement)
        {
            return ReplaceAsync(tokens, computeReplacement).Result;
        }

        public bool Replace(IEnumerable<SyntaxNode> nodes, Func<SyntaxNode, SyntaxNode> computeReplacement)
        {
            return ReplaceAsync(nodes, computeReplacement).Result;
        }


        public async Task<bool> ReplaceAsync<TNode>(IEnumerable<TNode> nodes, Func<TNode, SyntaxNode> computeReplacement)
            where TNode : SyntaxNode
        {
            var workspace = Workspace;
            var currentSolution = workspace.CurrentSolution;

            var groupedByDocument = nodes.GroupBy(n => currentSolution.GetDocument(n.SyntaxTree));

            var updatedSolution = currentSolution;
            foreach (var nodesForDocument in groupedByDocument)
            {
                var document = nodesForDocument.Key;
                var syntaxRoot = await document.GetSyntaxRootAsync(CancellationToken);
                var newRoot = syntaxRoot.ReplaceNodes(nodesForDocument, (o, n) => computeReplacement(n));
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
            }

            return await ApplyChangesAsync(workspace, updatedSolution);
        }

        public async Task<bool> ReplaceAsync(IEnumerable<SyntaxToken> tokens, Func<SyntaxToken, SyntaxToken> computeReplacement)            
        {
            var workspace = Workspace;
            var currentSolution = workspace.CurrentSolution;

            var groupedByDocument = tokens.GroupBy(n => currentSolution.GetDocument(n.SyntaxTree));

            var updatedSolution = currentSolution;
            foreach (var nodesForDocument in groupedByDocument)
            {
                var document = nodesForDocument.Key;
                var syntaxRoot = await document.GetSyntaxRootAsync(CancellationToken);
                var newRoot = syntaxRoot.ReplaceTokens(nodesForDocument, (o,n) =>computeReplacement(n));
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
            }

            return await ApplyChangesAsync(workspace, updatedSolution);
        }

        private async Task<bool> ApplyChangesAsync(Workspace workspace, Solution newSolution)
        {

            return await Application.Current.Dispatcher.InvokeAsync(() => workspace.TryApplyChanges(newSolution));
        }
    }
}