# Risk of Cheese: Risk of Rain 2 mods

Various mods for Risk of Rgain 2:

 - [VoidSacrifice](https://thunderstore.io/package/Recheesers/VoidSacrifice/) Void monsters can drop void items while artifact of sacrifice is enabled
 - AmmoLocker - a deployable interactable ammo locker
 
## Build workflow

### Blender

Export models as `.fbx` files including only empties, meshes, and armatures. Export them to the corresponding Unity `Assets/FBX` folder.

### Unity 

Run the `BuildAssetBundles` editor script.

### Visual studio

Build the release solution.

### Packaged mod

Run the `package.bat` script in the c# project folder