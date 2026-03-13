using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SubsystemTemplateGenerator : EditorWindow
{
    string _subsystemName = "MySubsystem";

    [MenuItem("Assets/Create/Subsystem Template", false, 80)]
    public static void OpenWindow()
    {
        GetWindow<SubsystemTemplateGenerator>("Create Subsystem Template");
    }

    void OnGUI()
    {
        GUILayout.Label("Subsystem Template Generator", EditorStyles.boldLabel);
        _subsystemName = EditorGUILayout.TextField("Subsystem Name", _subsystemName);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate"))
        {
            if (string.IsNullOrWhiteSpace(_subsystemName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a valid subsystem name.", "Ok");
                return;
            }

            GenerateSubsystem(_subsystemName.Trim());
            Close();
        }
    }

    static void GenerateSubsystem(string name)
    {
        // Resolve parent folder asset path from selection
        var selected = Selection.activeObject;
        var selectedPath = AssetDatabase.GetAssetPath(selected);
        string parentAssetPath;

        if (string.IsNullOrEmpty(selectedPath))
            parentAssetPath = "Assets";
        else if (AssetDatabase.IsValidFolder(selectedPath))
            parentAssetPath = selectedPath;
        else
            parentAssetPath = Path.GetDirectoryName(selectedPath).Replace('\\', '/');

        var folderAssetPath = parentAssetPath.TrimEnd('/') + "/" + name;
        CreateFolderRecursive(folderAssetPath);

        // Map asset path to full system path
        string fullFolderPath = Path.Combine(Application.dataPath, folderAssetPath.Substring("Assets/".Length));
        Directory.CreateDirectory(fullFolderPath);

        // Files to create (filename -> content)
        var templates = new (string file, string content)[]
        {
            ($"I{name}Model.cs", GetIModelTemplate(name)),
            ($"{name}Model.cs", GetModelTemplate(name)),
            ($"I{name}Controller.cs", GetIControllerTemplate(name)),
            ($"{name}Controller.cs", GetControllerTemplate(name)),
            ($"I{name}Subsystem.cs", GetISubsystemTemplate(name)),
            ($"{name}Subsystem.cs", GetSubsystemTemplate(name)),
            ($"{name}View.cs", GetViewTemplate(name)),
            ($"{name}InstallerSnippet.txt", GetInstallerSnippet(name))
        };

        foreach (var tpl in templates)
        {
            var assetFilePath = folderAssetPath + "/" + tpl.file;
            var fullFilePath = Path.Combine(fullFolderPath, tpl.file);
            File.WriteAllText(fullFilePath, tpl.content);
            AssetDatabase.ImportAsset(assetFilePath);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"Created subsystem '{name}' in {folderAssetPath}", "Ok");
    }

    static void CreateFolderRecursive(string assetFolderPath)
    {
        var parts = assetFolderPath.Split('/').ToList();
        string current = parts[0];
        for (int i = 1; i < parts.Count; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // --- Templates below ---

    static string GetIModelTemplate(string N) => $@"using UnityObservables;

public interface I{N}Model
{{
    // Add state observables here
    Observable<bool> IsActive {{ get; }}
    Observable<int> Counter {{ get; }}
}}";

    static string GetModelTemplate(string N) => $@"using System;
using UnityObservables;
using Zenject;

public class {N}Model : I{N}Model, IInitializable, IDisposable
{{
    private Observable<bool> _isActive;
    private Observable<int> _counter;

    public Observable<bool> IsActive => _isActive;
    public Observable<int> Counter => _counter;

    public void Initialize()
    {{
        _isActive = new Observable<bool>();
        _counter = new Observable<int>();
        _isActive.Value = false;
        _counter.Value = 0;
    }}

    public void Dispose()
    {{
        // Reset or dispose resources
        _isActive.Value = false;
        _counter.Value = 0;
    }}
}}";

    static string GetIControllerTemplate(string N) => $@"using System.Threading.Tasks;

public interface I{N}Controller
{{
    Task ToggleActive();
    void Increment();
    int GetCounter();
}}";

    static string GetControllerTemplate(string N) => $@"using System.Threading.Tasks;
using Zenject;

public class {N}Controller : I{N}Controller
{{
    readonly I{N}Model _model;

    [Inject]
    public {N}Controller(I{N}Model model)
    {{
        _model = model;
    }}

    public async Task ToggleActive()
    {{
        var next = !_model.IsActive.Value;
        await Task.Yield(); // placeholder for async work
        _model.IsActive.Value = next;
    }}

    public void Increment()
    {{
        _model.Counter.Value = _model.Counter.Value + 1;
    }}

    public int GetCounter() => _model.Counter.Value;
}}";

    static string GetISubsystemTemplate(string N) => $@"using UnityEngine.Events;
using System.Threading.Tasks;

public interface I{N}Subsystem : ISubsystem
{{
    event UnityAction<bool> IsActiveChanged;
    event UnityAction<int> CounterChanged;

    Task ToggleActive();
    void Increment();
    int GetCounter();
}}";

    static string GetSubsystemTemplate(string N) => $@"using System;
using UnityEngine.Events;
using Zenject;

public class {N}Subsystem : I{N}Subsystem, IInitializable, IDisposable
{{
    [Inject] readonly I{N}Controller _controller;
    [Inject] readonly I{N}Model _model;

    public event UnityAction<bool> IsActiveChanged;
    public event UnityAction<int> CounterChanged;

    public void Initialize()
    {{
        if (_model?.IsActive != null) _model.IsActive.OnChanged += HandleIsActiveChanged;
        if (_model?.Counter != null) _model.Counter.OnChanged += HandleCounterChanged;
    }}

    public void Dispose()
    {{
        if (_model?.IsActive != null) _model.IsActive.OnChanged -= HandleIsActiveChanged;
        if (_model?.Counter != null) _model.Counter.OnChanged -= HandleCounterChanged;
    }}

    // Forwarded controller methods
    public System.Threading.Tasks.Task ToggleActive() => _controller.ToggleActive();
    public void Increment() => _controller.Increment();
    public int GetCounter() => _controller.GetCounter();

    void HandleIsActiveChanged()
    {{
        try {{ IsActiveChanged?.Invoke(_model.IsActive.Value); }} catch {{ }}
    }}

    void HandleCounterChanged()
    {{
        try {{ CounterChanged?.Invoke(_model.Counter.Value); }} catch {{ }}
    }}
}}";

    static string GetViewTemplate(string N) => $@"using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class {N}View : MonoBehaviour
{{
    [SerializeField] Button _toggleButton;
    [SerializeField] Text _counterText;

    [Inject] I{N}Subsystem _subsystem;

    void OnEnable()
    {{
        _subsystem.IsActiveChanged += OnActiveChanged;
        _subsystem.CounterChanged += OnCounterChanged;
        if (_toggleButton != null) _toggleButton.onClick.AddListener(OnToggleClicked);
    }}

    void OnDisable()
    {{
        _subsystem.IsActiveChanged -= OnActiveChanged;
        _subsystem.CounterChanged -= OnCounterChanged;
        if (_toggleButton != null) _toggleButton.onClick.RemoveListener(OnToggleClicked);
    }}

    void OnActiveChanged(bool isActive)
    {{
        gameObject.SetActive(isActive);
    }}

    void OnCounterChanged(int value)
    {{
        if (_counterText != null) _counterText.text = value.ToString();
    }}

    async void OnToggleClicked()
    {{
        await _subsystem.ToggleActive();
        _subsystem.Increment();
    }}
}}";

    static string GetInstallerSnippet(string N) => $@"// Installer bindings for {N} subsystem
Container.BindInterfacesAndSelfTo<{N}Model>().AsSingle();
Container.BindInterfacesAndSelfTo<{N}Controller>().AsSingle();
Container.BindInterfacesAndSelfTo<{N}Subsystem>().AsSingle();";
}