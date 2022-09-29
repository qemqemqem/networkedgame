using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Faction faction;
    public Renderer unitRenderer;
    public string unitType;

    public float speed = .001f;
    public Vector2 desiredPosition = MainGameLogic.BOGUS_VEC2;

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
    public int rowWidth=4;
    public float spacing=4f;
    public float followDistance=5f;
    public Vector2 pos=Vector2.zero;
    public Vector2 orientation = Vector2.zero;
    public List<Vector2> rowPositions = new List<Vector2>();
    public List<Vector2> rowOrientations = new List<Vector2>();
    public bool following = true;

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
        this.units.Add(unit);
        
    }
}
