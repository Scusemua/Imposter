using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    [SerializeField] private List<Gun> AllGuns = new List<Gun>();

    private Dictionary<int, Gun> gunMap = new Dictionary<int, Gun>();

    public Gun GetGunByID(int id)
    {
        Gun gun = null;

        if (gunMap.ContainsKey(id))
            gun = gunMap[id];

        return gun;
    }

    public Gun GetGunByName(string name)
    {
        return AllGuns.Find(gun => gun.Name.Equals(name));
    }

    private void ConstructDatabase()
    {
        foreach (Gun gun in AllGuns)
        {
            gunMap[gun.Id] = gun;
        }
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
