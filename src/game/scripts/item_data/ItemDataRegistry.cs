using System.Linq;
using Godot;
using Godot.Collections;

public partial class ItemDataRegistry : Node
{
    #region Singleton
    private static ItemDataRegistry _instance;
    public static ItemDataRegistry Instance => _instance;

    public override void _Ready()
    {
        if (_instance != null)
        {
            QueueFree();
            return;
        }
        
        _instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }
    #endregion
    
    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    [Export] private Dictionary<string, ItemData> _register = [];
    public bool Has(string id) => _register.ContainsKey(id);

    
    private void Log(string what)
    {
        GD.PrintRich($"[color=white][ItemDataRegistry] {what}[/color]");
    }

    private void LogError(string what)
    {
        GD.PrintRich($"[color=red][ItemDataRegistry] {what}[/color]");
    }

    #region Get by id
    public ItemData Get(string id)
    {
        if (_register.TryGetValue(id, out var item))
            return item;
        
        LogError($"Item with ID '{id}' not found in registry");
        return null;
    }
    public ItemData get_by_id(string id) { return Get(id); }
    #endregion

    public ItemData Get(int idx)
    {
        if (_register.Count < idx)
        {
            LogError($"Item with index '{idx}' not found in registry");
            return null;
        }

        return _register.ElementAt(idx).Value;
    }
    public System.Collections.Generic.IEnumerable<ItemData> GetAll() => _register.Values;

    #region Register resource
    public void Register(ItemData item)
    {
        if (item.Id == "")
        {
            LogError("Cannot register item with empty Id");
            return;
        }

        
        if (_register.ContainsKey(item.Id))
        {
            LogError($"Item with ID '{item.Id}' already registered");
            return;
        }
        
        _register[item.Id] = item;
        Log($"Successfully registred item '{item.Id}'");
    }
    public void register_resource(ItemData item) { Register(item); }
    #endregion

    #region Register directiories
    public void Register(string[] dir_paths, bool recursive = true)
    {
        if (dir_paths.IsEmpty()) return;

        foreach (string dir_path in dir_paths)
        {
            using var dir = DirAccess.Open(dir_path);
            if (dir == null)
            {
                LogError($"Cannot open directory: {dir_path}");
                continue;
            }

            foreach (string file in dir.GetFiles())
            {
                if (file.EndsWith(".tres") || file.EndsWith(".res"))
                {
                    Register(dir_path.PathJoin(file));
                }
            }

            if (recursive)
            {
                foreach (string subDir in dir.GetDirectories())
                {
                    Register([dir_path.PathJoin(subDir)], true);
                }
            }
        }
    }
    public void register_directories(string[] dir_paths, bool recursive = true) { Register(dir_paths, recursive); }
    #endregion

    #region Register path
    public void Register(string item_path)
    {
        ItemData item = ResourceLoader.Load<ItemData>(item_path);
        if (item == null) return;
        Register(item);
    }
    public void register_path(string path) { Register(path); }
    #endregion
}