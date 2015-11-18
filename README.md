# IntelliFind
This Visual Studio Extension allows Developers to search their code base using C# script and the Roslyn API. 
This allowes you to take advanrage of Roslyns knowledge of your code's structure when searching

Inside The IntelliFind Console you can type a C# Expression like 

AllNodes.OfType&lt;ParameterListSyntax&gt;().Where(pl=>pl.Parameters.Count() > 3)

Which will find all Parameter lists in the current solution with more than 3 parameters. Clicking on a search result will jump to the search result in the editor
