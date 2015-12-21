using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace IntelliFind.ScriptContext
{
    public class ScriptGlobals
    {
        private Workspace _workspace;

        public ScriptGlobals(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Provides access to the Workspace
        /// </summary>
        public Workspace Workspace
        {
            get
            {
                if (_workspace == null) _workspace = RoslynVisxHelpers.GetWorkspace();
                return _workspace;
            }
            internal set { _workspace = value; }
        }

        /// <summary>
        /// Returns the SyntaxToken that is currently selected in the active code window
        /// </summary>
        public SyntaxToken SelectedToken
        {
            get
            {
                var token = RoslynVisxHelpers.GetSelectedToken();
                if (!token.HasValue) throw new InvalidOperationException("No SyntaxToken is currently selected");
                    
                return token.Value;
            }
        }

        public Document ActiveDocument => RoslynVisxHelpers.GetCodeAnalysisDocument();

        /// <summary>
        /// Returns all SyntaxNodes for all the Documents for all Projects in the current Solution
        /// </summary>
        public IEnumerable<SyntaxNode> AllNodes =>
            Workspace.CurrentSolution.Projects
            .SelectMany(p => p.Documents)
            .SelectMany(d => d.GetSyntaxRootAsync(CancellationToken).Result.DescendantNodesAndSelf());

        public CancellationToken CancellationToken { get; }


        //public bool Replace(IEnumerable<SyntaxToken> tokens, Func<SyntaxToken, SyntaxToken> computeReplacement) 
        //    => ReplaceHelpers.ReplaceAsync(tokens, computeReplacement, CancellationToken, RoslynVisxHelpers.GetWorkspace()).Result;

        //public bool Replace(IEnumerable<SyntaxNode> nodes, Func<SyntaxNode, SyntaxNode> computeReplacement) 
        //    => ReplaceHelpers.ReplaceAsync(nodes, computeReplacement, CancellationToken, RoslynVisxHelpers.GetWorkspace()).Result;
    }
}