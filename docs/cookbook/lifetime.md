# Component Lifetime

<br/>

<div style="display: flex">
    <div style="width: 90%">
        <h5>Lifetime</h5>
        <div>
            <p>All components have a lifetime based on when it enters and exits the world.<p>
            <p>You can hook onto these events with the `Initable` and `IDestroyable` interfaces.</p>
            <table>
            <thead>
                <tr>
                    <th><code>Interface</code></th>
                    <th>Invoked when?</th>
                </tr>
            </thead>
                <tbody>
                <tr>
                    <td><code>IInitable</code></td>
                    <td>Component Added, Entity Created</td>
                </tr>
                <tr>
                    <td>IDestroyable</td>
                    <td>Component Removed, Entity Deletion</td>
                </tr>
            </tbody>
        </div>
        <p>Note that since components can added to multiple entities, the <code>Init</code>/<code>Destroy</code> methods are not always guaranteed to occur as a pair.</p>
        <p>In addition, all destroyers are called when a world is disposed of. Try removing the <code>e.Delete()</code> line on the right, and the <code>using</code> statement will still implictly call the destroyer.</p>
    </div>
    <iframe src="https://itsbuggingme.github.io/InteractiveDocHosting/?code=using%20World%20world%20%3D%20new%28%29%3B%0D%0A%0D%0AEntity%20e%20%3D%20world.Create%28new%20ReportLifetime%28%29%29%3B%0D%0A%0D%0Ae.Remove%3CReportLifetime%3E%28%29%3B%0D%0A%0D%0Ae.Add%3CReportLifetime%3E%28default%29%3B%0D%0A%0D%0Ae.Delete%28%29%3B%0D%0A%0D%0Astruct%20ReportLifetime%20%3A%20IInitable%2C%20IDestroyable%0D%0A%7B%0D%0A%20%20%20%20public%20void%20Init%28Entity%20self%29%0D%0A%20%20%20%20%7B%0D%0A%20%20%20%20%20%20%20%20Console.WriteLine%28%22Initalize%22%29%3B%0D%0A%20%20%20%20%7D%0D%0A%20%20%20%20public%20void%20Destroy%28%29%0D%0A%20%20%20%20%7B%0D%0A%20%20%20%20%20%20%20%20Console.WriteLine%28%22Destroy%22%29%3B%0D%0A%20%20%20%20%7D%0D%0A%7D" onload='javascript:(function(o){window.addEventListener("message", function(event){if(event.data.type=="setHeight"){o.style.height=event.data.height+"px";}});}(this));' style="height:200px;width:100%;border:none;overflow:hidden;"></iframe>
</div>