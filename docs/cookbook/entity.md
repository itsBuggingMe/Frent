# The Entity

<br/>

The `Entity` struct is a powerful struct for getting accessing data about an entity.

Components are explored in depth on the [component composition](/cookbook/component-composition.html) page.

<iframe src="https://itsbuggingme.github.io/InteractiveDocHosting/?code=using%20World%20world%20%3D%20new%28%29%3B%0D%0AEntity%20e%20%3D%20world.Create%28new%20Name%28%22Ryland%20Grace%22%29%2C%20new%20Occupation%28%22Teacher%22%29%29%3B%0D%0A%0D%0AConsole.WriteLine%28%22Old%20Job%3A%20%22%20%2B%20e.Get%3COccupation%3E%28%29.Title%29%3B%0D%0A%0D%0A%2F%2F%20ref%20is%20used%20so%20we%20can%20modify%20the%20struct%20in%20memory%0D%0A%2F%2F%20otherwise%2C%20we%20would%20be%20only%20modifying%20a%20copy%0D%0Aref%20Occupation%20job%20%3D%20ref%20e.Get%3COccupation%3E%28%29%3B%0D%0Ajob.Title%20%3D%20%22Researcher%22%3B%0D%0A%0D%0AConsole.WriteLine%28%22New%20Job%3A%20%22%20%2B%20e.Get%3COccupation%3E%28%29.Title%29%3B%0D%0A%0D%0Arecord%20struct%20Name%28string%20Value%29%3B%0D%0Arecord%20struct%20Occupation%28string%20Title%29%3B" onload='javascript:(function(o){window.addEventListener("message", function(event){if(event.data.type=="setHeight"){o.style.height=event.data.height+"px";}});}(this));' style="height:200px;width:100%;border:none;overflow:hidden;"></iframe>

### Tags

What if you wanted to have a component as a marker, but not to actually hold any data or behavior? This is where tags come in. Tags are any type `T` that you can add to/detach from an entity. To check if an entity has a tag, use the `Tagged` method.

<iframe src="https://itsbuggingme.github.io/InteractiveDocHosting/?code=using%20World%20world%20%3D%20new%28%29%3B%0D%0AEntity%20e%20%3D%20world.Create%28%29%3B%0D%0Ae.Tag%3CMyTag%3E%28%29%3B%0D%0AConsole.WriteLine%28e.Tagged%3CMyTag%3E%28%29%29%3B%0D%0A%0D%0Ae.Detach%3CMyTag%3E%28%29%3B%0D%0AConsole.WriteLine%28e.Tagged%3CMyTag%3E%28%29%29%3B%0D%0A%0D%0Astruct%20MyTag%3B" onload='javascript:(function(o){window.addEventListener("message", function(event){if(event.data.type=="setHeight"){o.style.height=event.data.height+"px";}});}(this));' style="height:200px;width:100%;border:none;overflow:hidden;"></iframe>

> [!TIP]
> See the [API Reference](/api/Frent.Entity.html) for all `Entity` apis.
> Unfamilar with `ref`? See the [csharp language reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/ref).