using UnityEngine;

public class SkinnedCosmeticPiece : CosmeticPiece
{
    public SkinnedMeshRenderer skm;
    public string[] skmBones;

    public bool buildBones = true;

    private void OnValidate()
    {
        if (buildBones)
        {
            buildBones = false;
            if(skm != null)
            {
                skmBones = new string[skm.bones.Length];
                for (int i = 0; i < skmBones.Length; i++)
                {
                    skmBones[i] = skm.bones[i].name;
                }
            }
            else
            {
                Debug.LogWarning("Cannot build bone array! Make sure that the Skinned Mesh Renderer is assigned.");
            }
        }
    }
}
