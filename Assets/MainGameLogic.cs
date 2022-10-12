using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem;

public enum GameState {PLAYING, CHARACTER_SELECTION};

public class ClearableInputAction
{
    public readonly InputAction action;
    private Action<InputAction.CallbackContext> callback;
    public ClearableInputAction(InputAction inputAction)
    {
        this.action = inputAction;
    }

    public void SetCallback(Action<InputAction.CallbackContext> callback)
    {
        if (this.callback != null)
            this.action.performed -= this.callback;
        if(callback!=null)
            this.action.performed += callback;
        this.callback = callback;
    }
}

public class MainGameLogic : MonoBehaviour
{
    public static readonly Vector2 BOGUS_VEC2 = new Vector2(float.NaN, float.NaN);
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
    public TMPro.TextMeshProUGUI screenCenterText;
    public UnityEngine.UI.Image selectedGroupIcon;
    public GameObject infoPanelBackground;
    public TMPro.TextMeshProUGUI infoPanelText;


    private GameState state = GameState.CHARACTER_SELECTION;

    public Transform cursor3D;

    //controls stuff  should be per human leader/faction
    public InputAction move;
    public InputAction look;
    public InputAction mousePos;
    public InputAction mouseMove;

    public InputAction primary;
    public ClearableInputAction primaryButton;
    public InputAction secondary;
    public ClearableInputAction secondaryButton;
    public InputAction cycleUnitGroupLeft;
    public ClearableInputAction cycleUnitGroupLeftButton;
    public InputAction cycleUnitGroupRight;
    public ClearableInputAction cycleUnitGroupRightButton;









    public Camera mainCamera;
    public Transform targetCursor;
    public Vector3 cameraOffset = new Vector3(0,50,-50);
    public float targetRange=10f;
    private Vector2 lookDirection=Vector2.up;
    private Leader playerLeader;
    private Faction playerFaction;
    private Transform target = null;
    private BulidingComponent captureObjective = null;

    private static MainGameLogic instance;


    private ISet<Unit> highlightedUnits = new HashSet<Unit>();



    private int currentLeaderIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        primaryButton = new ClearableInputAction(primary);
        secondaryButton = new ClearableInputAction(secondary);
        cycleUnitGroupLeftButton = new ClearableInputAction(cycleUnitGroupLeft);
        cycleUnitGroupRightButton = new ClearableInputAction(cycleUnitGroupRight);
        
            
        instance = this;
        unitPrefabsByType.Clear();
        foreach(Transform prefab in unitPrefabs)
        {
            unitPrefabsByType.Add(prefab.name, prefab);
        }
        CreateWorld();
        SetCharacterSelectionControls();
        

    }

    void ClearInputActions()
    {
        primaryButton.SetCallback(null);
        secondaryButton.SetCallback(null);
        cycleUnitGroupLeftButton.SetCallback(null);
        cycleUnitGroupRightButton.SetCallback(null);
    }

    void SetPlayControls()
    {
        if (playerLeader == null)
            return;
        ClearInputActions();
        mainCamera.transform.SetParent(playerLeader.transform);
        mainCamera.transform.localPosition = cameraOffset;
        mainCamera.transform.localRotation = Quaternion.LookRotation(-cameraOffset, Vector3.up);
        infoPanelBackground.SetActive(false);
        foreach (var b in bulidings)
        {
            if (b.faction.isNeutral)
                continue;
            if (b.faction == playerFaction)
                continue;
            captureObjective = b;
            break;
        }

        //This highlighting logic should be moved to a method, also it's wrong and should only highlight the selected units
        ISet<Unit> unitsToHighlight = new HashSet<Unit>();
        foreach(var group in playerLeader.GetUnitGroups())
        {
            foreach (var u in group.units)
                unitsToHighlight.Add(u);
        }
        ResetUnitHighlighting(unitsToHighlight);


        move.Enable();
        look.Enable();
        mousePos.Enable();
        mouseMove.Enable();
        secondary.Enable();
        primary.Enable();
        cycleUnitGroupLeft.Enable();
        cycleUnitGroupRight.Enable();
        cycleUnitGroupLeftButton.SetCallback(ctx => {
            if (playerLeader == null || playerLeader.GetUnitGroups().Count <= 1)
                return;
            CycleUnitGroupSelection(true);
            Debug.Log("Previous unit group");
        });
        cycleUnitGroupRightButton.SetCallback(ctx => {
            if (playerLeader == null || playerLeader.GetUnitGroups().Count == 0)
                return;
            CycleUnitGroupSelection(false);
            Debug.Log("Next unit group");
        });
        secondaryButton.SetCallback(ctx => {
            Debug.Log("secondary button");
            if (target == null || playerLeader == null) {
                CycleUnitGroupSelection(true);
                return;
            }
                

            BulidingComponent bc;
            Unit playerUnit;
            if (target.TryGetComponent(out bc) && playerLeader.TryGetComponent(out playerUnit))
            {
                if (bc.faction != playerUnit.faction || bc.garrisonCount == 0)
                {
                    CycleUnitGroupSelection(true);
                    return;
                }
                UnitGroup groupAddedTo=null;
                for (int i = 0; i < bc.garrisonCount; ++i)
                {
                    GameObject unit = SpawnUnit(unitPrefabsByType[bc.unitType], ToVec2(target.position), playerUnit.faction);
                    Unit uc;
                    if (unit.TryGetComponent(out uc))
                    {
                        groupAddedTo = playerLeader.AddUnit(uc);
                    }
                }
                if(playerLeader.GetUnitGroups().Count==1||playerLeader.GetUnitGroups()[playerLeader.selectedUserGroup]==groupAddedTo)
                    ResetUnitHighlighting(new HashSet<Unit>(groupAddedTo.units));
                bc.UpdateCount(0);
            }
            else
            {
                CycleUnitGroupSelection(true);
            }
        });

        primaryButton.SetCallback(ctx => {
            if (target == null || playerLeader == null)
                return;
            BulidingComponent bc;
            Unit playerUnit;
            if (target.TryGetComponent(out bc) && playerLeader.TryGetComponent(out playerUnit) && playerLeader.GetUnitGroups().Count > 0)
            {
                UnitGroup unitGroup = playerLeader.GetUnitGroups()[playerLeader.selectedUserGroup];
                int numToSend = unitGroup.rowWidth;
                List<Unit> units = unitGroup.Remove(numToSend);
                foreach (var u in units)
                {
                    u.action = UnitAction.CAPTURE;
                    u.target = target;
                    u.desiredPosition = ToVec2(target.position);
                }
                if (unitGroup.units.Count == 0)
                {
                    playerLeader.RemoveUnitGroup(unitGroup);
                    if (playerLeader.GetUnitGroups().Count > 0)
                        ResetUnitHighlighting(new HashSet<Unit>(playerLeader.GetUnitGroups()[playerLeader.selectedUserGroup].units));
                }
            }
        });
    }

    private void ResetUnitHighlighting(ISet<Unit> unitsToHighlight)
    {
        highlightedUnits.UnionWith(unitsToHighlight);
        foreach (var unit in highlightedUnits)
            if (!unitsToHighlight.Contains(unit))
                unit.selectionHighlight.SetActive(false);
            else
                unit.selectionHighlight.SetActive(true);
        highlightedUnits=unitsToHighlight;
    }

    private void CycleUnitGroupSelection(bool backwards)
    {
        if (playerLeader == null || playerLeader.GetUnitGroups().Count <= 1)
            return;

        

        int numGroups = playerLeader.GetUnitGroups().Count;
        if (backwards)
            playerLeader.selectedUserGroup--;
        else
            playerLeader.selectedUserGroup++;
        playerLeader.selectedUserGroup %= numGroups;
        if (playerLeader.selectedUserGroup < 0)
            playerLeader.selectedUserGroup += numGroups;

        UnitGroup selectedGroup = playerLeader.GetUnitGroups()[playerLeader.selectedUserGroup];
        selectedGroupIcon.sprite = selectedGroup.unitGroupImage;
        ResetUnitHighlighting(new HashSet<Unit>(selectedGroup.units));
    }

    public bool IsPlayable(Leader leader)
    {
        return true;
    }

    void SetCharacterSelectionControls()
    {
        timeSinceCharSelectStart = 0f;
        ClearInputActions();
        move.Enable();
        look.Enable();
        mousePos.Enable();
        mouseMove.Enable();
        secondary.Enable();
        primary.Enable();
        cycleUnitGroupLeft.Enable();
        cycleUnitGroupRight.Enable();
        playerLeader = null;
        cursor3D.position = leaders[currentLeaderIndex].transform.position;
        mainCamera.transform.SetParent(cursor3D);
        mainCamera.transform.localPosition = cameraOffset;
        mainCamera.transform.localRotation = Quaternion.LookRotation(-cameraOffset, Vector3.up);
        infoPanelBackground.SetActive(true);
        ClearInputActions();
        cycleUnitGroupLeftButton.SetCallback(ctx => {
            currentLeaderIndex--;
            if (currentLeaderIndex < leaders.Count)
                currentLeaderIndex += leaders.Count;
            currentLeaderIndex %= leaders.Count;
            int count = 0;
            while (!IsPlayable(leaders[currentLeaderIndex]) && count <= leaders.Count)
            {
                currentLeaderIndex--;
                if (currentLeaderIndex < 0)
                    currentLeaderIndex += leaders.Count;
                currentLeaderIndex %= leaders.Count;
            }
            screenCenterText.gameObject.SetActive(false);
            HighlightCurrentLeader();

        });

        cycleUnitGroupRightButton.SetCallback(ctx => {
            currentLeaderIndex++;
            currentLeaderIndex %= leaders.Count;
            if (currentLeaderIndex < 0)
                currentLeaderIndex += leaders.Count;
            int count = 0;
            while (!IsPlayable(leaders[currentLeaderIndex])&&count<=leaders.Count)
            {
                currentLeaderIndex++;
                currentLeaderIndex %= leaders.Count;
                if (currentLeaderIndex < leaders.Count)
                    currentLeaderIndex += leaders.Count;
            }
            screenCenterText.gameObject.SetActive(false);
            HighlightCurrentLeader();
        });

        primaryButton.SetCallback(ctx => {
            if (screenCenterText.gameObject.activeSelf)
            {
                screenCenterText.gameObject.SetActive(false);
                return;
            }


            if (IsPlayable(leaders[currentLeaderIndex])){ 
                playerLeader = leaders[currentLeaderIndex];
                Unit uc;
                if (playerLeader.TryGetComponent(out uc))
                    playerFaction = uc.faction;
                SetPlayControls();
                state = GameState.PLAYING;
            }
        });
    }

    void HighlightCurrentLeader()
    {
        if (leaders == null || leaders.Count <= currentLeaderIndex)
            return;
        Leader leaderTohighlight = leaders[currentLeaderIndex];
        infoPanelText.text = "A leader is been selected this is where the text description should go";
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        switch (state)
        {
            case GameState.CHARACTER_SELECTION:
                CharacterSelectionStateUpdate();
                break;
            case GameState.PLAYING:
                PlayStateUpdate();
                break;
            default:
                Debug.Log("not yet implemented");
                break;
        }
    }


    float timeSinceCharSelectStart = 0f;
    public void CharacterSelectionStateUpdate()
    {
        if (instance == null || currentLeaderIndex<0 || currentLeaderIndex>=leaders.Count)
            return;
        timeSinceCharSelectStart += Time.fixedDeltaTime;
        if(timeSinceCharSelectStart>2f)
            screenCenterText.gameObject.SetActive(false);
        Vector3 diff = leaders[currentLeaderIndex].transform.position - cursor3D.position;
        cursor3D.position = Vector3.Lerp(cursor3D.position, leaders[currentLeaderIndex].transform.position, 100f*Time.fixedDeltaTime/diff.magnitude);
        //pan the camera to look at the currently selected leader
        //show the leaders info in the information panel
        //clicking the button for the leader should select the leader and start the play state





        
    }

    public void PlayStateUpdate()
    {
        if (instance == null)
            return;
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
        if (gamepadLook.sqrMagnitude > .25f)
        {
            lookDirection = gamepadLook;
        }




        foreach (var leader in leaders)
        {
            Unit uc;
            if (!leader.TryGetComponent(out uc)||leader!=playerLeader)
                continue;
            leader.transform.position += 20f * new Vector3(moveVector.x, 0, moveVector.y) * Time.fixedDeltaTime;


            //ANDREW you will appreciate this brute force methodology here....
            List<Transform> nearbyThings = new List<Transform>();
            foreach (var unit in instance.units)
            {
                if (unit.faction != uc.faction && (leader.transform.position - unit.transform.position).sqrMagnitude < targetRange * targetRange)
                    nearbyThings.Add(unit.transform);

            }
            foreach (var b in instance.bulidings)
            {
                if ((leader.transform.position - b.transform.position).sqrMagnitude < targetRange * targetRange)
                    nearbyThings.Add(b.transform);
            }

            //Debug.Log(nearbyThings.Count);
            float maxAlignment = -1f;

            foreach (var t in nearbyThings)
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(t.position) - new Vector3(centerX, centerY, 0f);
                float alignment = Vector2.Dot(lookDirection, new Vector2(screenPos.x, screenPos.y).normalized);
                if (alignment > maxAlignment)
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

            //Following behavior
            Vector2 followPos = ToVec2(leader.transform.position);
            foreach (var group in leader.GetUnitGroups())
            {
                if (!group.following)
                    continue;
                Vector2 currentOffset = followPos - group.pos;
                if (currentOffset.magnitude > group.followDistance)
                {
                    group.pos = followPos - currentOffset.normalized * group.followDistance;
                    group.orientation = currentOffset.normalized;
                }

                followPos = group.pos;
                for (int i = 0; i < group.rowPositions.Count; ++i)
                {
                    Vector2 rowPos = group.rowPositions[i];
                    currentOffset = followPos - rowPos;
                    if (currentOffset.magnitude > group.followDistance)
                    {
                        rowPos = followPos - currentOffset.normalized * group.followDistance;
                        group.rowPositions[i] = rowPos;
                        group.rowOrientations[i] = currentOffset.normalized;
                    }
                    followPos = rowPos;
                }

                float startXOffset = -(group.rowWidth * group.spacing) / 2f;
                for (int i = 0; i < group.units.Count; ++i)
                {
                    int row = i / group.rowWidth;
                    int col = i % group.rowWidth;
                    Vector2 rowPos = group.rowPositions[row];
                    Vector2 rowOrientation = group.rowOrientations[row];
                    Vector2 unitPos = Vector2.Perpendicular(rowOrientation) * startXOffset + (Vector2.Perpendicular(rowOrientation) * group.spacing * col);
                    Unit u = group.units[i];
                    u.desiredPosition = unitPos + rowPos;
                    //u.transform.position =  ToVec3(unitPos+rowPos);
                }
            }


            //issue orders to target
        }
        foreach (var building in bulidings)
        {
            if (building.garrisonCount == building.maxGarrisonCount)
                continue;
            building.timeSinceSpawn += Time.deltaTime;
            if (building.timeSinceSpawn > 1f / building.spawnRate)
            {
                building.timeSinceSpawn = 0f;
                building.UpdateCount(building.garrisonCount + 1);
            }
        }
        ISet<Unit> unitsToDestroy = new HashSet<Unit>();
        foreach (var unit in units)
        {
            if (IsBogusVector(unit.desiredPosition))
                continue;
            float maxDist = unit.speed * Time.fixedDeltaTime;
            if (maxDist == 0)
                continue;
            Vector3 desiredPos = ToVec3(unit.desiredPosition);
            Vector3 desiredDelta = desiredPos - unit.transform.position;
            if (desiredDelta.magnitude < maxDist)
                unit.transform.position = desiredPos;
            else
            {
                float progress = Mathf.Clamp01(maxDist / desiredDelta.magnitude);
                unit.transform.position = Vector3.Lerp(unit.transform.position, desiredPos, progress);
            }


            if (unit.action == UnitAction.CAPTURE && unit.target != null)
            {
                BulidingComponent bc;
                if (unit.target.TryGetComponent(out bc))
                {
                    if (Vector2.Distance(ToVec2(unit.transform.position), ToVec2(unit.target.transform.position)) < 1E-4)
                    {
                        if (bc.faction == unit.faction)
                            bc.UpdateCount(bc.garrisonCount + 1);
                        else
                        {
                            if (bc.garrisonCount <= 0)
                            {
                                bc.OnSpawn(unit.faction);
                                bc.UpdateCount(1);
                                if (bc == captureObjective)
                                {
                                    screenCenterText.text = "Victory!";
                                    screenCenterText.gameObject.SetActive(true);
                                    //TODO actually have function for entering the state and do all the appropriate cleanup etc.
                                    //create the world in startup
                                    //cycle through the list of leaders and pan the camera between them
                                    //
                                    state = GameState.CHARACTER_SELECTION;
                                    SetCharacterSelectionControls();
                                }
                            }
                            else
                            {
                                bc.UpdateCount(bc.garrisonCount - 1);
                            }
                        }
                        unitsToDestroy.Add(unit);
                    }
                }
            }
        }
        foreach (var unit in unitsToDestroy)
        {
            units.Remove(unit);
            highlightedUnits.Remove(unit);
            GameObject.Destroy(unit.gameObject);//TODO should call method to make sure it is cleared from every locations (groups, nearby units/things etc.)
            
        }
    }

    public static bool IsBogusVector(Vector2 vec)
    {
        return float.IsNaN(vec.x) || float.IsNaN(vec.y);
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
                BulidingComponent bc = SpawnBuliding(buildingPrefab, faction.startPosition+UnityEngine.Random.insideUnitCircle * startOffset, faction, unitType);
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

        }
        
    }

    public static GameObject SpawnUnit(Transform prefab, Vector2 mapPosition, Faction faction)
    {
        GameObject newUnit = GameObject.Instantiate(prefab.gameObject, ToVec3(mapPosition), Quaternion.identity);
        Unit unitComponent;
        if (newUnit.TryGetComponent<Unit>(out unitComponent))
            unitComponent.OnSpawn(faction);
        instance.units.Add(unitComponent);
        return newUnit;
    }

    public static BulidingComponent SpawnBuliding(Transform prefab, Vector2 mapPosition, Faction faction, string unitType)
    {
        GameObject newUnit = GameObject.Instantiate(prefab.gameObject, new Vector3(mapPosition.x, 0, mapPosition.y), Quaternion.identity);
        BulidingComponent unitComponent;
        if (newUnit.TryGetComponent<BulidingComponent>(out unitComponent)) {
            unitComponent.OnSpawn(faction);
            unitComponent.unitType = unitType;
        }
        instance.bulidings.Add(unitComponent);
        return unitComponent;
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