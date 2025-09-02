using System;
using UnityEngine;

public abstract class Define
{
    #region Enum
    //public enum EquipmentType
    //{
    //    Head,
    //    Chest,
    //    Legs,
    //    Weapon,
    //    Accessory
    //}
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
