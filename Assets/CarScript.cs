using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarScript : MonoBehaviour
{

    public Rigidbody2D myRigidbody;
    public float flapStrength;


    public SpriteRenderer spriteRenderer;
    public Sprite carDown;
    public Sprite carUp;
    public LogicScript Logic;
    public bool carIsAlive;
    public AudioClip boostSound;
    public GameObject interstitial;
    private Interstitial interstitialScript;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer.sprite = carDown;
        flapStrength = 5;
        Logic = GameObject.FindGameObjectWithTag("Logic").GetComponent<LogicScript>();
        carIsAlive = false;

        GetComponent<AudioSource>().playOnAwake = false;

        interstitialScript = interstitial.GetComponent<Interstitial>();

    }

    // Update is called once per frame
    void Update()
    {

        if ((Input.GetKeyDown(KeyCode.Space) == true || ( Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) && carIsAlive == true)
        {
            spriteRenderer.sprite = carUp;
            myRigidbody.velocity = Vector3.up * flapStrength;

            GetComponent<AudioSource>().clip = boostSound;
            GetComponent<AudioSource>().volume = 0.5f;
            GetComponent<AudioSource>().Play();
        }
        else if (myRigidbody.velocity.y < 0)
        {
            spriteRenderer.sprite = carDown;
        }
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (carIsAlive)
        {
            gameOver();
        }
        
    }

    private void OnBecameInvisible()
    {
        if (carIsAlive)
        {
            gameOver();
        }
    }

    private void gameOver()
    {
        Logic.gameOver();
        carIsAlive = false;
        interstitialScript.LoadInterstitialAd();
    }
}
