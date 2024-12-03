using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnchorController : MonoBehaviour
{
    [SerializeField]
    Rigidbody anchorTarget0;

    [SerializeField]
    Rigidbody anchorTarget1;

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
        float[] dimensions = { 6, 3, 0.2f };
        SpawnerController spawner = GetComponentInChildren<SpawnerController>();
        GameObject sheet = spawner.SpawnSoftbody(SpawnerController.SoftbodyType.SHEET, dimensions, 4);
        for (int i = 0; i < sheet.transform.childCount; i++)
        {
            GameObject child = sheet.transform.GetChild(i).gameObject;

            SpringJoint[] springJoints = child.GetComponents<SpringJoint>();

            for (int j = 0; j < springJoints.Length; j++) {
                //TEST: Will changing the damper prevent jitter?
                springJoints[j].damper = 100;
            }

            if (i % (Mathf.Pow(2, 4) + 1) == 0)
            {
                child.GetComponent<Rigidbody>().isKinematic = true;
            }
        }
    }
}
