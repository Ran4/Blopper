using UnityEngine;
using System.Collections;
using Windows.Kinect;



public class mainscript : MonoBehaviour {

	//initialize geometry
	GameObject cylinder;
	/*Color yellowBallColor = new Color ();
	Color pinkBallColor = new Color ();
	Color greenBallColor = new Color ();*/
    
    // Use this for initialization
	void Start () {


		//create some random geometry for testing

		//GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
		GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cube.transform.position = new Vector3(0, 0.5F, 0);
		GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		sphere.transform.position = new Vector3(0, 1.5F, 0);
		GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
		capsule.transform.position = new Vector3(2, 1, 0);
		cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		cylinder.transform.position = new Vector3(-2, 1, 0);	

	}


    
    // Update is called once per frame
    void Update () {
        System.Diagnostics.Debug.WriteLine("blah");

		//moves the cylinder 0.1 +x units per frame
		cylinder.transform.Translate (new Vector3 (0.1f, 0.0f, 0.0f));
	}
	



}
