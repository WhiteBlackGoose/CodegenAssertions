﻿using System;

namespace CodegenAssertions;

public abstract class CodegenAssertionFailedException : Exception
{
    internal CodegenAssertionFailedException() { }
    internal CodegenAssertionFailedException(string msg) : base(msg) { }
}

public sealed class RequestedTierNotFoundException : CodegenAssertionFailedException
{
    internal RequestedTierNotFoundException(OptimizationTier tier) : base($"Tier {tier} not found") { }
}

public sealed class RequestedMethodNotCapturedForJittingException : CodegenAssertionFailedException
{
    internal RequestedMethodNotCapturedForJittingException(string method)
        : base($"Method {method} wasn't JIT-ted or JIT-ted too early. Make sure you don't run it before the test.") { }
}

public sealed class ExpectedActualException<T> : CodegenAssertionFailedException
{
    internal ExpectedActualException(T expected, T actual, string msg) : base($"Expected: {expected}\nActual: {actual}\nMessage: {msg}") { }
}