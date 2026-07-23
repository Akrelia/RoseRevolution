# Skill Damage Formula Reference

> **Sources**
>
> Original ROSE Online Server
>
> Function:
>
> `CCal::Get_SkillDAMAGE()`
>
> File:
>
> `src/common/calculation.cpp`
>
> This document describes the server-side skill damage calculations.

---

# Overview

Skill damage in ROSE is calculated differently depending on the skill damage
type.

Each skill contains a Damage Type value which determines the formula branch:

| Damage Type | Description |
|-------------|-------------|
| 1 | Weapon Skill |
| 2 | Magic Skill |
| 3 | Unarmed Skill |
| Default | Basic Attack Style |

Each formula performs its own hit success calculation before determining damage.

A failed success calculation results in:

```text
Damage = 0
or a miss.
```

Successful skills then apply:

1. Skill Power scaling
2. Attacker stats
3. Defender stats
4. Random variation
5. Minimum damage clamp
6. Hit count multiplier
7. Maximum damage clamp

---

# Skill Damage Pipeline

```text
Skill Used

    ->

Damage Type Selection

    ->

Success Rate Calculation

    ->

Miss Check

    ->

Damage Formula

    ->

Minimum Damage (5)

    ->

Hit Count Multiplier

    ->

Maximum Damage (9999) (this is for my case the original irose is 2047, also irose does a bit set so you dont overflow if I remember correctly)

    ->

Final Damage
```

---

# Attacker Variables

| Variable | Description |
|----------|-------------|
| Level | Character level |
| AttackPower | Total calculated AP |
| HitRate | Accuracy value |
| INT | Intelligence |
| Sense | Sense attribute |
| Critical | Critical value |

---

# Defender Variables

| Variable | Description |
|----------|-------------|
| Level | Monster level |
| Defence | Physical defence |
| Resist | Magical resistance |
| Avoid | Dodge rate |

---

# Damage Type 1 - Weapon Skill

## Success Formula

```cpp
Suc =
(
(Level + 20)
- TargetLevel
+ Random(1,60)
)

×

(
HitRate
- Avoid × 0.6
+ Random(1,70)
+ 10
)

÷ 110
```

---

## Failed Hit

```text
Suc < 10

Damage = 0
```

---

## Low Success Damage

```text
10 <= Suc < 20
```

Formula:

```text
Damage =

(
SkillPower × 0.4
)

×

(AttackPower + 50)

×

(Random(1,30)
+
Sense × 1.2
+
340)

÷

(TargetDefence
+
TargetResistance
+
20)

÷

(250
+
TargetLevel
-
AttackerLevel)

+
20
```

---

## Normal Weapon Skill Damage

```text
Suc >= 20
```

Formula:

```text
Damage =

(
SkillPower
+
AttackPower × 0.2
)

×

(AttackPower + 60)

×

(Random(1,30)
+
Sense × 0.7
+
370)

×

0.01

×

(120
-
TargetLevel
+
AttackerLevel)

÷

(
Defence
+
Resistance × 0.8
+
Avoid × 0.4
+
20
)

÷270

+20
```

---

# Damage Type 2 - Magic Skill

## Success Formula

```text
Suc =

(
(Level + 30)
-
TargetLevel
+
Random(1,50)
)

×

(
HitRate
-
Avoid × 0.56
+
Random(1,70)
+
10
)

÷110
```

---

## Failed Hit

```text
Suc < 8

Damage = 0
```

---

## Low Success Damage

```text
8 <= Suc < 20
```

```text
Damage =

SkillPower

×

(
AttackPower ×0.8
+
INT
+
80
)

×

(
Random(1,30)
+
Sense ×1.3
+
280
)

×

0.2

÷

(
Defence ×0.3
+
Resistance
+
30
)

÷

(
250
+
TargetLevel
-
AttackerLevel
)

+20
```

---

## Normal Magic Damage

```text
Suc >= 20
```

```text
Damage =

SkillPower

×

(
AttackPower ×0.8
+
INT ×1.2
+
100
)

×

(
Random(1,30)
+
Sense ×0.7
+
350
)

×

0.01

×

(
150
-
TargetLevel
+
AttackerLevel
)

÷

(
Defence ×0.3
+
Resistance
+
Avoid ×0.3
+
60
)

÷350

+20
```

---

# Damage Type 3 - Unarmed Skill

## Success Formula

```text
Suc =

(
(Level +10)
-
TargetLevel
+
Random(1,80)
)

×

(
HitRate
-
Avoid ×0.5
+
Random(1,50)
+
50
)

÷90
```

---

## Failed Hit

```text
Suc < 6

Damage = 0
```

---

## Low Success Damage

```text
6 <= Suc < 20
```

```text
Damage =

SkillPower

×

(
SkillPower
+
INT
+
80
)

×

(
Random(1,30)
+
Sense ×2
+
290
)

×

0.2

÷

(
Defence ×0.2
+
Resistance
+
30
)

÷

(
250
+
TargetLevel
-
AttackerLevel
)

+20
```

---

## Normal Unarmed Damage

```text
Suc >=20
```

```text
Damage =

(35 + SkillPower)

×

(
SkillPower
+
INT
+
140
)

×

(
Random(1,30)
+
Sense
+
380
)

×

0.01

×

(
150
-
TargetLevel
+
AttackerLevel
)

÷

(
Defence ×0.35
+
Resistance ×1.2
+
Avoid ×0.4
+
10
)

÷730

+20
```

---

# Default Damage Type

Used when no specific skill damage type exists.

This behaves similarly to a basic attack.

---

# Final Damage Processing

After every successful formula:

```cpp
Dmg = Max(Dmg,5)

Dmg *= Max(1,HitCount)

Dmg = Min(Dmg,9999)
```

Meaning:

| Rule | Value |
|------|------:|
| Minimum Damage | 5 |
| Maximum Damage | 9999 |
| Multi-hit Support | Yes |

---

# Implementation Reference

| System | Source |
|-|-|
| Skill Damage | `CCal::Get_SkillDAMAGE()` |
| Skill Buff Scaling | `CCal::Get_SkillAdjustVALUE()` |
| Combat Constants | `game_config.h` |

Implementation:

[RoseStats.h](RoseStats.h)
