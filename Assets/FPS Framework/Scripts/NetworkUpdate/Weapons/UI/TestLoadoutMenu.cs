using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestLoadoutMenu : LunarScript
{
    public static TestLoadoutMenu Instance { get; private set; }
    public List<BaseNetWeapon> weapons;

    public List<BaseNetWeapon> selectedWeapons = new(4);

    public Button[] slotButtons;

    public TestLoadoutButton buttonPrefab;
    List<TestLoadoutButton> loadoutButtons;
    public RectTransform weaponListRoot;

    public enum SlotSelection : int
    {
        slot1 = 0,
        slot2 = 1,
        slot3 = 2,
        slot4 = 3,
    }
    public SlotSelection currentSlotSelection;

    private void Awake()
    {
        if (Instance != null)
        {
            gameObject.SetActive(false);
            return;
        }
        Instance = this;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            slotButtons[i].onClick.AddListener(() =>
            {
                SetSelection(i);
            });
        }

    }

    void SetSelection(int slotIndex)
    {
        currentSlotSelection = (SlotSelection)slotIndex;
        weaponListRoot.gameObject.SetActive(true);
    }

    public void SelectWeapon(BaseNetWeapon weapon)
    {
        selectedWeapons[(int)currentSlotSelection] = weapon;
        weaponListRoot.gameObject.SetActive(false);
    }

    public void BuildUI()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            var button = Instantiate(buttonPrefab, weaponListRoot);
            button.Initialise(weapons[i]);
            loadoutButtons.Add(button);
        }
    }

}
