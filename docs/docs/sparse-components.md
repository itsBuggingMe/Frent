# Sparse Components

Also known as non-fragmenting components, sparse components are stored differently compared to regular components.

Sparse components can be added and removed from entities much faster but have slower iteration speeds.

To make a component sparse, simply implement `ISparseComponent`.