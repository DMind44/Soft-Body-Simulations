using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LauncherController : MonoBehaviour
{
    [SerializeField]
    int scenarioNumber = 0;

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
        SpawnerController spawner = GetComponentInChildren<SpawnerController>();
        switch (scenarioNumber)
        {
            case 0:
                {
                    float[] dimensions = { 2, 2, 2 };
                    GameObject cube = spawner.SpawnSoftbody(SpawnerController.SoftbodyType.CUBE, dimensions, 3);
                    cube.GetComponent<SoftbodyController>().ImpulseSoftbody(new Vector3(.75f, .5f, 0));
                    break;
                }
        }
    }


}
