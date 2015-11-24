using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Document = Microsoft.CodeAnalysis.Document;

namespace IntelliFind
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
        public Workspace Workspace
        {
            get
            {
                var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                return componentModel.GetService<VisualStudioWorkspace>();
            }
        }

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
            .SelectMany(d => d.GetSyntaxRootAsync(CancellationToken).Result.DescendantNodes());

        public CancellationToken CancellationToken { get; }
    }
}