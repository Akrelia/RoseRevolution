using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityRose.Formats;

namespace UnityRose
{

    public enum Job1Type
    {
        VISITOR,
        HAWKER,
        SOLDIER,
        MUSE,
        DEALER
    }

    public enum Job2Type
    {
        NONE,
        SCOUT,
        RAIDER,
        KNIGHT,
        CHAMPION,
        MAGE,
        CLERIC,
        ARTISAN,
        BOURGEOIS
    }


    public enum WeaponType
    {
        EMPTY = 1,
        OHSWORD,
        OHAXE,
        OHMACE,
        OHTOOL,
        THSWORD,
        THSPEAR,
        DSW,
        THBLUNT,
        CANNON,
        BOW,
        XBOX,
        GUN,
        STAFF,
        WAND,
        BOOK,
        KATAR,
        SHIELD,
    };

    public enum ActionType
    {
        STANDING = 0,
        TIRED,
        WALK,
        RUN,
        SIT,
        SITTING,
        STANDUP,
        WARNING,
        ATTACK1,
        ATTACK2,
        ATTACK3,
        HIT,
        FALL,
        DIE,
        RAISE,
        JUMP1,
        JUMP2,
        PICKUP,
    };

    public enum RigType
    {
        FOOT = 0,
        CART,
        CASTLEGEAR,
        FLIGHT,
        CHARSELECT,
    }

    public enum GenderType : byte
    {
        MALE = 1,
        FEMALE = 2,
    };

    public enum BodyPartType
    {
        ARMS = 1,
        FOOT = 2,
        BODY = 3,
        CAP = 4,
        FACE = 5,
        FACEITEM = 6,
        BACK = 7,
        HAIR = 8,
        WEAPON = 9,
        SUBWEAPON = 10,
    }
}