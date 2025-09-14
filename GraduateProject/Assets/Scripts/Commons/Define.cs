using System;
using System.ComponentModel;
using UnityEngine;

public abstract class Define
{
    #region Enum
    public enum BodyPart
    {
        Hair, Hat, Face,
        Chest, ShoulderL, ShoulderR,
        HandL, HandR,
        WeaponL, WeaponR,
        LegL, LegR
    }

    public enum EquipmentSlot
    {
        Head, Chest, Legs, Weapon, Ring, Amulet
    }

    public enum StatType
    {
        MaxHp, PhysAtk, MagicAtk, Defense, CritChance, CritDamage, MoveSpeed, JumpForce
    }

    public enum ModifierOp { Add, Percent } // Percent = +x%

    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModifierOp op;
        public float value; // Add면 절대값, Percent면 0.15f = +15%
    }

    [Description("적 종류(외형/개체군)")]
    public enum EnemyKind
    {
        OrcWarrior,      // 오크 전사 (Melee)
        OrcGreatsword,   // 오크 대검 전사 (Melee)
        OrcArcher,       // 오크 궁수 (Ranged)
        OrcShaman        // 오크 샤먼 (Ranged + Buffer)
    }

    [Description("전투 방식")]
    public enum AttackMode
    {
        Melee,
        Ranged,
        RangedBuffer // 원거리 + 버프 오라
    }
    #endregion

    #region Struct
    #endregion

    #region Class
    public class AttackContext
    {
        /// <summary>공격을 실행하는 주체(플레이어나 몬스터 등)</summary>
        public GameObject Attacker { get; set; }

        /// <summary>공격 대상(없을 수도 있습니다)</summary>
        public GameObject Target { get; set; }

        /// <summary>공격 방향 단위 벡터</summary>
        public Vector2 Direction { get; set; }

        /// <summary>스킬 레벨이나 공격 등급</summary>
        public int SkillLevel { get; set; }

        /// <summary>치명타 확률이나 추가 보정치 등</summary>
        public float CritChance { get; set; }

        // 필요에 따라 이펙트 핸들러, 애니메이션 트리거 이름 등 추가 가능

        public AttackContext(GameObject Attacker, GameObject Target, Vector2 Direction)
        {
            this.Attacker = Attacker;
            this.Target = Target;
            this.Direction = Direction;
        }
    }
    #endregion
}
