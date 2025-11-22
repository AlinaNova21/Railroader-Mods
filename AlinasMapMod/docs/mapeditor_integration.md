# Map Editor Integration

There are new classes adding in AlinasMapMod that allows
itself and other mods to register their objects in MapEditor.
This allows MapEditor to edit and save these objects.

There not much documentation yet, but there is a few implementations in the Loaders, Map, and Station folders. The main interfaces are IEditableObject, ITransformableObject, and IObjectFactory. There is also ICustomHelper for custom helper objects.

