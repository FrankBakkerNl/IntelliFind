using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;


namespace IntelliFind
{
    class RoslynVisxHelpers
    {
        private static EnvDTE80.DTE2 Dte2 => Package.GetGlobalService(typeof(SDTE)) as EnvDTE80.DTE2;

        public static Workspace GetWorkspace()
        {
            if (!Dte2?.Solution?.IsOpen ?? false) throw new InvalidOperationException("No Solution is opened");

            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            return componentModel?.GetService<VisualStudioWorkspace>();
        }

        public static void SelectSpanInCodeWindow(FileLinePositionSpan span)
        {
            // If the path is not available we cannot jump to it
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

        public static Document GetCodeAnalysisDocument()
        {
            var activeDocument = Dte2?.ActiveDocument;
            if (activeDocument == null) return null;
            return GetCodeAnalysisDocumentFromDteDocument(activeDocument, GetWorkspace());
        }

        /// <summary>
        /// Finds the SyntaxToken that is currently selected in the Active Document.
        /// </summary>
        public static SyntaxToken? GetSelectedToken()
        {
            var activeDteDocument = Dte2?.ActiveDocument;
            var dteTextDocument = activeDteDocument?.Object() as EnvDTE.TextDocument;

            var selectionPoint = dteTextDocument?.Selection?.AnchorPoint;
            if (selectionPoint == null) return null;

            var codeAnalysisDocument = GetCodeAnalysisDocumentFromDteDocument(activeDteDocument, GetWorkspace());
            var syntaxTree = codeAnalysisDocument?.GetSyntaxTreeAsync().Result;
            if (syntaxTree == null) return null;

            var absolutePosition = GetAbsolutePosition(syntaxTree, selectionPoint.Line, selectionPoint.LineCharOffset);
            return syntaxTree.GetRoot().FindToken(absolutePosition, true);
        }

        /// <summary>
        /// Gets the absolute position in the synatxtree from the line and character offset
        /// </summary>
        private static int GetAbsolutePosition(SyntaxTree syntaxTree, int line, int lineCharOffset) => 
            syntaxTree.GetText().Lines[line - 1].Start + lineCharOffset;

        private static Document GetCodeAnalysisDocumentFromDteDocument(EnvDTE.Document activeDocument, Workspace workspace)
        {
            var documentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(activeDocument.FullName).FirstOrDefault();
            if (documentId == null) return null;

            return workspace.CurrentSolution.GetDocument(documentId);
        }
    }
}