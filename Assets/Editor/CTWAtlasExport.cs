using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using DCM.ReorderableList;

public class CTWAtlasExport : EditorWindow {
	
	Object _atlasXmlFile = null;
	private string _atlasFile = string.Empty;
	private string _atlasFilePath = string.Empty;
	
    Object _exportFolder = null;
    private string _exportPath = string.Empty;
    private string realExportPath = string.Empty;
    
    private class AtlasImage {
    	public string path;
    	public int x;
    	public int y;
    	public int w;
    	public int h;
    }
    private Dictionary<string, List<AtlasImage> > atlasDictionary = new Dictionary<string, List<AtlasImage> >();

	[MenuItem("Window/CTWAtlasExport")]
	static void Init() { EditorWindow.GetWindow<CTWAtlasExport>(); }
	
	void OnEnable() {
	}
	
	void OnDestroy() {
	}
	
	void OnGUI() {
		GUILayout.Button(new GUIContent("Options"), ReorderableListGUI.defaultTitleStyle, GUILayout.Height(20f));
        GUILayout.Space(-6);

        EditorGUILayout.BeginVertical(ReorderableListGUI.defaultContainerStyle);
        EditorGUI.BeginChangeCheck();
        _atlasXmlFile = EditorGUILayout.ObjectField(new GUIContent("Atlas File"), _atlasXmlFile, typeof(Object), false);
        if (EditorGUI.EndChangeCheck()) {
            if (_atlasXmlFile.GetType() == typeof(TextAsset)) {
                _atlasFile = AssetDatabase.GetAssetPath(_atlasXmlFile);
                if (Path.GetExtension(_atlasFile) != ".xml") {
                	Debug.LogError("Invalid xml file: " + _atlasFile);
                } else {
                	_atlasFilePath = Path.GetDirectoryName(_atlasFile);
                	if (_atlasFilePath[_atlasFilePath.Length - 1] != '/')
                		_atlasFilePath = _atlasFilePath + "/";
                	//Debug.Log("_atlasFilePath: " + _atlasFilePath);
                }
            } else {
            	Debug.LogError("atlas list file type: " + _atlasXmlFile.GetType());
            }
        }
        
        EditorGUI.BeginChangeCheck();
        _exportFolder = EditorGUILayout.ObjectField(new GUIContent("Export Path"), _exportFolder, typeof(Object), false);
        if (EditorGUI.EndChangeCheck()) {
            string assetPath = AssetDatabase.GetAssetPath(_exportFolder);
            if (assetPath[assetPath.Length - 1] == '/')
            	assetPath = assetPath.Remove(assetPath.LastIndexOf("/", System.StringComparison.Ordinal));
            _exportFolder = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
            _exportPath = AssetDatabase.GetAssetPath(_exportFolder) + "/";
            realExportPath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("Assets", System.StringComparison.Ordinal)) + _exportPath;
        }
		EditorGUILayout.EndVertical();

        if (GUILayout.Button(new GUIContent("Export"))) {
            ExportTexture();
        }
	}
    
    private void ExportTexture() {
    	atlasDictionary.Clear();
    	
    	var xmlDoc = new XmlDocument();
    	var xmlFile = AssetDatabase.LoadAssetAtPath<TextAsset>(_atlasFile);
   		xmlDoc.LoadXml(xmlFile.text);
   		XmlNodeList atlasNodeList = xmlDoc.GetElementsByTagName("list");
   		foreach (XmlNode listNodeInfo in atlasNodeList) {
   			XmlNode atlasNameAttr = listNodeInfo.Attributes.GetNamedItem("name");
   			XmlNode atlasCountAttr = listNodeInfo.Attributes.GetNamedItem("count");
   			
   			if (atlasNameAttr == null) {
   				Debug.LogWarning("atlas name is null, can't parse it");
   				continue;
   			}
   			if (atlasCountAttr == null) {
   				Debug.LogWarning("atlas count is null, can't parse it: " + atlasNameAttr.Value);
   				continue;
   			}
   			string atlasFileName = _atlasFilePath + atlasNameAttr.Value + ".png";
   			//Debug.Log("atlasFileName = " + atlasFileName);
   			List<AtlasImage> atlasImageList = GetAtlasImageList(atlasFileName);
   			int atlasCount = System.Convert.ToInt32(atlasCountAttr.Value);
   			if (atlasCount == 1) {
   				XmlNode atlasPathAttr = listNodeInfo.Attributes.GetNamedItem("path");
   				if (atlasPathAttr == null) {
   					Debug.LogWarning("atlas count = 1, but path is null, can't parse it: " + atlasNameAttr.Value);
   				} else {
   					AtlasImage img = new AtlasImage();
   					img.path = atlasPathAttr.Value;
   					img.x = 0;
   					img.y = 0;
   					img.w = 0;
   					img.h = 0;
   					atlasImageList.Add(img);
   					//Debug.Log("whole atlas picture copy to " + img.path + ", atlas name: " + atlasFileName);
   				}
   				continue;
   			}
   			XmlNode atlasSizeAttr = listNodeInfo.Attributes.GetNamedItem("size");
   			if (atlasSizeAttr == null) {
   				Debug.LogWarning("atlas size is null, can't parse it: " + atlasNameAttr.Value);
   				continue;
   			}
   			string[] sizeSplits = atlasSizeAttr.Value.Split('|');
   			if (sizeSplits.Length != 2) {
   				Debug.LogWarning("atlas size format invalid: " + atlasSizeAttr.Value);
   			}
   			int atlasImageHeight = System.Convert.ToInt32(sizeSplits[1]);
   			
   			int nodeCount = 0;
   			XmlNodeList imgNodeList = listNodeInfo.ChildNodes;
   			foreach (XmlNode imageNodeInfo in imgNodeList) {
   				XmlNode imgPathAttr = imageNodeInfo.Attributes.GetNamedItem("path");
   				XmlNode imgXAttr = imageNodeInfo.Attributes.GetNamedItem("x");
   				XmlNode imgYAttr = imageNodeInfo.Attributes.GetNamedItem("y");
   				XmlNode imgWAttr = imageNodeInfo.Attributes.GetNamedItem("w");
   				XmlNode imgHAttr = imageNodeInfo.Attributes.GetNamedItem("h");
   				if (imgPathAttr == null || imgXAttr == null || imgYAttr == null || imgWAttr == null || imgHAttr == null) {
   					Debug.LogWarning("invalid image node: " + imgPathAttr.Value);
   					continue;
   				}
   				//Debug.Log("image path: " + imgPathAttr.Value);
   				AtlasImage img = new AtlasImage();
				img.path = imgPathAttr.Value;
				img.x = System.Convert.ToInt32(imgXAttr.Value);
				img.w = System.Convert.ToInt32(imgWAttr.Value);
				img.h = System.Convert.ToInt32(imgHAttr.Value);
				img.y = atlasImageHeight - System.Convert.ToInt32(imgYAttr.Value) - img.h;
				atlasImageList.Add(img);
   				nodeCount++;
   			}
   			
   			if (nodeCount != atlasCount) {
   				Debug.LogWarning("xml node count " + nodeCount + " don't equal to atlas count " + atlasCount);
   			}
   		}
   		
   		GenerateTextures();
    }
    
    private List<AtlasImage> GetAtlasImageList(string name) {
    	if (atlasDictionary.ContainsKey(name))
    		return atlasDictionary[name];
    	List<AtlasImage> list = new List<AtlasImage>();
    	atlasDictionary.Add(name, list);
    	return list;
    }
    
    private bool EnsureDirectory(string path) {
    	string[] directories = path.Split('\\');
    	if (directories.Length <= 1)
    		return true;
    	string lastDireictory = _exportPath;
    	for (int i = 0; i < directories.Length - 1; i++) {
    		string currentPath = lastDireictory + directories[i];
    		if (!AssetDatabase.IsValidFolder(currentPath)) {
    			string parentFolder = lastDireictory.Remove(lastDireictory.LastIndexOf("/", System.StringComparison.Ordinal));
    			AssetDatabase.CreateFolder(parentFolder, directories[i]);
    		}
    		lastDireictory = currentPath + "/";
    	}
    	return true;
    }
    
    private bool SaveTexture(Texture2D tex, string textureName) {
    	using (FileStream f = new FileStream(realExportPath + textureName, FileMode.Create)) {
            using (BinaryWriter b = new BinaryWriter(f)) {
                b.Write(((Texture2D)tex).EncodeToPNG());
            }
        }

        AssetPostprocessor proc = new AssetPostprocessor();
        AssetDatabase.Refresh();
        proc.assetPath = _exportPath + textureName;

        TextureImporter importer = proc.assetImporter as TextureImporter;
        if (importer == null) {
        	Debug.LogError("TextureImporter is null");
        	return false;
        }
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
        AssetDatabase.ImportAsset(_exportPath + textureName, ImportAssetOptions.ForceUpdate);
        AssetDatabase.LoadAssetAtPath(_exportPath + textureName, typeof(Texture2D));
        AssetDatabase.Refresh();
        return true;
    }
    
    private void GenerateTextures() {
    	foreach (var atlas in atlasDictionary) {
    		string atlasFile = atlas.Key;
    		var imgList = atlas.Value;
    		if (imgList.Count == 0) {
    			Debug.LogWarning("atlas image count is 0: " + atlasFile);
    			continue;
    		}
    		
    		Texture2D atlasTexture = AssetDatabase.LoadAssetAtPath(atlasFile, typeof(Texture2D)) as Texture2D;
    		if (atlasTexture == null) {
    			Debug.LogWarning("atlas file not exist: " + atlasFile);
    			continue;
    		}
    		
    		foreach (var image in imgList) {
    			if (!EnsureDirectory(image.path)) {
    				Debug.LogWarning("image path not exist: " + image.path);
    				continue;
    			}
    			
    			if (image.w == 0 && image.h == 0) {
    				//Debug.Log("whole atlas texture copy to " + image.path);
    				SaveTexture(atlasTexture, image.path.Replace('\\', '/'));
    				continue;
    			}
    			
    			Texture2D tex = new Texture2D(image.w, image.h, atlasTexture.format, false);
    			Color[] colors = atlasTexture.GetPixels(image.x, image.y, image.w, image.h);
    			tex.SetPixels(colors);
    			tex.Apply();
    			
    			SaveTexture(tex, image.path.Replace('\\', '/'));
    		}
    	}
    }
}
