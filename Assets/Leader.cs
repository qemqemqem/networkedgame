using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leader : MonoBehaviour
{
    public bool isPc=false;
    private Dictionary<string, UnitGroup> units = new Dictionary<string, UnitGroup>();
    private List<UnitGroup> unitGroups = new List<UnitGroup>();
    public int selectedUserGroup = -1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public UnitGroup AddUnit(Unit unit)
    {
        UnitGroup unitGroup;
        if (!units.TryGetValue(unit.unitType, out unitGroup)) {
            unitGroup = new UnitGroup();
            units.Add(unit.unitType, unitGroup);
            unitGroups.Add(unitGroup);
        }
        unitGroup.Add(unit);
        if (unitGroups.Count == 1)
            selectedUserGroup = 0;
        return unitGroup;
    }

    public UnitGroup GetUnitGroup(string unitType)
    {
        UnitGroup unitGroup;
        if (units.TryGetValue(unitType, out unitGroup))
            return unitGroup;
        return null;
    }

    public void RemoveUnitGroup(UnitGroup unitGroup)
    {
        unitGroups.Remove(unitGroup);
        if (selectedUserGroup == unitGroups.Count)
            selectedUserGroup--;
        ISet<string> typesToRemove = new HashSet<string>();
        foreach(var entry in units)
        {
            if (entry.Value == unitGroup)
                typesToRemove.Add(entry.Key);
        }
        foreach(string key in typesToRemove)
            units.Remove(key);
    }

    public List<UnitGroup> GetUnitGroups()
    {
        return unitGroups;
    }
}