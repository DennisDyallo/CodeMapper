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
