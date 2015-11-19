using System;
using System.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Document = Microsoft.CodeAnalysis.Document;

namespace IntelliFind
{
    class RoslynVisxHelpers
    {
        public static void SelectSpanInCodeWindow(FileLinePositionSpan span)
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

        public static SyntaxNode GetSelectedNode(Workspace workspace)
        {
            return GetSelectedToken(workspace)?.Parent;
        }

        public static SyntaxToken? GetSelectedToken(Workspace workspace)
        {
            var activeDocument = GetActiveDteDocument();

            var textDocument = activeDocument?.Object() as EnvDTE.TextDocument;
            var selectionPoint = textDocument?.Selection.AnchorPoint;
            if (selectionPoint == null) return null;

            var document = GetCodeAnalysisDocumentFromDteDocument(activeDocument, workspace);
            var syntaxTree = document?.GetSyntaxTreeAsync().Result;
            if (syntaxTree == null) return null;

            var absolutePosition = GetAbsolutePosition(syntaxTree, selectionPoint.Line, selectionPoint.LineCharOffset);
            return syntaxTree.GetRoot().FindToken(absolutePosition, true);
        }

        /// <summary>
        /// Gets the absolute position in the synatxtree from the line and character offset
        /// </summary>
        private static int GetAbsolutePosition(SyntaxTree syntaxTree, int line, int lineCharOffset) => 
            syntaxTree.GetText().Lines[line - 1].Start + lineCharOffset;

        public static Document GetCodeAnalysisDocumentFromDteDocument(EnvDTE.Document activeDocument, Workspace workspace)
        {
            var documentids = workspace.CurrentSolution.GetDocumentIdsWithFilePath(activeDocument.FullName);
            var document = workspace.CurrentSolution.GetDocument(documentids.FirstOrDefault());
            return document;
        }

        public static EnvDTE.Document GetActiveDteDocument()
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            return dte?.ActiveDocument;
        }
    }
}