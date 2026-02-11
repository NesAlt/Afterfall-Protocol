using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerDeathController : MonoBehaviour
{
    public Animator animator; 
    public float deathDelay = 2f;
    public GameObject gameOverPanel;
    public string gameOverSceneName = "GameOver";

    private bool isDead = false;

    public void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
        Time.timeScale = 0.2f;
        yield return new WaitForSecondsRealtime(0.3f);

        Time.timeScale = 0f;

        animator.SetTrigger("isDead");

        yield return new WaitForSecondsRealtime(deathDelay);

        gameOverPanel.SetActive(true);

        // SceneManager.LoadScene(gameOverSceneName);
    }
}
