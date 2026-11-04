using UnityEngine;

public class EnemyTest : MonoBehaviour, IDamageable

{
    public float health = 50f;
    private Rigidbody2D rb;

    private void Start() => rb = GetComponent<Rigidbody2D>();

    public void TakeDamage(float amount, Vector2 knockback)
    {
        health -= amount;
        rb.linearVelocity = knockback;
        Debug.Log($"{name} hit for {amount}, hp={health}");

        if (health <= 0f)
            Destroy(gameObject);
    }
}
