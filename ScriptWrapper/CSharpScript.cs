using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace ScriptWrapper
{
    /// <summary>
    /// This class wrapps 
    /// </summary>
    public class CSharpScript
    {
        public static Task<T> EvaluateAsync<T>(string code, object globals)
        {
            var options = ScriptOptions.Default
                .WithReferences(GetReferencedAssemblies())
                .WithNamespaces(Usings);

            return Microsoft.CodeAnalysis.Scripting.CSharp.CSharpScript.EvaluateAsync<T>(code, options, globals);
        }

        private static IEnumerable<Assembly> GetReferencedAssemblies()
        {
            // Reference all Assemblies currently loaded in the running AppDomain
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                // If versions of the same assembly are loaded we only reference the one with the lowest version
                // This is neede because currently we use two versions of Microsoft.VisualStudio.LanguageServices

                .GroupBy(a => a.GetName().Name).Select(g => g.OrderBy(a => a.GetName().Version).First());
        }

        private static readonly string[] Usings =
        {
            "System",
            "System.Linq",
            "Microsoft.CodeAnalysis",
            "Microsoft.CodeAnalysis.CSharp",
            "Microsoft.CodeAnalysis.CSharp.Syntax"
        };
    }
}
