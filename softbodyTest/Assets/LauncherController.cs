using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LauncherController : MonoBehaviour
{
    [SerializeField]
    int scenarioNumber = 0;

    List<GameObject> gameObjects = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ExecuteScenario());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator ExecuteScenario()
    {
        yield return new WaitForSeconds(1);
        while (true)
        {
            SpawnerController spawner = GetComponentInChildren<SpawnerController>();
            switch (scenarioNumber)
            {
                case 0:
                    {
                        float[] dimensions = { 1, 1, 1 };
                        GameObject cube = spawner.SpawnSoftbody(SpawnerController.SoftbodyType.CUBE, dimensions, 2);
                        cube.GetComponent<SoftbodyController>().ImpulseSoftbody(new Vector3(.75f, .5f, 0));
                        gameObjects.Add(cube);
                        break;
                    }
                case 1:
                    {
                        float[] dimensions = { 1, 1, 1 };
                        GameObject cube = spawner.SpawnSoftbody(SpawnerController.SoftbodyType.CUBE, dimensions, 2);
                        cube.GetComponent<SoftbodyController>().ImpulseSoftbody(new Vector3(-.75f, .5f, 0));
                        gameObjects.Add(cube);
                        break;
                    }
            }
            yield return new WaitForSeconds(7.5f);
            while (gameObjects.Count > 0)
            {
                GameObject gameObject = gameObjects[0];
                gameObjects.Remove(gameObject);
                GameObject.Destroy(gameObject);
            }
        }
    }


}