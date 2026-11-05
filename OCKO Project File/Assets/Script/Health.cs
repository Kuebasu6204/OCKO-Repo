using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Health : MonoBehaviour, IDamageable 
{
    public float health = 100f;
    public float hitstunDuration = 0.15f;

    private Rigidbody2D rb;
    private bool isInHitstun;

    public bool IsInHitstun => isInHitstun;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    public void TakeDamage(float amount, Vector2 knockback)
    {
        if (isInHitstun) return;

        health -= amount;
        rb.linearVelocity = knockback;
        StartCoroutine(Hitstun());
    }

    private IEnumerator Hitstun()
    {
        isInHitstun = true;
        yield return new WaitForSeconds(hitstunDuration);
        isInHitstun = false;
    }
}
