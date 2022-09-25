using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leader : MonoBehaviour
{
    public bool isPc=false;
    private Dictionary<string, UnitGroup> units = new Dictionary<string, UnitGroup>();
    private List<UnitGroup> unitGroups = new List<UnitGroup>();

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
            unitGroups.Add(unitGroup);
        }
        unitGroup.Add(unit);
    }

    public UnitGroup GetUnitGroup(string unitType)
    {
        UnitGroup unitGroup;
        if (units.TryGetValue(unitType, out unitGroup))
            return unitGroup;
        return null;
    }

    public List<UnitGroup> GetUnitGroups()
    {
        return unitGroups;
    }
}





//This is buggy
public class CyclicList<T> : IEnumerable<T>
{
    public readonly List<T> list;
    private int startIndex = 0;
    public CyclicList(List<T> list, int selectedIndex)
    {
        this.list = list;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return new CyclicEnumerator<T>(list, startIndex);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetCyclicEnumerator();
    }

    CyclicEnumerator<T> GetCyclicEnumerator()
    {
        return new CyclicEnumerator<T>(list, startIndex);
    }

    public void NextStartIndex()
    {
        SetStartIndex(startIndex + 1);
    }

    public void PrevStartIndex()
    {
        SetStartIndex(startIndex - 1);
    }

    public void SetStartIndex(int startIndex)
    {
        startIndex %= list.Count;
        if (startIndex < 0)
            this.startIndex = startIndex+list.Count;
    }
}


public class CyclicEnumerator<T> : IEnumerator<T>
{
    private readonly List<T> list;
    private readonly int startIndex;
    private int currentIndex = 0;
    private bool started=false;
    
    public T Current => currentIndex==-1?default(T):list[currentIndex];

    public CyclicEnumerator(List<T> list, int startIndex)
    {
        this.list = list;
        this.startIndex = startIndex;
        this.currentIndex = startIndex;
    }

    object IEnumerator.Current => currentIndex<0?default(T):list[currentIndex];

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
        SetIndex(currentIndex+1);
        if (started)
            return startIndex != currentIndex;
        started = true;
        return true;
    }

    public bool MovePrev()
    {
        SetIndex(currentIndex - 1);
        if (started)
            return startIndex!=currentIndex;
        started = true;
        return true;
    }

    private void SetIndex(int index)
    {
        this.currentIndex = NormalizeIndex(index);
    }

    private int NormalizeIndex(int index)
    {
        if (list.Count==0)
            return -1;
        if (index >= 0 && index < list.Count)
            return index;
        int normalized = index % list.Count;
        if (normalized < 0)
            normalized += list.Count;
        return normalized;
    }

    public void Reset()
    {
        currentIndex = startIndex-1;
        started = false;
    }
}