using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Canvas deathui;
    [SerializeField] private float dead;
    private List<Vector3> checkPoints = new List<Vector3>();
    private Vector3 lastPoint;


    private Rigidbody playerRb;
    private Canvas deathScreen;

    public static bool playerDied = false;

    private void Start()
    {
        playerRb = rb.GetComponent<Rigidbody>();
        deathScreen = deathui.GetComponent<Canvas>();
        lastPoint = player.transform.position;

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
        if (other.CompareTag("SpawnPoint"))
        {
            lastPoint = player.transform.position;

        } else if (other.CompareTag("FallTrigger"))
        {
            PauseGame();
        }
    }

    private void Respawn()
    {
        player.transform.position = lastPoint;
        playerRb.velocity = Vector3.zero;
        Physics.SyncTransforms();
        ResumeGame();
    }

    private void PlayerInputRespawn()
    {
        if (Input.GetKeyDown(KeyCode.R))
            Respawn();
        if (Input.GetKeyDown(KeyCode.Escape) && playerDied)
            SceneManager.LoadScene(0);
    }
}
 