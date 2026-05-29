using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightCycleController : MonoBehaviour
{
    // ----------------------
    // Movement settings
    // ----------------------
    [Header("Movement")]
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 150f;
    [SerializeField] private float minSpeed = 50f;
    [SerializeField] private float leanAngle = 15f;
    [SerializeField] private float leanSpeed = 8f;

    // ----------------------
    // References
    // ----------------------
    [Header("References")]
    [SerializeField] private Transform lightCycleModel;
    [SerializeField] private Transform trailBottomPoint;
    [SerializeField] private Transform trailTopPoint;
    [SerializeField] private GameObject trailMeshObject;
    [SerializeField] private Transform trailContainer;
    [SerializeField] private Material defaultTrailMaterial;
    [SerializeField] private HUDController hudController;


    // ----------------------
    // TRON trail settings
    // ----------------------
    [Header("Trail Settings")]
    [SerializeField] private float minDistance = 0.2f;
    [SerializeField] private float wallThickness = 0.1f;
    [SerializeField] private float trailLifetime = 6f;
    private List<GameObject> segmentColliders = new List<GameObject>();

    // ----------------------
    // Death settings
    // ----------------------
    [Header("Death Settings")]
    [SerializeField] private GameObject deathExplosionPrefab;   // Particles
    [SerializeField] private AudioClip deathSound;               // Sound

    // ----------------------
    // Respawn Settings
    // ----------------------
    [Header("Respawn Settings")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 1.0f;
    [SerializeField] private GameObject lightCyclePrefab; // prefab limpio de LightCycle
    [SerializeField] private Unity.Cinemachine.CinemachineCamera cinemachineCam;
    [SerializeField] private float respawnInvulnerableSeconds = 1.0f;
    [SerializeField] private int initialLives = 3;
    private int lives;
    private bool isInvulnerable = false;
    private float invulnerableEndTime = 0f;

    // ----------------------
    // Death settings
    // ----------------------
    [Header("Engine Audio Settings")]
    [SerializeField] private AudioSource engineAudioSource;
    [SerializeField] private float engineBasePitch = 1.0f;
    [SerializeField] private float engineMaxPitch = 2.2f;
    [SerializeField] private float enginePitchSmooth = 5f;
    [SerializeField] private float turningPitchInfluence = 0.15f;
    // Sound turn smoothing
    private float turnAmountForSound = 0f;

    //----------------------
    // Trail Audio Settings
    //----------------------
    [Header("Trail Audio Settings")]
    [SerializeField] private AudioSource trailAudioSource;
    [SerializeField] private float trailVolume = 0.5f;
    [SerializeField] private float trailFadeSpeed = 4f;

    private Rigidbody rb;
    private float horizontalInput;
    private float currentLean;
    private float currentMaxSpeed;
    private float currentSpeed;
    private bool isBraking;

    // ----------------------
    // TRAIL DATA
    // ----------------------
    private class Segment
    {
        public Vector3 bl, br, tl, tr;
        public float time;
    }

    private readonly List<Segment> segments = new List<Segment>();

    private Mesh trailMesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private Vector3 lastBottomWorld;
    private bool hasLastPoint = false;
    private bool gameStarted = false;

    public System.Action<int> onCycleDeath;

    // ----------------------
    // INIT
    // ----------------------
    void Start()
    {
        InitComponents();
    }
    public void InitComponents()
    {
        if (lives <= 0)
            lives = initialLives;

        // --------------------
        // Ensure trail anchor points exist (create if missing)
        // --------------------
        if (trailBottomPoint == null)
        {
            GameObject b = new GameObject("trailBottomPoint");
            trailBottomPoint = b.transform;
            trailBottomPoint.SetParent(transform);
            trailBottomPoint.localPosition = new Vector3(0f, 0.25f, -1f);
        }

        if (trailTopPoint == null)
        {
            GameObject t = new GameObject("trailTopPoint");
            trailTopPoint = t.transform;
            trailTopPoint.SetParent(transform);
            trailTopPoint.localPosition = new Vector3(0f, 1f, -1f);
        }

        // --------------------
        // Ensure trail mesh object exists (create if missing)
        // --------------------
        if (trailMeshObject == null)
        {
            GameObject trailObj = new GameObject("trailMeshObject");
            trailMeshObject = trailObj;
            trailMeshObject.layer = LayerMask.NameToLayer("TrailWall");

            // Keep trail in world space
            trailMeshObject.transform.SetParent(null);
            trailMeshObject.transform.position = transform.position;

            // Add required rendering/collision components if missing
            // MeshFilter
            MeshFilter mf = trailObj.GetComponent<MeshFilter>();
            if (mf == null) mf = trailObj.AddComponent<MeshFilter>();

            // MeshRenderer
            MeshRenderer mr = trailObj.GetComponent<MeshRenderer>();
            if (mr == null) mr = trailObj.AddComponent<MeshRenderer>();

            // Assign default material if available
            if (defaultTrailMaterial != null)
            {
                mr.material = defaultTrailMaterial;
            }
            else
            {
                Debug.LogWarning("[Trail] defaultTrailMaterial is NULL. Assign a material in the inspector.");
            }

            // MeshCollider
            MeshCollider mc = trailObj.GetComponent<MeshCollider>();
            if (mc == null) mc = trailObj.AddComponent<MeshCollider>();

            // Configure collider (will set sharedMesh later once mesh is created)
            mc.convex = false;
            mc.isTrigger = true;
        }
        else
        {
            // If object exists, ensure it has the components we need (safety)
            if (trailMeshObject.GetComponent<MeshFilter>() == null)
                trailMeshObject.AddComponent<MeshFilter>();

            if (trailMeshObject.GetComponent<MeshRenderer>() == null)
                trailMeshObject.AddComponent<MeshRenderer>();

            if (trailMeshObject.GetComponent<MeshCollider>() == null)
            {
                var mcAdd = trailMeshObject.AddComponent<MeshCollider>();
                mcAdd.convex = false;
                mcAdd.isTrigger = true;
            }

            // Ensure material is applied if possible
            var existingMR = trailMeshObject.GetComponent<MeshRenderer>();
            if (existingMR != null && defaultTrailMaterial != null)
                existingMR.material = defaultTrailMaterial;
        }

        // --------------------
        // Cinemachine follow/LookAt re-assignment (safe)
        // --------------------
        if (cinemachineCam != null)
        {
            try
            {
                cinemachineCam.Follow = transform;
                cinemachineCam.LookAt = transform;

                // Try to avoid visual snapping by telling Cinemachine about the warp
                cinemachineCam.OnTargetObjectWarped(transform, transform.position - cinemachineCam.transform.position);
            }
            catch
            {
                Debug.LogWarning("[InitComponents] Cinemachine reassign failed or not supported.");
            }
        }

        // --------------------
        // Rigidbody and movement defaults
        // --------------------
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogWarning("[LightCycle] Rigidbody not found on InitComponents!");

        currentMaxSpeed = maxSpeed;
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // --------------------
        // Create and assign the mesh & collider safely
        // --------------------
        meshFilter = trailMeshObject.GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = trailMeshObject.AddComponent<MeshFilter>();

        meshCollider = trailMeshObject.GetComponent<MeshCollider>();
        if (meshCollider == null) meshCollider = trailMeshObject.AddComponent<MeshCollider>();

        // Create the mesh used by filter and collider
        if (trailMesh == null)
            trailMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        else
            trailMesh.Clear();

        meshFilter.sharedMesh = trailMesh;

        // Assign shared mesh to collider (safe assignment)
        try
        {
            meshCollider.sharedMesh = trailMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[InitComponents] Could not assign sharedMesh to MeshCollider: " + ex.Message);
        }

        // --------------------
        // Reset internal collections/state for the trail logic
        // --------------------
        segments.Clear();
        // Reset other trail-related state variables you use elsewhere
        // e.g. lastSegmentPos = transform.position; (if you have such a field)
        // currentSegmentIndex = 0; etc.

        isInvulnerable = true;
        invulnerableEndTime = Time.time + respawnInvulnerableSeconds;
    }

    // ----------------------
    // UPDATE LOGIC
    // ----------------------
    void Update()
    {
        if (!gameStarted) return;

        HandleInputs();
        ApplyLean();
        UpdateEngineSound();
        UpdateTrailSound();

        UpdateTrailLogic();
        RebuildTrailMesh();
    }

    void FixedUpdate()
    {
        if (!gameStarted) return;

        ApplyMovement();
        ApplyTurning();
    }

    private void HandleInputs()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        isBraking = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
    }

    // ----------------------
    // START GAME
    // ----------------------
    public void BeginGame()
    {
        gameStarted = true;

        if (engineAudioSource != null)
            engineAudioSource.Play();

        if (trailAudioSource != null)
            trailAudioSource.Play();
    }

    private void ApplyMovement()
    {
        float speed = rb.linearVelocity.magnitude;

        if (isBraking)
        {
            // Reduce speed directly while keeping forward direction
            speed = Mathf.Lerp(speed, minSpeed, Time.fixedDeltaTime * 4f);

            rb.linearVelocity = transform.forward * speed;

            return;
        }

        Vector3 forward = transform.forward;

        if (rb.linearVelocity.magnitude < currentMaxSpeed)
            rb.AddForce(forward * acceleration, ForceMode.Acceleration);


        Vector3 newVel = Vector3.Lerp(rb.linearVelocity, forward * speed, Time.fixedDeltaTime * 5f);

        rb.linearVelocity = newVel;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= 0.85f;
        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    private void ApplyTurning()
    {
        float turnAmount = horizontalInput * turnSpeed * Time.fixedDeltaTime;
        turnAmountForSound = Mathf.Lerp(turnAmountForSound, Mathf.Abs(turnAmount), Time.deltaTime * 5f);

        Quaternion delta = Quaternion.Euler(0f, turnAmount, 0f);
        rb.MoveRotation(rb.rotation * delta);
    }

    private void ApplyLean()
    {
        float targetLean = -horizontalInput * leanAngle;
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        if (lightCycleModel != null)
        {
            Vector3 e = lightCycleModel.localEulerAngles;
            lightCycleModel.localEulerAngles = new Vector3(e.x, e.y, currentLean);
        }
    }

    // -----------------------------------------------------------------------
    //  TRAIL LOGIC
    // -----------------------------------------------------------------------
    private void UpdateTrailLogic()
    {
        if (!trailBottomPoint || !trailTopPoint)
            return;

        Vector3 bottomWorld = trailBottomPoint.position;
        Vector3 topWorld = trailTopPoint.position;

        if (!hasLastPoint)
        {
            lastBottomWorld = bottomWorld;
            hasLastPoint = true;
            return;
        }

        if (Vector3.Distance(bottomWorld, lastBottomWorld) < minDistance)
            return;

        lastBottomWorld = bottomWorld;

        Vector3 rightWorld = transform.right * (wallThickness * 0.5f);

        Quaternion leanRot = Quaternion.Euler(0f, 0f, currentLean);

        Vector3 localBottom = trailContainer.InverseTransformPoint(bottomWorld);
        Vector3 localTop = trailContainer.InverseTransformPoint(topWorld);

        Vector3 twistedTop =
            leanRot * (localTop - localBottom) + localBottom;

        Vector3 finalBottom = trailContainer.TransformPoint(localBottom);
        Vector3 finalTop = trailContainer.TransformPoint(twistedTop);

        Vector3 b = trailMeshObject.transform.InverseTransformPoint(finalBottom);
        Vector3 t = trailMeshObject.transform.InverseTransformPoint(finalTop);

        Vector3 rightLocal = trailMeshObject.transform.InverseTransformDirection(rightWorld);

        Segment seg = new Segment
        {
            bl = b - rightLocal,
            br = b + rightLocal,
            tl = t - rightLocal,
            tr = t + rightLocal,
            time = Time.time
        };

        segments.Add(seg);

        float expiry = Time.time - trailLifetime;
        segments.RemoveAll(s => s.time < expiry);
    }

    // ------------------------------------------------------
    // TRAIL MESH REBUILD
    // ------------------------------------------------------
    private void RebuildTrailMesh()
    {
        if (segments.Count < 2)
        {
            if (trailMesh != null) trailMesh.Clear();
            if (meshCollider != null) meshCollider.sharedMesh = null;
            // destroy segment colliders when no geometry
            foreach (var sc in segmentColliders)
                if (sc != null) Destroy(sc);
            segmentColliders.Clear();
            return;
        }

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int i = 0; i < segments.Count - 1; i++)
        {
            Segment a = segments[i];
            Segment b = segments[i + 1];

            int idx = verts.Count;

            verts.Add(a.bl);
            verts.Add(a.tl);
            verts.Add(a.br);
            verts.Add(a.tr);

            verts.Add(b.bl);
            verts.Add(b.tl);
            verts.Add(b.br);
            verts.Add(b.tr);

            // LEFT
            tris.Add(idx + 1); tris.Add(idx + 0); tris.Add(idx + 4);
            tris.Add(idx + 4); tris.Add(idx + 5); tris.Add(idx + 1);

            // RIGHT
            tris.Add(idx + 2); tris.Add(idx + 3); tris.Add(idx + 7);
            tris.Add(idx + 7); tris.Add(idx + 6); tris.Add(idx + 2);

            // FRONT
            tris.Add(idx + 0); tris.Add(idx + 2); tris.Add(idx + 6);
            tris.Add(idx + 6); tris.Add(idx + 4); tris.Add(idx + 0);

            // BACK
            tris.Add(idx + 1); tris.Add(idx + 5); tris.Add(idx + 7);
            tris.Add(idx + 7); tris.Add(idx + 3); tris.Add(idx + 1);

            // DOUBLE-SIDED (duplicate reversed triangles)
            int t0 = tris.Count - 24;
            for (int t = t0; t < t0 + 24; t += 3)
            {
                tris.Add(tris[t + 2]);
                tris.Add(tris[t + 1]);
                tris.Add(tris[t]);
            }
        }

        trailMesh.Clear();
        trailMesh.SetVertices(verts);
        trailMesh.SetTriangles(tris, 0);
        trailMesh.RecalculateNormals();
        trailMesh.RecalculateBounds();

        // assign mesh to filter
        meshFilter.sharedMesh = trailMesh;

        // Assign mesh to meshCollider for visuals/physics (non-trigger)
        // but ALSO create small convex trigger colliders per segment for detection.
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = trailMesh;
            // keep as concave for visuals – do not set isTrigger here (we use segment colliders)
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
        }

        // ----------------------------
        // Rebuild segment trigger colliders (CONVEX, isTrigger = true)
        // ----------------------------
        // destroy old colliders
        for (int i = 0; i < segmentColliders.Count; i++)
        {
            if (segmentColliders[i] != null)
                Destroy(segmentColliders[i]);
        }
        segmentColliders.Clear();

        // create a collider per "segment quad" to approximate wall volume
        for (int i = 0; i < segments.Count - 1; i++)
        {
            Segment a = segments[i];
            Segment b = segments[i + 1];

            // compute local centers for the two segment cross-sections
            Vector3 aCenter = (a.bl + a.br + a.tl + a.tr) * 0.25f;
            Vector3 bCenter = (b.bl + b.br + b.tl + b.tr) * 0.25f;

            Vector3 segCenter = (aCenter + bCenter) * 0.5f;

            // length along forward (between centers)
            float segLength = Vector3.Distance(aCenter, bCenter);
            if (segLength < 0.01f) segLength = 0.01f;

            // approximate height (top-bottom average)
            float aHeight = ((a.tl - a.bl).magnitude + (a.tr - a.br).magnitude) * 0.5f;
            float bHeight = ((b.tl - b.bl).magnitude + (b.tr - b.br).magnitude) * 0.5f;
            float height = Mathf.Max(0.05f, (aHeight + bHeight) * 0.5f);

            // approximate width (distance between left and right vertices)
            float aWidth = ((a.br - a.bl).magnitude + (a.tr - a.tl).magnitude) * 0.5f;
            float bWidth = ((b.br - b.bl).magnitude + (b.tr - b.tl).magnitude) * 0.5f;
            float width = Mathf.Max(0.02f, (aWidth + bWidth) * 0.5f);

            // create collider object
            GameObject colObj = new GameObject("segCollider_" + i);
            colObj.transform.SetParent(trailMeshObject.transform, false); // local coords
            colObj.layer = trailMeshObject.layer;

            // position in local coordinates (since segments are in trailMeshObject local)
            colObj.transform.localPosition = segCenter;
            // orientation: align forward from aCenter -> bCenter
            Vector3 forward = (bCenter - aCenter).normalized;
            if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;
            // compute rotation that maps local Z to 'forward' direction
            colObj.transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);

            BoxCollider bc = colObj.AddComponent<BoxCollider>();
            bc.isTrigger = true;

            // size: x = width, y = height, z = length
            bc.size = new Vector3(width, height, segLength + 0.01f); // small padding

            segmentColliders.Add(colObj);
        }
    }


    // ----------------------
    // DEATH LOGIC
    // ----------------------
    private void Kill()
    {
        int currentLives = lives - 1;
        lives = currentLives;

        if (hudController != null)
            hudController.SetLives(lives);

        // Explosion
        if (deathExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(deathExplosionPrefab, transform.position, transform.rotation);
            Destroy(explosion, 3f);
        }

        // Sound
        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound, transform.position, 1f);

        // Camera shake (Impulse)
        var impulse = GetComponent<Unity.Cinemachine.CinemachineImpulseSource>();
        if (impulse != null)
            impulse.GenerateImpulse();

        // Clear trail
        segments.Clear();

        // Destroy trail mesh BEFORE object
        if (trailMeshObject != null)
            Destroy(trailMeshObject);

        if (currentLives > 0)
        {
            // RespawnManager now handles everything internally
            RespawnManager.Instance.RespawnAfter(respawnDelay, currentLives);
        }

        if (onCycleDeath != null)
            onCycleDeath(currentLives);

        // Destroy the bike last
        gameObject.SetActive(false);
        Destroy(gameObject, 0.1f);
    }

    // ----------------------
    // SET REMAINING LIVES
    // ----------------------
    public void SetLivesRemaining(int value)
    {
        lives = value;

        if (hudController != null)
            hudController.SetLives(lives);
    }

    // ----------------------
    // COLLISION & DEATH
    // ----------------------
    private void OnTriggerEnter(Collider other)
    {
        // quick guard for respawn invulnerability
        if (isInvulnerable && Time.time < invulnerableEndTime) return;

        // if hit a TrailWall by tag OR layer OR by being one of our segment colliders
        bool hitTrail = false;

        if (other.CompareTag("TrailWall")) hitTrail = true;

        if (!hitTrail && trailMeshObject != null)
        {
            // check if collider is one of our generated segment colliders
            for (int i = 0; i < segmentColliders.Count; i++)
            {
                if (segmentColliders[i] == other.gameObject) { hitTrail = true; break; }
            }
        }

        // also check by layer match (if you set TrailWall layer)
        int trailLayer = LayerMask.NameToLayer("TrailWall");
        if (!hitTrail && trailLayer >= 0 && other.gameObject.layer == trailLayer) hitTrail = true;

        if (hitTrail)
        {
            Kill();
        }
    }


    // ----------------------
    // ENGINE SOUND UPDATE
    // ----------------------
    private void UpdateEngineSound()
    {
        if (engineAudioSource == null) return;

        // Current speed factor (0 → stopped, 1 → maxSpeed)
        float speedFactor = Mathf.InverseLerp(0f, currentMaxSpeed, rb.linearVelocity.magnitude);

        // Turning influence (small pitch variation during curves)
        float turnFactor = turnAmountForSound * turningPitchInfluence;

        // Desired pitch
        float targetPitch = engineBasePitch + (engineMaxPitch - engineBasePitch) * speedFactor + turnFactor;

        // Smooth pitch transition
        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, Time.deltaTime * enginePitchSmooth);

        // Update HUD speed display each frame (safely)
        if (hudController != null)
        {
            float speedNow = rb != null ? rb.linearVelocity.magnitude : 0f;
            hudController.SetSpeed(speedNow);
        }
    }

    // ----------------------
    // TRAIL SOUND UPDATE
    // ----------------------
    private void UpdateTrailSound()
    {
        if (trailAudioSource == null) return;

        // La moto siempre deja estela, así que el sonido debe ser continuo
        float targetVolume = trailVolume;

        // Si en el futuro quisieras apagar la estela, puedes poner condiciones aquí
        trailAudioSource.volume = Mathf.Lerp(
            trailAudioSource.volume,
            targetVolume,
            Time.deltaTime * trailFadeSpeed
        );
    }

    // ----------------------
    // HUD INITIALIZATION
    // ----------------------
    public void InitHUD(HUDController hud)
    {
        hudController = hud;
    }

    // ----------------------
    // GETTERS
    // ----------------------
    public int GetLives()
    {
        return lives;
    }
}