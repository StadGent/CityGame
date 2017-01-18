using UnityEngine;
using System.Collections;

namespace Core
{
	public class CoreMovement : MonoBehaviour
	{
		private Camera _Camera;
		// Use this for initialization
		void Start ()
		{
			this._Camera = GameObject.FindGameObjectWithTag ("MainCamera").GetComponent<Camera>();
		}
		
		// Update is called once per frame
		void Update ()
		{
			float v = Input.GetAxis ("Vertical") * 32 * Time.deltaTime;
			float h = Input.GetAxis ("Horizontal") * 32 * Time.deltaTime;

			this.transform.position = this.transform.position + this._Camera.transform.forward*v + this._Camera.transform.right*h;
		}
	}
}