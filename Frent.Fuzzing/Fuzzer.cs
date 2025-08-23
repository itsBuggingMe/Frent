using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Frent.Fuzzing;

internal static class Fuzzer
{
    public static void CreateFuzzProcesses()
    {
        for (int i = 0; i < 1000; i++)
        {
            LaunchFuzzProcess(i, 1000);
        }
    }

    private static void LaunchFuzzProcess(int seed, int count)
    {
        string executing = Environment.ProcessPath ??
                           throw new Exception("Could not find entry executable.");

        ProcessStartInfo startInfo = new ProcessStartInfo(executing)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            ArgumentList = { seed.ToString(), count.ToString() },
        };

        var runner = new Process
        {
            StartInfo = startInfo,
        };

        Console.WriteLine($"Starting process with seed {seed}:");

        runner.Start();
        string stdOutput = runner.StandardOutput.ReadToEnd();
        string stdErr = runner.StandardError.ReadToEnd();
        runner.WaitForExit();

        Console.WriteLine($"Process finished with stdout: \n{stdOutput}");
    }
}