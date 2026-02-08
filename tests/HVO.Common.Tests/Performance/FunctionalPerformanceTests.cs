using System;
using System.Diagnostics;
using HVO.Common.OneOf;
using HVO.Common.Options;
using HVO.Common.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Common.Tests.Performance;

[TestClass]
public class FunctionalPerformanceTests
{
    [TestMethod]
    public void Result_Creation_IsFast()
    {
        var sw = Stopwatch.StartNew();
        Result<int> result = default;

        for (int i = 0; i < 1_000_000; i++)
        {
            result = Result<int>.Success(i);
        }

        sw.Stop();
        Assert.IsTrue(result.IsSuccessful);
        Assert.IsTrue(sw.ElapsedMilliseconds < 500,
            $"Result<T> creation should be fast. Took {sw.ElapsedMilliseconds}ms.");
    }

    [TestMethod]
    public void Option_Creation_IsFast()
    {
        var sw = Stopwatch.StartNew();
        Option<string> option = default;

        for (int i = 0; i < 1_000_000; i++)
        {
            option = new Option<string>("value");
        }

        sw.Stop();
        Assert.IsTrue(option.HasValue);
        Assert.IsTrue(sw.ElapsedMilliseconds < 500,
            $"Option<T> creation should be fast. Took {sw.ElapsedMilliseconds}ms.");
    }

    [TestMethod]
    public void OneOf_Creation_IsFast()
    {
        var sw = Stopwatch.StartNew();
        OneOf<int, string> oneOf = default;

        for (int i = 0; i < 1_000_000; i++)
        {
            oneOf = OneOf<int, string>.FromT1(i);
        }

        sw.Stop();
        Assert.IsTrue(oneOf.IsT1);
        Assert.IsTrue(sw.ElapsedMilliseconds < 500,
            $"OneOf<T1, T2> creation should be fast. Took {sw.ElapsedMilliseconds}ms.");
    }
}
