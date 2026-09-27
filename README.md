# LivingNorsemen

**Current Version:** 0.1.0-beta

LivingNorsemen is a gameplay extension for **VikingNPC Continued** that expands Norsemen into working members of your Valheim settlement.

Assign NPCs homes, workplaces, shifts, professions, patrol routes, and storage containers to create a settlement that feels more alive.

> **Beta:** LivingNorsemen is actively being developed and tested. Features and behaviors may change as development continues.

## Requirements

- BepInExPack for Valheim
- VikingNPC Continued 0.4.1 or later
- YamlDotNet

LivingNorsemen requires **VikingNPC Continued** (`Norsemen.dll`) and does not replace it.

Do **not** install the original VikingNPC/Norsemen package and VikingNPC Continued simultaneously.

## About

LivingNorsemen is an extension of VikingNPC/Norsemen that adds additional NPC professions, settlement behaviors, controls, tagged storage containers, work schedules, and quality-of-life systems.

The project grew out of my love for Valheim and my desire to make its settlements feel less empty. It also started as my first attempt at building a Valheim mod.

I develop LivingNorsemen in my free time while progressing through the game at my own pace. Testing of later biomes and progression tiers may therefore be more limited. Bug reports and suggestions are welcome, but I cannot promise immediate updates.

LivingNorsemen depends on **VikingNPC Continued**, my maintained continuation of RustyMods' original VikingNPC/Norsemen mod. The continuation and this extension are maintained separately so VikingNPC Continued can remain comparatively faithful to the original while LivingNorsemen can expand the NPC system.

The original VikingNPC/Norsemen mod was created by **RustyMods**. VikingNPC Continued is maintained and published with the original author's permission.

## Current Professions

- Lumberjack - Axe
- Miner - Pickaxe
- Fisherman - Fishing Rod and Bait
- Guard - Stationary
- Guard - Patrol
- Farmer - Cultivator
- Cook - Raw meats and fish

## Major Features

- Home Points
- Work Points
- Guard Patrol Points
- Go Home / Go To Work commands
- Day and Night work shifts
- Tagged settlement storage containers
- Profession-based resource gathering
- Focused work for Lumberjacks, Miners, Fishermen, and Cooks
- Automated resource deposits
- Cooked food distribution
- Door interaction during NPC travel
- Guard patrol utility behaviors

---

# NPC Behavior

## Wild Norsemen

Untamed Norsemen may appear randomly during your adventures.

- Feed them to tame them.
- Wild Norsemen are not inherently afraid of the player.
- Players can steal from them, although doing so may cause them to become hostile.
- Wild Norsemen that die cannot be resurrected.

## Tamed Norsemen

Once an NPC is tamed, the standard interaction options include:

```text
[E] Follow / Stay
[Shift + E] Rename
[Left Alt + E] Open NPC Inventory
```

Opening a tamed NPC's inventory provides additional controls.

### Aggressive / Passive

**Aggressive**

The NPC will engage nearby threats.

**Passive**

The NPC will attempt to avoid threats.

### Guard / Patrol

**Guard**

The NPC remains associated with its assigned location.

**Patrol**

Allows an appropriately configured Guard to patrol between its Work Point and Patrol Point.

### Public / Private

**Public**

Other players may interact with the NPC.

**Private**

Restricts interaction to the owning player.

### Assign Home

Records the NPC's current position as its **Home Point**.

### Assign Work

Records the NPC's current position as its **Work Point**.

### Assign Patrol B

Used by Guards to establish the second point of a patrol route.

Move the NPC to the desired patrol location and select **Assign Patrol B**.

## Home and Work Commands

Once Home and/or Work Points have been established:

```text
[H] Go Home
[K] Go To Work
```

The NPC will attempt to travel to the corresponding assigned point.

---

# Work Shifts

LivingNorsemen supports **Day** and **Night** shifts.

## Day Shift

NPCs **without a torch** in their inventory work during the day.

When their shift begins, they travel to their Work Point and perform their assigned profession.

## Night Shift

NPCs **with a torch** in their inventory work during the night.

Night shift begins during the nighttime period associated with the outdoor Cold status.

When their shift begins, they travel to their Work Point and perform their assigned profession.

---

# Eating and Happiness

NPCs periodically consume food from their inventory.

NPCs generally need to be **Happy** to perform resource-producing professions.

- NPCs consume food when Hungry.
- Hungry NPCs may stop performing their assigned job.
- NPC status can be seen by looking at the NPC.

Example:

```text
Name (Happy)
```

Players may also feed an NPC by placing food on the action bar and using the corresponding action-bar key while looking at the NPC.

NPCs will not consume additional food while already Happy.

---

# Following

When commanded to follow:

- NPCs follow the player.
- NPCs may teleport to the player's position if separated by roughly 20-30 meters while sufficiently healthy.
- This behavior can include portals and dungeons.
- NPCs attempt to sit/hold on while following the player aboard a boat.

---

# Tamed NPC Death and Resurrection

When a tamed Norseman dies, the NPC leaves a tomb.

The tomb supports:

```text
[E] View Inventory
[Shift + E] Resurrect NPC
```

VikingNPC Continued 0.4.1 includes persistence fixes for resurrected Norsemen so they remain persistent after leaving load distance or reloading the world.

---

# Typical NPC Work Cycle

A typical working NPC follows this general cycle:

1. The NPC becomes active at the beginning of its assigned shift.
2. The NPC travels toward its Work Point.
3. If an appropriate Cooked Foods container is available, the NPC may collect food for its inventory.
4. During travel, NPCs can open nearby doors.
5. The NPC reaches its Work Point and begins its assigned profession.
6. At the end of the shift, resource-producing NPCs attempt to deposit eligible items into nearby tagged storage.
7. The NPC travels back toward its Home Point.
8. Doors opened by the traveling NPC should be closed behind it when possible.

Unintended door interaction may occasionally occur.

---

# Tagged Storage Containers

LivingNorsemen adds profession deposit controls to storage containers.

Available tags include:

### Deposit: None

NPCs do not use the container for profession deposits.

### Deposit: Farmer

Farmers deposit supported harvested foods and resources.

### Deposit: Lumberjack

Lumberjacks deposit supported wood products, resin, and feathers.

### Deposit: Miner

Miners deposit supported ores and stone.

### Deposit: Fisherman

Fishermen deposit caught fish.

### Deposit: Raw Meat

Cooks retrieve supported raw meats and fish from this container.

### Deposit: Cooked Foods

Cooks deposit supported cooked foods into this container.

Nearby working NPCs may also retrieve up to three cooked food items from an available Cooked Foods container for personal use.

---

# Professions

## Lumberjack

**Required Tool:** Axe

It is recommended not to place tools or marker items for unrelated professions in the NPC's inventory.

### Setup

- Give the NPC an axe capable of harvesting the desired tier of wood.
- Assign a Work Point near the desired trees.
- Optionally place the corresponding tree seed in the NPC's inventory to focus its work.

### Behavior

- Requires the NPC to be Happy.
- Works within approximately a 50-meter radius of the Work Point.
- Selects appropriate trees and performs a chopping action.
- Generates the wood, resin, and/or feathers associated with its work.
- Trees are **not physically damaged or felled**, eliminating the need to replant them.
- At the end of the shift, supported resources are deposited into a nearby **Lumberjack** container when available.

---

## Miner

**Required Tool:** Pickaxe

It is recommended not to place tools or marker items for unrelated professions in the NPC's inventory.

### Setup

- Give the NPC a pickaxe capable of mining the desired resource tier.
- Assign a Work Point near the desired ore deposits.
- Optionally place the corresponding processed metal bar in the NPC's inventory to focus the Miner on that resource.

### Behavior

- Requires the NPC to be Happy.
- Works within approximately a 50-meter radius of the Work Point.
- Selects an appropriate ore node and performs a mining action.
- Generates the corresponding ore and stone.
- Ore nodes are **not physically damaged or destroyed**.
- At the end of the shift, supported ore and stone are deposited into a nearby **Miner** container when available.

---

## Fisherman

**Required Equipment:** Fishing Rod and Bait

It is recommended not to place tools or marker items for unrelated professions in the NPC's inventory.

### Setup

- Give the NPC a Fishing Rod.
- Give the NPC appropriate bait.
- Assign a Work Point near the coast or another suitable fishing area.
- Biome-specific bait can be used to focus the Fisherman toward corresponding fish.

### Behavior

- Requires the NPC to be Happy.
- Works within approximately a 50-meter radius of the Work Point.
- Moves toward suitable fishing positions near water and performs a casting action.
- Fish are generated through the profession system rather than physically caught from visible swimming fish.
- At the end of the shift, fish are deposited into a nearby **Fisherman** container when available.

---

## Guard

It is recommended not to place tools or marker items for unrelated professions in the NPC's inventory.

### Setup

- Set the NPC to **Aggressive**.
- Configure the NPC for Guard behavior.
- Equip the NPC with appropriate weapons and armor.
- Avoid axes if you do not want the NPC interpreted as a Lumberjack.
- Assign a Work Point at the location the NPC should defend.

### Stationary Guard

Without a Patrol B point, the Guard defends its assigned area.

### Patrol Guard

To establish a patrol:

1. Assign the Guard's Work Point.
2. Have the NPC follow you to the second desired patrol location.
3. Select **Assign Patrol B**.

The Guard will walk between its Work Point and Patrol Point.

### Behavior

- Guards can work regardless of Happy/Hungry state.
- Guards engage nearby threats according to their aggression settings.
- Patrol Guards can relight nearby light sources.
- Patrol Guards can close nearby open doors.
- Weapon durability can decrease normally.
- Ammunition/projectiles can be consumed normally.

---

## Farmer

**Required Tool:** Cultivator

It is recommended not to place tools or marker items for unrelated professions in the NPC's inventory.

### Setup

- Give the NPC a Cultivator.
- Establish planted crops around the desired work area.
- Build beehives nearby if Honey collection is desired.
- Assign the Work Point near the center of the field.

### Behavior

- Requires the NPC to be Happy.
- Works within approximately a 5-meter radius of the Work Point.
- Walks between locations in the field and performs harvesting-style actions.
- Eligible crops can produce their associated food.
- Nearby beehives can provide Honey.
- Plants are **not physically harvested**, eliminating the need to replant after NPC collection.
- At the end of the shift, supported produce is deposited into a nearby **Farmer** container when available.

---

## Cook

The Cook processes supported raw meats and fish using actual cooking stations.

It is recommended not to place tools or marker items for unrelated professions in the NPC's inventory.

### Setup

The player should establish:

- A cooking station near the Cook's Work Point.
- A container tagged **Raw Meat**.
- Preferably a container tagged **Cooked Foods** nearby.
- The appropriate creature trophy in the Cook's inventory for meats requiring authorization.

Set the Cook to **Passive** and establish its Work Point near the cooking area.

### Trophy Recipes

Current supported trophy-controlled cooking includes:

```text
TrophyNeck  -> NeckTail       -> NeckTailGrilled
TrophyBoar  -> RawMeat        -> CookedMeat
TrophyDeer  -> DeerMeat       -> CookedDeerMeat
TrophyWolf  -> WolfMeat       -> CookedWolfMeat
```

Raw fish can be cooked **without a trophy**:

```text
FishRaw -> FishCooked
```

### Behavior

- The Cook can work regardless of Happy/Hungry state.
- A Hungry Cook may eat cooked food carried in its inventory.
- The Cook checks the Raw Meat container for ingredients matching recipes it is authorized to prepare.
- If no matching ingredients are available, the Cook returns to its Work Point and waits before checking again.
- The Cook retrieves ingredients from the Raw Meat container.
- Roastable meats are placed onto an actual cooking station.
- The Cook collects finished food from the cooking station.
- Raw ingredients are genuinely removed from storage rather than merely simulated.
- At the end of the shift, the Cook finishes handling occupied cooking slots before leaving.
- Supported finished foods are deposited into a nearby **Cooked Foods** container when available.

---

# Beta Status

LivingNorsemen is currently in beta.

The systems documented here are functional in the current development build, but testing is ongoing. Because development follows my own Valheim progression, later biomes and less frequently used combinations may receive less testing than early-game systems.

If you encounter a reproducible issue, providing the circumstances, NPC profession, inventory contents, nearby tagged containers, and relevant BepInEx log output can make troubleshooting considerably easier.

## Version 0.1.0-beta

Initial public beta.

Current systems include:

- Home and Work Points
- Day and Night shifts
- Tagged profession storage
- Lumberjack profession
- Miner profession
- Fisherman profession
- Farmer profession
- Cook profession
- Stationary and patrolling Guards
- Profession-focused resource targeting
- Automated resource deposits
- Cooked food distribution
- NPC travel and door interaction
- Guard patrol utility behavior

---

# Credits

**LivingNorsemen:** SephrinMods  
**VikingNPC Continued maintainer:** SephrinMods  
**Original VikingNPC/Norsemen author:** RustyMods

LivingNorsemen is an extension of VikingNPC Continued and relies on the underlying VikingNPC/Norsemen systems originally created by RustyMods.

VikingNPC Continued is maintained and published with permission from the original author.

## License

No new license has been applied to this repository at this time.

Do not assume licensing rights to the original VikingNPC/Norsemen source beyond those granted by its original author.