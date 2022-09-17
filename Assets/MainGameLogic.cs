using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainGameLogic : MonoBehaviour
{
    private static readonly int NUM_BUILDINGS_PLAYER_FACTIONS = 1;
    private static readonly int NUM_BUILDINGS_NEUTRAL_FACTION = 50;

    public List<GameObject> leaders = new List<GameObject>();
    public ISet<GameObject> units = new HashSet<GameObject>();

    public Transform buildingPrefab;

    public Transform leaderPrefab;

    public List<Transform> unitPrefabs = new List<Transform>();
    private Dictionary<string, Transform> unitPrefabsByType = new Dictionary<string, Transform>();


    public List<Material> playerFactionMaterials = new List<Material>();
    public List<string> startUnitType = new List<string>();
    public List<int> startUnitCount = new List<int>();
    public List<Vector2> startPosition = new List<Vector2>();



    private static MainGameLogic instance;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        unitPrefabsByType.Clear();
        foreach(Transform prefab in unitPrefabs)
        {
            unitPrefabsByType.Add(prefab.name, prefab);
        }
        CreateWorld();
    }

    // Update is called once per frame
    void Update()
    {
        //50
        //50 red - 5 blue
        //45 red
        //0 red -
        //5 blue
    }


    private void CreateWorld()
    {
        List<Faction> factions = new List<Faction>();
        for(int i=0; i<playerFactionMaterials.Count; ++i)
        {
            factions.Add(new Faction(playerFactionMaterials[i], new string[] {startUnitType[i]}, new int[] {startUnitCount[i]}, startPosition[i]));
        }


        foreach(Faction faction in factions)
        {
            int numBuildings = faction.Equals(Color.white) ? NUM_BUILDINGS_NEUTRAL_FACTION : NUM_BUILDINGS_PLAYER_FACTIONS;
            float startOffset = faction.Equals(Color.white) ? 100f : 20f;

            for (int i = 0; i < numBuildings; ++i)
            {
                SpawnUnit(buildingPrefab, faction.startPosition+UnityEngine.Random.insideUnitCircle * startOffset, faction.unitMaterial);
            }
        }
        
    }

    public static void SpawnUnit(Transform prefab, Vector2 mapPosition, Material factionMaterial)
    {
        GameObject newUnit = GameObject.Instantiate(prefab.gameObject, new Vector3(mapPosition.x, 0, mapPosition.y), Quaternion.identity);
        Unit unitComponent;
        if (newUnit.TryGetComponent<Unit>(out unitComponent))
            unitComponent.OnSpawn(factionMaterial);
        instance.units.Add(newUnit);
    }

    public static void SpawnBuilding(Transform prefab, Vector2 mapPosition, Material factionMaterial)
    {
        GameObject newUnit = GameObject.Instantiate(prefab.gameObject, new Vector3(mapPosition.x, 0, mapPosition.y), Quaternion.identity);
        Unit unitComponent;
        if (newUnit.TryGetComponent<Unit>(out unitComponent))
            unitComponent.OnSpawn(factionMaterial);
        instance.units.Add(newUnit);
    }
}

public class Faction
{
    public readonly Material unitMaterial;
    public readonly string[] startUnitTypes;
    public readonly int[] startUnitCounts;
    public readonly Vector2 startPosition;
    public Faction(Material unitMaterial, string[] startUnitTypes, int[] startUnitCounts, Vector2 startPosition)
    {
        this.unitMaterial = unitMaterial;
        this.startUnitTypes = startUnitTypes;
        this.startUnitCounts = startUnitCounts;
        this.startPosition = startPosition;
    }
}