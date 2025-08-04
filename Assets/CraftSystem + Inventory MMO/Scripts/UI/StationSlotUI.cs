using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StationSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
	public Image Icon;
	public TMP_Text CountText;

	private CraftingStation station;
	private int index;

	public void Bind(CraftingStation s, int idx)
	{
		station = s;
		index = idx;
		Refresh();
	}

	public void Refresh()
	{
		if (station == null) return;
		var slot = station.Inputs[index];

		if (slot.IsEmpty)
		{
			if (Icon) { Icon.sprite = null; Icon.enabled = true; } // можно поставить "пустую рамку"
			if (CountText) CountText.text = "";
		}
		else
		{
			if (Icon) { Icon.sprite = slot.Item.Icon; Icon.enabled = true; }
			if (CountText) CountText.text = slot.Quantity > 1 ? $"x{slot.Quantity}" : "";
		}
	}

	public void OnDrop(PointerEventData eventData)
	{
		if (station == null || !DragManager.I.Active) return;

		var item = DragManager.I.Item;
		int want = DragManager.I.Amount;

		if (!station.CanAccept(item)) return;

		int added = station.AddToSlot(index, item, want);
		if (added > 0)
		{
			DragManager.I.Consume(added); // снимет added из инвентаря-источника
			Refresh();
		}
	}


	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Right || station == null) return;
		var s = station.Inputs[index];
		if (s.IsEmpty) return;

		
		station.RemoveFromSlot(index, s.Quantity);
		Refresh();
	}
}
