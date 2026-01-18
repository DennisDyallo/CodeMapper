using Xunit;

namespace CodeMapper.Tests;

public class ParserTests
{
    [Fact]
    public void ParsesClassDeclaration()
    {
        var code = "public class MyClass { }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        Assert.Single(collector.RootNode.Members);
        Assert.Equal("Class", collector.RootNode.Members[0].Type);
        Assert.Equal("MyClass", collector.RootNode.Members[0].Signature);
    }

    // Phase 1.1: Visibility Filtering Tests
    [Fact]
    public void SkipsPrivateClasses()
    {
        var code = "private class PrivateClass { }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        Assert.Empty(collector.RootNode.Members);
    }

    [Fact]
    public void SkipsPrivateMethods()
    {
        var code = @"public class MyClass { private void SecretMethod() { } }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        Assert.Single(collector.RootNode.Members);
        Assert.Empty(collector.RootNode.Members[0].Children);
    }

    [Fact]
    public void IncludesInternalClasses()
    {
        var code = "internal class InternalClass { }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        Assert.Single(collector.RootNode.Members);
        Assert.Equal("InternalClass", collector.RootNode.Members[0].Signature);
    }

    // Phase 1.2: Line Number Tests
    [Fact]
    public void ExtractsLineNumbers()
    {
        var code = @"
public class MyClass 
{ 
    public void MyMethod() { }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        Assert.Equal(2, collector.RootNode.Members[0].LineNumber);
        Assert.Equal(4, collector.RootNode.Members[0].Children[0].LineNumber);
    }

    // Phase 2.1: Namespace Tests
    [Fact]
    public void ParsesNamespace()
    {
        var code = @"namespace MyApp.Services { public class UserService { } }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        Assert.Single(collector.RootNode.Members);
        Assert.Equal("Namespace", collector.RootNode.Members[0].Type);
        Assert.Equal("MyApp.Services", collector.RootNode.Members[0].Signature);
        Assert.Single(collector.RootNode.Members[0].Children);
    }

    [Fact]
    public void ParsesFileScopedNamespace()
    {
        var code = @"namespace MyApp.Services;
public class UserService { }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        Assert.Single(collector.RootNode.Members);
        Assert.Equal("Namespace", collector.RootNode.Members[0].Type);
        Assert.Equal("MyApp.Services", collector.RootNode.Members[0].Signature);
    }

    // Phase 2.2: Base Types Tests
    [Fact]
    public void ExtractsBaseTypesAndInterfaces()
    {
        var code = "public class UserService : BaseService, IUserService { }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        var classNode = collector.RootNode.Members[0];
        Assert.NotNull(classNode.BaseTypes);
        Assert.Equal(2, classNode.BaseTypes!.Count);
        Assert.Contains("BaseService", classNode.BaseTypes);
        Assert.Contains("IUserService", classNode.BaseTypes);
    }

    // Phase 2.4: Constructor Tests
    [Fact]
    public void ParsesPublicConstructors()
    {
        var code = @"public class MyClass 
{ 
    public MyClass(ILogger logger, IService service) { }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        var classNode = collector.RootNode.Members[0];
        Assert.Single(classNode.Children);
        Assert.Equal("Constructor", classNode.Children[0].Type);
        Assert.Contains("ILogger logger", classNode.Children[0].Signature);
    }

    // Phase 3.1: Record Tests
    [Fact]
    public void ParsesRecords()
    {
        var code = "public record UserDto(string Name, int Age);";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        Assert.Single(collector.RootNode.Members);
        Assert.Equal("Record", collector.RootNode.Members[0].Type);
        Assert.Contains("UserDto", collector.RootNode.Members[0].Signature);
        Assert.Contains("string Name", collector.RootNode.Members[0].Signature);
    }

    // Phase 3.2: Enum Tests
    [Fact]
    public void ParsesEnums()
    {
        var code = "public enum Status { Active, Inactive, Pending }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        Assert.Single(collector.RootNode.Members);
        Assert.Equal("Enum", collector.RootNode.Members[0].Type);
        Assert.Contains("Active", collector.RootNode.Members[0].Signature);
        Assert.Contains("Inactive", collector.RootNode.Members[0].Signature);
    }

    // Phase 3.3: Attributes Tests
    [Fact]
    public void ExtractsAttributes()
    {
        var code = @"[Obsolete]
[ApiController]
public class MyController { }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        var classNode = collector.RootNode.Members[0];
        Assert.NotNull(classNode.Attributes);
        Assert.Contains("Obsolete", classNode.Attributes);
        Assert.Contains("ApiController", classNode.Attributes);
    }

    // Phase 4.2: Static Indicator Tests
    [Fact]
    public void DetectsStaticMembers()
    {
        var code = @"public static class Helpers 
{ 
    public static void DoSomething() { }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        Assert.True(collector.RootNode.Members[0].IsStatic);
        Assert.True(collector.RootNode.Members[0].Children[0].IsStatic);
    }

    [Fact]
    public void ParsesMethodSignature()
    {
        var code = @"
            public class MyClass 
            { 
                public int Calculate(string input, bool flag) { return 0; }
            }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        var classNode = collector.RootNode.Members[0];
        Assert.Single(classNode.Children);
        Assert.Equal("Method", classNode.Children[0].Type);
        Assert.Equal("int Calculate(string input, bool flag)", classNode.Children[0].Signature);
    }

    [Fact]
    public void ParsesPropertyDeclaration()
    {
        var code = @"
            public class MyClass 
            { 
                public string Name { get; set; }
            }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        var classNode = collector.RootNode.Members[0];
        Assert.Single(classNode.Children);
        Assert.Equal("Property", classNode.Children[0].Type);
        Assert.Equal("string Name", classNode.Children[0].Signature);
    }

    [Fact]
    public void ParsesInterface()
    {
        var code = "public interface IService { void Execute(); }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        Assert.Single(collector.RootNode.Members);
        Assert.Equal("Interface", collector.RootNode.Members[0].Type);
        Assert.Equal("IService", collector.RootNode.Members[0].Signature);
    }

    [Fact]
    public void ParsesMultipleMembersInClass()
    {
        var code = @"public class MyClass 
{ 
    public string Name { get; set; }
    public int GetValue() { return 0; }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        var classNode = collector.RootNode.Members[0];
        Assert.Equal(2, classNode.Children.Count);
        Assert.Equal("Property", classNode.Children[0].Type);
        Assert.Equal("Method", classNode.Children[1].Type);
    }

    [Fact]
    public void NestedClassesAreCaptured()
    {
        var code = @"
            public class Outer 
            { 
                public class Inner { }
            }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var collector = new StructureCollector("test.cs");
        collector.Visit(root);
        
        var outer = collector.RootNode.Members[0];
        Assert.Equal("Outer", outer.Signature);
        Assert.Single(outer.Children);
        Assert.Equal("Inner", outer.Children[0].Signature);
    }
}
