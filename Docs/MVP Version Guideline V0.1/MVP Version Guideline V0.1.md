# MVP Version Guideline V0.1

Duration (sec): X seconds
Status: Not started
Decorative Color Band: $\colorbox{fffde7}{~~}
\colorbox{fff9c4}{~~}
\colorbox{fff59d}{~~}
\colorbox{fff176}{~~}
\colorbox{ffee58}{~~}
\colorbox{ffeb3b}{~~}
\colorbox{fdd835}{~~}
\colorbox{fbc02d}{~~}
\colorbox{f9a825}{~~}
\colorbox{f57f17}{~~}$

# FARM

#### Seed Economy

At the start of the game, Player will do finish the tutorial and recieved a Farm equipment:

Starter gear: 

1. Straw Hats(no def no hp) + 5% growing rate (only for farming)
2. farmer Suit(no def no hp) + 10% crop drops (only for farming)
3. Hoe(only for farming, unavailable at night): +2/4/6% growing rate & 1/3/6% crop drops(depend on upgrade)
4. Bag of seeds: 3 sweet potato seeds
5. Watering cans: +4/8/10% growing rates (only for farming)

The farming economy is built around a simple resource loop:

**Seed → Plant → Harvest → Sell → Gold**

Or

**Harvest → Cooking → Food Buff 1:1**

| Seed | Growth | Harvest Value | Monster | Seed Drop |
| --- | --- | --- | --- | --- |
| Sweet Potato | Fast (6min) | 750-1000 Gold (random) | Tuber Maw | 1-4 SP(60/20/10/10%) |
| Taro | Medium(15min) | 1400-1800 Gold (random) | Taro Brute | 1-3 T (70/20/10%) |
| Corn | Medium(15min) | 1400-1800 Gold (random) | Corn Musketeer | 1-3 C (70/20/10%) |

Each crop have purpose:

1. Economic value:
- Harvest and sell them for Gold.
- Golds are used for upgrade, cooking and preparation.
1. Combat preparation:
- Keep harvested resources to create food buffs before nighttime battles.
1. Crop idenity:

Taro → Health / Defense

Sweet Potato → Stamina / Mobility

Corn → Recovery / Utility

**Crops Growth:**

![image.png](MVP%20Version%20Guideline%20V0%201/90006524-a19b-41a7-b3aa-799db6b08e85.png)

Corn:

![image.png](MVP%20Version%20Guideline%20V0%201/b4461ee1-cc8d-4579-a011-f75a1b3934fa.png)

Taro & Sweet potato:

![image.png](MVP%20Version%20Guideline%20V0%201/f5d9fb8e-e9ff-45a0-99b9-8bae1c2e8048.png)

# Cooking

Cooking is performed inside the player’s kitchen.

The MVP version focuses on simplicity:

**3 Ingredients → 1 Food
Note: Some recipe need water to cook**

At the start of the game, Player recieved a bottle(can be upgrade): 100/100 litre (use to drink or cook) after using it, player can repurchase it in shop with 5k golds.

MVP start with **3 core recipes**:

| Food Type | Fungsi |
| --- | --- |
| Recovery Food | Restore HP(small recovery+10%, medium recovery+25%, large recovery+50% max HP) |
| Stamina Food | Temporary restore / improve total stamina  |
| Battle Food | Temporary combat buff for 25%(Dmg+, Atk spd+, Spd+)   |

Cooking creates another layer of resource management:

**Farm Resources → Food Preparation → Night Combat Advantage**

Players must decide whether resources are more valuable as:

- Immediate Gold income
- Long-term survival preparation

[Food buff:](MVP%20Version%20Guideline%20V0%201/Food%20buff%203da97560e11a808da4dfde70b8c9607a.csv)

# Brawl

The MVP combat system focuses on:

[**FTUE Content Definition**](https://app.notion.com/p/FTUE-Content-Definition-3cd97560e11a8045b747d2cd2853878e?pvs=21)

- 1 Weapon (Root Cleaver)
- 3 Base Enemies
- 3 Boss Forms
- Wave System
- Monster Resources

---

### Enemy Evolution System:

The system expands existing enemies through evolution.

**3 Seeds → 6 Forms**

| Seed | Normal | Boss Evolution |
| --- | --- | --- |
| Sweet Potato | Tuber Maw | Cyclops Tuber Maw |
| Taro | Taro Brute | Taro Colossus |
| Corn | Corn Musket | Corn Ranger |

Jadi guideline 6 **Enemy Variant Based on Seeds** tetap terpenuhi tanpa menambah jumlah seed.

| Enemy | HP | Armor | Movement spd | DMG (contact) | Atk Spd | Atk range | aggro range | Knockback res | Unique Skill |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Tuber Maw | 100 | 20 | 3m/s | 15 | 0.4 atk/s | 1.5m | 6m | 20% | -Burrow strike: dive into ground, track(1-1.5s) and strike the player.(5s cooldown)                                  -Tunnel Rush: getaway when low HP |
| Cyclops Tuber Maw (Boss) | 600 | 40 | 3m/s | 30 | 0.4 atk/s | 1.5m | 10m (based on arena) | 35% | -Tracking Beam: fire lazer at player that track(0.5s) and deal 25 (15s cooldown)  (cant be stagger during charge)(+4.5m range)                                                 -Summon: spawn 3 Tuber Maw (45s cooldown) (cant be stagger during charge) |
| Taro Brute | 150 | 15 | 1m/s | 25 | 0.5 atk/s | 2.5m | 5m | 80% | -Special hit: knockback when hit player for 1.5m                                  -Root guard: boost armor by 50%.(30s cooldown)  |
| Taro Colossus (Boss) | 900 | 55 | 2m/s | 45 | 0.5 atk/s | 2m | 8m | immune | -AirBourne: Jump up on air out of screen leave a shadow on ground & track player(0.5m/s) after 5s, Slam down deal dmg to player and create 3 rock flying straight at player deal 5dmg each(can be stagger if hit 4 times)                                                  -Grapple (if player shadow touch boss shadow): pick player and slam in ground (stun:1s)(30s cooldown) (can be stagger)  |
| Corn Musketeer |  85 | 0 | 3m/s | 15 | 0.8 atk/s | 10m | arena wide | 15% | -Kernel shot: Charged shot(3s) and deal (45dmg)(20s cooldown)(can be stagger)                               -Burst shot: fire 3 shot same time(10s cooldown) |
| The Ranger(Boss) | 750 | 30 | Stationary | 60 | 0.5 atk/s | 5m | arena wide | immune | -Corn Heavens Fall: Fire 4 corn cob in the air and drop down on player head deals 30dmg(30s cooldown)                                         -Summoning: 3 musketeer (10s cooldown)                                          -Kernel Burst (when >50%): boss angry then fire 20 corn kernel(shot random from itself)   |

---

## Wave Progression

Jumlah wave sekaligus menunjukkan progression harian:

**Day 1 → 1 Wave**

**Day 2 → 2 Waves**

**Day 3 → 3 Waves**

**Day 4 → 4 Waves**

**Day 5 → 5 Waves**

### Day 5 Structure

- Wave 1–3:
    - Normal enemy encounters
- Wave 4–5:
    - Boss encounters

Boss progression:

- Day 4:
    - First Boss Introduction + 4 normal enemy
- Day 5:
    - Two Boss Encounters + 3 normal enemy

## BRAWL RESOURCE

### Monster Material

**Fight → Monster Drop → Weapon Upgrade**

Sweet Potato dan Taro memberikan material berbeda sehingga menentukan **jenis upgrade**, bukan hanya nilai damage.

| Enemy | Drop | Type | Kegunaan |
| --- | --- | --- | --- |
| **Tuber Maw** | **1-3 Mutated Root 50% 200-500 Golds 50%**          | Normal Material | Upgrade jalur **Speed / Mobility** |
| **Cyclops Tuber Maw** | **1-2 Cyclops Eye 30%      700-1000 Golds 70%** | Boss Material | Membuka special effect berbasis **Precision / Tracking** |
| **Taro Brute** | **1-3 Hardened Root 50% 200-500 Golds 50%**          | Normal Material | Upgrade jalur **Power / Defense / Knockback** |
| **Taro Colossus** | **1 Colossus Core 20%     700-1000 Golds 80%** | Boss Material | Membuka special effect berbasis **Heavy Impact / Armor** |
| Corn Musketeer | 1-3Kernel Shrapnel 50%   200-500 Golds | normal Material | Upgrade jalur **Area / Splash** |
| The Ranger | 1-2 Cob Core 15%             700-1000 Golds 85% | Boss Material | Membuka special effect berbasis **Chain Hit / Crowd Control**  |

## **Weapon Upgrade System**

[1. Upgrade System](MVP%20Version%20Guideline%20V0%201/1%20Upgrade%20System%2040b97560e11a80228321f02c72a51dac.csv)

# 2. Three Seed Specialization Paths

Each seed has its own combat identity.

2.1 Sweet Potato Path – Speed & Mobility

[The way of Sweet Potato](MVP%20Version%20Guideline%20V0%201/The%20way%20of%20Sweet%20Potato%2040b97560e11a80e9bcdbc9b6dd450fbb.csv)

2.2 Taro Path – Heavy Damage & Defense

[Harden Mind Of Taro](MVP%20Version%20Guideline%20V0%201/Harden%20Mind%20Of%20Taro%2040b97560e11a80b9bb2edc08783e6b44.csv)

2.3 Corn Path – Ranged Damage & Critical Hits

[Deadly Accuracy of Corn](MVP%20Version%20Guideline%20V0%201/Deadly%20Accuracy%20of%20Corn%2040b97560e11a805882a0d19a957fb0b9.csv)

### Special Upgrade – Level 1 to Level 5

- Each seed path has 5 upgrade nodes.
- Players must unlock nodes sequentially.
- Each node requires its corresponding monster material.
- The final node unlocks an Ultimate Specialization.
- Specialization does not reset Basic Upgrade levels.

### 3. BASIC UPGRADE lv1 → lv10

Proposed stat progression (Weapons):

| Level | Damage Bonus |
| --- | --- |
| Lv.1 | Base Damage |
| Lv.2 | +15% |
| Lv.3 | +30% |
| Lv.4 | +45% |
| Lv.5 | +60% |
| Lv.6 | +75% |
| Lv.7 | +90% |
| Lv.8 | +105% |
| Lv.9 | +120% |
| Lv.10 | +150% |

Proposed stat progression (Armor)

| Upgrade | Gold | Stat Bonus |
| --- | --- | --- |
| Lv.1 → 2 | 300 | Base Stats |
| Lv.2 → 3 | 500 | +2% |
| Lv.3 → 4 | 800 | +2% |
| Lv.4 → 5 | 1,200 | +2% |
| Lv.5 → 6 | 1,800 | +2% |
| Lv.6 → 7 | 12,500 | +4% |
| Lv.7 → 8 | 23,500 | +5% |
| Lv.8 → 9 | 35,000 | +5% |
| Lv.9 → 10 | 47,000 | +10% |

# **Combat Set:**

01 – Sweet Potato Set Assassin / Mobility
Fast attacks, dashing, evasion and combo damage.

1. Sweet Potato Combat Set

Lore: Equipment developed from Sweet Potato monster materials. Its lightweight structure allows the player to move quickly and strike enemies before they can react. 

| Equipment | Name | Function |
| --- | --- | --- |
| Weapon | Root Cleaver | Fast melee attacks |
| Helmet | Burrow Hood | Increased critical chance |
| Chest | Swift Root Armor | Increased movement speed |
| Gloves | Fang Grips | Increased attack speed |
| Boots | Underground Striders | Improved dash |
| Accessory | Sweet Potato Heart | Bonus damage after dodging |

### Combat Stats

Suggested bonuses when the complete set is equipped

| Attribute | Bonus |
| --- | --- |
| Attack Speed | +20% |
| Movement Speed | +15% |
| Critical Chance | +10% |
| Dash Cooldown Reduction | -20% |
| Defense | -5% |

### Set Bonus

- 2 Pieces: +10% Attack Speed.
- 4 Pieces: Successful dodges increase damage by 15% for 3 seconds.
- 6 Pieces: Shadow Dash resets its cooldown after defeating an enemy (once every 5 seconds).

Combat identity: Hit fast → Dodge → Reposition → Attack again.

## Time Management Day & Night

### 1. Proposed Parameters

| Parameter | Value |
| --- | --- |
| Day phase | **18 real minutes** (06:00 → 18:00 in-game, 1 real minute = 40 in-game minutes) |
| Dusk bell | **Minute 15:00** (16:00 in-game), 3 minutes left |
| Auto-sleep | Minute 18:00 (18:00 in-game) (forced player to sleep) |
| Night phase | No timer, ends when waves are cleared |
| Crop growth | **Daytime only**; frozen at night, progress is kept |
| Sleeping early | Allowed, with a confirmation dialog if crops are about to ripen |

### 2. Timeline of One Day Phase (18 minutes) -ideal play

| Real minute | In-game time | Activity |
| --- | --- | --- |
| 0:00 - 1:00 | 06:00 - 06:40 | Collect overnight harvest, sell, **plant Taro/Corn + Sweet Potato** |
| 1:00 - 3:00 | 06:40 - 08:00 | Fetch water, start the pot (cooking runs in the background) |
| 3:00 - 6:00 | 08:00 - 10:00 | Upgrade at the Workbench, trade |
| **6:30 - 7:30** | 10:20 - 11:00 | **Sweet Potato cycle 1 ripens**: harvest, sell, replant |
| 7:30 - 12:00 | 11:00 - 14:00 | Cook food, upgrade, expand |
| **12:30 - 13:30** | 14:20 - 15:00 | **Sweet Potato cycle 2 ripens**: harvest, replant |
| 13:30 - 15:00 | 15:00 - 16:00 | Finalize 3 food slots, check water |
| **15:00** | 16:00 | **Dusk bell DING DONG** |
| 15:30 - 16:30 | 16:20 - 17:00 | Taro/Corn ripen: harvest, sell, replant |
| 16:30 - 18:00 | 17:00 - 18:00 | Go home, eat buff food, go to bed |

### 3. Cooking

| Food | Cook time | Water | Effect |
| --- | --- | --- | --- |
| Recovery | 20 seconds | 10 L | Instant heal (+10/25/50% HP) |
| Stamina | 30 seconds | 15 L | 4 minutes |
| Battle (+25%) | 45 seconds | 20 L | 3 minutes |
- 100 L bottle, well refills in 10 seconds, max 2 times per day.
- Only **3 food slots** can be carried into the night.
- Battle Food (3 minutes) should be used **right before a boss wave**. Nights 4 and 5 therefore need a 30-second “prep” window between waves.

### 4. Night Phase

Each wave takes about 2:30 minutes (boss waves about 3:00), with a 30-second break between waves.

| Day | Wave structure | Night | Total day + night |
| --- | --- | --- | --- |
| 1 | 1 normal wave (5 enemies) | ~2:30 | ~20:30 |
| 2 | 2 normal waves (4+5) | ~5:30 | ~23:30 |
| 3 | 3 normal waves (4+5+6) | ~9:00 | ~27:00 |
| 4 | 3 normal + W4 (1 boss + 4 enemies) | ~12:30 | ~30:30 |
| 5 | 3 normal + W4 boss + W5 boss + 3 enemies | ~15:30 | ~33:30 |

### 5. Economy Balance (6 plots)

With the current values:

| Crop | Gold per harvest (6 plots) | Realistic harvests/day | Gold/day |
| --- | --- | --- | --- |
| Sweet Potato (6 min) | 4,500 (all plot) | ~2.5 (accounting for travel delay) | **~13,100** |
| Taro/Corn (15 min) | 9,600 (all plot) | ~1.1 | **~10,500** |
- Sweet Potato becomes the **active** choice (run back to replant 2-3 times per day).
- Taro/Corn becomes the **passive** choice (plant once, then go upgrade and cook).

Total crop income over 5 days is roughly 50-60k gold, plus about 13k from night monsters. To keep the player feeling slightly short on money, total upgrade costs (weapon, water bottle, plots, cooking ingredients) should be about **45-50k**.

Opportunity cost of food: 1 dish = 3 ingredients, worth about 2,100 gold (Sweet Potato at the new value) to 4,800 gold (Taro/Corn), or 20-45% of daily income. This keeps the “sell or keep for cooking” dilemma the document is aiming for.

# Provide details on character movement, game saves, weapon, changing outfits and achievements

![Area Rumah Harvest Pipeline-2026-09-10-105210.png](MVP%20Version%20Guideline%20V0%201/Area_Rumah_Harvest_Pipeline-2026-09-10-105210.png)

**Animation Drafting**

Tuber Maw: - preference

![image.png](MVP%20Version%20Guideline%20V0%201/image.png)

**Cyclop Tuber Maw: - preference**

![image.png](MVP%20Version%20Guideline%20V0%201/image%201.png)

**Taro Brute: - preference**

![image.png](MVP%20Version%20Guideline%20V0%201/image%202.png)

**Taro Colossus (boss): - preference**

![image.png](MVP%20Version%20Guideline%20V0%201/image%203.png)

Corn musketeer: -preference

![image.png](MVP%20Version%20Guideline%20V0%201/image%204.png)

The Ranger: -preference

![image.png](MVP%20Version%20Guideline%20V0%201/image%205.png)

# Frame

1. Combat

![1000274575.jpg](MVP%20Version%20Guideline%20V0%201/1000274575.jpg)

# Storyline overview: