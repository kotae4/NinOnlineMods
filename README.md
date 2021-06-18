# NinOnlineMods #
Modding framework and set of tools for the game Nin Online

## HOW TO USE ##
1. Open solution
2. Fix references to game assemblies in the Internal_TestMod project
    * NinOnline and SFML.Portable should be added as references
    * You will have to remove the existing (broken) references and re-add them (these references use absolute paths which won't match up with your installation)
3. Make sure target platform matches the game (currently x86, but devs are working on x64 builds)
4. Build solution
5. Navigate to Releases folder (in root directory)
6. Run Injector.exe and fill in the required info
    * ".NET Runtime Version" must match the game's (currently filled in for you - I have not seen any .NET game use a different version)
    * "Full Typename" should include the full namespace and class, eg: InjectedLibrary.InjectedClass
    * "Entrypoint Method" should be the method within the above class that you want to execute, eg: InjectedEntryPoint
        * This method needs to be `static`, have `int` as return type, and have only one argument which must be a `string` of any name.
        * eg: `static int InjectedEntryPoint(string mandatoryArgument)`
7. Launch the game
8. Press the Inject button in the injector

The injector should auto-detect the game installation directory and game process.  
If it can't find the installation directory it will prompt you on startup.  
Injector configuration is saved so you *should* only have to fill in everything once.

The mod itself is bare bones right now: it only demonstrates how to hook game methods and access game data (note: you don't need to hook anything to access the game data - just be wary of thread safety).