using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class Program
{
    static void Main(string[] args)
    {
        // Default to current directory if no path provided
        // Usage: dotnet run -- "../MyRepo"
        string path = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        
        // Find all .csproj files to auto-detect projects
        var projects = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories)
                                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) 
                                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                                .ToList();

        if (projects.Count == 0)
        {
            Console.WriteLine("No .csproj files found. Scanning entire directory as single project...");
            projects.Add(path); // Fallback to single-project mode
        }

        // Create output directory
        string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "codebase_ast");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine($"Found {projects.Count} project(s) in {path}");

        foreach (var project in projects)
        {
            string projectDir = File.Exists(project) ? Path.GetDirectoryName(project)! : project;
            string projectName = File.Exists(project) ? Path.GetFileNameWithoutExtension(project) : "codebase";
            
            // Find all .cs files in project, excluding bin/obj
            var files = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                                 .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) 
                                          && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));

            var codebaseMap = new List<FileNode>();

            foreach (var file in files)
            {
                try 
                {
                    var code = File.ReadAllText(file);
                    var tree = CSharpSyntaxTree.ParseText(code);
                    var root = tree.GetRoot();
                    
                    var collector = new StructureCollector(Path.GetRelativePath(projectDir, file));
                    collector.Visit(root);
                    
                    if (collector.RootNode.Members.Any())
                    {
                        codebaseMap.Add(collector.RootNode);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error parsing {file}: {ex.Message}");
                }
            }

            if (codebaseMap.Count == 0)
                continue;

            // Output compact tree format
            var sb = new StringBuilder();
            foreach (var file in codebaseMap)
            {
                sb.AppendLine($"# {file.FilePath}");
                WriteMembersCompact(sb, file.Members, 1);
                sb.AppendLine();
            }
            
            string outputPath = Path.Combine(outputDir, $"{projectName}.txt");
            File.WriteAllText(outputPath, sb.ToString());
            
            Console.WriteLine($"  ✅ {projectName}: {codebaseMap.Count} files");
        }

        Console.WriteLine($"\n📁 Output: {outputDir}");
    }
    
    static void WriteMembersCompact(StringBuilder sb, List<CodeMember> members, int depth)
    {
        string indent = new string(' ', depth * 2);
        foreach (var m in members)
        {
            string doc = !string.IsNullOrEmpty(m.DocString) ? $" {m.DocString}" : "";
            sb.AppendLine($"{indent}[{m.Type}] {m.Signature}{doc}");
            if (m.Children.Count > 0)
                WriteMembersCompact(sb, m.Children, depth + 1);
        }
    }
}

// ---------------- Data Structures ----------------

public class FileNode
{
    public string FilePath { get; set; } = "";
    public List<CodeMember> Members { get; set; } = new();
}

public class CodeMember
{
    public string Type { get; set; } = ""; // Class, Interface, Method, Property
    public string Signature { get; set; } = "";
    public string? DocString { get; set; }
    public List<CodeMember> Children { get; set; } = new();
}

// ---------------- Roslyn Syntax Walker ----------------

public class StructureCollector : CSharpSyntaxWalker
{
    public FileNode RootNode { get; }
    private Stack<CodeMember> _stack = new();

    public StructureCollector(string filePath)
    {
        RootNode = new FileNode { FilePath = filePath };
    }

    private void PushMember(string type, string signature, SyntaxNode node)
    {
        var member = new CodeMember 
        { 
            Type = type, 
            Signature = signature,
            // Extract the first line of XML comments if present
            DocString = node.GetLeadingTrivia()
                .Select(t => t.ToString().Trim())
                .FirstOrDefault(t => t.StartsWith("///"))
        };

        if (_stack.Count > 0)
            _stack.Peek().Children.Add(member);
        else
            RootNode.Members.Add(member);

        _stack.Push(member);
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        PushMember("Class", node.Identifier.Text, node);
        base.VisitClassDeclaration(node); // Recursion allowed inside classes
        _stack.Pop();
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        PushMember("Interface", node.Identifier.Text, node);
        base.VisitInterfaceDeclaration(node);
        _stack.Pop();
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Capture return type, name, and parameters
        string sig = $"{node.ReturnType} {node.Identifier}{node.ParameterList}";
        PushMember("Method", sig, node);
        // Do NOT call base.Visit... we don't want the code inside the method
        _stack.Pop();
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        string sig = $"{node.Type} {node.Identifier}";
        PushMember("Property", sig, node);
        _stack.Pop();
    }
}
