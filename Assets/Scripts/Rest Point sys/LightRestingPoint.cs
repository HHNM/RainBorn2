using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightRestingPoint : MonoBehaviour
{
    [SerializeField] private float refillRadius = 3f;
    [SerializeField] private GameObject visualEffect;
    [SerializeField] private AudioSource refillSound;
    [SerializeField] private float corruptionCleanseAmount = 20f; //  Amount to cleanse

    private bool playerInRange = false;
    private PlayerController playerController;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController controller = other.GetComponent<PlayerController>();
        if (controller != null)
        {
            playerController = controller;
            playerInRange = true;

            Debug.Log("Player entered Light Resting Point - Corruption system stopped!");

            // Stop corruption completely
            PlayerSoulCorruption corruption = controller.GetComponent<PlayerSoulCorruption>();
            if (corruption != null)
            {
                corruption.StopCorruption();
            }

            if (visualEffect != null)
                visualEffect.SetActive(true);

            if (refillSound != null)
                refillSound.Play();
        }
    }

    /*private void OnTriggerExit(Collider other)
    {
        ThirdPersonShooterController controller = other.GetComponent<ThirdPersonShooterController>();
        if (controller != null)
        {
            playerInRange = false;
            playerController = null;

            Debug.Log("Player exited Light Resting Point radius");

            if (visualEffect != null)
                visualEffect.SetActive(false);
        }
    }*/

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, refillRadius);
    }
}