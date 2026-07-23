# Formula Reference

> **Source**
>
> Original ROSE Online Server
>
> This document serves as a mathematical reference for all combat and character
> formulas currently implemented in `RoseStats.h`.
>
> Each section includes the original server function from which the formula was ripped from.

---

# Character Statistics

## Run Speed

**Source**

```
Cal_RunSPEED()
```

**Formula**

```text
RunSpeed =
(BootsSpeed + BackSpeed + 20)
× (DEX + 500)
÷ 100
+ AdditiveBonus
```

---

## Avoid Rate

**Source**

```
Cal_AvoidRATE()
```

**Formula**

```text
Avoid =
(DEX + 10) × 0.8
+
(Level × 0.5)
```

---

## Critical Rate

**Source**

```
Cal_CRITICAL()
```

**Formula**

```text
Critical =
SEN
+
(CON + 20) × 0.2
```

---

## Hit Rate (Armed)

**Source**

```
Cal_HIT()
```

```text
Hit =
(CON + 10) × 0.8
+
WeaponQuality × 0.6
+
WeaponDurability × 0.8
```

---

## Hit Rate (Unarmed)

```text
Hit =
(CON + 10) × 0.5
+
15
```

---

## Defence

**Source**

```
Cal_DEFENCE()
```

```text
Defence =
ItemDEF
+
(STR + 5) × 0.35
+
(Level + 15) × 0.7
```

---

## Resistance

**Source**

```
Cal_RESIST()
```

```text
Resistance =
ItemRES
+
(INT + 5) × 0.6
+
(Level + 15) × 0.8
```

---

## Maximum HP

```text
HP =
(Level + 12) × 8
+
STR × 2
```

---

## Maximum MP

```text
MP =
(Level + 4) × 3
+
INT × 4
```

---

## Maximum Weight

```text
Weight =
1100
+
Level × 5
+
STR × 6
```

---

## Attack Speed

**Source**

```
Cal_ATTACKSPEED()
```

```text
AttackSpeed =
1500
÷
(WeaponAttackSpeed + 5)
```

---

# Attack Power

---

## Unarmed

```text
Attack =
STR × 0.5
+
DEX × 0.3
+
Level × 0.2
```

---

## One-Hand / Two-Hand

```text
Attack =
STR × 0.75
+
Level × 0.2
+
WeaponAP ×
(STR × 0.05 + 29)
/ 30
```

---

## Bow

```text
Attack =
DEX × 0.62
+
STR × 0.20
+
Level × 0.20
+
ItemQuality

+

(WeaponAP + ItemQuality × 0.5 + 8)

×

(DEX × 0.04 + SEN × 0.03 + 29)

÷ 30
```

---

## Gun

```text
Attack =
DEX × 0.40
+
CON × 0.50
+
Level × 0.20
+
ItemQuality

+

(WeaponAP + ItemQuality × 0.6 + 8)

×

(CON × 0.03 + SEN × 0.05 + 29)

÷ 30
```

---

## Launcher

```text
Attack =
STR × 0.52
+
CON × 0.50
+
Level × 0.20
+
ItemQuality

+

(WeaponAP + ItemQuality + 12)

×

(CON × 0.04 + SEN × 0.05 + 29)

÷ 30
```

---

## Magic Staff

```text
Attack =
STR × 0.40
+
INT × 0.40
+
Level × 0.20

+

WeaponAP ×
(INT × 0.05 + 29)
÷ 30
```

---

## Magic Wand

```text
Attack =
INT × 0.60
+
Level × 0.20

+

WeaponAP ×
(SEN × 0.10 + 26)
÷ 27
```

---

## Katar

```text
Attack =
STR × 0.42
+
DEX × 0.55
+
Level × 0.20

+

WeaponAP ×
(DEX × 0.05 + 20)
÷ 21
```

---

## Dual Weapon

```text
Attack =
STR × 0.63
+
DEX × 0.45
+
Level × 0.20

+

WeaponAP ×
(DEX × 0.05 + 25)
÷ 26
```

---

# Animation

## Run Animation Rate (This is personally done for my unreal engine project, find what suits you best. It was also winged by AI with manual tweaking from my side)

```text
AnimationRate =
(RunSpeed + 180)
÷ 600
```

---

# Combat

## Success Rate

```text
Success =
((AtkLevel + 10)
-
DefLevel × 1.1
+
Random(1-50))

×

(AtkHit × 1.1
-
DefAvoid × 0.93
+
Random(1-60)
+
5
+
AtkLevel × 0.2)

÷ 80
```

---

## Critical Roll

```text
CriticalSuccess =

((Random(1-100)+1) × 3
+
Level
+
30)

×

16

÷

(Critical + 70)

Critical if < 20
```

---

## Normal Damage

```text
Damage =

AttackPower

×

(Success × 0.03 + 26)

×

(AttackPower - Defence + 250)

÷

((Defence + Avoid × 0.4 + 5)
×145)
```

Minimum Damage

```
5
```

---

## Critical Damage

```text
Damage =

AttackPower

×

(Success × 0.05 + 29)

×

(AttackPower - Defence + 230)

÷

((Defence + Avoid × 0.3 + 5)
×100)
```

Minimum Damage

```
10
```

---

# Skill Damage

## Weapon Skill

```
Get_SkillDAMAGE()
DamageType = 1
```

Uses

- Attack Power
- Defence
- Resistance
- Avoid
- Sense
- Hit Rate
- Skill Power

---

## Magic Skill

```
DamageType = 2
```

Uses

- Intelligence
- Resistance
- Attack Power
- Sense
- Skill Power

---

## Unarmed Skill

```
DamageType = 3
```

Uses

- Intelligence
- Skill Power
- Sense
- Defence
- Resistance

---

# Buff Scaling

```
Get_SkillAdjustVALUE()
```

```text
FinalValue =

AbilityValue × Rate

÷100

+

Value ×

(CasterINT + 300)

÷315
```

---

# Constants

| Constant | Value |
|----------|-------:|
| Maximum Damage | 9999 |
| Minimum Damage | 5 |
| Minimum Critical Damage | 10 |
| Success RNG | 1-50 |
| Hit RNG | 1-60 |
| Critical RNG | 1-100 |
| Damage RNG | 1-30 |

---

# Original Server Functions

| Function | Source |
|----------|--------|
| Cal_RunSPEED | cuserdata.cpp |
| Cal_AvoidRATE | cuserdata.cpp |
| Cal_CRITICAL | cuserdata.cpp |
| Cal_HIT | cobjavt.cpp |
| Cal_DEFENCE | cuserdata.cpp |
| Cal_RESIST | cuserdata.cpp |
| Cal_ATTACKPOWER | cobjavt.cpp |
| Cal_RunAniSPEED | cobjai.h |
| Get_SuccessRATE | calculation.cpp |
| Get_CriSuccessRATE | calculation.cpp |
| Get_BasicDAMAGE | calculation.cpp |
| Get_SkillDAMAGE | calculation.cpp |
| Get_SkillAdjustVALUE | calculation.cpp |