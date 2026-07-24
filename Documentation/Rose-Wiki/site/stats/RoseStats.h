/// This isnt the bible this is my own representation in unreal engine 
/// So you can reference back to the original source (in my case I used rose-next).

#include "CoreMinimal.h"



namespace RoseStats
{
	// Cal_RunSPEED (cuserdata.cpp): boots+back item speed scaled by DEX.
	//   speed = (bootsSpd + backSpd + 20) * (DEX + 500) / 100 + add
	// BOOTS_MOVE_SPEED(0) — barefoot — is the foot table's row 0 (~65).
	inline float RunSpeed(int32 Dex, int32 BootsSpeed, int32 BackSpeed = 0, int32 Add = 0)
	{
		return (BootsSpeed + BackSpeed + 20) * (Dex + 500.f) / 100.f + Add;
	}

	// Cal_AvoidRATE: (DEX + 10) * 0.8 + LEVEL * 0.5
	inline int32 Avoid(int32 Dex, int32 Level)
	{
		return (int32)((Dex + 10) * 0.8f + Level * 0.5f);
	}

	// Cal_CRITICAL (on foot): SEN + (CON + 20) * 0.2
	inline int32 Critical(int32 Sen, int32 Con)
	{
		return (int32)(Sen + (Con + 20) * 0.2f);
	}

	// Cal_HIT (cobjavt.cpp).  Armed: (CON+10)*0.8 + quality*0.6 + gradeHIT + dur*0.8
	// (quality = weapons.csv Quality; grade is 0 for the ungraded items we spawn).
	// Bare: (CON+10)*0.5+15.
	inline int32 HitRate(int32 Con, bool bArmed, int32 WeaponDurability = 0, int32 WeaponQuality = 0)
	{
		if (bArmed)
			return (int32)((Con + 10) * 0.8f) + (int32)(WeaponQuality * 0.6f + WeaponDurability * 0.8f);
		return (int32)((Con + 10) * 0.5f + 15);
	}

	// Cal_DEFENCE (on foot): itemDEF + (STR + 5) * 0.35 + (LEVEL + 15) * 0.7
	inline int32 Defence(int32 ItemDef, int32 Str, int32 Level)
	{
		return (int32)(ItemDef + (Str + 5) * 0.35f + (Level + 15) * 0.7f);
	}

	// Cal_RESIST: itemRES + (INT + 5) * 0.6 + (LEVEL + 15) * 0.8
	inline int32 Resist(int32 ItemRes, int32 Int, int32 Level)
	{
		return (int32)(ItemRes + (Int + 5) * 0.6f + (Level + 15) * 0.8f);
	}

	// Cal_MaxHP, visitor/default class row: (LEVEL + 12) * 8 + STR * 2
	inline int32 MaxHP(int32 Level, int32 Str)
	{
		return (Level + 12) * 8 + Str * 2;
	}

	// Cal_MaxMP, visitor/default class row: (LEVEL + 4) * 3 + INT * 4
	inline int32 MaxMP(int32 Level, int32 Int)
	{
		return (int32)((Level + 4) * 3.f + Int * 4);
	}

	// Cal_MaxWEIGHT: 1100 + LEVEL * 5 + STR * 6
	inline int32 MaxWeight(int32 Level, int32 Str)
	{
		return 1100 + Level * 5 + Str * 6;
	}

	// cobjavt.cpp: attack speed = 1500 / (weaponAtkSpd + 5); ~100 = normal rate.
	inline int32 AttackSpeed(float WeaponAtkSpd)
	{
		return (int32)(1500.f / (WeaponAtkSpd + 5.f));
	}

	// Cal_ATTACK.  Melee one-hand (types 21x/22x):
	//   STR*0.75 + LVL*0.2 + weaponAP * (STR*0.05 + 29) / 30
	// Unarmed (type 0): STR*0.5 + DEX*0.3 + LVL*0.2
	inline int32 AttackPowerMelee(int32 Str, int32 Level, int32 WeaponAP)
	{
		return (int32)((Str * 0.75f + Level * 0.2f) + (WeaponAP * (Str * 0.05f + 29.f) / 30.f));
	}
	inline int32 AttackPowerUnarmed(int32 Str, int32 Dex, int32 Level)
	{
		return (int32)(Str * 0.5f + Dex * 0.3f + Level * 0.2f);
	}

	// Full per-weapon-type attack power — CObjAVT::Cal_ATTACKPOWER (cobjavt.cpp).
	// WeaponType is the STB WEAPON_TYPE (211 one-hand, 221/222 two-hand, 231/271
	// bow, 232/253 gun, 233 launcher, 241 magic-staff, 242 magic-wand, 251 katar,
	// 252 dual).  Ranged weapons follow CItem::GetShotTYPE (arrow/bullet/throw)
	// and scale off DEX/CON/SEN; melee off STR; magic off INT/SEN; katar off DEX.
	// ItemQ = weapon ITEM_QUALITY.  Weapon GRADE bonus is not modelled (0).
	inline int32 AttackPower(int32 WeaponType, int32 Str, int32 Dex, int32 Intel,
		int32 Con, int32 Sen, int32 Level, int32 WeaponAP, int32 ItemQ)
	{
		// Ranged (shot) weapons first — GetShotTYPE only maps these exact types.
		switch (WeaponType)
		{
		case 231: case 271:   // SHOT_TYPE_ARROW  (bow / one-hand bow)
			return (int32)((Dex * 0.62f + Str * 0.2f + Level * 0.2f + ItemQ)
				+ ((WeaponAP + ItemQ * 0.5f + 8) * (Dex * 0.04f + Sen * 0.03f + 29) / 30.f));
		case 232: case 253:   // SHOT_TYPE_BULLET (gun / dual gun)
			return (int32)((Dex * 0.4f + Con * 0.5f + Level * 0.2f + ItemQ)
				+ ((WeaponAP + ItemQ * 0.6f + 8) * (Con * 0.03f + Sen * 0.05f + 29) / 30.f));
		case 233:             // SHOT_TYPE_THROW  (launcher)
			return (int32)((Str * 0.52f + Con * 0.5f + Level * 0.2f + ItemQ)
				+ ((WeaponAP + ItemQ + 12) * (Con * 0.04f + Sen * 0.05f + 29) / 30.f));
		}
		// Melee / magic / katar — by category (WEAPON_TYPE / 10).
		switch (WeaponType / 10)
		{
		case 21: case 22:     // one-hand / two-hand melee (STR)
			return (int32)((Str * 0.75f + Level * 0.2f) + (WeaponAP * (Str * 0.05f + 29) / 30.f));
		case 24:              // magic
			if (WeaponType == 241)   // magic staff (STR + INT)
				return (int32)((Str * 0.4f + Intel * 0.4f + Level * 0.2f)
					+ (WeaponAP * (Intel * 0.05f + 29) / 30.f));
			return (int32)((Intel * 0.6f + Level * 0.2f)     // magic wand (INT + SEN)
				+ (WeaponAP * (Sen * 0.1f + 26) / 27.f));
		case 25:              // katar
			if (WeaponType == 252)   // dual wield (STR + DEX)
				return (int32)((Str * 0.63f + Dex * 0.45f + Level * 0.2f)
					+ (WeaponAP * (Dex * 0.05f + 25) / 26.f));
			return (int32)((Str * 0.42f + Dex * 0.55f + Level * 0.2f)   // single katar
				+ (WeaponAP * (Dex * 0.05f + 20) / 21.f));
		}
		// Unarmed / unknown.
		return AttackPowerUnarmed(Str, Dex, Level);
	}

	// Cal_RunAniSPEED (cobjai.h): run-anim play rate for a move speed in cm/s.
	inline float RunAnimRate(float CmPerSec)
	{
		return (CmPerSec + 180.f) / 600.f;
	}

	// Get_SuccessRATE (calculation.cpp), player-vs-monster branch.  Returns the
	// success value fed into the damage formula; <= 0 means the target DODGED.
	inline int32 SuccessRate(int32 AtkLevel, int32 DefLevel, int32 AtkHit, int32 DefAvoid)
	{
		const int32 R1 = FMath::RandRange(1, 50);
		const int32 R2 = FMath::RandRange(1, 60);
		const int32 Suc = (int32)((AtkLevel + 10) - DefLevel * 1.1f + R1);
		if (Suc <= 0)
			return 0;
		return (int32)(Suc * (AtkHit * 1.1f - DefAvoid * 0.93f + R2 + 5 + AtkLevel * 0.2f) / 80.f);
	}

	// Get_CriSuccessRATE: crit when ((1+rand(100))*3 + LVL + 30) * 16 / (CRIT+70) < 20.
	inline bool RollCritical(int32 Level, int32 Critical)
	{
		const int32 CriSuc = ((1 + FMath::RandRange(1, 100)) * 3 + Level + 30) * 16 / (Critical + 70);
		return CriSuc < 20;
	}

	// Get_BasicDAMAGE, player-vs-monster branch (normal min 5, crit min 10).
	inline int32 BasicDamage(int32 AtkPower, int32 DefDef, int32 DefAvoid, int32 Suc, bool bCrit)
	{
		int32 Dmg;
		if (bCrit)
			Dmg = (int32)(AtkPower * (Suc * 0.05f + 29) * (AtkPower - DefDef + 230)
				/ ((DefDef + DefAvoid * 0.3f + 5) * 100.f));
		else
			Dmg = (int32)(AtkPower * (Suc * 0.03f + 26) * (AtkPower - DefDef + 250)
				/ ((DefDef + DefAvoid * 0.4f + 5) * 145.f));
		return FMath::Max(Dmg, bCrit ? 10 : 5);
	}

	// ── Skill formulas (CCal::Get_SkillDAMAGE / Get_SkillAdjustVALUE,
	//    src/common/calculation.cpp:552/755) ─────────────────────────────────

	// Attacker-side inputs to the skill damage roll (the pATK side).
	struct FSkillAttacker
	{
		int32 Level = 1;
		int32 AttackPower = 0;   // total_attack_power()
		int32 HitRate = 0;       // total_hit_rate()
		int32 Int = 0;           // Get_INT
		int32 Sense = 0;         // Get_SENSE
		int32 Critical = 0;      // Get_CRITICAL
	};
	// Defender-side inputs (the pDEF side).
	struct FSkillDefender
	{
		int32 Level = 1;
		int32 Defense = 0;       // Get_DEF
		int32 Resist = 0;        // Get_RES
		int32 Avoid = 0;         // Get_AVOID
	};

	// CCal::Get_SkillDAMAGE (calculation.cpp:552), PLAYER-vs-MONSTER branches only
	// (the !(IsUSER && IsUSER) arms).  DamageType = LIST_SKILL col 15:
	//   1 weapon skill, 2 magic skill, 3 unarmed skill, default = basic-attack-like.
	// Each type embeds its own success roll; a low roll returns 0 = MISS.
	// HitCount multiplies at the end (SKILL_ANI_HIT_COUNT — the server passes the
	// motion's total attack frames).  Cap = GameStaticConfig::MAX_DAMAGE (9999,
	// src/common/include/rose/common/game_config.h:11); floor 5.
	inline int32 SkillDamage(int32 SkillPower, int32 DamageType,
		const FSkillAttacker& A, const FSkillDefender& D, int32 HitCount = 1)
	{
		int32 Dmg = 0;
		switch (DamageType)
		{
		case 1: // weapon skill (calculation.cpp:558)
		{
			const int32 R1 = FMath::RandRange(1, 60), R2 = FMath::RandRange(1, 70);
			const int32 Suc = (int32)(((A.Level + 20) - D.Level + R1)
				* (A.HitRate - D.Avoid * 0.6f + R2 + 10) / 110.f);
			if (Suc < 20)
			{
				if (Suc < 10) return 0;
				Dmg = (int32)(((SkillPower * 0.4f) * (A.AttackPower + 50)
						* (FMath::RandRange(1, 30) + A.Sense * 1.2f + 340))
					/ (D.Defense + D.Resist + 20) / (250 + D.Level - A.Level) + 20);
			}
			else
			{
				Dmg = (int32)(((SkillPower + A.AttackPower * 0.2f) * (A.AttackPower + 60)
						* (FMath::RandRange(1, 30) + A.Sense * 0.7f + 370))
					* 0.01 * (120 - D.Level + A.Level)
					/ (D.Defense + D.Resist * 0.8f + D.Avoid * 0.4f + 20) / 270 + 20);
			}
			break;
		}
		case 2: // magic skill (calculation.cpp:600) — divides by RES-weighted terms
		{
			const int32 R1 = FMath::RandRange(1, 50), R2 = FMath::RandRange(1, 70);
			const int32 Suc = (int32)(((A.Level + 30) - D.Level + R1)
				* (A.HitRate - D.Avoid * 0.56f + R2 + 10) / 110.f);
			if (Suc < 20)
			{
				if (Suc < 8) return 0;
				Dmg = (int32)((SkillPower * (A.AttackPower * 0.8f + A.Int + 80)
						* (FMath::RandRange(1, 30) + A.Sense * 1.3f + 280) * 0.2f)
					/ (D.Defense * 0.3f + D.Resist + 30) / (250 + D.Level - A.Level) + 20);
			}
			else
			{
				Dmg = (int32)((SkillPower * (A.AttackPower * 0.8f + A.Int * 1.2f + 100)
						* (FMath::RandRange(1, 30) + A.Sense * 0.7f + 350) * 0.01f)
					* (150 - D.Level + A.Level)
					/ (D.Defense * 0.3f + D.Resist + D.Avoid * 0.3f + 60) / 350.f + 20);
			}
			break;
		}
		case 3: // unarmed skill (calculation.cpp:645) — no weapon-life decay
		{
			const int32 R1 = FMath::RandRange(1, 80), R2 = FMath::RandRange(1, 50);
			const int32 Suc = (int32)(((A.Level + 10) - D.Level + R1)
				* (A.HitRate - D.Avoid * 0.5f + R2 + 50) / 90.f);
			if (Suc < 20)
			{
				if (Suc < 6) return 0;
				Dmg = (int32)((SkillPower * ((float)SkillPower + A.Int + 80)
						* (FMath::RandRange(1, 30) + A.Sense * 2 + 290) * 0.2f)
					/ (D.Defense * 0.2f + D.Resist + 30) / (250 + D.Level - A.Level) + 20);
			}
			else
			{
				Dmg = (int32)(((35 + SkillPower) * ((float)SkillPower + A.Int + 140)
						* (FMath::RandRange(1, 30) + A.Sense + 380) * 0.01f)
					* (150 - D.Level + A.Level)
					/ (D.Defense * 0.35f + D.Resist * 1.2f + D.Avoid * 0.4f + 10) / 730 + 20);
			}
			break;
		}
		default: // basic unarmed attack (calculation.cpp:684)
		{
			const int32 R1 = FMath::RandRange(1, 80), R2 = FMath::RandRange(1, 50);
			const int32 Suc = (int32)(((A.Level + 8) - D.Level + R1)
				* (A.HitRate - D.Avoid * 0.6f + R2 + 50) / 90);
			if (Suc < 20)
			{
				if (Suc < 10) return 0;
				Dmg = (int32)(((SkillPower + 40) * (A.AttackPower + 40)
						* (FMath::RandRange(1, 30) + A.Critical * 0.2f + 40)) * 0.4f
					/ (D.Defense + D.Resist * 0.3f + D.Avoid * 0.4f + 10) / 80 + 5);
			}
			else
			{
				Dmg = (int32)(((SkillPower + A.Critical * 0.15f + 40) * A.AttackPower
						* (FMath::RandRange(1, 30) + A.Critical * 0.32f + 35))
					* 0.01f * (120 - D.Level + A.Level)
					/ (D.Defense + D.Resist * 0.3f + D.Avoid * 0.4f + 10) / 100.f + 20);
			}
			break;
		}
		}
		Dmg = FMath::Max(Dmg, 5);                    // calculation.cpp:732
		Dmg *= FMath::Max(1, HitCount);              // calculation.cpp:735
		return FMath::Min(Dmg, 9999);                // MAX_DAMAGE clamp, :742
	}

	// CCal::Get_SkillAdjustVALUE (calculation.cpp:755): the amount a buff/heal
	// applies.  AbilityValue = the TARGET's current value of the skill's
	// IncAbility (e.g. AT_RES for a "25% of resist" buff), Rate/Value from the
	// skill row, CasterInt = the caster's INT (scales the flat part).
	inline int32 SkillAdjustValue(int32 AbilityValue, int32 Rate, int32 Value, int32 CasterInt)
	{
		return (int32)(AbilityValue * Rate / 100.f + Value * (CasterInt + 300) / 315.f);
	}
}