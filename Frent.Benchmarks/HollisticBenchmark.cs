using BenchmarkDotNet.Attributes;
using System.Reflection;

namespace Frent.Benchmarks;

[MemoryDiagnoser]
[DisassemblyDiagnoser(5)]
internal class HollisticBenchmark
{
    private World _world;
    private Type[] _componentTypes;


    public void Setup()
    {
        Random random = new Random();

        _world = new World();
        _componentTypes = typeof(HollisticBenchmark)
                    .Assembly
                    .GetTypes()
                    .Where(t => t.IsAssignableTo(typeof(IDummyComponent)) && t.IsValueType)
                    .ToArray();

        BuildEntityCreationDelegates(random);
    }


    private Func<Entity>[] BuildEntityCreationDelegates(Random random)
    {
        throw new NotImplementedException();
        var x = typeof(World).GetMethods();
        MethodInfo[] method = typeof(World).GetMethods().Where(m => m.Name == "CreateNoArgs").ToArray();
        Type[] componentTypes = _componentTypes;
        HashSet<int> chosenIndidies = new HashSet<int>();

        const int DelegateCount = 200;
        Func<Entity>[] finalDelegates = new Func<Entity>[componentTypes.Length];

        //unqiue entity types
        for (int i = 0; i < DelegateCount; i++)
        {
            int componentCount = (int)(random.NextSingle() * random.NextSingle() * 16);//[0..15]
            Console.WriteLine(componentCount);
            while (chosenIndidies.Count < componentCount)
                chosenIndidies.Add(random.Next(componentTypes.Length));

            MethodInfo rightArity = method[i];
            rightArity.MakeGenericMethod(chosenIndidies.Select(i => componentTypes[i]).ToArray());
        }


        return null;
    }

    internal class Categories
    {
        public const string Creation = "Creation";
        public const string Deletion = "Deletion";
        public const string GetComponent = "GetComponent";
    }
}
