using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]

public class PlayerPlatformDropController : MonoBehaviour
{
    [SerializeField] float dropDuration = 0.17f;

    BoxCollider2D bCol;

    void Awake()
    {
        bCol = GetComponent<BoxCollider2D>();
    }

    public void DropThrough(CompositeCollider2D comCol)
    {
        StartCoroutine(DisableCollision(comCol));
    }

    IEnumerator DisableCollision(CompositeCollider2D comCol)
    {
        Physics2D.IgnoreCollision(bCol, comCol);
        yield return new WaitForSeconds(dropDuration);
        Physics2D.IgnoreCollision(bCol, comCol, false);
    }
}
