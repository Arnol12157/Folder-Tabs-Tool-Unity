using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class FolderTabsToolEditorWindow : EditorWindow
{
    private List<string> folderPaths = new List<string>();
    private List<string> navigationHistory = new List<string>();
    private int selectedTab = 0;
    private Vector2 scrollPosition;
    private string currentPath;
    private bool showListView = false; // Toggle between list and icon view
    private string searchQuery = "";
    private string[] fileTypes = { "All", "Textures", "Scripts", "Models", "Scenes", "Prefabs" };
    private int selectedFileTypeIndex = 0;
    private List<string> favorites = new List<string>();
    private string selectedPath = null; // Store the selected path for preview

    [MenuItem("Tools/Custom Project Window")]
    public static void ShowWindow()
    {
        FolderTabsToolEditorWindow window = GetWindow<FolderTabsToolEditorWindow>("Custom Project");
        window.Show();
    }

    private void OnEnable()
    {
        LoadFolderPaths();
        LoadFavorites();
    }

    private void OnDisable()
    {
        SaveFolderPaths();
        SaveFavorites();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.7f));
        CaptureTabs();
        DisplayTabs();
        DisplaySearchAndFilter();

        if (folderPaths.Count > 0 && selectedTab >= 0 && selectedTab < folderPaths.Count)
        {
            if (currentPath == null)
                currentPath = folderPaths[selectedTab];

            GUILayout.Label("Content of: " + currentPath, EditorStyles.boldLabel);
            DisplayFolderContents(currentPath);
        }

        HandleDragAndDrop();
        DisplayFavorites();
        EditorGUILayout.EndVertical();

        // Preview Section
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.3f));
        DisplayPreview();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void CaptureTabs()
    {
        GUILayout.BeginHorizontal();
        for (int i = 0; i < folderPaths.Count; i++)
        {
            if (GUILayout.Button(Path.GetFileName(folderPaths[i]), GUILayout.Width(100)))
            {
                selectedTab = i;
                currentPath = folderPaths[i];
                navigationHistory.Clear();
            }

            if (GUILayout.Button("x", GUILayout.Width(20)))
            {
                folderPaths.RemoveAt(i);
                if (selectedTab >= folderPaths.Count)
                {
                    selectedTab = folderPaths.Count - 1;
                }
                return;
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DisplayTabs()
    {
        GUILayout.Space(10);

        // Toggle view mode
        showListView = GUILayout.Toggle(showListView, "List View");

        GUILayout.Space(10);
    }

    private void DisplaySearchAndFilter()
    {
        GUILayout.BeginHorizontal();
        searchQuery = GUILayout.TextField(searchQuery, "SearchTextField");
        selectedFileTypeIndex = EditorGUILayout.Popup(selectedFileTypeIndex, fileTypes, GUILayout.Width(100));
        GUILayout.EndHorizontal();
    }

    private void DisplayFolderContents(string folderPath)
    {
        if (navigationHistory.Count > 0)
        {
            if (GUILayout.Button("Back"))
            {
                currentPath = navigationHistory[navigationHistory.Count - 1];
                navigationHistory.RemoveAt(navigationHistory.Count - 1);
            }
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        string[] fileEntries = Directory.GetFiles(folderPath);
        string[] dirEntries = Directory.GetDirectories(folderPath);

        if (showListView)
        {
            foreach (string dir in dirEntries)
            {
                if (IsValidSearch(dir))
                {
                    DisplayAsset(dir, true);
                }
            }

            foreach (string file in fileEntries)
            {
                if (file.EndsWith(".meta") || !IsValidSearch(file)) continue; // Skip .meta files and invalid searches
                DisplayAsset(file, false);
            }
        }
        else
        {
            GUILayout.BeginHorizontal();
            foreach (string dir in dirEntries)
            {
                if (IsValidSearch(dir))
                {
                    DisplayIcon(dir, true);
                }
            }

            foreach (string file in fileEntries)
            {
                if (file.EndsWith(".meta") || !IsValidSearch(file)) continue; // Skip .meta files and invalid searches
                DisplayIcon(file, false);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private bool IsValidSearch(string path)
    {
        string fileName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(searchQuery) && !fileName.ToLower().Contains(searchQuery.ToLower()))
        {
            return false;
        }

        switch (selectedFileTypeIndex)
        {
            case 1: // Textures
                return fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg");
            case 2: // Scripts
                return fileName.EndsWith(".cs");
            case 3: // Models
                return fileName.EndsWith(".fbx") || fileName.EndsWith(".obj");
            case 4: // Scenes
                return fileName.EndsWith(".unity");
            case 5: // Prefabs
                return fileName.EndsWith(".prefab");
            default:
                return true;
        }
    }

    private void DisplayAsset(string path, bool isDirectory)
    {
        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (obj == null) return;

        GUILayout.BeginHorizontal();
        GUILayout.Label(AssetDatabase.GetCachedIcon(path), GUILayout.Width(20), GUILayout.Height(20));
        if (isDirectory)
        {
            if (GUILayout.Button(Path.GetFileName(path), EditorStyles.linkLabel))
            {
                navigationHistory.Add(currentPath);
                currentPath = path;
            }
        }
        else
        {
            if (GUILayout.Button(Path.GetFileName(path), EditorStyles.linkLabel))
            {
                selectedPath = path; // Set selected path for preview
            }
        }

        if (GUILayout.Button("★", GUILayout.Width(20)))
        {
            ToggleFavorite(path);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // Handle drag and drop
        Rect lastRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new[] { obj };
            DragAndDrop.StartDrag("Dragging " + obj.name);
            Event.current.Use();
        }
    }

    private void DisplayIcon(string path, bool isDirectory)
    {
        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (obj == null) return;

        GUILayout.BeginVertical(GUILayout.Width(100));
        GUILayout.Label(AssetDatabase.GetCachedIcon(path), GUILayout.Width(64), GUILayout.Height(64));
        if (isDirectory)
        {
            if (GUILayout.Button(Path.GetFileName(path), EditorStyles.linkLabel))
            {
                navigationHistory.Add(currentPath);
                currentPath = path;
            }
        }
        else
        {
            if (GUILayout.Button(Path.GetFileName(path), EditorStyles.linkLabel))
            {
                selectedPath = path; // Set selected path for preview
            }
        }

        if (GUILayout.Button("★", GUILayout.Width(20)))
        {
            ToggleFavorite(path);
        }

        GUILayout.EndVertical();

        // Handle drag and drop
        Rect lastRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new[] { obj };
            DragAndDrop.StartDrag("Dragging " + obj.name);
            Event.current.Use();
        }
    }

    private void HandleDragAndDrop()
    {
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag Folders Here");

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (string draggedObject in DragAndDrop.paths)
                    {
                        if (Directory.Exists(draggedObject))
                        {
                            folderPaths.Add(draggedObject);
                        }
                    }
                }
                Event.current.Use();
                break;
        }
    }

    private void DisplayFavorites()
    {
        GUILayout.Label("Favorites", EditorStyles.boldLabel);

        foreach (string favorite in favorites)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(AssetDatabase.GetCachedIcon(favorite), GUILayout.Width(20), GUILayout.Height(20));
            GUILayout.Label(Path.GetFileName(favorite));

            if (GUILayout.Button("Open", GUILayout.Width(50)))
            {
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(favorite));
            }

            if (GUILayout.Button("Show in Project", GUILayout.Width(120)))
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(favorite));
            }

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                favorites.Remove(favorite);
                break;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }

    private void DisplayPreview()
    {
        GUILayout.Label("Preview", EditorStyles.boldLabel);
        if (!string.IsNullOrEmpty(selectedPath))
        {
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectedPath);
            if (obj != null)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(obj);
                if (preview != null)
                {
                    GUILayout.Label(preview, GUILayout.Width(200), GUILayout.Height(200));
                }
                else
                {
                    GUILayout.Label("No preview available");
                }

                GUILayout.Label("Path: " + selectedPath);
                GUILayout.Label("Type: " + obj.GetType().Name);

                if (GUILayout.Button("Open", GUILayout.Width(200)))
                {
                    AssetDatabase.OpenAsset(obj);
                }

                if (GUILayout.Button("Show in Project", GUILayout.Width(200)))
                {
                    EditorGUIUtility.PingObject(obj);
                }
            }
            else
            {
                GUILayout.Label("No valid object selected");
            }
        }
        else
        {
            GUILayout.Label("Select an object to see a preview");
        }
    }

    private void ToggleFavorite(string path)
    {
        if (favorites.Contains(path))
        {
            favorites.Remove(path);
        }
        else
        {
            favorites.Add(path);
        }
    }

    private void SaveFolderPaths()
    {
        EditorPrefs.SetInt("CustomProjectWindow_Count", folderPaths.Count);
        for (int i = 0; i < folderPaths.Count; i++)
        {
            EditorPrefs.SetString("CustomProjectWindow_Path_" + i, folderPaths[i]);
        }
    }

    private void LoadFolderPaths()
    {
        folderPaths.Clear();
        int count = EditorPrefs.GetInt("CustomProjectWindow_Count", 0);
        for (int i = 0; i < count; i++)
        {
            string path = EditorPrefs.GetString("CustomProjectWindow_Path_" + i, "");
            if (!string.IsNullOrEmpty(path))
            {
                folderPaths.Add(path);
            }
        }
    }

    private void SaveFavorites()
    {
        EditorPrefs.SetInt("CustomProjectWindow_Favorite_Count", favorites.Count);
        for (int i = 0; i < favorites.Count; i++)
        {
            EditorPrefs.SetString("CustomProjectWindow_Favorite_" + i, favorites[i]);
        }
    }

    private void LoadFavorites()
    {
        favorites.Clear();
        int count = EditorPrefs.GetInt("CustomProjectWindow_Favorite_Count", 0);
        for (int i = 0; i < count; i++)
        {
            string path = EditorPrefs.GetString("CustomProjectWindow_Favorite_" + i, "");
            if (!string.IsNullOrEmpty(path))
            {
                favorites.Add(path);
            }
        }
    }
}
