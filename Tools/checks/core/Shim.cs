using System;
using System.Linq;
using System.Reflection;
namespace NUnit.Framework {
  [AttributeUsage(AttributeTargets.Method)] public sealed class TestAttribute : Attribute {}
  public sealed class Constraint { public Func<object,bool> Check; public string Desc; }
  public static class Is {
    public static Constraint EqualTo(object expected) => new Constraint { Check = a => Equals(a, expected), Desc = "equal to " + expected };
    public static Constraint True => new Constraint { Check = a => a is bool b && b, Desc = "true" };
    public static Constraint False => new Constraint { Check = a => a is bool b && !b, Desc = "false" };
  }
  public static class Assert {
    public static void That(object actual, Constraint c) { if (!c.Check(actual)) throw new Exception("Expected " + c.Desc + " but was " + actual); }
  }
}
public static class Runner {
  public static int Main() {
    int pass = 0, fail = 0;
    foreach (var t in Assembly.GetExecutingAssembly().GetTypes().OrderBy(t => t.Name))
      foreach (var m in t.GetMethods().Where(m => m.GetCustomAttribute<NUnit.Framework.TestAttribute>() != null)) {
        try { m.Invoke(Activator.CreateInstance(t), null); pass++; }
        catch (TargetInvocationException e) { fail++; Console.WriteLine("FAIL " + t.Name + "." + m.Name + ": " + e.InnerException.Message); }
      }
    Console.WriteLine("passed " + pass + ", failed " + fail); fail += MapCheck.Run();
    return fail == 0 ? 0 : 1;
  }
}
