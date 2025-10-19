using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public abstract class Define
{
    #region Enum
    #region Boss_Orc
    public enum BossState { Intro, Phase1, Transition, Phase2, Death }
    public enum AnimKey
    {
        // 공통
        Death, Fall, Fallen, Falling,
        StunIn, StunOut, StunLoop,
        BackDash,
        Run, RunIn, RunOut,
        WalkIn, WalkOut, Walking,

        // 1 Phase 전용
        Idle, Land,
        Atk1, Atk2, Atk3, Recover,
        DashAtk,
        ChargeJump, JumpIn, JumpAtkLand, JumpAtkLoop,

        // 2 Phase 전용
        Idle2, Entry2, Taunt, TauntOut,
        HeavyAtk, HeavyAtk2, HeavyAtk3,
        LightAtk, StompAtk,
        FrontHeavyAtk,
        ToradoAtkIn, ToradoAtkOut, ToradoAtkLoop
        
    }
    #endregion

    public enum RoomType
    {
        Normal,
        Start,
        Boss,
        SemiBoss,
        Shop,
    }
    public enum PortalDir
    {
        up,
        down,
        left,
        right
    }
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

    // Define.cs 혹은 Stats 공용 파일에 넣기
    public enum StatType { PhysAtk, MagicAtk, BaseDmg, MaxHp, CritChance, CritDamage /* 필요시 추가 */ }
    public enum ModMethod { Flat, Percent }

    public enum ModifierOp { Add, Percent } // Percent = +x%

    [System.Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModMethod method;
        public int value; // 퍼센트는 10000 = +100% 같은 스케일을 쓸 거면 int로 두면 편함
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

    public class Edge : IComparable<Edge>
    {
        public MapNode Start { get; }
        public MapNode End { get; }
        public float Distance { get; }

        public Edge(MapNode start, MapNode end)
        {
            Start = start;
            End = end;
            Distance = Vector2.Distance(start.SpaceArea.position, end.SpaceArea.position);
        }

        public int CompareTo(Edge other)
        {
            return this.Distance.CompareTo(other.Distance);
        }
    }

    public class UnionFind
    {
        private int[] parent;
        private int[] rank;

        public UnionFind(int size)
        {
            parent = new int[size];
            rank = new int[size];
            for (int i = 0; i < size; i++)
            {
                parent[i] = i;
                rank[i] = 0;
            }
        }

        public int Find(int u)
        {
            if (parent[u] != u)
            {
                parent[u] = Find(parent[u]);
            }
            return parent[u];
        }

        public bool Union(int u, int v)
        {
            int parentU = Find(u);
            int parentV = Find(v);

            // If Same Link? Union? Tree?
            if (parentU == parentV) return false;

            if (rank[parentU] > rank[parentV])
                parent[parentV] = parentU;
            else if (rank[parentU] < rank[parentV])
                parent[parentU] = parentV;
            else
            {
                parent[parentV] = parentU;
                rank[parentU]++;
            }
            return true;
        }
    }
    public class PortalInfo
    {
        public PortalDir dir;
        // 'id' means Connected Room's Id
        public MapNode connected;

        public PortalInfo(PortalDir dir, MapNode connected)
        {
            this.dir = dir;
            this.connected = connected;
        }
    }

    public class MapNode
    {
        // Modify Protection Level if we need
        public RectInt SpaceArea;

        // For Defending Dupicated Connections
        public int Id;

        public List<PortalInfo> Portals;

        public MapNode()
        {
            Portals = new List<PortalInfo>();
        }
    }
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
