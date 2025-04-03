using UnityEngine;

public class DestructibleWall : MonoBehaviour
{
    public Damageable[] damageables;
    bool damaged;
    public MeshRenderer render;

    private void Awake()
    {
        ShowDamageables(false);
        for (int i = 0; i < damageables.Length; i++)
        {
            damageables[i].onHit += ChildHit;
        }
    }
    public void ChildHit(Damageable d)
    {
        if(!damaged && d.CurrentHealth <= 0)
        {
            damaged = true;
            ShowDamageables(true);
        }
    }
    void ShowDamageables(bool value)
    {
        render.enabled = !value;
        for (int i = 0; i < damageables.Length; i++)
        {
            damageables[i].render.enabled = value;
            if (!value)
            {
                damageables[i].onHit -= ChildHit;    
            }
        }
    }
    private void Reset()
    {
        damageables = GetComponentsInChildren<Damageable>();

    }
}
