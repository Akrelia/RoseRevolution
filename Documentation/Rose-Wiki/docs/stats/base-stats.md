# Base Character Stats

> **Sources**
>
> - Original ROSE Online Server
> - `src/sho_gameserver/src/common/cuserdata.cpp`
> - `src/sho_gameserver/src/cobjavt.cpp`
>
> This document describes the six primary character attributes used throughout
> ROSE Online.

---

# Overview

Every player character begins with **15 points** in each primary attribute.

Unlike **Derived Stats**, such as Health Points (HP), Movement Speed (MS), or
Attack Power (AP), these attributes are increased manually by the player through
level progression (stat points), or modified through equipment bonus stats, and passive skills.

---

# Starting Attributes

| Attribute | Initial Value |
|-----------|--------------:|
| STR | 15 |
| DEX | 15 |
| INT | 15 |
| CON | 15 |
| SEN | 15 |
| CHA | 10 (I think) | 

---

# Attribute Overview

| Attribute | Description |
|-----------|-------------|
| **STR** | Increases Health Points, Defence, Carry Weight and Attack Power for most melee weapon classes. |
| **DEX** | Increases Movement Speed, Dodge Rate and Attack Power for Bows, Katars and Dual Weapons. |
| **CON** | Increases Accuracy (Hit Rate), Critical Rate, and contributes heavily to Gun and Launcher damage. Also affects crafting success rates. |
| **INT** | GIncreases Mana Points, Magical Resistance, Buff potency, Healing effectiveness and Magic Weapon damage. |
| **SEN** | Increases Critical Rate while improving Weapon Attack Power scaling for ranged and magical weapons. Also affects crafting success rates. |
| **CHA** | Primarily affects NPC interactions, quest rewards and drop rewards. *(Current implementation unconfirmed.)* |

---

# Abbreviation Index

| Abbreviation | Meaning |
|--------------|---------|
| STR | Strength |
| DEX | Dexterity |
| CON | Constitution |
| INT | Intelligence |
| SEN | Sense |
| CHA | Charm |
| AP | Attack Power |
| DEF | Defence |
| RES | Resistance |
| CRIT | Critical Rate |
| HIT | Hit Rate |
| HP | Health Points |
| MP | Mana Points |
| MS | Movement Speed |
| 1H | One-Handed |
| 2H | Two-Handed |

---

# Strength (STR)


## Primary Effects

- Melee Attack Power
- Physical Defence
- Maximum Health Points
- Maximum Carry Weight
- Weapon Skill Damage
- Unarmed Attack Power

## Derived Stats

| Derived Stat | Formula Contribution |
|--------------|----------------------|
| Defence | `(STR + 5) × 0.35` |
| Max HP | `STR × 2` |
| Max Weight | `STR × 6` |
| Attack Power | Primary scaling attribute for melee weapons |

---

# Dexterity (DEX)


## Primary Effects

- Movement Speed
- Dodge Rate
- Bow Attack Power
- Katar Attack Power
- Dual-Wield Attack Power
- Unarmed Attack Power

## Derived Stats

| Derived Stat | Formula Contribution |
|--------------|----------------------|
| Movement Speed | `(DEX + 500)` multiplier |
| Dodge Rate | `(DEX + 10) × 0.8` |
| Attack Power | Primary scaling for Bows and Katars |

---

# Constitution (CON)

## Primary Effects

- Hit Rate
- Critical Rate
- Gun Attack Power
- Launcher Attack Power
- Crafting Success

## Derived Stats

| Derived Stat | Formula Contribution |
|--------------|----------------------|
| Hit Rate | `(CON + 10) × 0.8` |
| Critical Rate | `(CON + 20) × 0.2` |

---

# Intelligence (INT)

## Primary Effects

- Mana Points
- Magic Attack Power
- Magical Resistance
- Healing
- Buff Potency
- Magic Skill Damage

## Derived Statis

| Derived Stat | Formula Contribution |
|--------------|----------------------|
| Maximum MP | `INT × 4` |
| Resistance | `(INT + 5) × 0.6` |
| Magic Weapon Scaling | Primary attribute |

---

# Sense (SEN)
## Primary Effects

- Critical Rate
- Bow Weapon Scaling
- Gun Weapon Scaling
- Launcher Weapon Scaling
- Wand Weapon Scaling
- Crafting Success

---

# Charm (CHA)


## Primary Effects *(Unconfirmed)*

- Quest Rewards *(Unconfirmed)*
- Drop Rewards *(Unconfirmed)*
- NPC Interaction *(Unconfirmed)*

---

# Weapon Attribute Scaling

The following table shows the **base attribute contribution** for each weapon
type before Weapon Attack Power is applied.

| Attribute | Swords | Axes | Maces | Wands | Staffs | Katars | Dual-Wield | Bows | Guns | Launchers |
|-----------|:------:|:----:|:-----:|:-----:|:------:|:-------:|:-----------:|:----:|:----:|:---------:|
| **STR** | 0.75 | 0.75 | 0.75 | — | 0.40 | 0.42 | 0.63 | 0.20 | — | 0.52 |
| **DEX** | — | — | — | — | — | 0.55 | 0.45 | 0.62 | 0.40 | — |
| **INT** | — | — | — | 0.60 | 0.40 | — | — | — | — | — |
| **CON** | — | — | — | — | — | — | — | — | 0.50 | 0.50 |
| **Level** | 0.20 | 0.20 | 0.20 | 0.20 | 0.20 | 0.20 | 0.20 | 0.20 | 0.20 | 0.20 |

---

# Weapon AP Scaling

Several weapon types use additional attribute modifiers when calculating the
Weapon Attack Power multiplier.

| Weapon | Scaling Formula |
|---------|-----------------|
| Swords / Axes / Maces | `WeaponAP × (STR × 0.05 + 29) / 30` |
| Staffs | `WeaponAP × (INT × 0.05 + 29) / 30` |
| Wands | `WeaponAP × (SEN × 0.10 + 26) / 27` |
| Katars | `WeaponAP × (DEX × 0.05 + 20) / 21` |
| Dual-Wield | `WeaponAP × (DEX × 0.05 + 25) / 26` |
| Bows | `WeaponAP × (DEX × 0.04 + SEN × 0.03 + 29) / 30` |
| Guns | `WeaponAP × (CON × 0.03 + SEN × 0.05 + 29) / 30` |
| Launchers | `WeaponAP × (CON × 0.04 + SEN × 0.05 + 29) / 30` |

---

# Attribute Interaction Summary


| Gameplay System | Primary Attributes |
|-----------------|--------------------|
| Sword Damage | STR |
| Axe Damage | STR |
| Mace Damage | STR |
| Staff Damage | STR + INT |
| Wand Damage | INT + SEN |
| Bow Damage | DEX + STR + SEN |
| Gun Damage | DEX + CON + SEN |
| Launcher Damage | STR + CON + SEN |
| Katar Damage | STR + DEX |
| Dual-Wield Damage | STR + DEX |
| Critical Rate | SEN + CON |
| Hit Rate | CON |
| Defence | STR |
| Resistance | INT |
| Health Points | STR |
| Mana Points | INT |
| Movement Speed | DEX |
| Dodge Rate | DEX |

---

# References

| Function | Source |
|----------|--------|
| `Cal_ATTACKPOWER()` | `cobjavt.cpp` |
| `Cal_RunSPEED()` | `cuserdata.cpp` |
| `Cal_AvoidRATE()` | `cuserdata.cpp` |
| `Cal_CRITICAL()` | `cuserdata.cpp` |
| `Cal_HIT()` | `cobjavt.cpp` |
| `Cal_DEFENCE()` | `cuserdata.cpp` |
| `Cal_RESIST()` | `cuserdata.cpp` |
| `Cal_MaxHP()` | `cuserdata.cpp` |
| `Cal_MaxMP()` | `cuserdata.cpp` |
| `Cal_MaxWEIGHT()` | `cuserdata.cpp` |


---

> Note that for leveling up the AP and SP given to the player is to be determined by you specifically.
> If you plan to add more skills the skill points given should be determined based on feel.