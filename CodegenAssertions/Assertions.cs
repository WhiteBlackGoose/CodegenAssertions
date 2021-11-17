﻿using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodegenAssertions;

public enum CompilationTier
{
    Default,
    AO,
    Tier1
}

public static partial class AssertCodegen
{
    private static readonly Func<Instruction, bool> isBranch = i => i.Code.ToString().StartsWith("Cmp") || i.Code.ToString().StartsWith("Test");
    private static readonly Func<Instruction, bool> isCall = i => i.Code.ToString().StartsWith("Call");

    private static void AssertFact<T>(bool fact, T expected, T actual, CodegenInfo ci, string comment)
    {
        if (!fact)
        {
            throw new ExpectedActualException<T>(expected, actual, $"{comment}\n\nCodegen:\n\n{ci}");
        }
    }

    private static void AssertFact(bool fact, CodegenInfo ci, IEnumerable<int>? problematicLines, string comment)
    {
        if (!fact)
        {
            throw new CodegenAssertionFailedException($"{comment}\n\nCodegen:\n\n{ci.ToString(problematicLines)}");
        }
    }

    public static void LessThan(int expectedLengthBytes, CompilationTier tier, Expr func)
    {
        var (mi, args) = ExpressionUtils.LambdaToMethodInfo(func);
        LessThan(expectedLengthBytes, tier, mi, args);
    }
    public static void LessThan(int expectedLength, CompilationTier tier, MethodInfo? mi, params object?[] arguments)
    {
        var ci = CodegenInfoResolver.GetCodegenInfo(tier, mi, arguments);
        AssertFact(ci.Bytes.Length <= expectedLength, expectedLength, ci.Bytes.Length, ci, "The method was expected to be smaller");
    }


    public static void NoCalls(CompilationTier tier, Expr expr)
    {
        var (mi, args) = ExpressionUtils.LambdaToMethodInfo(expr);
        NoCalls(tier, mi, args);
    }
    public static void NoCalls(CompilationTier tier, MethodInfo? mi, params object?[] arguments)
    {
        HasInRange(tier, null, 0, isCall, "calls", mi, arguments);
    }


    public static void NoBranches(CompilationTier tier, Expr expr)
    {
        var (mi, args) = ExpressionUtils.LambdaToMethodInfo(expr);
        NoBranches(tier, mi, args);
    }
    public static void NoBranches(CompilationTier tier, MethodInfo? mi, params object?[] arguments)
    {
        HasInRange(tier, null, 0, isBranch, "cmps", mi, arguments);
    }

    public static void HasCalls(CompilationTier tier, Expr expr)
    {
        var (mi, args) = ExpressionUtils.LambdaToMethodInfo(expr);
        HasCalls(tier, mi, args);
    }
    public static void HasCalls(CompilationTier tier, MethodInfo? mi, params object?[] arguments)
    {
        HasInRange(tier, 1, null, isCall, "calls", mi, arguments);
    }


    public static void HasBranches(CompilationTier tier, Expr expr)
    {
        var (mi, args) = ExpressionUtils.LambdaToMethodInfo(expr);
        HasBranches(tier, mi, args);
    }
    public static void HasBranches(CompilationTier tier, MethodInfo? mi, params object?[] arguments)
    {
        HasInRange(tier, 1, null, isBranch, "branches", mi, arguments);
    }


    public static void HasBranchesAtLeast(int atLeast, CompilationTier tier, Expr expr)
        => HasInRange(tier, atLeast, null, isBranch, "branches", expr);

    public static void HasBranchesNoMoreThan(int upperLimit, CompilationTier tier, Expr expr)
        => HasInRange(tier, null, upperLimit, isBranch, "branches", expr);

    public static void HasCallsAtLeast(int atLeast, CompilationTier tier, Expr expr)
        => HasInRange(tier, atLeast, null, isCall, "calls", expr);

    public static void HasCallsNoMoreThan(int upperLimit, CompilationTier tier, Expr expr)
        => HasInRange(tier, null, upperLimit, isCall, "calls", expr);


    internal static void HasInRange(CompilationTier tier, int? from, int? to, Func<Instruction, bool> pred, string comment, Expr expr)
    {
        var (mi, args) = ExpressionUtils.LambdaToMethodInfo(expr);
        HasInRange(tier, from, to, pred, comment, mi, args);
    }

    internal static void HasInRange(CompilationTier tier, int? from, int? to, Func<Instruction, bool> pred, string comment, MethodInfo? mi, params object?[] arguments)
    {
        var ci = CodegenInfoResolver.GetCodegenInfo(tier, mi, arguments);
        var problematicLines = ci.Instructions
            .Select((i, index) => (Instruction: i, Index: index))
            .Where(p => pred(p.Instruction))
            .Select(p => p.Index);
        var count = problematicLines.Count();
        var message = $"It was supposed to contain ";

        if (from is { } aFrom)
            message += $"at least {aFrom}";
        if (from is not null && to is not null)
            message += " no more than ";
        if (to is { } aTo)
            message += aTo;
        message += $" {comment}, got {count} instead";

        AssertFact(
            (from is not { } nnFrom || count >= nnFrom)
            && (to is not { } nnTo || count <= nnTo),
            ci, problematicLines, message);
    }
}