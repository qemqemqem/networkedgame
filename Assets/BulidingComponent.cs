using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulidingComponent : MonoBehaviour
{
    public Faction faction;
    public Renderer bulidingRenderer;
    public string unitType;
    public float spawnRate=.1f;
    public float timeSinceSpawn = 0f;
    public TMPro.TextMeshPro textMesh;
    public int garrisonCount=0;
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
        bulidingRenderer.material = faction.unitMaterial;
        textMesh.color = faction.factionColor;
    }

    public void UpdateCount(int garrisonCount)
    {
        this.garrisonCount = garrisonCount;

        textMesh.text = "" + this.garrisonCount;
    }
}
