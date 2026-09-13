#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class IncreaseResolution : EditorWindow {
	
	private string error = "";
	
	[MenuItem("Window/Increase Resolution")]
	public static void ShowWindow() {
		EditorWindow.GetWindow(typeof(IncreaseResolution));
	}
	
	public static void CloseWindow() {
		EditorWindow window = EditorWindow.GetWindow (typeof(IncreaseResolution));
		window.Close ();
	}

	Texture2D text;
	
	void OnGUI() {
		GUILayout.Label ("There might be a small delay while the texture is getting imported.");
		GUILayout.Space(20);
		text = EditorGUILayout.ObjectField("Image", text, typeof(Texture2D), false) as Texture2D;
		
		if (GUILayout.Button ("Process")) {
			error = "";
			CreateColorPicture();
			if (error == "") {
				CloseWindow ();
			}
		}
		
		GUILayout.Space(20);
		GUI.color = Color.red;
		EditorGUILayout.LabelField(error,EditorStyles.whiteLabel, GUILayout.Width(250f));
	}
	
	void CreateColorPicture() {
		if (!File.Exists (Application.dataPath + "/Textures/HighResTex("+text.name+").png")) {
			Texture2D tex = new Texture2D(512,512);
			for (int x = 0; x < tex.width; x++) {
				for (int y = 0; y < tex.width; y++) {
					Color normalColor = text.GetPixel (Mathf.RoundToInt ((x/tex.width)*text.width),Mathf.RoundToInt ((y/tex.height)*text.height));
					tex.SetPixel (x,y, normalColor);
				}
			}
			tex.Apply ();
			if (!Directory.Exists (Application.dataPath + "/Textures/")) {
				Directory.CreateDirectory (Application.dataPath + "/Textures/");
			}
			byte[] bytes = tex.EncodeToPNG ();
			DestroyImmediate (tex);
			File.WriteAllBytes (Application.dataPath + "/Textures/HighResTex("+tex.name+").png", bytes);
		} else {
			error = "The defined texture already exists.";
		}
	}
}
#endif