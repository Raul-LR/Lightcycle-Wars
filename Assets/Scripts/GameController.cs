using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button restartButton;

    [Header("Gameplay References")]
    [SerializeField] private HUDController hudController;
    [SerializeField] private Unity.Cinemachine.CinemachineCamera cinemachineCam;
    [SerializeField] private GameObject lightCyclePrefab;
    [SerializeField] private Transform spawnPoint;

    private GameObject currentCycle;
    private bool gameIsRunning = false;

    private void Awake()
    {
        if (playButton != null)
            playButton.onClick.AddListener(StartGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(ReturnToMainMenu);

        ShowMainMenu();
    }

    // --------------------
    // MAIN MENU LOGIC
    // --------------------
    private void ShowMainMenu()
    {
        gameIsRunning = false;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (hudController != null)
            hudController.gameObject.SetActive(false);

        UnlockCursor();
    }

    // --------------------
    // START GAME LOGIC
    // --------------------
    private void StartGame()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        gameOverPanel.SetActive(false);
        
        // Clean any leftover LightCycle (safety)
        var oldCycles = Object.FindObjectsByType<LightCycleController>(FindObjectsSortMode.None);
        foreach (var c in oldCycles)
            Destroy(c.gameObject);

        // Activate HUD
        hudController.gameObject.SetActive(true);

        // Spawn the FIRST real playable cycle
        SpawnNewCycle();

        currentCycle.GetComponent<LightCycleController>().BeginGame();

        LockCursor();
    }

    private void SpawnNewCycle()
    {
        // Clear previous cycle (if exists)
        if (currentCycle != null)
            Destroy(currentCycle);

        currentCycle = Instantiate(lightCyclePrefab, spawnPoint.position, spawnPoint.rotation);

        var cycleController = currentCycle.GetComponent<LightCycleController>();
        if (cycleController != null)
        {
            cycleController.InitHUD(hudController);

            // Attach callback when cycle loses all lives
            cycleController.onCycleDeath += OnCycleDeath;
        }

        if (cinemachineCam != null)
        {
            cinemachineCam.Follow = currentCycle.transform;
            cinemachineCam.LookAt = currentCycle.transform;
        }
    }

    // --------------------
    // GAME OVER LOGIC
    // --------------------
    public void OnCycleDeath(int remainingLives)
    {
        Debug.Log("[GameController] Cycle has died. Remaining Lives: " + remainingLives);
        if(remainingLives == 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        gameIsRunning = false;

        if (hudController != null)
            hudController.gameObject.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        UnlockCursor();
    }

    // --------------------
    // RETURN TO MAIN MENU
    // --------------------
    private void ReturnToMainMenu()
    {
        if (currentCycle != null)
            Destroy(currentCycle);

        ShowMainMenu();
    }

    // --------------------
    // CURSOR CONTROL
    // --------------------
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
