using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using IntelliFind.ScriptContext;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace IntelliFind.Test
{
    public class TestHelper
    {
        public AdhocWorkspace Workspace { get; }
        public Document Document { get; }

        public TestHelper(string source)
        {
            Workspace = new AdhocWorkspace();

            string projName = "NewProject";
            var projectId = ProjectId.CreateNewId();
            var versionStamp = VersionStamp.Create();
            var projectInfo = ProjectInfo.Create(projectId, versionStamp, projName, projName, LanguageNames.CSharp);
            var newProject = Workspace.AddProject(projectInfo);
            var sourceText = SourceText.From(source);
            Document = Workspace.AddDocument(newProject.Id, "NewFile.cs", sourceText);
        }
    }
}
