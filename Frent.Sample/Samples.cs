using Frent.Components;
using Frent.Core;
using Frent.Systems;

namespace Frent.Sample;

internal class Samples
{
    #region Cookbook 1
    [Sample]
    public static void Update_Component()
    {
        using World world = new World();

        //Create three entities
        for (int i = 0; i < 3; i++)
        {
            world.Create<string, ConsoleText>("Hello, World!", new(ConsoleColor.Blue));
        }

        //Update the three entities
        world.Update();
    }
    #endregion

    #region Cookbook 2
    [Sample]
    public static void Uniforms_And_Entities()
    {
        DefaultUniformProvider uniforms = new DefaultUniformProvider();
        //add delta time as a float
        uniforms.Add(0.5f);

        using World world = new World(uniforms);

        world.Create<Vel, Pos>(default, default);
        world.Create<Pos>(default);

        world.Update();
    }
    #endregion

    #region Cookbook 3
    [Sample]
    public static void Queries()
    {
        DefaultUniformProvider provider = new DefaultUniformProvider();
        provider.Add<byte>(5);
        using World world = new World(provider);

        for (int i = 0; i < 5; i++)
            world.Create<int>(i);

        world.Query<With<int>>().Run((ref int x) => Console.Write($"{x++}, "));
        Console.WriteLine();

        world.Query<With<int>>().InlineUniform<WriteQuery, byte, int>(default);
    }
    #endregion

    #region Cookbook 4
    [Sample]
    public static void Entities()
    {
        using World world = new World();
        Entity ent = world.Create<int, double, float>(69, 3.14, 2.71f);
        //true
        Console.WriteLine(ent.IsAlive);
        //true
        Console.WriteLine(ent.Has<int>());
        //false
        Console.WriteLine(ent.Has<bool>());
        //You can also add and remove components
        ent.Add<string>("I like Frent");

        if (ent.TryGet<string>(out Ref<string> strRef))
        {
            Console.WriteLine(strRef);
            //reassign the string value
            strRef.Value = "Do you like Frent?";
        }

        //If we didn't add a string earlier, this would throw instead
        Console.WriteLine(ent.Get<string>());

        //You can also deconstruct components from the entity to reassign many at once
        ent.Deconstruct(out Ref<double> d, out Ref<int> i, out Ref<float> f, out Ref<string> str);
        d.Value = 4;
        str.Value = "Hello, World!";
        
        Console.WriteLine(str);
    }
    #endregion
}
record struct Pos(float X) : IEntityComponent
{
    public void Update(Entity entity)
    {
        Console.WriteLine(entity.Has<Vel>() ?
            "I have velocity!" :
            "No velocity here!");
    }
}

record struct Vel(float DX) : IUniformComponent<float, Pos>
{
    public void Update(float dt, ref Pos pos)
    {
        pos.X += DX * dt;
    }
}

struct WriteQuery : IUniformAction<byte, int>
{
    public void Run(byte uniform, ref int x) => Console.Write($"{x + uniform}, ");
}

struct ConsoleText(ConsoleColor Color) : IComponent<string>
{
    public void Update(ref string str)
    {
        Console.ForegroundColor = Color;
        Console.Write(str);
    }
}