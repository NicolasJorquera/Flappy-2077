using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pickMusic : MonoBehaviour
{

    public AudioClip song1;
    public AudioClip song2;
    public AudioClip song3;
    public AudioClip song4;
    public AudioClip song5;
    public AudioClip song6;
    public AudioClip song7;
    public AudioClip song8;
    public AudioClip song9;
    public AudioClip song10;
    private AudioClip[] songsPlaylist;
    private int randIndex;
    AudioSource audioSource;


    // Start is called before the first frame update
    void Start()
    {
        songsPlaylist = new AudioClip[10];
        songsPlaylist[0] = song1;
        songsPlaylist[1] = song2;
        songsPlaylist[2] = song3;
        songsPlaylist[3] = song4;
        songsPlaylist[4] = song5;
        songsPlaylist[5] = song6;
        songsPlaylist[6] = song7;
        songsPlaylist[7] = song8;
        songsPlaylist[8] = song9;
        songsPlaylist[9] = song10;

        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;

    }

    // Update is called once per frame
    void Update()
    {
        if (!audioSource.isPlaying)
        {
            pickSong();
        }

    }

    void pickSong()
    {
        Random.seed = System.DateTime.Now.Millisecond;

        int auxRandIndex = Random.Range(0, songsPlaylist.Length);
        while (auxRandIndex == randIndex)
        {
            auxRandIndex = Random.Range(0, songsPlaylist.Length);
        }
        randIndex = auxRandIndex;
        AudioClip pickedSong = songsPlaylist[randIndex];

        audioSource.clip = pickedSong;
        //GetComponent<AudioSource>().volume = 1;
        audioSource.Play();


    }
    
}
