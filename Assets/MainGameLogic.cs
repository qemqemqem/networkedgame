using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class MainGameLogic : MonoBehaviour
{
    private static readonly int NUM_BUILDINGS_PLAYER_FACTIONS = 1;
    private static readonly int NUM_BUILDINGS_NEUTRAL_FACTION = 50;

    public List<Leader> leaders = new List<Leader>();
    public ISet<Unit> units = new HashSet<Unit>();
    public ISet<BulidingComponent> bulidings = new HashSet<BulidingComponent>();

    //TODO actually maintain this and update things as they move around (should be easy), use it for distance queries
    //private Dictionary<Vector2Int, GameObject> courseCollision = new Dictionary<Vector2Int, GameObject>();

    public Transform buildingPrefab;

    public Transform leaderPrefab;

    public List<Transform> unitPrefabs = new List<Transform>();
    private Dictionary<string, Transform> unitPrefabsByType = new Dictionary<string, Transform>();


    public List<Faction> factions = new List<Faction>();

    //controls stuff  should be per human leader/faction
    public InputAction move;
    public InputAction look;
    public InputAction primary;
    public InputAction mousePos;
    public InputAction mouseMove;
    public InputAction cycleUnitGroupLeft;
    public InputAction cycleUnitGroupRight;
    public Camera mainCamera;
    public Transform targetCursor;
    public Vector3 cameraOffset = new Vector3(0,50,-50);
    public float targetRange=10f;
    private Vector2 lookDirection=Vector2.up;
    private Leader playerLeader;




    private static MainGameLogic instance;

    // Start is called before the first frame update
    void Start()
    {
        //controlls stuff shoudl be per human leader/faction
        move.Enable();
        look.Enable();
        primary.Enable();
        mousePos.Enable();
        mouseMove.Enable();

        cycleUnitGroupLeft.Enable();
        cycleUnitGroupRight.Enable();
            
        instance = this;
        unitPrefabsByType.Clear();
        foreach(Transform prefab in unitPrefabs)
        {
            unitPrefabsByType.Add(prefab.name, prefab);
        }
        CreateWorld();

        //hacky actions should be per human player 
        cycleUnitGroupLeft.performed += ctx=>playerLeader.GetUnitGroups().PrevStartIndex();
        cycleUnitGroupRight.performed += ctx => playerLeader.GetUnitGroups().NextStartIndex();
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        //controls stuff should be per human leader/faction
        Vector2 moveVector = move.ReadValue<Vector2>();

        float centerX = Screen.width / 2;
        float centerY = Screen.height / 2;
        if (mouseMove.ReadValue<Vector2>().sqrMagnitude > 1E-4)
        {
            Vector2 mpos = mousePos.ReadValue<Vector2>();
            lookDirection = new Vector2(mpos.x - centerX, mpos.y - centerY).normalized;
        }
        Vector2 gamepadLook = look.ReadValue<Vector2>();
        if (gamepadLook.sqrMagnitude > 1E-4)
        {
            lookDirection = gamepadLook;
        }
        
        
        

        foreach (var leader in leaders)
        {
            Unit uc;
            if (!leader.TryGetComponent(out uc))
                continue;
            if (!uc.faction.isPlayerFaction)
                continue;
            leader.transform.position += 20f * new Vector3(moveVector.x, 0, moveVector.y) * Time.fixedDeltaTime;


            //ANDREW you will appreciate this brute force methodology here....
            List<Transform> nearbyThings = new List<Transform>();
            foreach (var unit in instance.units)
            {
                if (unit.faction!=uc.faction&&(leader.transform.position - unit.transform.position).sqrMagnitude < targetRange * targetRange)
                    nearbyThings.Add(unit.transform);

            }
            foreach(var b in instance.bulidings)
            {
                if ((leader.transform.position - b.transform.position).sqrMagnitude < targetRange * targetRange)
                    nearbyThings.Add(b.transform);
            }

            //Debug.Log(nearbyThings.Count);
            float maxAlignment = -1f;
            Transform target=null;
            foreach (var t in nearbyThings) {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(t.position)-new Vector3(centerX, centerY, 0f);
                float alignment = Vector2.Dot(lookDirection, new Vector2(screenPos.x, screenPos.y).normalized);
                if(alignment > maxAlignment)
                {
                    //Debug.Log(screenPos);
                    maxAlignment = alignment;
                    target = t;
                }
            }

            if (target != null)
            {
                targetCursor.transform.position = new Vector3(target.position.x, 5f, target.position.z);
            }
            //TODO consider prototyping lockon mode



            //update units following leader
            UnitGroup prev = null;
            foreach (var group in leader.GetUnitGroups().list)
            {
                for(int i=0; i<group.units.Count; ++i)
                {

                }

                Vector2 followPos;
                if (prev == null)
                    followPos = ToVec2(leader.transform.position);
                else
                    followPos = prev.pos;
                Vector2 towardTarget = followPos - group.pos;
                if(towardTarget.magnitude > group.followDistance)
                {
                    group.pos = followPos - towardTarget;
                    group.orientation = towardTarget.normalized;
                }
                Debug.Log("Group unit count = "+group.units.Count);
                foreach(Unit u in group.units)
                {
                    //u.transform.position = ToVec3(group.pos);
                }
            }


            //issue orders to target
        }
        foreach (var building in bulidings)
        {
            building.timeSinceSpawn += Time.deltaTime;
            if (building.timeSinceSpawn > 1f/building.spawnRate)
            {
                building.timeSinceSpawn = 0f;
                building.UpdateCount(building.garrisonCount+1);
            }
        }
    }


    private void CreateWorld()
    {
        foreach(Faction faction in factions)
        {
            int numBuildings = faction.isNeutral ? NUM_BUILDINGS_NEUTRAL_FACTION : NUM_BUILDINGS_PLAYER_FACTIONS;
            float startOffset = faction.isNeutral ? 100f : 20f;
            string unitType = faction.startUnitTypes[0];

            for (int i = 0; i < numBuildings; ++i)
            {
                SpawnBuliding(buildingPrefab, faction.startPosition+UnityEngine.Random.insideUnitCircle * startOffset, faction, unitType);
            }
            if (faction.isNeutral)
                continue;

            GameObject leader = SpawnUnit(leaderPrefab, faction.startPosition, faction);
            Leader lc;
            if(leader.TryGetComponent(out lc))
            {
                leaders.Add(lc);
                if (faction.isPlayerFaction)
                    playerLeader = lc;
            }
            for(int i=0; i<faction.startUnitCounts[0]; ++i)
            {
                GameObject unit = SpawnUnit(unitPrefabsByType[unitType], faction.startPosition + UnityEngine.Random.insideUnitCircle * 20f, faction);
                Unit uc;
                if(unit.TryGetComponent(out uc))
                    lc.AddUnit(uc);
            }
            if (faction.isPlayerFaction)
            {
                mainCamera.transform.SetParent(leader.transform);
                mainCamera.transform.localPosition = cameraOffset;
                mainCamera.transform.localRotation = Quaternion.LookRotation(-cameraOffset, Vector3.up);
            }

        }
        
    }

    public static GameObject SpawnUnit(Transform prefab, Vector2 mapPosition, Faction faction)
    {
        GameObject newUnit = GameObject.Instantiate(prefab.gameObject, new Vector3(mapPosition.x, 0, mapPosition.y), Quaternion.identity);
        Unit unitComponent;
        if (newUnit.TryGetComponent<Unit>(out unitComponent))
            unitComponent.OnSpawn(faction);
        instance.units.Add(unitComponent);
        return newUnit;
    }

    public static void SpawnBuliding(Transform prefab, Vector2 mapPosition, Faction faction, string unitType)
    {
        GameObject newUnit = GameObject.Instantiate(prefab.gameObject, new Vector3(mapPosition.x, 0, mapPosition.y), Quaternion.identity);
        BulidingComponent unitComponent;
        if (newUnit.TryGetComponent<BulidingComponent>(out unitComponent)) {
            unitComponent.OnSpawn(faction);
            unitComponent.unitType = unitType;
        }
        instance.bulidings.Add(unitComponent);
    }

    public static Vector2 ToVec2(Vector3 vec3)
    {
        return new Vector2(vec3.x, vec3.z);
    }

    public static Vector3 ToVec3(Vector2 vec2)
    {
        return new Vector3(vec2.x, 0f, vec2.y);
    }
}

[Serializable]
public class Faction
{
    public Material unitMaterial;
    public string[] startUnitTypes;
    public int[] startUnitCounts;
    public Vector2 startPosition;
    public bool isPlayerFaction;
    public bool isNeutral = false;
    public Color factionColor;
    public Faction(Material unitMaterial, string[] startUnitTypes, int[] startUnitCounts, Vector2 startPosition, bool isPlayerFaction)
    {
        this.unitMaterial = unitMaterial;
        this.startUnitTypes = startUnitTypes;
        this.startUnitCounts = startUnitCounts;
        this.startPosition = startPosition;
        this.isPlayerFaction = isPlayerFaction;
    }
}