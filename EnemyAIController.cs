using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CapsuleCollider))]
public class EnemyAIController : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    public float health = 100f;
    public float sightRange = 20f;
    public float attackRange = 15f;
    public float fieldOfView = 90f;
    
    [Header("Weapon Settings")]
    public GameObject weaponPrefab;
    public Transform weaponHolder;
    public float fireRate = 0.2f;
    public float bulletDamage = 10f;
    public float bulletSpeed = 40f;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public AudioClip shootSound;
    public GameObject muzzleFlashEffect;
    
    [Header("Grenade Settings")]
    public GameObject grenadePrefab;
    public float grenadeThrowForce = 15f;
    public float grenadeThrowCooldown = 10f;
    public float grenadeDetectionRadius = 5f;
    
    [Header("Combat Behavior")]
    [Range(0f, 1f)] public float aggressiveness = 0.7f;
    [Range(0f, 1f)] public float accuracy = 0.85f;
    public float coverSearchRadius = 10f;
    public LayerMask coverLayers;
    public LayerMask playerLayer;
    
    // Private variables
    private NavMeshAgent agent;
    private Transform player;
    private Vector3 lastKnownPlayerPosition;
    private AIState currentState;
    private float nextFireTime;
    private float nextGrenadeTime;
    private bool playerInSight;
    private GameObject currentWeapon;
    private AudioSource audioSource;
    private bool isRepositioning = false;
    private Vector3 coverPosition;
    private float stateUpdateInterval = 0.5f;
    private float lastStateUpdateTime;
    private int burstCount;
    private int maxBurst = 5;
    private float burstPause = 0.4f;
    private float lastBurstTime;
    
    // Attack pattern variables
    private int currentAttackPattern = 0;
    private float patternSwitchTime = 10f;
    private float lastPatternSwitchTime;
    
    private enum AIState
    {
        Idle,
        Patrolling,
        Investigating,
        Attacking,
        TakingCover,
        Flanking,
        Retreating
    }
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogWarning("Player not found. Make sure player has 'Player' tag.");
        }
        
        currentState = AIState.Patrolling;
        
        // Initialize weapon if provided
        if (weaponPrefab != null && weaponHolder != null)
        {
            EquipWeapon();
        }
        
        // Start behavior tree
        StartCoroutine(AIBehaviorRoutine());
        
        // Initialize times
        nextGrenadeTime = Time.time + Random.Range(3f, grenadeThrowCooldown);
        lastPatternSwitchTime = Time.time;
    }
    
    void Update()
    {
        if (Time.time - lastStateUpdateTime > stateUpdateInterval)
        {
            UpdatePlayerVisibility();
            UpdateState();
            lastStateUpdateTime = Time.time;
        }
        
        // Check for pattern switch
        if (Time.time - lastPatternSwitchTime > patternSwitchTime)
        {
            SwitchAttackPattern();
            lastPatternSwitchTime = Time.time;
        }
        
        // Execute current state behavior
        ExecuteStateBehavior();
    }
    
    void EquipWeapon()
    {
        currentWeapon = Instantiate(weaponPrefab, weaponHolder.position, weaponHolder.rotation);
        currentWeapon.transform.SetParent(weaponHolder);
    }
    
    void UpdatePlayerVisibility()
    {
        playerInSight = false;
        
        if (player == null) return;
        
        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        if (distanceToPlayer <= sightRange)
        {
            // Check if player is within field of view
            float angle = Vector3.Angle(transform.forward, directionToPlayer.normalized);
            if (angle <= fieldOfView / 2)
            {
                // Check if there's an obstacle between enemy and player
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up * 1.5f, directionToPlayer.normalized, out hit, sightRange, ~coverLayers))
                {
                    if (hit.transform == player)
                    {
                        playerInSight = true;
                        lastKnownPlayerPosition = player.position;
                    }
                }
            }
        }
        
        // Can also detect player by being shot at (implemented in TakeDamage)
    }
    
    void UpdateState()
    {
        if (health <= 30 && Random.value < 0.7f)
        {
            currentState = AIState.Retreating;
            return;
        }
        
        if (playerInSight)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= attackRange)
            {
                if (NeedsCover() && FindCoverPosition())
                {
                    currentState = AIState.TakingCover;
                }
                else
                {
                    currentState = AIState.Attacking;
                }
            }
            else if (aggressiveness > 0.5f)
            {
                currentState = AIState.Investigating;
            }
            else
            {
                currentState = AIState.Patrolling;
            }
        }
        else if (lastKnownPlayerPosition != Vector3.zero)
        {
            currentState = AIState.Investigating;
        }
        else
        {
            currentState = AIState.Patrolling;
        }
    }
    
    bool NeedsCover()
    {
        // Decide if AI should seek cover based on health and aggressiveness
        return (health < 50 && Random.value > aggressiveness) || (Random.value < 0.3f && Time.time > nextFireTime + 1.5f);
    }
    
    bool FindCoverPosition()
    {
        // Find potential cover positions
        Collider[] coverObjects = Physics.OverlapSphere(transform.position, coverSearchRadius, coverLayers);
        List<Vector3> potentialPositions = new List<Vector3>();
        
        foreach (Collider cover in coverObjects)
        {
            // Get positions around the cover object
            Vector3 directionToPlayer = (player.position - cover.transform.position).normalized;
            Vector3 coverPosition = cover.transform.position - directionToPlayer * 2f;
            
            // Check if position is on navmesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(coverPosition, out hit, 2f, NavMesh.AllAreas))
            {
                // Check if position provides cover
                RaycastHit lineOfSightCheck;
                if (!Physics.Linecast(hit.position + Vector3.up * 1f, player.position + Vector3.up * 1f, out lineOfSightCheck, ~coverLayers))
                {
                    continue;
                }
                
                potentialPositions.Add(hit.position);
            }
        }
        
        if (potentialPositions.Count > 0)
        {
            // Choose nearest cover position
            coverPosition = potentialPositions[Random.Range(0, potentialPositions.Count)];
            return true;
        }
        
        return false;
    }
    
    void ExecuteStateBehavior()
    {
        switch (currentState)
        {
            case AIState.Idle:
                // Just stand still and look around occasionally
                break;
                
            case AIState.Patrolling:
                // Simple patrol behavior - implement more complex patrol paths as needed
                if (agent.remainingDistance < 0.5f || !agent.hasPath)
                {
                    Vector3 randomPoint = transform.position + Random.insideUnitSphere * 10f;
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
                break;
                
            case AIState.Investigating:
                // Move to last known player position
                agent.SetDestination(lastKnownPlayerPosition);
                
                // If reached position, go back to patrolling
                if (agent.remainingDistance < 0.5f)
                {
                    currentState = AIState.Patrolling;
                    lastKnownPlayerPosition = Vector3.zero;
                }
                break;
                
            case AIState.Attacking:
                // Stop moving when attacking
                agent.isStopped = true;
                
                // Look at player
                if (player != null)
                {
                    Vector3 lookDirection = player.position - transform.position;
                    lookDirection.y = 0;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 0.1f);
                    
                    // Execute current attack pattern
                    ExecuteAttackPattern();
                    
                    // Reposition periodically
                    if (!isRepositioning && Random.value < 0.01f)
                    {
                        StartCoroutine(RepositionRoutine());
                    }
                }
                break;
                
            case AIState.TakingCover:
                // Move to cover position
                agent.isStopped = false;
                agent.SetDestination(coverPosition);
                
                // If reached cover, attack from cover or wait
                if (agent.remainingDistance < 0.5f)
                {
                    if (playerInSight && Random.value < 0.3f)
                    {
                        AttackFromCover();
                    }
                }
                break;
                
            case AIState.Flanking:
                // Try to find position to flank the player
                if (!isRepositioning)
                {
                    FindFlankingPosition();
                    isRepositioning = true;
                }
                
                // If reached flanking position, attack
                if (agent.remainingDistance < 0.5f)
                {
                    currentState = AIState.Attacking;
                    isRepositioning = false;
                }
                break;
                
            case AIState.Retreating:
                // Find retreat position away from player
                if (!isRepositioning)
                {
                    FindRetreatPosition();
                    isRepositioning = true;
                }
                
                // If reached retreat position, take cover or continue patrolling
                if (agent.remainingDistance < 0.5f)
                {
                    currentState = NeedsCover() ? AIState.TakingCover : AIState.Patrolling;
                    isRepositioning = false;
                    health += 10; // Simulate recovering a small amount of health when successfully retreated
                }
                break;
        }
    }
    
    IEnumerator AIBehaviorRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            
            // Throw grenades occasionally if player is in sight
            if (playerInSight && Time.time > nextGrenadeTime && grenadePrefab != null && Random.value < 0.1f)
            {
                ThrowGrenade();
                nextGrenadeTime = Time.time + grenadeThrowCooldown;
            }
        }
    }
    
    IEnumerator RepositionRoutine()
    {
        isRepositioning = true;
        agent.isStopped = false;
        
        // Find a random nearby position
        Vector3 randomDirection = Random.insideUnitSphere;
        randomDirection.y = 0;
        Vector3 repositionTarget = transform.position + randomDirection.normalized * 5f;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(repositionTarget, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            yield return new WaitForSeconds(2f);
        }
        
        isRepositioning = false;
        currentState = AIState.Attacking;
    }
    
    void FindFlankingPosition()
    {
        if (player == null) return;
        
        // Calculate a position to the side of the player
        Vector3 playerForward = player.forward;
        Vector3 flankDirection = Quaternion.Euler(0, Random.Range(80, 100) * (Random.value > 0.5f ? 1 : -1), 0) * playerForward;
        Vector3 potentialPosition = player.position + flankDirection.normalized * Random.Range(5f, 8f);
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(potentialPosition, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    
    void FindRetreatPosition()
    {
        if (player == null) return;
        
        // Move in opposite direction from player
        Vector3 retreatDirection = transform.position - player.position;
        Vector3 potentialPosition = transform.position + retreatDirection.normalized * 15f;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(potentialPosition, out hit, 15f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    
    void AttackFromCover()
    {
        // Temporarily peek out from cover to shoot
        StartCoroutine(PeekAndShootRoutine());
    }
    
    IEnumerator PeekAndShootRoutine()
    {
        // Peek out
        if (player != null)
        {
            Vector3 originalPosition = transform.position;
            Vector3 peekPosition = transform.position + (player.position - transform.position).normalized * 1.5f;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(peekPosition, out hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                
                // Wait until peek position is reached
                yield return new WaitForSeconds(0.5f);
                
                // Look at player and shoot
                Vector3 lookDirection = player.position - transform.position;
                lookDirection.y = 0;
                transform.rotation = Quaternion.LookRotation(lookDirection);
                
                // Fire several shots
                for (int i = 0; i < Random.Range(3, 6); i++)
                {
                    Shoot();
                    yield return new WaitForSeconds(fireRate);
                }
                
                // Move back to cover
                agent.SetDestination(originalPosition);
            }
        }
    }
    
    void SwitchAttackPattern()
    {
        // Choose a new attack pattern
        currentAttackPattern = Random.Range(0, 4);
        
        // Reset burst counters
        burstCount = 0;
        lastBurstTime = 0;
    }
    
    void ExecuteAttackPattern()
    {
        switch (currentAttackPattern)
        {
            case 0: // Single precise shots
                if (Time.time > nextFireTime)
                {
                    Shoot(true); // More accurate
                    nextFireTime = Time.time + fireRate * 2f; // Slower fire rate but more accurate
                }
                break;
                
            case 1: // Burst fire
                if (Time.time > nextFireTime)
                {
                    if (burstCount < maxBurst)
                    {
                        Shoot(false); // Less accurate in burst
                        burstCount++;
                        nextFireTime = Time.time + fireRate * 0.7f; // Faster fire rate in burst
                    }
                    else if (Time.time > lastBurstTime + burstPause)
                    {
                        burstCount = 0;
                        lastBurstTime = Time.time;
                    }
                }
                break;
                
            case 2: // Suppressive fire - lots of bullets but less accurate
                if (Time.time > nextFireTime)
                {
                    Shoot(false); // Less accurate
                    nextFireTime = Time.time + fireRate * 0.5f; // Very fast fire rate
                    
                    // Occasionally need to reposition during suppressive fire
                    if (Random.value < 0.05f && !isRepositioning)
                    {
                        StartCoroutine(RepositionRoutine());
                    }
                }
                break;
                
            case 3: // Tactical shots with movement
                if (Time.time > nextFireTime)
                {
                    Shoot(true);
                    nextFireTime = Time.time + fireRate * 1.5f;
                    
                    // Move after shooting
                    if (Random.value < 0.3f && !isRepositioning)
                    {
                        StartCoroutine(RepositionRoutine());
                    }
                }
                break;
        }
    }
    
    void Shoot(bool preciseShot = false)
    {
        if (firePoint == null || bulletPrefab == null) return;
        
        // Play sound
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        
        // Show muzzle flash
        if (muzzleFlashEffect != null)
        {
            GameObject flash = Instantiate(muzzleFlashEffect, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.1f);
        }
        
        // Instantiate bullet
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        
        if (bulletRb != null)
        {
            Vector3 targetDirection;
            
            if (player != null)
            {
                // Apply accuracy/inaccuracy
                float accuracyFactor = preciseShot ? accuracy * 1.5f : accuracy;
                if (accuracyFactor > 1f) accuracyFactor = 1f; // Cap at 1
                
                // Add spread based on accuracy
                float spreadFactor = (1f - accuracyFactor) * 0.1f;
                Vector3 spread = new Vector3(
                    Random.Range(-spreadFactor, spreadFactor),
                    Random.Range(-spreadFactor, spreadFactor),
                    Random.Range(-spreadFactor, spreadFactor)
                );
                
                // Aim at player's position with some prediction and spread
                targetDirection = (player.position - firePoint.position).normalized + spread;
                bulletRb.velocity = targetDirection * bulletSpeed;
            }
            else
            {
                bulletRb.velocity = firePoint.forward * bulletSpeed;
            }
            
            // Set damage
            BulletController bulletController = bullet.GetComponent<BulletController>();
            if (bulletController != null)
            {
                bulletController.damage = bulletDamage;
                bulletController.shooter = gameObject;
            }
            else
            {
                // If no BulletController, add one
                bulletController = bullet.AddComponent<BulletController>();
                bulletController.damage = bulletDamage;
                bulletController.shooter = gameObject;
            }
            
            // Destroy bullet after some time if it doesn't hit anything
            Destroy(bullet, 5f);
        }
    }
    
    void ThrowGrenade()
    {
        if (grenadePrefab == null || player == null) return;
        
        // Calculate where to throw the grenade
        Vector3 targetPosition = player.position;
        Vector3 throwPosition = firePoint.position;
        
        // Instantiate grenade
        GameObject grenade = Instantiate(grenadePrefab, throwPosition, Quaternion.identity);
        Rigidbody grenadeRb = grenade.GetComponent<Rigidbody>();
        
        if (grenadeRb != null)
        {
            // Calculate throw arc
            Vector3 targetDirection = (targetPosition - throwPosition).normalized;
            float distance = Vector3.Distance(throwPosition, targetPosition);
            
            // Adjust throw angle based on distance
            Vector3 throwVelocity = targetDirection * grenadeThrowForce;
            throwVelocity.y += distance * 0.1f; // Add some arc
            
            grenadeRb.AddForce(throwVelocity, ForceMode.Impulse);
            
            // Add some rotation
            grenadeRb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            
            // Configure grenade behavior
            GrenadeController grenadeController = grenade.GetComponent<GrenadeController>();
            if (grenadeController != null)
            {
                grenadeController.explosionDamage = bulletDamage * 5f;
                grenadeController.explosionRadius = grenadeDetectionRadius;
                grenadeController.fuseTime = 3f;
                grenadeController.thrower = gameObject;
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        health -= damage;
        
        // Alert when being shot
        if (!playerInSight && player != null)
        {
            lastKnownPlayerPosition = player.position;
            
            // Chance to immediately spot player when hit
            if (Random.value < 0.8f)
            {
                playerInSight = true;
                currentState = AIState.Attacking;
            }
        }
        
        if (health <= 0)
        {
            Die();
        }
        else if (health < 30 && Random.value < 0.7f)
        {
            currentState = AIState.Retreating;
        }
        else if (NeedsCover() && Random.value < 0.7f)
        {
            currentState = AIState.TakingCover;
        }
    }
    
    private void Die()
    {
        // Play death animation
        // Drop weapon
        if (currentWeapon != null)
        {
            currentWeapon.transform.parent = null;
            Rigidbody weaponRb = currentWeapon.GetComponent<Rigidbody>();
            if (weaponRb == null)
            {
                weaponRb = currentWeapon.AddComponent<Rigidbody>();
            }
            weaponRb.isKinematic = false;
            weaponRb.AddForce(Random.insideUnitSphere * 3f, ForceMode.Impulse);
        }
        
        // Disable agent
        agent.enabled = false;
        
        // Enable ragdoll if any
        
        // Destroy the enemy after a delay
        Destroy(gameObject, 5f);
    }
    
    // Helper for visualizing in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw field of view
        Vector3 leftViewBoundary = Quaternion.Euler(0, -fieldOfView / 2, 0) * transform.forward;
        Vector3 rightViewBoundary = Quaternion.Euler(0, fieldOfView / 2, 0) * transform.forward;
        
        Gizmos.DrawLine(transform.position, transform.position + leftViewBoundary * sightRange);
        Gizmos.DrawLine(transform.position, transform.position + rightViewBoundary * sightRange);
    }
}