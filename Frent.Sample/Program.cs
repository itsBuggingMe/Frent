using Frent.Sample.Asteroids;
using System.Reflection;

namespace Frent.Sample;
internal class Program
{
    static void Main(string[] args)
    {
        MethodInfo[] methods = typeof(Samples).GetMethods().Where(m => m.GetCustomAttribute<SampleAttribute>() is not null).ToArray();
        Console.WriteLine($"Pick a sample: 0-{methods.Length + 1}");
        Console.WriteLine("[0] Asteroids");
        Console.WriteLine("[1] Monogame Square Sample");
        for (int i = 0; i < methods.Length; i++)
        {
            Console.WriteLine($"[{i + 2}] {methods[i].Name.Replace('_', ' ')}");
        }

        int userOption;
        while (!int.TryParse(Console.ReadLine(), out userOption) || userOption > methods.Length + 1 || userOption < 0)
            Console.WriteLine("Write a valid input");

        if (userOption == 0)
        {
            using var p = new AsteroidsGame();
            p.Run();
        }
        else if(userOption == 1)
        {
            using var p = new GameRoot(args.Length == 0 ? 100_000 : int.Parse(args[0]));
            p.Run();
        }
        else
        {
            methods[userOption - 2].Invoke(null, []);
        }

        Console.WriteLine("\n\nSample Completed. Press Enter to exit");
        Console.ReadLine();
    }
}
