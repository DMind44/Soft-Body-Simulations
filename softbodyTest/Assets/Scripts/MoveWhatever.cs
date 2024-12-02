// Aaron Lanterman, June 19, 2014
using UnityEngine;
using System.Collections;

public class MoveWhatever : MonoBehaviour {

	Vector3 startingPosition;

	// Use this for initialization
	void Start() {
		startingPosition = transform.position;
	}
	
	// Update is called once per frame
	void Update() {
		transform.position = startingPosition + (5f*new Vector3 (Mathf.Sin(2f * Time.time), 0f, 0f));
	}
}
