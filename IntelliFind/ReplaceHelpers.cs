using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace IntelliFind
{
    static class ReplaceHelpers
    {
        public static Task<Solution> ReplaceNodesWithLiteralAsync(this Solution solution, IEnumerable<SyntaxNodeOrToken> nodesAndTokensToReplace, string replaceExpression, CancellationToken cancellationToken)
        {
            return ReplaceNodesWithLiteralAsync(solution, nodesAndTokensToReplace, token => replaceExpression, cancellationToken);
        }

        public static async Task<Solution> ReplaceNodesWithLiteralAsync(Solution solution, IEnumerable<SyntaxNodeOrToken> nodes, Func<SyntaxNodeOrToken, string> computeReplacement, CancellationToken cancellationToken)
        {
            var groupedByDocument = nodes.GroupBy(n => solution.GetDocument(n.SyntaxTree));

            var updatedSolution = solution;
            foreach (var nodesForDocument in groupedByDocument)
            {
                var document = nodesForDocument.Key;
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);

                var changes = nodesForDocument.Select(node => new TextChange(node.Span, computeReplacement(node)));
                var newText = syntaxRoot.GetText().WithChanges(changes);
                updatedSolution = updatedSolution.WithDocumentText(document.Id, newText);
            }

            return updatedSolution;
        }
    }
}
