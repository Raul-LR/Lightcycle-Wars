using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody targetRigidbody;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI livesText;

    [Header("HUD Settings")]
    [SerializeField] private float speedMultiplier = 0.001f; // meters per second → kilometers per second

    // Lives (can be updated externally)
    private int currentLives = 3;

    private void Update()
    {
        UpdateSpeedHUD();
        UpdateLivesHUD();
    }

    private void UpdateSpeedHUD()
    {
        if (targetRigidbody == null || speedText == null) return;

        float speed = targetRigidbody.linearVelocity.magnitude;
        float kmPerSecond = speed * speedMultiplier;

        speedText.text = kmPerSecond.ToString("0") + " km/s";
    }

    private void UpdateLivesHUD()
    {
        if (livesText == null) return;
        livesText.text = "Lives: " + currentLives;
    }

    // Call this from your game logic when the player loses a life
    public void SetLives(int newLives)
    {
        currentLives = newLives;
    }

    public void SetSpeed(float newSpeed)
    {
        float kmPerSecond = newSpeed * speedMultiplier;
        speedText.text = kmPerSecond.ToString("0") + " km/s";
    }
}
