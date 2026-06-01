using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{

    [Header("Combat")]
    public Transform attackPoint;

    public float attackRadius = 0.5f;

    public LayerMask enemyLayer;

    public int damage = 1;

    [Header("Attack Settings")]
    public float attackInterval = 0.1f;

    [Header("References")]
    public GameObject hitbox;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("Debug")]
    public bool isAttacking;
    public Direction currentDirection;


    // Dictionary containing all attack data
    public Dictionary<Direction, AttackData> attacks =
        new Dictionary<Direction, AttackData>();

    // Attack directions
    private void Update()
    {
        UpdateCurrentDirection();

        if (Input.GetKeyDown(KeyCode.J))
        {
            StartCoroutine(AttackRoutine(Direction.Right));
        }
    }

    private void UpdateCurrentDirection()
    {
        if (spriteRenderer != null)
        {
            // Hollow Knight style: ataca para esquerda/direita baseado no flip
            if (spriteRenderer.flipX)
                currentDirection = Direction.Left;
            else
                currentDirection = Direction.Right;
        }
        else
        {
            // Fallback: usa a escala local
            if (transform.localScale.x < 0)
                currentDirection = Direction.Left;
            else
                currentDirection = Direction.Right;
        }
    }
    public void SetDirection(Direction newDirection)
    {
        currentDirection = newDirection;

        // Opcional: atualizar flip do sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (newDirection == currentDirection);
        }
    }
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    [System.Serializable]
    public class AttackData
    {
        public string animation;
        public Vector2 hitboxOffset;
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (attackPoint == null)
            Debug.LogError("AttackPoint não foi atribuído!");

        if (hitbox == null)
            Debug.LogError("Hitbox não foi atribuída!");
        else
            hitbox.SetActive(false);

        attacks = new Dictionary<Direction, AttackData>()
    {
        {
            Direction.Up,
            new AttackData()
            {
                animation = "attack",
                hitboxOffset = new Vector2(0, 1)
            }
        },

        {
            Direction.Down,
            new AttackData()
            {
                animation = "attack",
                hitboxOffset = new Vector2(0, -1)
            }
        },

        {
            Direction.Left,
            new AttackData()
            {
                animation = "attack",
                hitboxOffset = new Vector2(-1, 0)
            }
        },

        {
            Direction.Right,
            new AttackData()
            {
                animation = "attack",
                hitboxOffset = new Vector2(1, 0)
            }
        }
    };
    }

    // Public coroutine so other scripts can call it
    public IEnumerator AttackRoutine(Direction dir)
    {
        // Prevent spam
        if (isAttacking)
            yield break;

        isAttacking = true;

        // Get attack data
        AttackData data = attacks[dir];

        // Play animation
        animator.Play(data.animation);

        // Startup frames
        yield return new WaitForSeconds(0.1f);

        // Move hitbox visual
        hitbox.transform.localPosition = data.hitboxOffset;

        attackPoint.transform.localPosition = data.hitboxOffset;

        // Enable visual hitbox
        hitbox.SetActive(true);

        // REAL DAMAGE HAPPENS HERE
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRadius,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            Debug.Log("Something detected");

            EnemyHealth enemyHealth =
                hit.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                Debug.Log("Enemy Hit!");

                enemyHealth.TakeDamage(damage);
            }
        }

        // Active frames
        yield return new WaitForSeconds(0.15f);

        // Disable visual hitbox
        hitbox.SetActive(false);

        // Recovery
        yield return new WaitForSeconds(attackInterval);

        animator.Play("idle");

        isAttacking = false;
    }

    private void OnDrawGizmos()
    {
        if (hitbox == null)
            return;

        BoxCollider2D box = hitbox.GetComponent<BoxCollider2D>();

        if (box == null)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(
            box.bounds.center,
            box.bounds.size
        );
    }
}