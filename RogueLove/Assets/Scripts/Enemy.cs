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
        CONTACT, RANGED, SPLITTER, STATIONARY, MINIBOSS, BOSS
    }

    [Header("SCRIPT REFERENCES")]

    // This enemy's Animator component
    public Animator animator;

    // This enemy's map reference
    public WalkerGenerator map;

    // This enemy's target
    public Vector3 target;

    // This enemy's player transform reference;
    [SerializeField]
    protected Transform player;

    // This enemy's pathfinder script
    [SerializeField]
    private Seeker seeker;

    // This enemy's followRange collider
    [SerializeField]
    protected CircleCollider2D followCollider;

    // This enemy's contact collider
    public Collider2D contactColl;

    // This enemy's Rigidbody component
    [SerializeField]
    protected Rigidbody2D rb;

    [SerializeField]
    // Enemy health bar
    private HealthBar healthBar;

    [Space(10)]
    [Header("ENEMY STATS")]

    // This enemy's attack damage
    public float damage;

    // This enemy's movement speed
    [SerializeField]
    protected float speed;

    [SerializeField]
    protected float wanderSpeed;

    // This enemy's attack speed
    public float attackSpeed;

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
    private int direc;
    protected Vector2 force;
    protected bool canWander;
    protected float wanderTimer = 0;
    protected float moveTime;
    protected float waitTime;
    protected float waitTimer = 0;
    protected bool waiting;

    // Start is called before the first frame update
    void Start()
    {
        if (healthBar == null) {
            Debug.Log("ContactEnemy healthbar is null! Reassigned.");
            healthBar = this.GetComponentInChildren<HealthBar>();
        }

        if (seeker == null) {
            Debug.Log("ContactEnemy seeker is null! Reassigned.");
            seeker = GetComponent<Seeker>();
        }
        if (rb == null) {
            Debug.Log("ContactEnemy rb is null! Reassigned.");
            rb = GetComponent<Rigidbody2D>();
        }

        player = GameObject.FindGameObjectWithTag("Player").transform;

        attackAnim = false;
        canWander = true;
        waiting = false;

        InvokeRepeating(nameof(UpdatePath), 0f, .5f);
    }

    void UpdatePath() {
        if (seeker.IsDone()) {
            seeker.StartPath(rb.position, target, OnPathComplete);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (waiting) {
            waitTimer += Time.deltaTime;
            //Debug.Log("waitTimer: " + waitTimer);
            if(waitTimer > waitTime) {
                waiting = false;
                canWander = true;
                waitTimer = 0;
            }
        }
        if (canWander == false && !waiting) {
            wanderTimer += Time.deltaTime;
            //Debug.Log("wanderTimer: " + wanderTimer);
            if(wanderTimer > moveTime) {
                waiting = true;
                wanderTimer = 0;
            }
        }
        
        // Pathfinding
        Pathfinder();

        DirectionFacing();

        AttackCheck();

    }

    // PATHFINDER MOVEMENT
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
        
        // Sprite direction facing
        if (rb.velocity.x >= 0.01f) {

            this.transform.localScale = new Vector3(1f, 1f, 1f);
            animator.SetBool("IsMoving", true);

        } else if (rb.velocity.x <= -0.01f) {

            this.transform.localScale = new Vector3(-1f, 1f, 1f);
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

    public virtual void PlayerCheck() {
        if (inFollowRadius == true) {
            canWander = false;
            force = Vector2.zero;
            target = player.position;
            Chase();
        } else if (inFollowRadius == false && canWander && !waiting) {
            Debug.Log("STARTED WANDERING");
            canWander = false;
            Wander();
        }
    }

    // Ends the attack animation (RUNS AT THE LAST FRAME OF ANIMATION)
    public void CheckTrigger() {
        //attackAnim = false;
        animator.SetBool("Attack", false);
        //contactColl.enabled = true;
    }

    void OnPathComplete(Pathfinding.Path p) {
        //Debug.Log("OnPathComplete CALLED");
        if (!p.error) {
            path = p;
            currentWaypoint = 0;
        }
    }

    public virtual void Chase() {
        // Sets direction and destination of path to Player
        force = speed * Time.deltaTime * direction;

        // Moves towards target
        rb.AddForce(force);
    }

    public virtual void Wander() {
        Debug.Log("Wandering");
        canWander = false;
        waiting = false;

        StartCoroutine(Roam());
        return;
    }

    // Wander logic
    public IEnumerator Roam() {

        // Picks a random direction
        direc = UnityEngine.Random.Range(0, 8);

        // Sets the amount of time spent moving
        moveTime = UnityEngine.Random.Range(1, 3);

        // Sets a cooldown before wandering again
        waitTime = UnityEngine.Random.Range(2, 5);

        switch (direc) {
            case 0:
                force = wanderSpeed * Time.deltaTime * Vector2.up;
                yield return null;
                break;
            case 1:
                force = wanderSpeed * Time.deltaTime * Vector2.down;
                yield return null;
                break;
            case 2:
                force = wanderSpeed * Time.deltaTime * Vector2.right;
                yield return null;
                break;
            case 3:
                force = wanderSpeed * Time.deltaTime * Vector2.left;
                yield return null;
                break;
            case 4:
                force = wanderSpeed * Time.deltaTime * Vector2.zero;
                yield return null;
                break;
            case 5:
                force = wanderSpeed * Time.deltaTime * Vector2.up;
                force += wanderSpeed * Time.deltaTime * Vector2.right;
                yield return null;
                break;
            case 6:
                force = wanderSpeed * Time.deltaTime * Vector2.up;
                force += wanderSpeed * Time.deltaTime * Vector2.left;
                yield return null;
                break;
            case 7:
                force = wanderSpeed * Time.deltaTime * Vector2.down;
                force += wanderSpeed * Time.deltaTime * Vector2.right;
                yield return null;
                break;
            case 8:
                force = wanderSpeed * Time.deltaTime * Vector2.down;
                force += wanderSpeed * Time.deltaTime * Vector2.left;
                yield return null;
                break;
            default:
                direc = UnityEngine.Random.Range(0, 4);
                yield return null;
                break;
        }
        //Debug.Log("Set Direction");
        while (canWander == false && !waiting && !inFollowRadius) {
            //Debug.Log("IN THE LOOP");

            // Moves in the set direction for wandering
            rb.AddForce(force);
            yield return null;
        }
        yield return null;
    }

    public virtual void AttackCheck() {
        
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

    public void RemoveEnemy() {
        Destroy(gameObject);
    }
}
