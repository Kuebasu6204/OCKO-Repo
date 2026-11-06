using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Health : MonoBehaviour, IDamageable 
{
    public float maxHealth = 100f;
    public float hitstunDuration = 0.15f;

    private float lastHitTime = -10f;
    public float iframeDuration = 0.05f;

    [Header("Lives")]
    public int startingLives = 3;
    public float respawnDelay = 1.0f;
    public float respawnInvincibility = 1.5f;

    [Header("Respawn")]
    public Transform spawnPoint;   // optional — defaults to start position

    public event Action<int> OnLivesChanged;
    public event Action OnDied;
    public event Action OnRespawned;

    public int Lives { get; private set; }
    public bool IsDead => isDead;
    public bool IsInvincible => Time.time - lastHitTime < iframeDuration
                                || Time.time < invincibleUntil;

    private Rigidbody2D rb;
    private bool isInHitstun;
    private float health;
    private bool isDead;
    private float invincibleUntil;
    private RealPlayerControls controls;
    private Vector3 originalSpawn;

    public bool IsInHitstun => isInHitstun;

    private void Awake() 
    { 
        rb = GetComponent<Rigidbody2D>();
        controls = GetComponent<RealPlayerControls>();
        originalSpawn = transform.position;
        health = maxHealth;
        Lives = startingLives;
    }

    private void Start() 
    {
        OnLivesChanged?.Invoke(Lives);
    }

    public void TakeDamage(float amount, Vector2 knockback)
    {
        if (isInHitstun) return;

        if (IsInvincible || isDead) return;

        lastHitTime = Time.time;

        health -= amount;
        rb.linearVelocity = knockback;
        StartCoroutine(Hitstun());

        if (health <= 0f)
            LoseLife();
        if (hitstunRoutine != null) StopCoroutine(hitstunRoutine);
        hitstunRoutine = StartCoroutine(Hitstun());
    }

    private void LoseLife()
    {
        if (isDead) return;

        Lives--;
        OnLivesChanged?.Invoke(Lives);

        if (Lives <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // --- freeze and hide the player ---
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        // disable sprite (adjust if your sprite is a child)
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        // --- move to spawn ---
        Transform sp = spawnPoint != null ? spawnPoint : null;
        transform.position = sp != null ? sp.position : originalSpawn;

        // --- reset state on the movement controller ---
        if (controls != null)
            controls.ResetForRespawn();

        if (hitstunRoutine != null)
        {
            StopCoroutine(hitstunRoutine);
            hitstunRoutine = null;
        }

        // --- re-enable ---
        health = maxHealth;
        invincibleUntil = Time.time + respawnInvincibility;
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        if (sr) sr.enabled = true;

        isDead = false;
        isInHitstun = false;

        OnRespawned?.Invoke();

    }

    private void Die()
    {
        if (hitstunRoutine != null)
        {
            StopCoroutine(hitstunRoutine);
            hitstunRoutine = null;
        }

        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        // disable input via PlayerInput if you have one
        var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi) pi.enabled = false;

        // optional visual cue
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.color = new Color(1f, 0.4f, 0.4f, 1f);

       
        OnDied?.Invoke();
        Debug.Log($"{name} is out of lives — game over.");
    }

    // Optional: for out-of-bounds / smash-style KOs
    public void Kill()
    {
        if (isDead) return;
        health = 0f;
        LoseLife();
    }

    // Optional: health pickups
    public void Heal(float amount)
    {
        health = Mathf.Min(maxHealth, health + amount);
    }

    private Coroutine hitstunRoutine;
    private IEnumerator Hitstun()
    {
        isInHitstun = true;
        yield return new WaitForSeconds(hitstunDuration);
        isInHitstun = false;
        hitstunRoutine = null;
    }
}
