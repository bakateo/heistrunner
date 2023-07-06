using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Canvas deathui;
    private Rigidbody playerRb;
    private Canvas deathScreen;

    public static bool playerDied = false;

    private void Start()
    {
        playerRb = rb.GetComponent<Rigidbody>();
        deathScreen = deathui.GetComponent<Canvas>();
    }

    private void Update()
    {
        PlayerInputRespawn();
    }

    private void PauseGame()
    {
        deathScreen.gameObject.SetActive(true);
        playerDied = true;
        Time.timeScale = 0f;
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        playerDied = false;
        deathScreen.gameObject.SetActive(false);
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
        if (Input.GetKeyDown(KeyCode.Escape) && playerDied)
            SceneManager.LoadScene(0);
    }
}
 