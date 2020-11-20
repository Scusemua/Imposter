using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using TMPro;
using UnityEngine.UI;

public class GameOptionsUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject WeaponScrollviewContent;

    [Header("UI Prefabs")]
    public GameObject WeaponGroupHeaderPrefab;
    public GameObject WeaponEntryPrefab;

    [Header("Data")]
    public Gun[] WeaponPrefabs;

    public Dictionary<Gun, GameObject> WeaponEntries = new Dictionary<Gun, GameObject>();

    public Dictionary<GunType, List<Gun>> GunsOrganizedByType = new Dictionary<GunType, List<Gun>>();

    //public override void OnStartLocalPlayer()
    //{
    //    // Create a list for each type of gun.
    //    foreach (GunType gunType in Enum.GetValues(typeof(GunType)))
    //    {
    //        GunsOrganizedByType[gunType] = new List<Gun>();
    //    }

    //    // Store all of the guns in their respective lists.
    //    foreach (Gun gun in WeaponPrefabs)
    //    {
    //        GunsOrganizedByType[gun.GunType].Add(gun);
    //    }

    //    foreach (KeyValuePair<GunType, List<Gun>> kvp in GunsOrganizedByType)
    //    {
    //        List<Gun> guns = kvp.Value;

    //        GameObject header = Instantiate(WeaponGroupHeaderPrefab, WeaponScrollviewContent.transform);
    //        header.GetComponentInChildren<TextMeshProUGUI>().text = kvp.Key.ToString();

    //        foreach (Gun gun in guns)
    //        {
    //            GameObject entry = Instantiate(WeaponEntryPrefab, WeaponScrollviewContent.transform);
    //            header.GetComponentInChildren<TextMeshProUGUI>().text = gun.Name;

    //            WeaponEntries[gun] = entry;
    //        }
    //    }
    //}

    // Start is called before the first frame update
    void Start()
    {
        // Create a list for each type of gun.
        foreach (GunType gunType in Enum.GetValues(typeof(GunType)))
        {
            GunsOrganizedByType[gunType] = new List<Gun>();
        }

        // Store all of the guns in their respective lists.
        foreach (Gun gun in WeaponPrefabs)
        {
            Debug.Log("Initial processing on gun " + gun.Name);
            GunsOrganizedByType[gun.GunType].Add(gun);
        }

        foreach (KeyValuePair<GunType, List<Gun>> kvp in GunsOrganizedByType)
        {
            List<Gun> guns = kvp.Value;

            GameObject header = Instantiate(WeaponGroupHeaderPrefab, WeaponScrollviewContent.transform);
            header.GetComponentInChildren<TextMeshProUGUI>().text = kvp.Key.ToString();

            foreach (Gun gun in guns)
            {
                Debug.Log("Creating entry for gun " + gun.Name);
                GameObject entry = Instantiate(WeaponEntryPrefab, WeaponScrollviewContent.transform);
                entry.GetComponentInChildren<Text>().text = gun.Name;

                WeaponEntries[gun] = entry;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
