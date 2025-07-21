# Queries

<br/>

Queries allow you to, well, query a subset of entities from a world, just like in a traditional ECS.

Queries are made from `World.CreateQuery`, which returns a `QueryBuilder`. You then apply filters, which are `With<T>`, `Without<T>`, `Tagged<T>`, and `Untagged<T>`.

`With<T>` includes all entities with a component of type `T`.<br/>
`Without<T>` excludes all entities without a component of type `T`.<br/>
`Tagged<T>` includes all entities with a tag of type `T`.<br/>
`Untagged<T>` excludes all entities with a tag of type `T`.<br/>

<iframe src="https://itsbuggingme.github.io/InteractiveDocHosting/?code=using%20World%20world%20%3D%20new%28%29%3B%0D%0A%0D%0AEntity%20joe%20%3D%20world.Create%28new%20Person%28%22Joe%22%29%2C%20new%20Age%2839%29%29%3B%0D%0AEntity%20cat%20%3D%20world.Create%28new%20Cat%28%22Misty%22%29%2C%20new%20Age%282%29%29%3B%0D%0A%0D%0AQuery%20allPeople%20%3D%20world.CreateQuery%28%29%0D%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20.With%3CPerson%3E%28%29%0D%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20.Build%28%29%3B%0D%0A%2F%2F%20The%20.Query%20api%20simply%20calls%20CreateQuery%20under%20the%20hood%2C%20so%20they%20are%20the%20same.%0D%0AConsole.WriteLine%28allPeople%20%3D%3D%20world.Query%3CPerson%3E%28%29%29%3B%0D%0A%0D%0AallPeople.Delegate%28%28ref%20Person%20p%29%20%3D%3E%20Console.WriteLine%28p.Name%29%29%3B%0D%0A%0D%0Aworld.CreateQuery%28%29%0D%0A%20%20%20%20%20%20%20%20.With%3CAge%3E%28%29%0D%0A%20%20%20%20%20%20%20%20.Without%3CPerson%3E%28%29%0D%0A%20%20%20%20%20%20%20%20.Build%28%29%0D%0A%20%20%20%20%20%20%20%20.Delegate%28%28ref%20Age%20a%29%20%3D%3E%20Console.WriteLine%28a%29%29%3B%0D%0A%0D%0Arecord%20struct%20Person%28string%20Name%29%3B%0D%0Arecord%20struct%20Cat%28string%20Name%29%3B%0D%0Arecord%20struct%20Age%28int%20Years%29%3B" onload='javascript:(function(o){window.addEventListener("message", function(event){if(event.data.type=="setHeight"){o.style.height=event.data.height+"px";}});}(this));' style="height:200px;width:100%;border:none;overflow:hidden;"></iframe>