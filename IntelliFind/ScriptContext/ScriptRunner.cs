using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;

namespace IntelliFind.ScriptContext
{
    class ScriptRunner
    { 
        public static Task<object> RunScriptAsync(string code, object globals, CancellationToken cancellationToken)
        {
            // Execute the whole script on the thread pool
            return Task.Run(async () => await CSharpScript.EvaluateAsync(code, ScriptOptions, globals, null, cancellationToken), cancellationToken);
        }

        public static Task<List<Diagnostic>> ValidateScriptAsync(string code, Type globalsType, CancellationToken cancellationToken)
        {
            return Task.Run(() => ValidateScript(code, globalsType), cancellationToken);
        }


        public static List<Diagnostic> ValidateScript(string code, Type globalsType)
        {
            var script = CSharpScript.Create(code, ScriptOptions, globalsType);
            return script.GetCompilation().GetDiagnostics().ToList();
        }

        // These types are used to set the references and Usings for the script
        private static readonly IEnumerable<Type> NeededTypes = new[]
        {
            typeof (object),
            typeof (Enumerable),
            typeof (Workspace),
            typeof (ModelExtensions),
            typeof (CSharpSyntaxNode),
            typeof (ClassDeclarationSyntax),
        };

        private static readonly string[] Usings = NeededTypes.Select(t => t.Namespace).Distinct().ToArray();
        private static readonly IEnumerable<Assembly> ReferencedAssemblies = NeededTypes.Select(t => t.Assembly).ToArray();

        private static readonly ScriptOptions ScriptOptions = ScriptOptions.Default
            .AddReferences(ReferencedAssemblies)
            .AddImports(Usings);
    }
}
