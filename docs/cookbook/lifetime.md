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
    <iframe src="https://itsbuggingme.github.io/InteractiveDocHosting/?code=using%20World%20world%20%3D%20new%28%29%3B%0A%0AEntity%20e%20%3D%20world.Create%28new%20ReportLifetime%28%29%29%3B%0A%0Ae.Remove%3CReportLifetime%3E%28%29%3B%0A%0Ae.Add%3CReportLifetime%3E%28default%29%3B%0A%0Ae.Delete%28%29%3B%0A%0Astruct%20ReportLifetime%20%3A%20IInitable%2C%20IDestroyable%0A%7B%0A%20%20%20%20public%20void%20Init%28Entity%20self%29%0A%20%20%20%20%7B%0A%20%20%20%20%20%20%20%20Console.WriteLine%28%22Initalize%22%29%3B%0A%20%20%20%20%7D%0A%20%20%20%20public%20void%20Destroy%28%29%0A%20%20%20%20%7B%0A%20%20%20%20%20%20%20%20Console.WriteLine%28%22Destroy%22%29%3B%0A%20%20%20%20%7D%0A%7D&spans=5%7Ckeyword%7C1%7Cwhitespace%7C5%7Cclass-name%7C1%7Cwhitespace%7C5%7Clocal-name%7C1%7Cwhitespace%7C1%7Coperator%7C1%7Cwhitespace%7C3%7Ckeyword%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C2%7Cwhitespace%7C6%7Cstruct-name%7C1%7Cwhitespace%7C1%7Clocal-name%7C1%7Cwhitespace%7C1%7Coperator%7C1%7Cwhitespace%7C5%7Clocal-name%7C1%7Coperator%7C6%7Cmethod-name%7C1%7Cpunctuation%7C3%7Ckeyword%7C1%7Cwhitespace%7C14%7Cstruct-name%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C2%7Cwhitespace%7C1%7Clocal-name%7C1%7Coperator%7C6%7Cmethod-name%7C1%7Cpunctuation%7C14%7Cstruct-name%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C2%7Cwhitespace%7C1%7Clocal-name%7C1%7Coperator%7C3%7Cmethod-name%7C1%7Cpunctuation%7C14%7Cstruct-name%7C1%7Cpunctuation%7C1%7Cpunctuation%7C7%7Ckeyword%7C1%7Cpunctuation%7C1%7Cpunctuation%7C2%7Cwhitespace%7C1%7Clocal-name%7C1%7Coperator%7C6%7Cmethod-name%7C1%7Cpunctuation%7C1%7Cpunctuation%7C1%7Cpunctuation%7C2%7Cwhitespace%7C6%7Ckeyword%7C1%7Cwhitespace%7C14%7Cstruct-name%7C1%7Cwhitespace%7C1%7Cpunctuation%7C1%7Cwhitespace%7C9%7Cinterface-name%7C1%7Cpunctuation%7C1%7Cwhitespace%7C12%7Cinterface-name%7C1%7Cwhitespace%7C1%7Cpunctuation%7C5%7Cwhitespace%7C6%7Ckeyword%7C1%7Cwhitespace%7C4%7Ckeyword%7C1%7Cwhitespace%7C4%7Cmethod-name%7C1%7Cpunctuation%7C6%7Cstruct-name%7C1%7Cwhitespace%7C4%7Cparameter-name%7C1%7Cpunctuation%7C5%7Cwhitespace%7C1%7Cpunctuation%7C9%7Cwhitespace%7C7%7Cclass-name%7C1%7Coperator%7C9%7Cmethod-name%7C1%7Cpunctuation%7C11%7Cstring%7C1%7Cpunctuation%7C1%7Cpunctuation%7C5%7Cwhitespace%7C1%7Cpunctuation%7C5%7Cwhitespace%7C6%7Ckeyword%7C1%7Cwhitespace%7C4%7Ckeyword%7C1%7Cwhitespace%7C7%7Cmethod-name%7C1%7Cpunctuation%7C1%7Cpunctuation%7C5%7Cwhitespace%7C1%7Cpunctuation%7C9%7Cwhitespace%7C7%7Cclass-name%7C1%7Coperator%7C9%7Cmethod-name%7C1%7Cpunctuation%7C9%7Cstring%7C1%7Cpunctuation%7C1%7Cpunctuation%7C5%7Cwhitespace%7C1%7Cpunctuation%7C1%7Cwhitespace%7C1%7Cpunctuation&output=Initalize%0ADestroy%0AInitalize%0ADestroy%0A" onload='javascript:(function(o){window.addEventListener("message", function(event){if(event.data.type=="setHeight"){o.style.height=event.data.height+"px";}});}(this));' style="height:200px;width:100%;border:none;overflow:hidden;"></iframe>
</div>
