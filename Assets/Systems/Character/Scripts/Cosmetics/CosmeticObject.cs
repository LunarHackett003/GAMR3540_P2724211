using System.Collections.Generic;
using UnityEngine;

public enum CosmeticSlot
{
    none = 0,
    head = 1,
    torso = 2,
    arms = 3,
    legs = 4,
    belt = 5,
    eyes = 6,
    hands = 7,
    leftWrist = 8,
    rightWrist = 9,
    chest = 10,
    back = 11,
    lowerBack = 12,
}




public class CosmeticObject : LunarScript
{
    [System.Serializable]
    public struct CosmeticRenderer
    {
        public CosmeticSlot slot;
        public Renderer renderer;
        public bool disableOriginal;
    }

    public CosmeticRenderer[] cosmeticRenderers;
    public Dictionary<CosmeticSlot, CosmeticRenderer> cosmeticDictionary = new();
    public Dictionary<CosmeticSlot, CosmeticPiece> spawnedCosmetics = new();
    public bool buildBones;
    public Transform rootBone;
    public Dictionary<string, Transform> bones = new Dictionary<string, Transform>();

    private void Awake()
    {
        for (int i = 0; i < cosmeticRenderers.Length; i++)
        {
            cosmeticDictionary.TryAdd(cosmeticRenderers[i].slot, cosmeticRenderers[i]);
        }

        BuildBoneDictionary();
    }

    public void EquipCosmetic(CosmeticPiece cosmetic)
    {
        Instantiate(cosmetic, Vector3.zero, Quaternion.identity, rootBone);

        if (cosmeticDictionary.TryGetValue(cosmetic.cosmeticSlot, out CosmeticRenderer cr))
        {
            spawnedCosmetics.TryAdd(cosmetic.cosmeticSlot, cosmetic);
            if(cr.disableOriginal)
                cr.renderer.enabled = false;
        }
    }
    public void UnequipCosmetic(CosmeticPiece cosmetic)
    {
        if (cosmeticDictionary.TryGetValue(cosmetic.cosmeticSlot, out CosmeticRenderer cr))
        {
            spawnedCosmetics.Remove(cosmetic.cosmeticSlot);
            if (cr.disableOriginal)
                cr.renderer.enabled = true;

            Destroy(cosmetic.gameObject);
        }
    }

    void BuildBoneDictionary()
    {
        bones = new();
        foreach (var item in rootBone.GetComponentsInChildren<Transform>())
        {
            bones.TryAdd(item.name, item);
        }
    }

    private void OnValidate()
    {
        if (buildBones)
        {
            buildBones = false;
            if(rootBone != null)
            {
                BuildBoneDictionary();
                Debug.Log("Built bone dictionary");
            }
            else
            {
                Debug.LogWarning("Cannot build bone dictionary! Make sure Root Bone has been assigned on the object!", this);
            }
        }
    }
}
