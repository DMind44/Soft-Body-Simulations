using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoftbodyController : MonoBehaviour
{
    InputAction impulse;
    float value = 1;

    //TODO: Add position synchronization

    // Start is called before the first frame update
    void Start()
    {
        impulse = InputSystem.actions.FindAction("Impulse");
    }

    // Update is called once per frame
    void Update()
    {
        if (impulse.WasPressedThisFrame())
        {
            ImpulseSoftbody();
        }
    }

    public void ImpulseSoftbody()
    {
        Vector3 impulse = new Vector3(Random.value * 2 - 1, Random.value, Random.value * 2 - 1);
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.GetComponent<Rigidbody>().AddForce(impulse * value, ForceMode.Impulse);
        }
    }

    public void ImpulseSoftbody(float customValue)
    {
        Vector3 impulse = new Vector3(Random.value * 2 - 1, Random.value, Random.value * 2 - 1);
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.GetComponent<Rigidbody>().AddForce(impulse * customValue, ForceMode.Impulse);
        }
    }
}
