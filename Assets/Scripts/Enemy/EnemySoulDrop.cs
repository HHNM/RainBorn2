using UnityEngine;

public class EnemySoulDrop : MonoBehaviour
{
    public float soulValue = 10f;
    private bool hasDroppedSoul = false;

    public void Die()
    {
        if (hasDroppedSoul) return; // Prevent multiple drops

        PlayerSoulCorruption player = FindObjectOfType<PlayerSoulCorruption>();
        if (player != null)
        {
            player.CollectSoul(soulValue);
            Debug.Log($"[Enemy Death] Enemy died. Dropped {soulValue} souls.");
        }
        else
        {
            Debug.LogWarning("[Enemy Death] No player found to collect souls.");
        }

        hasDroppedSoul = true;
        Destroy(gameObject); // Simulate death
    }

    // For testing with a key press
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) // Press K to kill this enemy
        {
            Die();
        }
    }
}