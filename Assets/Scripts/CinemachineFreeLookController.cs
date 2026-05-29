using UnityEngine;
using Unity.Cinemachine;

public sealed class CinemachineFreeLookController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Optional look target (for offsets)")]
    [SerializeField] private Transform lookTargetOverride;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera cinemachineCam; // assign the Cinemachine Camera/Vcam here if you have it on scene

    [Header("Orbit Settings")]
    [SerializeField] private float mouseSensitivity = 4f;
    [SerializeField] private float verticalSensitivity = 3f;
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 65f;

    [Header("Follow Behind Settings")]
    [SerializeField] private float followBehindHorizontal = 0f;
    [SerializeField] private float followBehindVertical = 15f;
    [SerializeField] private float recenterDelay = 2f;
    [SerializeField] private float recenterSpeed = 2f;

    private CinemachineOrbitalFollow orbital;
    private CinemachineRotationComposer composer;

    private float lastInputTime = 0f;
    private bool isRecentering = false;

    private float defaultHorizontal;
    private float defaultVertical;

    private void Awake()
    {
        if (target == null)
        {
            Debug.LogError("[FreeLookController] Target is NULL!");
            enabled = false;
            return;
        }

        // Try to fetch components from same GameObject (or children)
        orbital = GetComponentInChildren<CinemachineOrbitalFollow>(true);
        composer = GetComponentInChildren<CinemachineRotationComposer>(true);

        if (orbital == null)
            Debug.LogError("[FreeLookController] CinemachineOrbitalFollow NOT found");

        if (composer == null)
            Debug.LogError("[FreeLookController] CinemachineRotationComposer NOT found");

        if (cinemachineCam == null)
        {
            // try to find a CinemachineCamera in the same object or children
            cinemachineCam = GetComponentInChildren<CinemachineCamera>();
            if (cinemachineCam == null)
                Debug.LogWarning("[FreeLookController] CinemachineCamera not assigned - set Follow/LookAt manually.");
        }
    }

    private void Start()
    {
        if (orbital == null || composer == null || target == null)
        {
            enabled = false;
            return;
        }

        // Assign Follow and LookAt to the active CinemachineCamera so composer/orbital work properly
        AssignCinemachineTargets();

        // Inicial values
        defaultHorizontal = followBehindHorizontal;
        defaultVertical = followBehindVertical;

        orbital.HorizontalAxis.Value = defaultHorizontal;
        orbital.VerticalAxis.Value = defaultVertical;
    }

    private void AssignCinemachineTargets()
    {
        if (cinemachineCam == null)
            return;

        // If you provided a lookTargetOverride, use it as LookAt, otherwise use target
        Transform lookAt = lookTargetOverride != null ? lookTargetOverride : target;

        try
        {
            cinemachineCam.Follow = target;
            cinemachineCam.LookAt = lookAt;
        }
        catch
        {
            // Some Cinemachine versions expose different types - ignore safely
            Debug.Log("[FreeLookController] AssignCinemachineTargets: Follow/LookAt assignment attempted.");
        }
    }

    private void Update()
    {
        if (orbital == null || composer == null || target == null)
            return;

        HandleMouseOrbit();
        HandleAutoRecentering();

        // We do NOT write to composer internals here.
        // The composer will use the virtual camera's LookAt target (assigned above).
    }

    private void HandleMouseOrbit()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        bool hasInput = Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f;

        if (hasInput)
        {
            isRecentering = false;
            lastInputTime = Time.time;

            // Orbit around the target horizontally
            orbital.HorizontalAxis.Value += mouseX * mouseSensitivity;

            // Vertical axis clamp
            float v = orbital.VerticalAxis.Value;
            v -= mouseY * verticalSensitivity;
            v = Mathf.Clamp(v, minVerticalAngle, maxVerticalAngle);

            orbital.VerticalAxis.Value = v;
        }
    }

    private void HandleAutoRecentering()
    {
        if (Time.time - lastInputTime < recenterDelay)
            return;

        if (!isRecentering)
            isRecentering = true;

        orbital.HorizontalAxis.Value =
            Mathf.Lerp(orbital.HorizontalAxis.Value, defaultHorizontal, recenterSpeed * Time.deltaTime);

        orbital.VerticalAxis.Value =
            Mathf.Lerp(orbital.VerticalAxis.Value, defaultVertical, recenterSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget, Transform optionalLookTarget = null)
    {
        target = newTarget;
        lookTargetOverride = optionalLookTarget;
        AssignCinemachineTargets();
    }
}
