using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Faction faction;
    public Renderer unitRenderer;
    public string unitType;

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
    public float spacing=2f;
    public float followDistance=5f;
    public Vector2 pos=Vector2.zero;
    public Vector2 orientation = Vector2.zero;
}
