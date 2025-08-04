using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class OutputSlotUI : MonoBehaviour, IPointerClickHandler
{
	[Header("Data")]
	public CraftingStation Station;     // укажите станцию в инспекторе

	[Header("UI")]
	public Image Icon;                  // Raycast Target = ON на корневом графике (этого объекта)
	public TMP_Text PerCraftText;       // например: "x1" или "x5"
	public TMP_Text AvailableText;      // например: "Можно: 3"

	private void OnEnable()
	{
		if (Station) Station.OnChanged += Refresh;
		Refresh();
	}

	private void OnDisable()
	{
		if (Station) Station.OnChanged -= Refresh;
	}

	public void Refresh()
	{
		if (!Station || !Icon) return;

		var item = Station.OutputItem;
		var can = Station.MaxCraftsAvailable;
		var per = Station.OutputPerCraft;

		bool has = (item != null) && (can > 0);

		Icon.sprite = has ? item.Icon : null;
		Icon.enabled = true; // показываем рамку даже если пусто

		if (PerCraftText) PerCraftText.text = has ? $"x{per}" : "";
		if (AvailableText) AvailableText.text = has ? $"Можно: {can}" : "—";
	}

	// ЛКМ — 1 раз, ПКМ — всё; Shift + ЛКМ — всё
	public void OnPointerClick(PointerEventData e)
	{
		if (!Station) return;

		var hasRecipe = Station.CurrentRecipe != null && Station.MaxCraftsAvailable > 0;
		if (!hasRecipe) return;

		if (e.button == PointerEventData.InputButton.Left)
		{
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				Station.TryCraftAll();
			else
				Station.TryCraft(1);
		}
		else if (e.button == PointerEventData.InputButton.Right)
		{
			Station.TryCraftAll();
		}
		// Station вызовет OnChanged → UI обновится сам
	}
}
