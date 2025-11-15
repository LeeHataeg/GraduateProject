using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public abstract class Define
{
    #region Enum
    #region Boss
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
        ToradoAtkIn, ToradoAtkOut, ToradoAtkLoop,

        // 신규 Stage1 보스 전용 키 (그대로 유지)
        Entry,
        ToJump, Jumping, ToFall,
        ToWalk, Walk, WalkToIdle,
        WalkBlocking, WalkBLocking2, WalkBlocked, OutBlocked,
        ToBlock, Blocked
    }
    #endregion

    public enum RoomType { Normal, Start, Boss, SemiBoss, Shop }
    public enum PortalDir { up, down, left, right }

    public enum BodyPart
    {
        Hair, Hat, Face,
        Chest, ShoulderL, ShoulderR,
        HandL, HandR,
        WeaponL, WeaponR,
        LegL, LegR
    }

    public enum EquipmentSlot { Head, Chest, Legs, Weapon, Ring, Amulet }

    public enum WeaponType { Sword, Spear, SingleShot, AutoShot }

    // ---- 스탯/모디파이어 ----
    public enum StatType { PhysAtk, MagicAtk, BaseDmg, MaxHp, CritChance, CritDamage /* 필요시 추가 */ }
    public enum ModMethod { Flat, Percent }
    public enum ModifierOp { Add, Percent }

    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModMethod method;
        public int value;
    }

    // ==== ★ EnemyKind 확장: Undead 계열 추가 (Stage1) ====
    [Description("적 종류(외형/개체군)")]
    public enum EnemyKind
    {
        // Stage2 - 그거 그 오크
        OrcWarrior,
        OrcGreatsword,
        OrcArcher,
        OrcShaman,

        // Stage1 - 걔 그그그그그그 언데드
        UndeadFarmer,
        UndeadSwordsman,
        UndeadMage
    }

    [Description("전투 방식")]
    public enum AttackMode { Melee, Ranged, RangedBuffer /* 연발 사격 */}
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
        public int CompareTo(Edge other) => this.Distance.CompareTo(other.Distance);
    }

    public class UnionFind
    {
        private int[] parent;   // 각 노드의 부모 번호
        private int[] rank;     // 각 집합 트리의 대략적 높이
        public UnionFind(int size)
        {
            parent = new int[size];
            rank = new int[size];
            for (int i = 0; i < size; i++) { parent[i] = i; rank[i] = 0; }
        }
        // 부모를 따라가며 압축하면서 대표를 찾음
        public int Find(int u) { if (parent[u] != u) parent[u] = Find(parent[u]); return parent[u]; }
        // 두 대표간 비교로 트리 높이가 낮은 쪽을 높은 쪽  밑에 붙임.
        public bool Union(int u, int v)
        {
            int pu = Find(u), pv = Find(v);
            if (pu == pv) 
                return false;
            if (rank[pu] > rank[pv]) 
                parent[pv] = pu;
            else if (rank[pu] < rank[pv]) 
                parent[pu] = pv;
            else { 
                parent[pv] = pu; rank[pu]++; 
            }
            return true;
        }
    }

    public class PortalInfo
    {
        public PortalDir dir;
        public MapNode connected;
        public PortalInfo(PortalDir dir, MapNode connected) { this.dir = dir; this.connected = connected; }
    }

    public class MapNode
    {
        public RectInt SpaceArea;
        public int Id;
        public List<PortalInfo> Portals;
        public MapNode() { Portals = new List<PortalInfo>(); }
    }

    public class AttackContext
    {
        public GameObject Attacker { get; set; }
        public GameObject Target { get; set; }
        public Vector2 Direction { get; set; }
        public int SkillLevel { get; set; }
        public float CritChance { get; set; }
        public AttackContext(GameObject Attacker, GameObject Target, Vector2 Direction)
        { this.Attacker = Attacker; this.Target = Target; this.Direction = Direction; }
    }
    #endregion
}
