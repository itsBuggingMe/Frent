## Component Composition

<br/>

<div style="display: flex">
    <div style="width: 90%">
        <h5>Storing Data</h5>
            <p>In Frent, you <i>compose</i> entities using components. Components can be almost anything.</p>
            <p>To create an entity from some components, you must use the <code>World</code> class. Think of the <code>World</code> like an optimized collection of entities.</p>
            <p>In the example on the right, you can see the <code>Name</code> and <code>Species</code> component being used to create an entity. But components can do more than just store data - they can have behavior.</p>
        <h5>Adding Behavior</h5>
            <p>Implementing behavior in a component usually starts with some kind of a <code>IComponent</code> interface. The most simple of these interfaces is, well, <code>IComponent</code>. This interface requires an <code>Update</code> method with zero arguments. Different <code>IComponent</code> interfaces have different arguments for different purposes.</p>
            <p>On the right, you can see the behavior of <code>Name.Update</code>, but where is its output? Add a <code>world.Update();</code> call after entity creation, which calls all <code>Update</code> methods on all components in the world.</p>
        <div class="NOTE alert alert-info">
            <h5>Info</h5>
            What is a <a href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record">record struct</a>?
            <br/>
            What is a <a href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using">using statement</a>?
        </div>
    </div>
    <iframe src="https://itsbuggingme.github.io/InteractiveDocHosting/?code=using%20World%20world%20%3D%20new%28%29%3B%0D%0A%0D%0AName%20name%20%3D%20new%28%22Misty%22%29%3B%0D%0ASpecies%20species%20%3D%20new%28%22Cat%22%29%3B%0D%0A%0D%0A%2F%2F%20Create%20an%20entity%20that%20is%20a%20cat%20with%20the%20name%20Misty%0D%0AEntity%20myCat%20%3D%20world.Create%28name%2C%20species%29%3B%0D%0A%0D%0A%2F%2F%20Get%20the%20Species%20component%0D%0ASpecies%20myCatSpecies%20%3D%20myCat.Get%3CSpecies%3E%28%29%3B%0D%0A%0D%0AConsole.WriteLine%28%24%22myCat%20is%20a%20%7BmyCatSpecies.Kind%7D%22%29%3B%0D%0A%0D%0A%0D%0A%0D%0Astruct%20Name%28string%20Value%29%20%3A%20IComponent%0D%0A%7B%0D%0A%20%20%20%20public%20void%20Update%28%29%0D%0A%20%20%20%20%7B%0D%0A%20%20%20%20%20%20%20%20Console.WriteLine%28%24%22My%20name%20is%20%7BValue%7D%22%29%3B%0D%0A%20%20%20%20%7D%0D%0A%7D%0D%0A%0D%0Arecord%20struct%20Species%28string%20Kind%29%3B" onload='javascript:(function(o){window.addEventListener("message", function(event){if(event.data.type=="setHeight"){o.style.height=event.data.height+"px";}});}(this));' style="height:200px;width:100%;border:none;overflow:hidden;"></iframe>
</div>

<h3>Component interfaces</h3>

There are many component interfaces, each with a different purpose, all with a `Update` method. The main interfaces are: `IComponent`, `IEntityComponent`, `IUniformComponent<TUniform>`, and `IEntityUniformComponent<TUniform>`. Each interface name follows the scheme of `I[...]Component`. The content inside the brackets tell you what arguments the component will have. For example, `IComponent` has no arguments and `IEntityUniformComponent<TUniform>` has two arguments `(Entity self, TUniform uniform)`.

Try adding behavior to the `Decay` component below such that the entity is deleted once the decay timer is over. You will need to change the interface used to `IEntityComponent` to get access to the `Entity` from inside the component. You can delete an entity by calling the `.Delete()` instance method.

<iframe src="https://itsbuggingme.github.io/InteractiveDocHosting/?code=using%20World%20world%20%3D%20new%20World%28%29%3B%0D%0A%0D%0A%0D%0AEntity%20entity%20%3D%20world.Create%28new%20Decay%285%29%2C%20new%20DecaySpeed%281%29%29%3B%0D%0A%0D%0Afor%28int%20i%20%3D%200%3B%20i%20%3C%205%3B%20i%2B%2B%29%0D%0A%20%20%20%20world.Update%28%29%3B%0D%0A%0D%0AConsole.WriteLine%28entity.IsAlive%20%3F%20%22Still%20Alive%21%22%20%3A%20%22Decayed%20Away%22%29%3B%0D%0A%0D%0Astruct%20Decay%28int%20decayTimer%29%20%3A%20IComponent%0D%0A%7B%0D%0A%20%20%20%20public%20void%20Update%28%29%0D%0A%20%20%20%20%7B%0D%0A%20%20%20%20%20%20%20%20if%28--decayTimer%20%3C%3D%200%29%0D%0A%20%20%20%20%20%20%20%20%7B%0D%0A%20%20%20%20%20%20%20%20%20%20%20%20%2F%2F%20Delete%20me%21%0D%0A%20%20%20%20%20%20%20%20%7D%0D%0A%20%20%20%20%7D%0D%0A%7D%0D%0A%0D%0Arecord%20struct%20DecaySpeed%28int%20Value%29%3B" onload='javascript:(function(o){window.addEventListener("message", function(event){if(event.data.type=="setHeight"){o.style.height=event.data.height+"px";}});}(this));' style="height:200px;width:100%;border:none;overflow:hidden;"></iframe>

<h5>Interacting Components</h5>

A single component is boring. Components together strong. While you *can* use `entity.Get<T>` to get components on the same entity to interact with, this method is verbose and slow.

To automatically inject a component value into the `Update` method, simply add a generic type. `IComponent.Update()` becomes `IComponent<TComp>.Update(ref TComp)`. If you need another component injected, add another generic type.

Try modifying the `Decay` component above to take into account `DecaySpeed` when decrementing. You'll need `IEntityComponent<T>` for this one.