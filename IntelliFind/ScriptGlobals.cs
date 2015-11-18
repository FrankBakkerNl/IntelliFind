using System.Collections.Generic;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;
using Document = Microsoft.CodeAnalysis.Document;

namespace TestVisx
{
    public class ScriptGlobals
    {
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

        public SyntaxNode SelectedNode => GetSelectedNode();

        public Document ActiveDocument
        {
            get
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                var activeDocument = dte?.ActiveDocument;
                if (activeDocument == null) return null;

                var documentids = Workspace.CurrentSolution.GetDocumentIdsWithFilePath(activeDocument.FullName);
                return Workspace.CurrentSolution.GetDocument(documentids.FirstOrDefault());
            }
        }

        public IEnumerable<SyntaxNode> AllNodes =>
            Workspace.CurrentSolution.Projects
            .SelectMany(p => p.Documents)
            .SelectMany(d => d.GetSyntaxRootAsync().Result.DescendantNodes());

        private SyntaxNode GetSelectedNode()
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            var activeDocument = dte?.ActiveDocument;

            var textDocument = activeDocument?.Object() as EnvDTE.TextDocument;
            var selectionPoint = textDocument?.Selection.AnchorPoint;
            if (selectionPoint == null) return null;

            var documentids = Workspace.CurrentSolution.GetDocumentIdsWithFilePath(activeDocument.FullName);
            var document = Workspace.CurrentSolution.GetDocument(documentids.FirstOrDefault());

            var syntaxTree = document?.GetSyntaxTreeAsync().Result;
            if (syntaxTree == null) return null;

            var absPosition = syntaxTree.GetText().Lines[selectionPoint.Line - 1].Start + selectionPoint.LineCharOffset;
            var token = syntaxTree.GetRoot()?.FindToken(absPosition);
            return token?.Parent;
        }
    }
}