using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leader : MonoBehaviour
{
    public bool isPc=false;
    private Dictionary<string, UnitGroup> units = new Dictionary<string, UnitGroup>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddUnit(Unit unit)
    {
        UnitGroup unitGroup;
        if (!units.TryGetValue(unit.unitType, out unitGroup)) {
            unitGroup = new UnitGroup();
            units.Add(unit.unitType, unitGroup);
        }
        unitGroup.units.Add(unit);
    }

    public UnitGroup GetUnitGroup(string unitType)
    {
        UnitGroup unitGroup;
        if (units.TryGetValue(unitType, out unitGroup))
            return unitGroup;
        return null;
    }
}
