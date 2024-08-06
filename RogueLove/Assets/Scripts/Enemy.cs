using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.IO;
using NUnit.Framework.Constraints;
using System;
using UnityEngine.Serialization;
using TMPro;
using Game.Core.Rendering;
using UnityEngine.AI;

public enum EnemyType {
    CONTACT, RANGED, SPLITTER, STATIONARY, MINIBOSS, BOSS, DEAD
}

public abstract class Enemy : MonoBehaviour
{

    [Tooltip("This enemy's type.")]
    public EnemyType enemyType;

    [Header("SCRIPT REFERENCES")]

    [Tooltip("This enemy's animator component.")]
    public Animator animator;

    [Tooltip("This enemy's level map reference. (Assigned at runtime)")]
    public WalkerGenerator map;

    [Tooltip("This enemy's current target.")]
    protected Vector3 target;

    [Tooltip("This enemy's player Transform reference. (Assigned at runtime)")]
    public Transform player;

    [Tooltip("This enemy's pathfinder script.")]
    [SerializeField] private Seeker seeker;

    [Tooltip("This enemy's follow collider, responsible for how far away it will follow a target.")]
    [SerializeField]
    protected CircleCollider2D followCollider;

    [Tooltip("This enemy's contact collider, responsible for how far contact enemies can attack from (NOT EQUAL TO HITBOX). Should only be set for Contact enemies.")]
    public Collider2D contactColl;

    [Tooltip("This enemy's Rigidbody2D component.")]
    public Rigidbody2D rb;

    [Tooltip("This enemy's hitbox.")]
    public Collider2D hitbox;

    [Tooltip("This enemy's SpriteRenderer that is responsible for drawing the line of fire.")]
    public SpriteRenderer lineSpriteRenderer;

    [Tooltip("This enemy's LineRenderer2D component for drawing line of fire.")]
    public LineRenderer2D lineRenderer;

    [Tooltip("Time it takes for the enemy to charge a shot.")]
    public float chargeTime;

    [Tooltip("List of this enemy's possible loot drops (excluding coins and energy).")]
    [SerializeField] private List<GameObject> dropsList;

    [Tooltip("A reference to the bronze coin prefab for spawning.")]
    [SerializeField] private GameObject coinBronze;

    [Tooltip("A reference to the silver coin prefab for spawning.")]
    [SerializeField] private GameObject coinSilver;

    [Tooltip("A reference to the gold coin prefab for spawning.")]
    [SerializeField] private GameObject coinGold;

    [Tooltip("The minimum possible amount of coins this enemy drops upon death.")]
    [SerializeField] private int minCoins;

    [Tooltip("The maximum possible amount of coins this enemy drops upon death.")]
    [SerializeField] private int maxCoins;

    [Tooltip("The experience orb GameObject this enemy drops upon death.")]
    [SerializeField] private GameObject expOrb;

    // Minimum and maximum experience / energy to drop on death
    [SerializeField] private int minExp;
    [SerializeField] private int maxExp;

    [Space(10)]
    [Header("ENEMY STATS")]

    [Tooltip("The variable responsible for how much the spawn rate curve is vertically amplified. - (a)")]
    public float spawnChanceVertAmp = 1;

    [Tooltip("The variable responsible for how steep the spawn rate curve is across levels. - (m)")]
    public float spawnChanceMultiplier = 1;

    [Tooltip("The variable responsible for horizontal transformation of the spawn rate curve. - (h)")]
    public float spawnChanceXTransform = 1;

    [Tooltip("The variable responsible for the exponential amplification of the spawn rate curve. - (p)")]
    public float spawnChanceExponent = 2;

    [Tooltip("The variable responsible for vertical transformation of the spawn rate curve. - (v)")]
    public float spawnChanceYTransform = 0;

    [Tooltip("Maximum overall target chance for this enemy to spawn in any level.")]
    public float maxSpawnChance = 1;

    [Tooltip("This enemy's damage-per-hit.")]
    public int damage;

    [Tooltip("The speed that this enemy moves when it is chasing a target.")]
    [SerializeField] protected float chaseSpeed;

    [Tooltip("The speed that this enemy moves when it is wandering around.")]
    [SerializeField] protected float wanderSpeed;

    // This enemy's attack speed
    public float rangedAttackCooldownMin;
    public float rangedAttackCooldownMax;
    public float attackCooldown;

    // Boolean to determine whether attack animation is playing
    protected bool attackAnim;

    [Space(10)]
    [Header("PATHFINDING")]

    // This enemy's pathfinding waypoint distance
    [SerializeField] private float nextWaypointDistance = 3f;

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
    protected bool coinSpawn;

    public bool hitPlayer = false;

    // Start is called before the first frame update
    public virtual void Start()
    {
        if (enemyType != EnemyType.DEAD) {
            SetEnemyType();

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
            coinSpawn = false;

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
        if (GameStateManager.GetState() != GAMESTATE.GAMEOVER 
        && GameStateManager.GetState() != GAMESTATE.PAUSED) {

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

        if (distance < nextWaypointDistance) {
            currentWaypoint++;
        }
    }

    // Sprite direction facing
    public virtual void DirectionFacing() {

        if (!kbEd) {

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
        animator.SetBool("Attack", false);
    }

    void OnPathComplete(Pathfinding.Path p) {
        if (!p.error) {
            path = p;
            currentWaypoint = 0;
        }
    }

    public virtual void Chase() {

        // Sets direction and destination of path to Player
        force = chaseSpeed * Time.fixedDeltaTime * direction;

        // Moves towards target
        rb.AddForce(force);
    }

    public virtual void Wander() {        
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
        } else if (entity.GetComponentInChildren<Enemy>()) {
            entity.GetComponentInChildren<Enemy>().map = gen;
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

        int rand = UnityEngine.Random.Range(minCoins, maxCoins);

        Debug.Log("total coins:" + rand);

        // Separate coin values and incrementally drop from highest value to lowest value coins to meet total
        if (rand >= 20) {

            // Drop a single gold coin if total coins is exactly 20
            if (rand == 20) {
                Create(coinGold, this.transform.position, Quaternion.identity, this.map);
                return;
            }

            // Drop gold coins
            for (int g = 0; g < (rand - (rand % 20)) / 20; g++) {
                Create(coinGold, this.transform.position, Quaternion.identity, this.map);
            }

            // Update total amount of coins to exclude gold
            rand %= 20;

            // Drop silver coins
            for (int s = 0; s < (rand - (rand % 5)) / 5; s++) {
                Create(coinSilver, this.transform.position, Quaternion.identity, this.map);
            }

            // Drop bronze coins as leftover
            for (int b = 0; b < rand % 5; b++) {
                Create(coinBronze, this.transform.position, Quaternion.identity, this.map);
            }

        } else if (rand >= 5) {

            // Drop a single silver coin if total coins is exactly 5
            if (rand == 5) {
                Create(coinSilver, this.transform.position, Quaternion.identity, this.map);
                return;
            }

            // Drop silver coins
            for (int s = 0; s < (rand - (rand % 5)) / 5; s++) {
                Create(coinSilver, this.transform.position, Quaternion.identity, this.map);
            }

            // Drop bronze coins as leftover
            for (int b = 0; b < rand % 5; b++) {
                Create(coinBronze, this.transform.position, Quaternion.identity, this.map);
            }

        } else {

            // Drop bronze coins
            for (int b = 0; b < rand; b++) {
                Create(coinBronze, this.transform.position, Quaternion.identity, this.map);
            }
        }

        foreach (var drop in dropsList) {

            int rando = UnityEngine.Random.Range(0, 11);

            if (rando == 1) {
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
