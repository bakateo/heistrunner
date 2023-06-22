using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform spawnPoint;
    private Rigidbody playerRb;

    public static bool playerDied = false;

    private void Start()
    {
        playerRb = rb.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        PlayerInputRespawn();
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        playerDied = true;
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        playerDied = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PauseGame();
        }
    }

    private void Respawn()
    {
        player.transform.position = spawnPoint.gameObject.transform.position;
        playerRb.velocity = Vector3.zero;
        Physics.SyncTransforms();
        ResumeGame();
    }

    private void PlayerInputRespawn()
    {
        if (Input.GetKeyDown(KeyCode.R) && playerDied)
            Respawn();
    }
}
 