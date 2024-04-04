using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LogicScript : MonoBehaviour
{
    public int playerScore;
    public Text scoreText;
    public GameObject startScreen;
    public GameObject gameOverScreen;
    public GameObject car;
    private Rigidbody2D rb2d;
    public GameObject buildingsSpawner;
    private BuildingsSpawnScript buildingsSpawnScript;
    public CarScript carScript;
    public AudioClip onePoint;
    public AudioClip tenPoints;
    public AudioClip hundredPoints;
    public AudioClip thousandPoints;
    public AudioClip gameOverSound;

    public GameObject backgroundImage;


    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 300;

        rb2d = car.GetComponent<Rigidbody2D>();
        rb2d.gravityScale = 0;

        buildingsSpawnScript = buildingsSpawner.GetComponent<BuildingsSpawnScript>();
        buildingsSpawnScript.spawn = false;

        GetComponent<AudioSource>().playOnAwake = false;



        // resolution fix


    }

    public void addScore(int scoreToAdd)
    {
        if (carScript.carIsAlive == true)
        {
            playerScore = playerScore + scoreToAdd;
            scoreText.text = playerScore.ToString();

            if (playerScore % 1000 == 0)
            {
                GetComponent<AudioSource>().clip = thousandPoints;
                GetComponent<AudioSource>().volume = 1;
                GetComponent<AudioSource>().Play();
            }
            else if (playerScore % 100 == 0)
            {
                GetComponent<AudioSource>().clip = hundredPoints;
                GetComponent<AudioSource>().volume = 1;
                GetComponent<AudioSource>().Play();
            }
            else if (playerScore % 10 == 0)
            {
                GetComponent<AudioSource>().clip = tenPoints;
                GetComponent<AudioSource>().volume = 0.8f;
                GetComponent<AudioSource>().Play();
            }
            else
            {
                GetComponent<AudioSource>().clip = onePoint;
                GetComponent<AudioSource>().volume = 0.8f;
                GetComponent<AudioSource>().Play();
            }
        }
        

    }

    public void restartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void startGame()
    {
        startScreen.SetActive(false);
        rb2d.gravityScale = 1;
        buildingsSpawnScript.spawn = true;
        carScript.carIsAlive = true;
    }

    public void gameOver() 
    {
        if (gameOverScreen.activeSelf == false)
        {
            GetComponent<AudioSource>().clip = gameOverSound;
            GetComponent<AudioSource>().volume = 0.2f;
            GetComponent<AudioSource>().Play();
            gameOverScreen.SetActive(true);
        }
        
    }

}
