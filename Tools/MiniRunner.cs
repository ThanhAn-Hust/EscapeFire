using System;
using System.Linq;
using System.Reflection;

public static class MiniRunner
{
    public static int Main(string[] args)
    {
        var asm = Assembly.LoadFrom(args[0]);
        var skip = args.Skip(1).ToArray();
        int pass = 0, fail = 0, skipped = 0;
        foreach (var type in asm.GetTypes().Where(t => t.IsClass && t.IsPublic))
        {
            var tests = type.GetMethods().Where(m => m.GetCustomAttributes().Any(a => a.GetType().Name == "TestAttribute" || a.GetType().Name == "TestCaseAttribute")).ToList();
            if (tests.Count == 0) continue;
            var setUp = type.GetMethods().FirstOrDefault(m => m.GetCustomAttributes().Any(a => a.GetType().Name == "SetUpAttribute"));
            var tearDown = type.GetMethods().FirstOrDefault(m => m.GetCustomAttributes().Any(a => a.GetType().Name == "TearDownAttribute"));
            foreach (var test in tests)
            {
                string name = type.Name + "." + test.Name;
                if (skip.Any(s => name.Contains(s))) { Console.WriteLine("SKIP " + name); skipped++; continue; }
                var cases = test.GetCustomAttributes().Where(a => a.GetType().Name == "TestCaseAttribute")
                    .Select(a => (object[])a.GetType().GetProperty("Arguments").GetValue(a)).ToList();
                if (cases.Count == 0) cases.Add(null);
                foreach (var args0 in cases)
                {
                    var inst = Activator.CreateInstance(type);
                    try
                    {
                        setUp?.Invoke(inst, null);
                        test.Invoke(inst, args0);
                        tearDown?.Invoke(inst, null);
                        Console.WriteLine("PASS " + name); pass++;
                    }
                    catch (TargetInvocationException ex)
                    {
                        Console.WriteLine("FAIL " + name + " :: " + ex.InnerException?.GetType().Name + ": " + ex.InnerException?.Message); fail++;
                    }
                }
            }
        }
        Console.WriteLine($"passed={pass} failed={fail} skipped={skipped}");
        return fail == 0 ? 0 : 1;
    }
}
