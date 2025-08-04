using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingControlsUI : MonoBehaviour
{
	public CraftingStation Station;
	public Button CraftOneButton;
	public Button CraftAllButton;
	public Button ClearInputsButton;
	public TMP_Text HintText;   // "Нет места в инвентаре", "Нет рецепта" — по желанию

	private void OnEnable()
	{
		if (Station) Station.OnChanged += Refresh;
		Hook(true);
		Refresh();
	}

	private void OnDisable()
	{
		if (Station) Station.OnChanged -= Refresh;
		Hook(false);
	}

	private void Hook(bool on)
	{
		if (on)
		{
			if (CraftOneButton) CraftOneButton.onClick.AddListener(() => Station.TryCraft(1));
			if (CraftAllButton) CraftAllButton.onClick.AddListener(() => Station.TryCraftAll());
			if (ClearInputsButton) ClearInputsButton.onClick.AddListener(() => Station.ReturnInputsToInventory());
		}
		else
		{
			if (CraftOneButton) CraftOneButton.onClick.RemoveAllListeners();
			if (CraftAllButton) CraftAllButton.onClick.RemoveAllListeners();
			if (ClearInputsButton) ClearInputsButton.onClick.RemoveAllListeners();
		}
	}

	private void Refresh()
	{
		bool can = Station && Station.CurrentRecipe != null && Station.MaxCraftsAvailable > 0;
		if (CraftOneButton) CraftOneButton.interactable = can;
		if (CraftAllButton) CraftAllButton.interactable = can;

		if (HintText)
		{
			if (!Station || Station.CurrentRecipe == null) HintText.text = "Нет подходящего рецепта";
			else if (Station.MaxCraftsAvailable <= 0) HintText.text = "Недостаточно ресурсов";
			else HintText.text = "";
		}
	}
}
