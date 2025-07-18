---
_layout: landing
---

# Under Construction...

Check out the cookbook!

<iframe src="https://itsbuggingme.github.io/InteractiveDocHosting/?code=using%20System.Numerics%3B%0D%0A%0D%0Ausing%20World%20world%20%3D%20new%20World%28%29%3B%0D%0AEntity%20entity%20%3D%20world.Create%3CPosition%2C%20Velocity%3E%28new%28Vector2.Zero%29%2C%20new%28Vector2.One%29%29%3B%0D%0A%0D%0A%2F%2FCall%20Update%20to%20run%20the%20update%20functions%20of%20your%20components%0D%0Aworld.Update%28%29%3B%0D%0A%0D%0A%2F%2F%20Position%20is%20%281%2C%201%29%0D%0AConsole.WriteLine%28entity.Get%3CPosition%3E%28%29%29%3B%0D%0A%0D%0A%2F%2F%20Alternatively%2C%20use%20a%20system%0D%0Aworld.Query%3CPosition%2C%20Velocity%3E%28%29%0D%0A%20%20%20%20.Delegate%28%28ref%20Position%20p%2C%20ref%20Velocity%20v%29%20%3D%3E%20p.Value%20%2B%3D%20v.Delta%29%3B%0D%0A%0D%0Arecord%20struct%20Position%28Vector2%20Value%29%3B%0D%0Arecord%20struct%20Velocity%28Vector2%20Delta%29%20%3A%20IInitable%2C%20IComponent%3CPosition%3E%0D%0A%7B%0D%0A%20%20%20%20%2F%2F%20There%20is%20also%20IDestroyable%0D%0A%20%20%20%20public%20void%20Init%28Entity%20self%29%20%7B%20%7D%0D%0A%20%20%20%20public%20void%20Update%28ref%20Position%20position%29%20%3D%3E%20position.Value%20%2B%3D%20Delta%3B%0D%0A%7D" onload='javascript:(function(o){window.addEventListener("message", function(event){if(event.data.type=="setHeight"){o.style.height=event.data.height+"px";}});}(this));' style="height:200px;width:100%;border:none;overflow:hidden;"></iframe>
