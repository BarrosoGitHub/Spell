# Equipment drops

Create an asset through **Create > Equipment > Drop** for each equipment and rarity combination, such as Common Boots or Rare Boots. Set its equipment name, slot, rarity, and minimum/maximum stat bonuses.

`EquipmentDrop.Generate()` returns a new `Equipment` and a separate `Stats` instance on every call. Assign an `EquipmentRaritySettings` asset to each drop. Generation randomly selects distinct stats up to the maximum line count for that rarity, then rolls each selected range. A `(0, 0)` range excludes a stat, and equal nonzero bounds give a fixed bonus if selected. Reversed bounds are accepted. The asset's settings remain unchanged.

For example, Common Boots could have Max Health `(5, 10)` and Defense `(1, 2)`, with all other ranges left at zero. With a Common limit of one line, each generated pair receives either health or defense; with a limit of two, it receives both.

Percentage bonus ranges use fractions: `(0.02, 0.04)` means +2–4%. Critical Chance uses percentage points instead: `(2, 4)` means +2–4 percentage points. Critical Damage is an additive multiplier bonus. The generator only fills existing stat fields; whether those stats affect gameplay depends on their consumers.

Reference the asset from the caller and generate when a reward is earned:

```csharp
[SerializeField] private EquipmentDrop equipmentDrop;

// In the reward handler:
Equipment equipment = equipmentDrop.Generate();
```

## Drop tables

Create an asset through **Create > Equipment > Drop Table**. Add entries referencing existing `EquipmentDrop` assets and assign a weight to each:

| Drop asset | Weight | Chance |
| --- | ---: | ---: |
| Common Boots | 70 | 70% |
| Uncommon Boots | 20 | 20% |
| Rare Boots | 10 | 10% |

Weights are relative and do not have to total 100. Zero disables an entry. Each drop asset still defines its own rarity and stat ranges. Multiple equipment types or rarities can share a table; duplicate references count as separate weighted entries.

```csharp
[SerializeField] private EquipmentDropTable dropTable;

// In the reward handler:
Equipment equipment = dropTable.Generate();
```

`EquipmentDropTable.Generate()` selects one asset by weight and calls its `Generate()` method. Each call returns one new equipment instance. Empty tables, invalid weights, missing enabled drop references, or a zero total weight raise an actionable exception. Pickup spawning and automatic equipping remain the caller's responsibility.

## Starter assets

`Assets/Scripts/Infrastructure/Scriptable Objects/Equipment` contains Common, Uncommon, Rare, Epic, and Legendary Boots assets plus `BootsDropTable`. Boots is currently the only defined equipment slot.

The table uses weights 60/25/10/4/1, giving those same percentages. Sample health ranges are 5–10, 10–20, 20–35, 35–50, and 50–75 respectively. All ten stat ranges are populated with progressively stronger sample bonuses by rarity; the maximum line count still controls how many are selected. These are editable starting values, not finalized balance. Assign `BootsDropTable` to a reward component's `EquipmentDropTable` field to use it. No scene reward component is wired automatically.

## Equipment manager

Drag `Assets/Scripts/Game/Managers/EquipmentManager.prefab` into a scene. It already references `BootsDropTable`. Alternatively, add `EquipmentManager` to a GameObject and assign a drop table. Consumers reference the manager through a serialized field; no singleton or scene search is needed.

```csharp
[SerializeField] private EquipmentManager equipmentManager;
[SerializeField] private EquipmentDrop specificDrop;

// Generate from a specific definition:
Equipment specific = equipmentManager.GenerateEquipment(specificDrop);

// Roll any enabled drop using table weights:
Equipment random = equipmentManager.GetRandomEquipment();

// Roll only enabled Rare entries, keeping their relative weights:
Equipment rare = equipmentManager.GetRandomEquipment(Rarity.Rare);
```

Each call generates a fresh item. A rarity request never falls back to another rarity: if no enabled entry matches, generation throws a configuration exception. Zero-weight entries stay disabled for both kinds of roll. The manager does not retain generated equipment, equip it, or spawn pickups; the caller owns the returned item. The prefab must be added to a scene before scene components can reference its instance.

## Level-up rewards

`RewardManager` subscribes to `EntityManager.OnControllerLeveledUp` while enabled. For every notification it requests three independent weighted rolls from `EquipmentManager` and raises `OnRewardsGenerated(controller, rewards)`. The payload is an `IReadOnlyList<Equipment>` containing three fresh instances. Each offer owns a new array, so later level-ups do not overwrite earlier offers. Repeated equipment definitions or rarities are allowed.

The existing EquipmentManager prefab includes a RewardManager referencing its equipment component. Place it in the scene; EntityManager defaults to its existing singleton unless explicitly assigned. Consumers subscribe to the RewardManager instance:

```csharp
private void OnEnable()
{
    rewardManager.OnRewardsGenerated += ShowRewards;
}

private void OnDisable()
{
    rewardManager.OnRewardsGenerated -= ShowRewards;
}

private void ShowRewards(Controller controller, IReadOnlyList<Equipment> rewards)
{
    // Show the three choices for this controller.
}
```

RewardManager generates and announces choices; it does not equip them or own their UI. It unsubscribes when disabled or destroyed, preventing duplicate subscriptions on enable/reuse. Offers are generated for any controller reported by EntityManager. XP granted directly to an Entity must be reported by its caller through EntityManager's level-up notification flow to produce an offer.

## Maximum stat lines by rarity

Create **Equipment > Rarity Settings** to configure the maximum number of distinct bonus stats per item. The included `EquipmentRaritySettings.asset` is assigned to all five starter Boots definitions and defaults to:

| Rarity | Maximum lines |
| --- | ---: |
| Common | 1 |
| Uncommon | 2 |
| Rare | 3 |
| Epic | 4 |
| Legendary | 5 |

Change that shared asset to update all referencing drops. A line means one selected stat field (flat and percentage bonuses are separate lines). Selection is uniform among nonzero configured ranges, without duplicates. Generation selects the smaller of the rarity maximum and the number of eligible stats; zero maximum produces no bonuses. A selected range containing zero may roll zero, so the limit is an upper bound on visible nonzero bonuses. Missing settings cause a configuration exception. These limits apply through direct drops, tables, and the manager.

All ten stats are eligible on the starter Boots. Their positive ranges allow generated items to receive 1/2/3/4/5 distinct nonzero lines respectively. Sample ranges are:

| Stat | Common | Uncommon | Rare | Epic | Legendary |
| --- | --- | --- | --- | --- | --- |
| Max health | 5–10 | 10–20 | 20–35 | 35–50 | 50–75 |
| Base damage | 0.25–0.5 | 0.5–1 | 1–2 | 2–3 | 3–5 |
| Attack speed (flat) | 0.02–0.04 | 0.04–0.06 | 0.06–0.09 | 0.09–0.12 | 0.12–0.16 |
| Defense | 0.1–0.2 | 0.2–0.4 | 0.4–0.7 | 0.7–1 | 1–1.5 |
| Critical chance (percentage points) | 1–2 | 2–3 | 3–5 | 5–7 | 7–10 |
| Critical damage (added multiplier) | 0.05–0.1 | 0.1–0.15 | 0.15–0.25 | 0.25–0.4 | 0.4–0.6 |
| Health / damage / defense bonus | 2–4% | 4–6% | 6–9% | 9–12% | 12–16% |
| Attack speed bonus | 1–2% | 2–3% | 3–5% | 5–7% | 7–10% |

Percentage rows are stored as fractions in the assets. These values are starting configuration, not playtested balance.
