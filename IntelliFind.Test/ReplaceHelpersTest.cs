using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IntelliFind.Test
{
    [TestClass]
    public class ReplaceHelpersTest
    {
        [TestMethod]
        public void ReplaceNodesWithLiteralAsync()
        {
            var code = @"
class X
{
int orgVarName1;
object orgVarName2;
}";
            var testHelper = new TestHelper(code);

            var variables = RoslynHelpers.GetSyntaxNodesAndTokens(
                testHelper.Document.GetSyntaxRootAsync()
                    .Result.DescendantNodesAndSelf()
                    .OfType<VariableDeclaratorSyntax>());

            var newSolution = testHelper.Workspace.CurrentSolution.ReplaceNodesWithLiteralAsync(variables, "newName", CancellationToken.None)
                .Result;

            var newDoc = newSolution.GetDocument(testHelper.Document.Id);
            var newtext = newDoc.GetSyntaxTreeAsync().Result.ToString();

            Assert.IsTrue(newtext.Contains("int newName;"));
            Assert.IsTrue(newtext.Contains("object newName;"));
        }
    }
}