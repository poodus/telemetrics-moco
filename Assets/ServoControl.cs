using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using UnityEngine.UI;

public class ServoControl : MonoBehaviour {

	public Slider tiltSlider;
	public Slider panSlider;
	public Text panPositionText;
	public Text tiltPositionText;
	int lastTiltValue = 0;
	int lastPanValue = 0;
	SerialPort sp;

	// Use this for initialization
	void Start () {
		sp = new SerialPort("/dev/tty.usbserial-FT0EGQ74", 9600, Parity.None, 8);
		sp.Open();
		sp.Write ("L 1\r"); // Enable Camera
		Debug.Log("port open? " + sp.IsOpen);
	}

	// Update is called once per frame
	public void Update() {
		if ((int)tiltSlider.value != lastTiltValue) {
			sp.Write ("T " + (int)tiltSlider.value + "\r");
			Debug.Log ("tiltSliderValue: " + (int)tiltSlider.value);
			lastTiltValue = (int)tiltSlider.value;
		}
		if ((int)panSlider.value != lastPanValue) {
			sp.Write ("P " + (int)panSlider.value + "\r");
			Debug.Log ("panSliderValue: " + (int)panSlider.value);
			lastPanValue = (int)panSlider.value;
		}
	}

	public void StopMovement() {
		sp.WriteLine ("R\r");
	}

	void OnApplicationQuit() {
		Debug.Log ("Quit");
		sp.Close ();
	}
		
}
