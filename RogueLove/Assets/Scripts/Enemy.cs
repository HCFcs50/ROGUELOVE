using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.IO;
using NUnit.Framework.Constraints;
using System;
using UnityEngine.Serialization;
using TMPro;

public abstract class Enemy : MonoBehaviour
{
    public enum EnemyType {
        CONTACT, RANGED, SPLITTER, STATIONARY, MINIBOSS, BOSS, DEAD
    }

    public EnemyType enemyType;

    [Header("SCRIPT REFERENCES")]

    // This enemy's Animator component
    public Animator animator;

    // This enemy's map reference
    public WalkerGenerator map;

    // This enemy's target
    public Vector3 target;

    // This enemy's player transform reference;
    public Transform player;

    // This enemy's pathfinder script
    [SerializeField]
    private Seeker seeker;

    // This enemy's followRange collider
    [SerializeField]
    protected CircleCollider2D followCollider;

    // This enemy's contact collider
    public Collider2D contactColl;

    // This enemy's Rigidbody component
    public Rigidbody2D rb;

    public Collider2D hitbox;

    // Enemy health bar
    [SerializeField] protected HealthBar healthBar;

    [SerializeField] private List<GameObject> dropsList;

    [SerializeField] private List<GameObject> coinsList;

    [SerializeField] private int minCoins;
    [SerializeField] private int maxCoins;

    [SerializeField] private GameObject expOrb;

    // Minimum and maximum experience / energy to drop on death
    [SerializeField] private int minExp;
    [SerializeField] private int maxExp;

    [Space(10)]
    [Header("ENEMY STATS")]

    // This enemy's attack damage
    public int damage;

    // This enemy's movement speed
    [SerializeField]
    protected float chaseSpeed;

    [SerializeField]
    protected float wanderSpeed;

    // This enemy's attack speed
    public float rangedAttackCooldownMin;
    public float rangedAttackCooldownMax;
    public float attackCooldown;

    // Boolean to determine whether attack animation is playing
    protected bool attackAnim;

    [Space(10)]
    [Header("PATHFINDING")]

    // This enemy's pathfinding waypoint distance
    [SerializeField]
    private float nextWaypointDistance = 3f;

    Pathfinding.Path path;
    private int currentWaypoint = 0;
    protected bool reachedEndOfPath = false;
    public bool inFollowRadius;
    protected Vector2 direction;
    protected int direc;
    protected Vector2 force;

    [SerializeField]
    protected bool canWander;
    protected float wanderTimer = 0;
    protected float moveTime;
    protected float waitTime;
    protected float waitTimer = 0;

    protected bool timerSet = false;

    protected bool tileGot = false;

    public bool seen;

    public bool kbEd;

    protected bool expSpawn;

    public bool hitPlayer = false;

    // Start is called before the first frame update
    public virtual void Start()
    {
        if (enemyType != EnemyType.DEAD) {
            SetEnemyType();

            if (healthBar == null) {
                healthBar = this.GetComponentInChildren<HealthBar>();
                Debug.Log("ContactEnemy healthbar is null! Reassigned.");
            }
            if (rb == null) {
                rb = GetComponent<Rigidbody2D>();
                Debug.Log("ContactEnemy rb is null! Reassigned.");
            }
            if (hitbox == null) {
                hitbox = GetComponentInChildren<Collider2D>();
                Debug.Log("Collider2D hitbox is null! Reassigned.");
            }
            hitbox.enabled = true;

            player = GameObject.FindGameObjectWithTag("Player").transform;

            attackAnim = false;
            seen = false;
            expSpawn = false;

            if (enemyType != EnemyType.STATIONARY) {
                if (seeker == null) {
                    Debug.Log("ContactEnemy seeker is null! Reassigned.");
                    seeker = GetComponent<Seeker>();
                }
                canWander = true;
                kbEd = false;

                InvokeRepeating(nameof(UpdatePath), 0f, .5f);
            }
        }
    }

    public virtual void SetEnemyType() {
        enemyType = EnemyType.CONTACT;
    }

    void UpdatePath() {
        if (seeker.IsDone()) {
            seeker.StartPath(rb.position, target, OnPathComplete);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (GameStateManager.GetState() != GameStateManager.GAMESTATE.GAMEOVER 
        && GameStateManager.GetState() != GameStateManager.GAMESTATE.PAUSED) {

            if (enemyType != EnemyType.DEAD && enemyType != EnemyType.STATIONARY) {

                if (canWander && timerSet) {
                    wanderTimer += Time.fixedDeltaTime;
                    //Debug.Log("wanderTimer: " + wanderTimer);
                    if(wanderTimer > moveTime) {
                        //Debug.Log("Done With WanderTimer");
                        canWander = false;
                        tileGot = false;
                        wanderTimer = 0;
                    }
                }
                
                if (!canWander) {
                    waitTimer += Time.fixedDeltaTime;
                    //Debug.Log("waitTimer: " + waitTimer);
                    if(waitTimer > waitTime) {
                        canWander = true;
                        timerSet = false;
                        waitTimer = 0;
                    }
                }

                // Pathfinding
                Pathfinder();

                DirectionFacing();
            }
        }
    }

    // PATHFINDER MOVEMENT and calling PlayerCheck()
    public virtual void Pathfinder() {
        // 1.
        if (path == null)
            return;

        if (currentWaypoint >= path.vectorPath.Count) {
            //Debug.Log("REACHED END OF PATH");
            reachedEndOfPath = true;
            return;

        } else {
            reachedEndOfPath = false;
        }

        // 2.
        direction = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;

        // 3.
        PlayerCheck();

        // 4.
        float distance = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);
        //Debug.Log("DISTANCE " + distance);

        if (distance < nextWaypointDistance) {
            currentWaypoint++;
        }
    }

    public virtual void DirectionFacing() {
        if (!kbEd) {
            // Sprite direction facing
            if (rb.velocity.x >= 0.001f) {

                this.transform.localScale = new Vector3(1f, 1f, 1f);
                animator.SetBool("IsMoving", true);

            } else if (rb.velocity.x <= -0.001f) {

                this.transform.localScale = new Vector3(-1f, 1f, 1f);
                animator.SetBool("IsMoving", true);

            } else if (rb.velocity.y <= -0.001 || rb.velocity.y >= 0.001) {
                animator.SetBool("IsMoving", true);
            } else {
                animator.SetBool("IsMoving", false);
            }

            // Make health bar face the same way regardless of enemy sprite
            if (this.transform.localScale == new Vector3(1f, 1f, 1f)) {

                healthBar.transform.localScale = new Vector3(1f, 1f, 1f);

            } else if (this.transform.localScale == new Vector3(-1f, 1f, 1f)) {

                healthBar.transform.localScale = new Vector3(-1f, 1f, 1f);

            }
        }
    }

    public virtual void PlayerCheck() {

        if (!timerSet) {

            // Sets the amount of time spent moving
            moveTime = UnityEngine.Random.Range(3, 5);

            // Sets a cooldown before wandering again
            waitTime = UnityEngine.Random.Range(5, 7);
            
            timerSet = true;
        }

        // If enemy is not currently taking knockback
        if (!kbEd) {

            // If player is in follow radius then chase
            if (inFollowRadius == true) {
                seen = true;
                canWander = false;
                force = Vector2.zero;
                target = player.position;
                Chase();
            } 
            // If player is not in follow radius, and wander cooldown is reset, then wander
            else if (inFollowRadius == false && canWander) {

                // Gets target tile
                Vector3 randTile = GetWanderTile();

                // If tile hasn't been checked for validity
                if (!tileGot) {
                    tileGot = true;

                    // Set target to tile
                    target = randTile;
                }

                // Wander to tile
                Wander();
            }
        }
    }

    // Ends the attack animation (RUNS AT THE LAST FRAME OF ANIMATION)
    public void StopAttackAnim() {
        //attackAnim = false;
        animator.SetBool("Attack", false);
    }

    void OnPathComplete(Pathfinding.Path p) {
        //Debug.Log("OnPathComplete CALLED");
        if (!p.error) {
            path = p;
            currentWaypoint = 0;
        }
    }

    public virtual void Chase() {
        //Debug.Log("CHASING");
        // Sets direction and destination of path to Player
        force = chaseSpeed * Time.fixedDeltaTime * direction;

        // Moves towards target
        rb.AddForce(force);
    }

    public virtual void Wander() {
        //Debug.Log("WANDERING");
        
        force = wanderSpeed * Time.fixedDeltaTime * direction;

        rb.AddForce(force);
    }

    public virtual Vector3 GetWanderTile() {

        // Picks a random tile within radius
        float tileX = UnityEngine.Random.Range(this.transform.position.x - followCollider.radius, 
            this.transform.position.y + followCollider.radius);

        float tileY = UnityEngine.Random.Range(this.transform.position.y - followCollider.radius, 
            this.transform.position.y + followCollider.radius);

        Vector3 tile = new Vector3(tileX, tileY);

        if (map.CheckGroundTile(tile)) {
            return tile;
        } else {
            GetWanderTile();
        }

        return Vector3.zero;
    }

    public virtual IEnumerator AttackEntity(Collider2D target) {
        yield return null;
    }

    public UnityEngine.Object Create(UnityEngine.Object original, Vector3 position, Quaternion rotation, WalkerGenerator gen) {
        GameObject entity = Instantiate(original, position, rotation) as GameObject;
        
        if (entity.TryGetComponent<Enemy>(out var enemy)) {
            enemy.map = gen;
            return entity;
        } else {
            Debug.LogError("Could not find Enemy script or extension of such on this Object.");
            return null;
        }
    }

    public virtual void SpawnExp() {

        int rand = UnityEngine.Random.Range(minExp, maxExp);

        for (int i = 0; i < rand; i++) {
            Create(expOrb, this.transform.position, Quaternion.identity, this.map);
        }
    }

    public virtual void SpawnDrops() {

        foreach (var coin in coinsList) {

            int rand = UnityEngine.Random.Range(minCoins, maxCoins);

            Debug.Log("Drop coins!");
        }

        foreach (var drop in dropsList) {

            int rand = UnityEngine.Random.Range(0, 11);

            if (rand == 1) {
                Debug.Log("Created pickup drop!");
            }
        }
    }

    public virtual void RemoveHitbox() {
        hitbox.enabled = false;
    }

    public virtual void EnemyDeath() {

        // Sets force to 0 so that the enemy doesn't just fly off
        force = 0 * Time.fixedDeltaTime * direction;

        // Sets enemy type to DEAD
        enemyType = EnemyType.DEAD;

        // Spawns EXP
        SpawnExp();
        SpawnDrops();

        // Increments dead enemy counter
        WalkerGenerator.SetDeadEnemy();
        Debug.Log(WalkerGenerator.GetDeadEnemies() + "/" + WalkerGenerator.EnemyTotal);
    }

    public virtual void RemoveEnemy() {

        // Spawns EXP and drops
        SpawnExp();
        SpawnDrops();

        // Removes enemy
        Destroy(gameObject);

        // Increments dead enemy counter
        WalkerGenerator.SetDeadEnemy();
        Debug.Log(WalkerGenerator.GetDeadEnemies() + "/" + WalkerGenerator.EnemyTotal);
    }
}
