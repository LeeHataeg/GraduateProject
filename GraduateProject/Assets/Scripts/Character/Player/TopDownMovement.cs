using UnityEngine;

public class TopDownMovement : MonoBehaviour
{
    TopDownCharacterController _characterController;
    Rigidbody2D _rigidbody;
    SpriteRenderer _characterSprite;
    Vector2 _direction;
    [SerializeField][Range(10, 1000)] float _speed;
    void Awake()
    {
        _characterController = gameObject.GetComponent<TopDownCharacterController>();
        _rigidbody = gameObject.GetComponent<Rigidbody2D>();
        _characterSprite = gameObject.GetComponentInChildren<SpriteRenderer>();
    }
    private void Start()
    {
        // 이벤트 등록
        _characterController.OnMoveEvent += Move;
        _characterController.OnLookEvent += Look;
    }
    void FixedUpdate()
    {
        ApplayMovement();
    }
    private void Look(Vector2 direction)
    {
        // 2D Sprite를 뒤집어 마우스 방향을 바라보게 만든다
        _characterSprite.flipX = (direction.x < 0);
    }
    void Move(Vector2 direction)
    {
        // 이동 방향 등록
        _direction = direction;
        Debug.Log("Move!");
    }
    void ApplayMovement()
    {
        // rigidbody를 이용한 캐릭터 이동
        _rigidbody.linearVelocity = _direction * _speed;
    }
}
