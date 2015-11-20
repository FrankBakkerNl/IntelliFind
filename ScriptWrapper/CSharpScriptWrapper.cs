using System.Collections.Generic;
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
    public class CSharpScriptWrapper
    {
        public static Task<T> EvaluateAsync<T>(
            string code, 
            object globals, 
            IEnumerable<Assembly> referencedAssemblies, 
            IEnumerable<string> usings, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var options = ScriptOptions.Default
                .AddReferences(referencedAssemblies)
                .AddImports(usings);

            return MCCS.CSharpScript.EvaluateAsync<T>(code, options, globals, globals.GetType(), cancellationToken);
        }
    }
}
