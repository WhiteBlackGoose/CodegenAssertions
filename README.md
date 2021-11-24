﻿# CodegenAnalysis

> ⚠️ The library is in progress (alpha versioning). No expected behaviour, no documentation, no backward compatibility.

![](https://img.shields.io/static/v1?label=Lowest+target&message=netstandard2.0&color=purple&logo=dotnet)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/CodegenAssertions?label=NuGet&logo=nuget)](https://www.nuget.org/packages/CodegenAssertions)

![](https://img.shields.io/static/v1?label=Windows&message=Supported&color=brightgreen&logo=windows)
![](https://img.shields.io/static/v1?label=Linux&message=Supported&color=brightgreen&logo=linux)
![](https://img.shields.io/static/v1?label=MacOS&message=Supported&color=brightgreen&logo=apple)

Test library for verifying the expected characteristics of the machine code generated by JIT.

It is recommended to have a separate test project for codegen tests.

## Examples

### Get the codegen

```cs
static int AddAndMul(int a, int b) => a + b * a;

...

var codegenInfo = CodegenInfoResolver.GetCodegenInfo(CompilationTier.Tier1, () => AddAndMul(3, 5));
Console.WriteLine(codegenInfo);
```
Output:
```cs
00007FFD752E42F0 8BC2                 mov       eax,edx
00007FFD752E42F2 0FAFC1               imul      eax,ecx
00007FFD752E42F5 03C1                 add       eax,ecx
00007FFD752E42F7 C3                   ret
```

### Codegen size

```cs
using CodegenAssertions;
using Xunit;

public class CodegenSizeQuickJit
{
    public static int SomeMethod(int a, int b)
        => a + b;

    [Fact]
    public void Test1()
    {
        AssertCodegen.CodegenLessThan(20, CompilationTier.Tier0, () => SomeMethod(4, 5));
    }
}
```


### Having calls in the codegen

```cs
public class Tests
{
    public class A
    {
        public virtual int H => 3;
    }

    public sealed class B : A
    {
        public override int H => 6;
    }

    // this will get devirtualized at tier1, but not at tier0
    static int Twice(B b) => b.H * 2;

    [Fact]
    public void NotDevirtTier0()
    {
        AssertCodegen.CodegenHasCalls(CompilationTier.Tier0, () => Twice(new B()));
    }

    [Fact]
    public void DevirtTier1()
    {
        AssertCodegen.CodegenDoesNotHaveCalls(CompilationTier.Tier1, () => Twice(new B()));
    }
}
```

### Testing if we have branches

```cs
    private static readonly bool True = true;

    static int SmartThing()
    {
        if (True)
            return 5;
        return 10;
    }

    [Fact]
    public void BranchElimination()
    {
        AssertCodegen.CodegenDoesNotHaveBranches(CompilationTier.Tier1, () => SmartThing());
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    static int StupidThing()
    {
        if (True)
            return 5;
        return 10;
    }

    [Fact]
    public void NoBranchElimination()
    {
        AssertCodegen.CodegenHasBranches(CompilationTier.Default, () => StupidThing());
    }
```
