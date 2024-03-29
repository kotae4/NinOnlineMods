# Features #
- [-] Launcher/Injector
    - [x] Auto-detect game process
    - [x] Inject internal mod into game process
    - [ ] Allow user to launch the game via button press in Launcher/Injector itself
- [-] Inter-Process Communication
    - [x] Send log messages from bot to injector/launcher
    - [ ] Send commands from injector/launcher to bot
- [-] Logging
    - [x] Log to file
    - [x] Separate logging for packets
    - [x] Send log message to launcher/injector via IPC
    - [ ] Separate log files per-session
        - [ ] Keep only the last 10 session logs
- [x] Auto-Login
- [-] Hooking
    - [x] Purely managed
    - [ ] Detect when a target method is a Pre-JIT Stub
- [x] Drawing
    - [x] Screenspace
    - [x] Worldspace (tilespace)
- [-] Movement
    - [ ] Try to avoid hostile mob aggro radius
        - [ ] Unless we're looking to engage in AoE combat?
- [-] Pathfinding
    - [x] To mobs within a map
    - [-] To other maps
        - [-] Blacklist certain warp tiles within maps
        - [ ] Dump every map in the game and add to adjacency matrix, map png, and WarpMapping zip in the repository
        - [ ] Manually plug in bad warp tiles into blacklist for each map that has them
    - [ ] Through warp pads (black/white rune thingies on the ground that teleport you to another place within the same map)
- [-] Item collection
    - [x] Pick up mob loot
    - [ ] Pick up all loot belonging to the bot even if it dropped before the bot entered the map (better loot detection, basically)
- [x] Melee attacks
- [x] Jutsu casting
- [-] Health regen
    - [ ] Buy food from vendor to speed up health regen in the field
- [ ] Sell inventory to vendor in order to maintain ryo needed to buy food or other items
    - [ ] Blacklist certain items from being sold (scrolls, food, others?)
- [-] Chakra charging
    - [-] Path to nearest non-water tile
        - [ ] Unless from mist village
- [ ] Intelligent combat behaviors
    - [ ] Select appropriate jutsu for the situation, continuously
        - [ ] Be wary of design bottleneck. A good design is very important here.
    - [ ] Kiting
        - [ ] Maintain distance between 2 and lowest max range of available combat jutsus at all times
    - [ ] AoE kiting
        - [ ] Like kiting, but aggros multiple mobs and attempts to group them into a single line as close as possible to each other before casting AoE jutsus
    - [ ] Fleeing to another map when we detect the situation is dire
    - [ ] When bot level >= 20, prefer kiting w/ jutsus over melee attack
- [ ] Better game state detection
    - [ ] When an NPC becomes hostile toward us
        - [ ] Respond by either engaging in combat or moving maps
    - [ ] When we lose health (either from NPC, a player, anything else)
        - [ ] Respond by either engaging in combat or moving maps
    - [ ] When an admin enters our map
        - [ ] Respond by logging out
    - [ ] When a player mentions 'bot' in chat
        - [ ] Respond somehow, either with a chat message ("no you're a bot"), moving maps, or logging out
    - [ ] When we're disconnected or otherwise end up back at the main menu
        - [ ] Respond by logging back in automatically
    - [ ] When we gain or lose a buff or debuff
        - [ ] Figure out how the bot could use this information
    - [ ] When we inflict damage or otherwise successfully land an attack
        - [ ] Add to stat tracker (accuracy)
    - [ ] When we kill a mob
        - [ ] Add to stat tracker (average time to kill)
    - [ ] When we die
        - [ ] Add to stat tracker (number of deaths)
        - [ ] Respond by auto-releasing (instead of waiting for the timer)
- [ ] Stat tracking (to a separate log file and via IPC)
    - [ ] EXP per hour
    - [ ] Per-Jutsu accuracy
    - [ ] Average time to kill
    - [ ] Number of deaths
    - [ ] Number of players seen
    - [ ] Number of admins seen
    - [ ] Total session length
    - [ ] Per-Command stats
        - [ ] Average time spent in this command
        - [ ] Number of times this command failed irrecoverably
- [ ] Quest automation
    - [ ] Pathing to quest giver
    - [ ] Mapping of each quest in the game & how to complete it
    - [ ] Include mandatory tutorials (so the bot can start from a brand new account)
- [ ] Leveling automation
    - [ ] What weapons to buy and when
    - [ ] Auto-selection of mastery
    - [ ] What jutsus to learn and auto enchantment of blank scrolls to learn them
- [ ] (Wishlist) Moonsharp integration and scripting API :)
- [ ] (Wishlist) Entirely packet-based bot, allowing for hundreds of instances to be launched even without the game running or installed on the machine (no more injection, no more Internal_TestMod)
    - [ ] Mimic game logic well enough for this to work :)