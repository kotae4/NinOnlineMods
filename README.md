# NinOnlineMods #
Modding framework and set of tools for the game Nin Online.  
Mostly a grind bot right now.

## HOW TO USE ##
1. Open solution
2. Make sure target platform matches the game (currently x86, but devs are working on x64 builds)
3. Build solution
4. Navigate to Releases folder (in root directory)
5. Run Injector.exe and fill in the required info
    * ".NET Runtime Version" must match the game's (currently filled in for you - I have not seen any .NET game use a different version)
    * "Full Typename" should include the full namespace and class, eg: InjectedLibrary.InjectedClass
    * "Entrypoint Method" should be the method within the above class that you want to execute, eg: InjectedEntryPoint
        * This method needs to be `static`, have `int` as return type, and have only one argument which must be a `string` of any name.
        * eg: `static int InjectedEntryPoint(string mandatoryArgument)` is a valid method signature, and you would type `InjectedEntryPoint` into the textbox.
6. Launch the game
7. Press the Inject button in the injector once the splash screen has finished and you see the main menu
8. Once injected, the mod will auto-login. Once fully loaded into the world, press F3 to initiate the bot (currently, the bot is off by default).
    * Check `Main_OnKeyPress_Pre` in the `Internal_TestMod` project for up to date keybinds.

The injector should auto-detect the game installation directory and game process.  
If it can't find the installation directory it will prompt you on startup.  
Injector configuration is saved so you *should* only have to fill in everything once.

## HOW TO CONTRIBUTE ##
1. Fork it (https://github.com/kotae4/NinOnlineMods/fork)
2. Create your feature branch (`git checkout -b feature/fooBar`)
3. Commit your changes (`git commit -am 'Add some fooBar'`)
4. Push to the branch (`git push origin feature/fooBar`)
5. Create a new Pull Request when you think it's ready to be merged into the main branch

## SOLUTION OVERVIEW & HOW IT WORKS ##
Solution consists of three projects:
1. Injector
2. NET Bootstrapper
3. Internal Mod

### Injector ###
The injector continuously scans for the game process, and, when the inject button is pressed, passes the supplied information to the `NET Bootstrapper` via a text file in the game's directory, and then injects the `NET Bootstrapper` into the game process.

### NET Bootstrapper ###
The bootstrapper reads the information from the injector via the text file in the game's directory then injects the `Internal Mod` into the CLR VM within the process.

### Internal Mod ###
This is the managed (.NET) library that's loaded into the same managed execution environment (the CLR and its default appdomain) as the game code. This allows our Internal Mod to access the entire game state and call game functions as if we were part of the game's code.  
However, there are some things that simply aren't possible with purely managed code. Namely, hooking game methods. For that reason I have ported MinHook and hde over to C# (really sloppy and lazy port, but it works!) and written a somewhat managed layer on top of that via the ManagedHooker class.  
The ManagedHooker class supports multiple hook methods per target method (meaning multiple mods can be injected and all hook the same game method without conflicts).  
The ManagedHooker class works like this:
1. Create a `DynamicMethod` and emit IL that iterates the collection of hook methods and invokes each hook method delegate. Part of the emitted IL and the reason why we must use a DynamicMethod is to handle all kinds of parameters and return types. This DynamicMethod is finalized and its JIT'd address is stored.
2. Create a second `DynamicMethod` that will be used as the 'trampoline' that each hook method can invoke to call the target method without infinite recursion (since the target method is hooked, if the hook method tries calling it it will only end up calling itself over and over). This trampoline must be a DynamicMethod because it needs to be invocable from managed code and match the same method signature as the target method. We emit the bare minimum IL necessary for it to be a compilable method, and again take special care to handle all kinds of parameters and return types.
3. Using MinHook (which uses hde) we overwrite the target method's native code with a jmp leading to the first DynamicMethod we created. MinHook creates a trampoline for us, but because it's native we can't invoke it directly from managed code (this is why we need that second DynamicMethod)
4. We then overwrite the second DynamicMethod we created with a jmp leading to MinHook's trampoline. Now we can invoke the trampoline from managed code, and all of our hooking goals are satisfied :)

There are caveats to managed hooking. Because C# / .NET is **very** OOP, all methods exist in a class. This means our hook methods exist in a class, and so they obviously don't exist in the same class as the target method. Unfortunately, that means the 'this' parameter is almost useless to us. Attempting to access instance fields of the class that our hook method exists within will 100% cause a crash even though it compiles (the compiler thinks 'this' is an instance of the class our hook method is contained within, but at runtime 'this' will be the instance of the class the target method exists within). This means any of our fields we want to access from our hook method *must* be public & static. We can cast 'this' to the appropriate game class to access their instance fields, but we still won't be able to access private fields at compile-time. Use of the System.Reflection namespace solves that problem, but it isn't pretty.

## BOT OVERVIEW ##
Currently, the bot is capable of grinding mobs to gain experience and loot near Leaf Village up to lvl ~19.  
Its combat behavior is extremely basic so it should only be used to grind mobs you can reliably kill with basic attacks alone.  

Features:
* Auto-login. Inject at main menu and it'll enter your username, password, and select the first available game server.
* Movement
    * AStar pathfinding is used within maps
    * FloydWarshallAllShortestPathAlgorithm is used to find the path between one map and another
* Targeting
* Basic Attack
    * Currently bugged. Skips animations, resulting in slightly faster attacks (you're welcome? need to fix at some point).
* Spellcasting (jutsus)
    * Currently just uses whatever is available whenever it's available. Responsible for many deaths when a projectile / aoe hits a mob other than the one being targeted.
* Item collection
    * Will pick up any item belonging to the bot *that dropped after the bot entered the map*. Has some kinks to work out.
* Health recharging
    * Literally just sits still until the server regens our health for us...
* Chakra recharing
    * Detects water tiles & moves to nearest land tile before charging
* Pathfinds to a level-appropriate map near Leaf Village
    * Larva until 6, spiders until 10, wolves until 19
* Will pathfind from Leaf Village Hospital when death occurs

## FAQ ##
Q: How do you know what game methods to call and what they do?  
A: Download the latest version of ILSpy and load `Hitspark Interactive\Nin Online Inochi\app\NinOnline.exe` into it.