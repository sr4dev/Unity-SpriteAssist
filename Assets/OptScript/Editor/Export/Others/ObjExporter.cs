/*
 * https://wiki.unity3d.com/index.php/ExportOBJ
 */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Text;

public class ObjExporter : ScriptableObject
{
	[MenuItem("File/Export/tttt")]
	static void DoExportTTT()
	{
		var mat = new Material(Shader.Find("Unlit/Texture"));
		AssetDatabase.AddObjectToAsset(mat, "Assets/abs.fbx");
	}

	[MenuItem("File/Export/Wavefront OBJ")]
	static void DoExportWSubmeshes()
	{
		DoExport(true);
	}

	[MenuItem("File/Export/Wavefront OBJ (No Submeshes)")]
	static void DoExportWOSubmeshes()
	{
		DoExport(false);
	}

	public static void DoExport(bool makeSubmeshes)
	{
		if (Selection.gameObjects.Length == 0)
		{
			Debug.Log("Didn't Export Any Meshes; Nothing was selected!");
			return;
		}

		DoExport(Selection.gameObjects[0], makeSubmeshes);
	}

	public static void DoExport(GameObject gameObject, bool makeSubmeshes)
	{
		string meshName = gameObject.name;
		string fileName = EditorUtility.SaveFilePanel("Export .obj file", "", meshName, "obj");

		ObjExporterScript.Start();

		StringBuilder meshString = new StringBuilder();

		meshString.Append("#" + meshName + ".obj"
							+ "\n#-------"
							+ "\n\n");

		Transform t = gameObject.transform;

		Vector3 originalPosition = t.position;
		t.position = Vector3.zero;

		if (!makeSubmeshes)
		{
			meshString.Append("g ").Append(t.name).Append("\n");
		}
		meshString.Append(ProcessTransform(t, makeSubmeshes));

		WriteToFile(meshString.ToString(), fileName);

		t.position = originalPosition;

		ObjExporterScript.End();
		Debug.Log("Exported Mesh: " + fileName);
	}

	private static string ProcessTransform(Transform t, bool makeSubmeshes)
	{
		StringBuilder meshString = new StringBuilder();

		meshString.Append("#" + t.name
						+ "\n#-------"
						+ "\n");

		if (makeSubmeshes)
		{
			meshString.Append("g ").Append(t.name).Append("\n");
		}

		MeshFilter mf = t.GetComponent<MeshFilter>();
		MeshRenderer mr = t.GetComponent<MeshRenderer>();

		if (mf && mr)
		{
			meshString.Append(ObjExporterScript.MeshToString(mf, mr, t));
		}

		for (int i = 0; i < t.childCount; i++)
		{
			meshString.Append(ProcessTransform(t.GetChild(i), makeSubmeshes));
		}

		return meshString.ToString();
	}

	private static void WriteToFile(string s, string filename)
	{
		using (StreamWriter sw = new StreamWriter(filename))
		{
			sw.Write(s);
		}
	}
}