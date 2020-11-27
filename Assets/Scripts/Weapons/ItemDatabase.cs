using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    /// <summary>
    /// The unique item ID of the body scanner item.
    /// </summary>
    public static int BodyScannerItemId = 1000;

    /// <summary>
    /// Item ID's start at 1000.
    /// </summary>
    public static int FirstItemId = 1000;

    /// <summary>
    /// List of all guns in the game.
    /// </summary>
    [SerializeField] private List<Gun> AllGuns = new List<Gun>();

    /// <summary>
    /// This list contains ammo box variants. The only difference between each prefab in this list is cosmetic.
    /// </summary>
    [SerializeField] private List<AmmoBox> AmmoBoxes = new List<AmmoBox>();

    /// <summary>
    /// This list contains medkit variants. The only difference between each prefab in this list is cosmetic.
    /// </summary>
    [SerializeField] private List<AmmoBox> Medkits = new List<AmmoBox>();

    /// <summary>
    /// List of all the items in the game. This includes guns.
    /// </summary>
    [SerializeField] private List<UsableItem> AllItems = new List<UsableItem>();

    private Dictionary<int, UsableItem> itemMap = new Dictionary<int, UsableItem>();

    private Dictionary<int, Gun> gunMap = new Dictionary<int, Gun>();

    /// <summary>
    /// Return the gun with the given ID.
    /// </summary>
    public Gun GetGunByID(int id)
    {
        Gun gun = null;

        if (gunMap.ContainsKey(id))
            gun = gunMap[id];

        return gun;
    }

    /// <summary>
    /// Return the item with the given ID.
    /// </summary>
    public UsableItem GetItemById(int id)
    {
        UsableItem item = null;

        if (itemMap.ContainsKey(id))
            item = itemMap[id];

        return item;
    }

    /// <summary>
    /// The largest weapon ID.
    /// </summary>
    public int MaxWeaponId
    {
        get
        {
            return AllGuns.Count - 1;
        }
    }

    public int NumMedkitVariants { get => Medkits.Count; }
    public int NumAmmoBoxVariants { get => AmmoBoxes.Count; }

    public AmmoBox GetAmmoBoxByIndex(int index)
    {
        return AmmoBoxes[index];
    }

    public AmmoBox GetMedkitByIndex(int index)
    {
        return Medkits[index];
    }

    public Gun GetGunByName(string name)
    {
        return AllGuns.Find(gun => gun.Name.Equals(name));
    }

    private void ConstructDatabase()
    {
        foreach (Gun gun in AllGuns)
        gunMap[gun.Id] = gun;

        foreach (UsableItem item in AllItems)
            itemMap[item.ItemId] = item;
    }

    void Awake()
    {
        ConstructDatabase();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
