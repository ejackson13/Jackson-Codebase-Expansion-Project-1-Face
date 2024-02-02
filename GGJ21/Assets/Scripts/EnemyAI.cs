using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class EnemyAI : MonoBehaviour
{
    public float runSpeed = 2f;
    public Transform targetPosition;

    public GameObject player;

    public List<GameObject> patrolRooms;

    public AudioClip[] distantSounds;
    public AudioClip[] closerSounds;
    public AudioClip[] chaseSounds;

    public AudioClip calmMusic;
    public AudioClip tenseMusic;
    public AudioClip chaseMusic;

    public AudioSource musicSource;

    public SceneChanger sceneChanger;

    public Sprite defaultSprite;

    private float horizontal;
    private float vertical;
    private float moveLimiter = 0.7f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private AudioSource audioSource;

    private Seeker seeker;
    private Path path;
    private float nextWaypointDistance = 3;
    private int currentWaypoint = 0;
    public bool reachedEndOfPath;
    public float detectionDist; // the sitance at which the monster will detect the player without seeing them
    private Vector3 playerLastPos; // the position the player was last seen at
    public float visionDist;  // the distance the monster is able to see

    private int currentTargetPoint = 0;

    private bool isChasing = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = gameObject.transform.GetChild(0).transform.gameObject.GetComponent<SpriteRenderer>();
        anim = gameObject.transform.GetChild(0).transform.gameObject.GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        sr.sprite = defaultSprite;


        StartCoroutine(MonsterSounds());
    }

    // Update is called once per frame
    void Update()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        // send out raycast from monster in direction they are looking, filtering out all layers other than the default layer (where the player is)
        RaycastHit2D hit;
        string[] layersToMask = {LayerMask.LayerToName(0)};
        hit = Physics2D.Raycast(transform.position, new Vector2(horizontal, vertical), visionDist, LayerMask.GetMask(layersToMask));

        // if the player is within the detection radius or is hit by the raycast, enter the chase phase
        if ( distanceToPlayer < detectionDist || (hit.collider != null && hit.collider.gameObject.CompareTag("Player")))
        {
            //Debug.Log("I see you!");
            isChasing = true;
            targetPosition = player.transform;
            playerLastPos = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        }
        // resets the enemies target position to the current room it's patroling toward, but we don't want to override if the enemy is pursuing the player's last position
        else if (!isChasing)
        {
            PatrolPath();
        }

        if (path == null)
        {
            return;
        }

        reachedEndOfPath = false;

        float distanceToWaypoint;
        while (true)
        {
            distanceToWaypoint = Vector3.Distance(transform.position, path.vectorPath[currentWaypoint]);
            if (distanceToWaypoint < nextWaypointDistance)
            {
                if (currentWaypoint + 1 < path.vectorPath.Count)
                {
                    currentWaypoint++;
                }
                else
                {
                    SetPath();
                    isChasing = false;
                    reachedEndOfPath = true;
                    break;
                }
            }
            else break;
        }

        float speedFactor = reachedEndOfPath ? Mathf.Sqrt(distanceToWaypoint / nextWaypointDistance) : 1f;

        Vector3 dir = (path.vectorPath[currentWaypoint] - transform.position).normalized;

        //Vector2 velocity = dir * runSpeed * speedFactor;

        //horizontal += velocity.x * Time.deltaTime;
        //vertical += velocity.y * Time.deltaTime;

        
        horizontal = dir.x;
        vertical = dir.y;  
        
        // get the angle the enemy is looking at and turn the sprite in that direction (necessary for both aesthetics and the raycast to be looking in the proper direction)
        anim.SetBool("Vertical", true);
        float angleRad = Mathf.Atan2(vertical, horizontal);
        float angleDeg = angleRad * Mathf.Rad2Deg;
        sr.gameObject.transform.eulerAngles = new Vector3(0, 0, angleDeg + 90);
        

        // move toward the destination set before
        Pathfind();
    }

    private void FixedUpdate()
    {
        if (horizontal != 0 && vertical != 0)
        {
            horizontal *= moveLimiter;
            vertical *= moveLimiter;
        }

        rb.velocity = new Vector2(horizontal * runSpeed, vertical * runSpeed);
    }

    IEnumerator MonsterSounds()
    {
        Debug.Log("Monster Sounds playing");
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        int soundChance = Random.Range(0, 100);
        Debug.Log(soundChance);

        if (!isChasing)
        {
            if (soundChance <= 50 && !audioSource.isPlaying)
            {
                if (distanceToPlayer > 20)
                {
                    Debug.Log("Playing distant sound");
                    audioSource.volume = 0.5f;
                    audioSource.pitch = Random.Range(0.5f, 1.5f);
                    audioSource.PlayOneShot(distantSounds[Random.Range(0, distantSounds.Length)]);
                    yield return null;
                    StartCoroutine(MonsterSounds());
                }
                else if (distanceToPlayer > detectionDist && distanceToPlayer < 20)
                {
                    Debug.Log("Playing closer sound");
                    audioSource.volume = 0.8f;
                    audioSource.pitch = Random.Range(0.5f, 1.5f);
                    audioSource.PlayOneShot(closerSounds[Random.Range(0, closerSounds.Length)]);
                    yield return null;
                    StartCoroutine(MonsterSounds());
                }
            }
            else
            {
                yield return new WaitForSeconds(Random.Range(2.0f, 10f));
                StartCoroutine(MonsterSounds());
            }
        }
        else
        {
            if (soundChance <= 90 && !audioSource.isPlaying)
            {
                Debug.Log("Playing chase sound");
                audioSource.volume = 1.0f;
                audioSource.pitch = Random.Range(0.5f, 1.5f);
                audioSource.PlayOneShot(chaseSounds[Random.Range(0, chaseSounds.Length)]);
                yield return null;
                StartCoroutine(MonsterSounds());
            }
            else
            {
                yield return new WaitForSeconds(Random.Range(.5f, 2f));
                StartCoroutine(MonsterSounds());
            }
        }
    }

    public void StartPathfinding()
    {
        seeker = GetComponent<Seeker>();
        PatrolPath();
        Pathfind();
    }

    void PatrolPath()
    {
        targetPosition = patrolRooms[currentTargetPoint].transform;
    }

    void SetPath()
    {
        if (currentTargetPoint > 0)
        {
            currentTargetPoint--;
            if (patrolRooms[currentTargetPoint] != null)
            {
                targetPosition = patrolRooms[currentTargetPoint].transform;
            }
            else
            {
                SetPath();
            }
        }
        else
        {
            if (targetPosition.gameObject.name != "Player")
            {
                currentTargetPoint = Random.Range(2, patrolRooms.Count);
                if (patrolRooms[currentTargetPoint] != null)
                {
                    targetPosition = patrolRooms[currentTargetPoint].transform;
                }
                else
                {
                    SetPath();
                }
            }
        }
    }

    void Pathfind()
    {
        if (isChasing)
        {
            // move the enemy toward the player's last known position (keeps the enemy from immediately giving up the second the player is out of sight/range)
            seeker.StartPath(transform.position, playerLastPos, OnPathComplete);
        } 
        else
        {
            seeker.StartPath(transform.position, targetPosition.position, OnPathComplete);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name == "Player")
        {
            Debug.Log("I FOUND MY NEW FACE");
            Attack();
        }
    }

    void Attack()
    {
        //play sound
        sceneChanger.FailScene();
    }

    void OnPathComplete(Path p)
    {
        //Debug.Log("Path Found" + p.error);

        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    void OnDrawGizmos()
    {
        // Display the explosion radius when selected
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionDist);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, (new Vector3 (horizontal, vertical).normalized * visionDist) + transform.position);;
    }
}
