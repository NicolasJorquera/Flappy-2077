using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingsSpawnScript : MonoBehaviour
{

    public float spawnRate = 2;
    private float timer = 0;
    public bool spawn;
    private int randIndex;

    public GameObject comb1;
    public GameObject comb2;
    public GameObject comb3;
    public GameObject comb4;
    public GameObject comb5;
    public GameObject comb6;
    public GameObject comb7;
    public GameObject comb8;
    public GameObject comb9;
    public GameObject comb10;


    private GameObject[] combinatons;


    // Start is called before the first frame update
    void Start()
    {
        combinatons = new GameObject[10];
        combinatons[0] = comb1;
        combinatons[1] = comb2;
        combinatons[2] = comb3;
        combinatons[3] = comb4;
        combinatons[4] = comb5;
        combinatons[5] = comb6;
        combinatons[6] = comb7;
        combinatons[7] = comb8;
        combinatons[8] = comb9;
        combinatons[9] = comb10;
        spawn = false;

        randIndex = 0;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (spawn == true)
        {
            if (timer < spawnRate)
            {
                timer = timer + Time.deltaTime;


            }
            else
            {
                spawnBuildings();
                timer = 0;
            }
        }
        


    }


    void spawnBuildings()
    {
        Random.seed = System.DateTime.Now.Millisecond;

        int auxRandIndex = Random.Range(0, combinatons.Length);
        while (auxRandIndex == randIndex)
        {
            auxRandIndex = Random.Range(0, combinatons.Length);
        }
        randIndex = auxRandIndex;

        GameObject pickedCombination = combinatons[randIndex];
        Instantiate(pickedCombination, transform.position, transform.rotation);
    }
}
