using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace IntelliFind
{
    class RoslynHelpers
    {
        public static IEnumerable<SyntaxNodeOrToken> GetSyntaxNodesAndTokens(IEnumerable<object> source )
        {
            return source
                .Select(AsSyntaxNodeOrToken)
                .Where(snot => snot != null)
                .Select(n => n.Value);
        }

        public static SyntaxNodeOrToken? AsSyntaxNodeOrToken(object item)
        {
            
            if (item is SyntaxNode)
                return (SyntaxNode)item;

            if (item is SyntaxToken)
                return (SyntaxToken)item;

            return item as SyntaxNodeOrToken?;
        }
    }
}
