using UnityEngine;
using System.Collections;
using static Define;
using Unity.VisualScripting;
using TMPro;

[RequireComponent(typeof(ICombatStatHolder))]
[RequireComponent(typeof(IAnimationController))]
public class PlayerAttackController : MonoBehaviour
{
    private ICombatStatHolder stat;
    private IAnimationController anim;
    private PlayerInputController inputCtrl;
    private EquipmentManager equipMG;  // 이건 또 Player에 붙는거라 GM 통해서 못가져옴 ㅅㄱ

    private float attackDelay = 0.5f;
    private bool canAttack = true;

    [Header("공격")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer;

    [Header("무기")]
    private EquipmentItemData curWeapon;
    private AttackMode curAttackMode;

    private GameObject curBulletPrefab;// TODO - PoolManager
    private int maxMagCount = 0;       // 탄ㅊ창 최대 총알 개수(장전 시 총알 개숭ㅇ)
    private int curMagCount = 0;       // 현재 탄창 속 총알 개수

    // TODO UI랑 연동 - UIManager에서 가져오기
    private TextMeshProUGUI curBulletCountTxt;

    private bool isReloading = false;

    private Vector2 atkDir;

    [SerializeField] private GameObject rightArm;

    // 팔 각도 - Log 찍어서 확인해봤음
    private const float ARM_MIN_DEGREE = -135f;
    private const float ARM_MAX_DEGREE = 45f;
    private float armAngle;
    // 디버깅 후 제거할 것
    [SerializeField] private float armAngleOffset = 0f;  // 제거?할 것

    // 런타임 발사 변수
    private Transform firePoint;
    private Vector2 initPoint;
    private Vector2 dir;

    private void Awake()
    {
        stat = GetComponent<ICombatStatHolder>();
        anim = GetComponent<IAnimationController>();
        inputCtrl = GetComponent<PlayerInputController>();
        equipMG = GetComponent<EquipmentManager>();


        if (stat == null)
            Debug.LogError($"[{nameof(PlayerAttackController)}] ICombatStatHolder를 찾을 수 없습니다.");
        if (anim == null)
            Debug.LogError($"[{nameof(PlayerAttackController)}] IAnimationController를 찾을 수 없습니다.");
        if (attackPoint == null)
            Debug.LogError($"[{nameof(PlayerAttackController)}] attackPoint가 할당되지 않았습니다. 인스펙터에서 지정해주세요.");
    }

    private void OnEnable()
    {
        if (inputCtrl != null)
        {
            inputCtrl.OnHitEvent += HandleAttackInput;
            inputCtrl.OnLookEvent += HandleLook;
        }
        else
            Debug.LogWarning($"[{nameof(PlayerAttackController)}] PlayerInputController를 찾을 수 없습니다. 공격 입력이 불가능합니다.");


        if (equipMG != null)
        {
            equipMG.OnEquippedChanged += HandleEquipChanged;

            var weapon = equipMG.GetEquipped(EquipmentSlot.Weapon);
            SetWeapon(weapon);
        }
    }

    private void OnDisable()
    {
        if (inputCtrl != null)
        {
            inputCtrl.OnHitEvent -= HandleAttackInput;
            inputCtrl.OnLookEvent -= HandleLook;
        }

        if (equipMG != null)
        {
            equipMG.OnEquippedChanged -= HandleEquipChanged;
        }
    }

    // Look 설정 ㅇㅇ
    public void HandleLook(Vector2 dir)
    {
        // dir : 그그그 Player 기준 마우스 포인터 방향
        // normalized 필요

        atkDir = dir.normalized;

        if (atkDir.sqrMagnitude <= 0.000001f)
            atkDir = Vector2.left;

        if (rightArm == null)
            return;

        if (curAttackMode == AttackMode.Ranged)
        {
            // dir 방향으로 팔 회전
            // 어차피 dir 최신화야 외부 함수에서 알아서 처리하니까
            //      이제 Player 팔과 무기를 Look에 맞춰 움직이도록.

            // 1. default 방향 (왼쪽)을 바라보는 경우
            // dir이 (0, 1) ~ (-1,0) ~ (0 , -1) => x값이 음수
            if (atkDir.x < 0)       // 왼쪽 보고 있을 때
            {
                float radian = Mathf.Atan2(atkDir.y, atkDir.x); // atkDir을 각도(라디안)으로 변경
                armAngle = radian * Mathf.Rad2Deg;                 // 라디안을 degree로 변경
                                                                   // 라디안 디그리는 Notion 참고 ㅇㅇ.

                if (armAngle < 0)
                    armAngle += 360f;

                float local = (armAngle - 225f);

                // ARM_MIN_DEGREE~ARM_MAX_DEGREE로 잘라냄.
                local = Mathf.Clamp(local, ARM_MIN_DEGREE, ARM_MAX_DEGREE);
                rightArm.transform.localRotation = Quaternion.Euler(0f, 0f, local);
            }
            // 2. 오른쪽을 바라보는 경우
            // dir이 (0, 1) ~ (1,0) ~ (0 , -1) => x값이 양수
            else
            {                  // 오른쪽 조준하고 있을 때
                float radian = Mathf.Atan2(atkDir.y, atkDir.x); // atkDir을 각도(라디안)으로 변경
                armAngle = radian * Mathf.Rad2Deg;                 // 라디안을 degree로 변경
                                                                   // 라디안 디그리는 Notion 참고 ㅇㅇ.

                float local = (-1) * armAngle - 45f;

                // ARM_MIN_DEGREE~ARM_MAX_DEGREE로 잘라냄.
                local = Mathf.Clamp(local, ARM_MIN_DEGREE, ARM_MAX_DEGREE);
                rightArm.transform.localRotation = Quaternion.Euler(0f, 0f, local);
            }

        }
    }

    // equip  관련 스크립트
    public void HandleEquipChanged(EquipmentSlot slot, EquipmentItemData item)
    {
        if (slot == EquipmentSlot.Weapon)
        {
            SetWeapon(item);
        }
    }

    public void SetWeapon(EquipmentItemData item)
    {
        // 설정 할 것 : weaponItem, AtkMode, Bullet, curMagCount, maxMagCount
        curWeapon = item;

        // 무기 장착

        // 무기 없음(맨손 무기) 장착
        if (curWeapon == null)
        {
            curAttackMode = AttackMode.Melee; // or AttackMode.None 만들면 그걸로
            curBulletPrefab = null;
            isReloading = false;
            return;
        }

        // ㄹㅇ 찐 무기 장착
        if (curWeapon.WeaponType == WeaponType.Sword || curWeapon.WeaponType == WeaponType.Spear)
        {
            curAttackMode = AttackMode.Melee;

        }
        else if (curWeapon.WeaponType == WeaponType.SingleShot || curWeapon.WeaponType == WeaponType.AutoShot)
        {
            curAttackMode = AttackMode.Ranged;
            curBulletPrefab = curWeapon.Bullet;
            maxMagCount = curWeapon.MagMaxCount;
            curMagCount = maxMagCount;
            isReloading = false;
        }
    }

    // 공격 수행

    private void HandleAttackInput(bool isPressed)
    {
        if (!isPressed || !canAttack) return;

        switch (curAttackMode)
        {
            case AttackMode.Melee:
                StartCoroutine(PerformMeleeAttack());
                break;
            case AttackMode.Ranged:
                StartCoroutine(PerformRangedAttack());
                break;
            default:
                Debug.Log("[PAtkCtrl]- 지정되지 않은 AtkType 설정");
                break;
        }
        // TODO - 원거리 공격 추가 및 Case 예외처리 로직 작성 ㄱㄱ
    }

    private IEnumerator PerformMeleeAttack()
    {
        canAttack = false;

        // 1. 공격 애니메이션 시작
        anim.SetTrigger("2_Attack");

        // 2. 스텟에서 공격 간 딜레이 시간 체크 및 전달
        float delay = stat.Stats.AttackDelay;
        yield return new WaitForSeconds(delay);

        // TODO - Player의 공격 이펙ㅌ트 구현

        // 3.  데미지 전달
        float range = stat.Stats.AttackRange;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, range, enemyLayer);
        foreach (var hit in hits)
        {
            var enemyHitReactor = hit.GetComponent<IHitReactor>();
            if (enemyHitReactor != null)
            {
                enemyHitReactor.OnAttacked(stat.CalculatePhysicsDmg());
            }
        }

        // 4. 공격 딜레이 시작
        yield return new WaitForSeconds(attackDelay);
        canAttack = true;
    }

    private IEnumerator PerformRangedAttack()
    {
        // 재장전 중임?
        if (isReloading)
            yield break;

        // 탄창에 총알 없음?
        if (curMagCount <= 0)
        {
            yield return Reload();
            yield break;
        }

        // delay 구현똥마려
        canAttack = false;


        // TODO - RightNow - 문제점 발견
        // 딜레이는 무기에 따라 지정되어야함.
        //      따라서 Player에 속해있는 delay Time을 공속 증감 관련으로 변경해야함.
        //      근접 무기 포함. ㅇㅇ
        float delay = stat.Stats.AttackDelay;
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        curMagCount--;

        // 발사
        /* TODO
        총 쐈으면 반동(총이랑 팔만 뒤로 밀려나는 애니메이션?)있어야제
        반동 : return?time(지정된 복귀 시간)동안 수행
            1. 발사 방향과 정 정 반대 ㅏ방향으로 약간 뒤로 addforce
            2. 이후 천천히 돌아옴?
        문제 : 시간 배분은 어쩌지?
        */

        // 방향 설정
        if (attackPoint != null)
            firePoint = attackPoint;
        else
            firePoint = this.gameObject.transform;

        if (firePoint != null)
            initPoint = firePoint.position;
        else
            initPoint = this.gameObject.transform.position;

        dir = atkDir;

        Fire(initPoint, dir);

        // 공격 쿨타임 - 무기 종속
        if (curWeapon.atkCoolTime > 0f)
            yield return new WaitForSeconds(curWeapon.atkCoolTime);

        canAttack = true;
    }

    // TODO - 장전 애니메이션
    private IEnumerator Reload()
    {
        // 애니메이션을 넣을까?
        // bullet과 gray bullet 찾아다가 Player 머리 위에 하나씩 띄우며 장전 진행도 보여주도록 ㄱ
        if (maxMagCount <= 0)
            yield break;

        isReloading = true;

        float reloadTime = curWeapon.ReloadTime;

        // defualt 값 셑팅
        if (reloadTime < 0.1f)
            reloadTime = 0.1f;

        yield return new WaitForSeconds(reloadTime);

        curMagCount = maxMagCount;
        isReloading = false;
    }

    private void Fire(Vector2 initPoint, Vector2 dir)
    {
        if (curBulletPrefab == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            temp.name = "TempBullet";
            temp.AddComponent<Rigidbody2D>();
            temp.AddComponent<CapsuleCollider2D>();
            Debug.Log("총알 프리팹 없음 ㅇㅇ.");

            curBulletPrefab = temp;
        }

        GameObject prefab = Instantiate(curBulletPrefab, initPoint, Quaternion.identity);
        //prefab.transform.rotation = Quaternion.FromToRotation(Vector2.up, dir);

        Bullet sp = prefab.GetOrAddComponent<Bullet>();
        // 플레이어니까 무기에 따른 데미지 가감 추가
        //  생각해보니 무기별 modifier는 스텟SO에 직접 영향 미치니까 계산되어서 나오네 ㅇㅇ.
        sp.Init(dir, stat.CalculatePhysicsDmg(), GameManager.Instance.PoolObjects, enemyLayer, curWeapon.BulletSpeed, curWeapon.BulletLifeTime);
    }
}
