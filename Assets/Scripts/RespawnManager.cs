using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance;

    [Header("General")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Respawn Settings")]
    [SerializeField] private GameObject lightCyclePrefab;
    [SerializeField] private Transform respawnPoint;        
    [SerializeField] private HUDController hudController;   
    [SerializeField] private CinemachineCamera cineCam;     

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Called externally
    public void RespawnAfter(float delay, int livesRemaining)
    {
        StartCoroutine(RespawnRoutine(delay, livesRemaining));
    }

    private IEnumerator RespawnRoutine(float delay, int livesRemaining)
    {
        yield return new WaitForSeconds(delay);

        if (lightCyclePrefab == null)
        {
            Debug.LogError("[RespawnManager] lightCyclePrefab is NULL. Cannot respawn.");
            yield break;
        }

        if (respawnPoint == null)
        {
            Debug.LogError("[RespawnManager] respawnPoint is NULL!");
            yield break;
        }

        Vector3 pos = respawnPoint.position;
        Quaternion rot = respawnPoint.rotation;

        // Instantiate new cycle
        GameObject newCycle = Instantiate(lightCyclePrefab, pos, rot);

        LightCycleController controller = newCycle.GetComponent<LightCycleController>();
        GameController gc = Object.FindFirstObjectByType<GameController>();
        if (gc != null)
        {
            controller.onCycleDeath += gc.OnCycleDeath;
        }
        else
        {
            Debug.LogError("[RespawnManager] No se encontró GameController para suscribir onCycleDeath.");
        }

        if (controller != null)
        {
            controller.SetLivesRemaining(livesRemaining);
            controller.InitHUD(hudController);            
            controller.InitComponents();
            controller.BeginGame();
        }
        else
        {
            Debug.LogError("[RespawnManager] ERROR: El prefab instanciado NO contiene LightCycleController.");
        }

        if (newCycle == null)
        {
            Debug.LogError("[RespawnManager] Instantiation FAILED.");
            yield break;
        }

        // Update camera target
        if (cineCam != null)
        {
            cineCam.Follow = newCycle.transform;
            cineCam.LookAt = newCycle.transform;

            // Fuerza interpolación suave y evita saltos duros
            cineCam.OnTargetObjectWarped(
                newCycle.transform,
                newCycle.transform.position - cineCam.transform.position
            );

            // Reset del estado interno para evitar vibraciones
            var brain = Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            if (brain != null)
                brain.ManualUpdate();
        }

        // Update HUD values (especially lives)
        if (hudController != null && controller != null)
            hudController.SetLives(controller.GetLives());
    }
}
