using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitAction {ATTACK, FOLLOW, CAPTURE, IDLE};

public class Unit : MonoBehaviour
{
    public Faction faction;
    public Renderer unitRenderer;
    public string unitType;

    public float speed = .001f;
    public Vector2 desiredPosition = MainGameLogic.BOGUS_VEC2;
    public Transform target=null;
    public UnitAction action = UnitAction.FOLLOW;
    public Sprite unitIcon;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSpawn(Faction faction)
    {
        this.faction = faction;
        unitRenderer.material = faction.unitMaterial;
    }
}


public class UnitGroup
{
    public List<Unit> units = new List<Unit>();
    //how many rows
    public int rowWidth=8;
    public float spacing=2.1f;
    public float followDistance=2.2f;
    public Vector2 pos=Vector2.zero;
    public Vector2 orientation = Vector2.zero;
    public List<Vector2> rowPositions = new List<Vector2>();
    public List<Vector2> rowOrientations = new List<Vector2>();
    public bool following = true;
    public Sprite unitGroupImage;

    public UnitGroup()
    {
        rowPositions.Add(Vector2.zero);
        rowOrientations.Add(Vector2.zero);
    }

    public void Add(Unit unit)
    {
        if ((units.Count + 1) / rowWidth > units.Count / rowWidth)
        {
            rowPositions.Add(Vector2.zero);
            rowOrientations.Add(Vector2.zero);
        }
        if (this.units.Count == 0)
            unitGroupImage = unit.unitIcon;
        this.units.Add(unit);
        
    }

    public List<Unit> Remove(int num)
    {
        List<Unit> returnedUnits;
        int lastRowCount = units.Count / rowWidth;
        if (num >= units.Count) {
            returnedUnits = new List<Unit>(this.units);
            this.units.Clear();
            //TODO properly handle row pos and orientations
        } else {
            returnedUnits = this.units.GetRange(0, num);
            this.units.RemoveRange(0, num);
            int newRowCount = units.Count / rowWidth;
            if (lastRowCount != newRowCount) {
                int toRemove = lastRowCount - newRowCount;
                int start = rowPositions.Count - toRemove;
                this.rowPositions.RemoveRange(start, toRemove);
                this.rowOrientations.RemoveRange(start, toRemove);
            }
            
        }
        return returnedUnits;
    }
}
