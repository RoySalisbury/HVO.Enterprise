using System;
using BenchmarkDotNet.Running;

namespace HVO.Common.Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string[] effectiveArgs = args;
            if (args == null || args.Length == 0)
            {
                effectiveArgs = new[] { "--filter", "*" };
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(effectiveArgs);
        }
    }
}
