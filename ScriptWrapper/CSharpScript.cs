using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using MCCS = Microsoft.CodeAnalysis.CSharp.Scripting;

namespace ScriptWrapper
{
    /// <summary>
    /// This class wraps the Microsoft.CodeAnalysis.Scripting API
    /// This is needed because Visual Studio uses Microsoft.VisualStudio.LanguageServices Version 1.0
    /// while Microsoft.CodeAnalysis.Scripting requires version 1.1
    /// </summary>
    public class CSharpScript
    {
        public static Task<T> EvaluateAsync<T>(string code, object globals, CancellationToken cancellationToken= default(CancellationToken))
        {
            var options = ScriptOptions.Default
                .AddReferences(GetReferencedAssemblies())
                .AddImports(Usings);

            return Task.Run<T>(() => 
                MCCS.CSharpScript.EvaluateAsync<T>(code, options, globals, globals.GetType(), cancellationToken), 
                cancellationToken);

            //return MCCS.CSharpScript.EvaluateAsync<T>(code, options, globals, globals.GetType(), cancellationToken);
        }

        private static IEnumerable<Assembly> GetReferencedAssemblies()
        {
            // Reference all Assemblies currently loaded in the running AppDomain
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                // HACK! If multiple versions of the same assembly are loaded we only reference the one with the lowest version
                // This is needed because currently we use two versions of Microsoft.VisualStudio.LanguageServices
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
