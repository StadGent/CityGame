using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LongLat : MonoBehaviour {

	public float _Longitude;
	public float _Latitude;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnValidate ()
	{
		float radiusEarth = 1000f;
		float x = (_Longitude * Mathf.Cos (_Latitude / 2)) * radiusEarth;
		float z = _Latitude  * radiusEarth;

		transform.position = new Vector3 (x, transform.position.y, z);
	}
}
