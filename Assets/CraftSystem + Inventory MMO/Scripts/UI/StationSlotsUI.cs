using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StationSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
	[Header("UI")]
	public Image Icon;                // На корне слота должен быть Graphic с Raycast Target = ON
	public TMP_Text CountText;

	[Header("Ссылки")]
	public CraftingStation Station;   // назначьте в инспекторе (одна станция)
	public Inventory PlayerInventory; // инвентарь игрока для возвратов/замены

	[Header("Индекс слота станции")]
	public int Index = 0;             // задайте вручную для каждого слота: 0,1,2,...

	private void OnEnable()
	{
		if (Station) Station.OnChanged += Refresh;
		Refresh();
	}
	private void OnDisable()
	{
		if (Station) Station.OnChanged -= Refresh;
	}
	public void Bind(CraftingStation s, int idx)
	{
		// отписаться от старой станции (если была)
		if (Station != null) Station.OnChanged -= Refresh;

		Station = s;
		Index = idx;

		if (Station != null) Station.OnChanged += Refresh;
		Refresh();
	}

	public void Refresh()
	{
		if (!Station || Index < 0 || Index >= Station.Inputs.Count) return;

		var s = Station.Inputs[Index];
		if (s.IsEmpty)
		{
			if (Icon) { Icon.sprite = null; Icon.enabled = true; }
			if (CountText) CountText.text = "";
		}
		else
		{
			if (Icon) { Icon.sprite = s.Item.Icon; Icon.enabled = true; }
			if (CountText) CountText.text = s.Quantity > 1 ? $"x{s.Quantity}" : "";
		}
	}

	// ==========================
	//    D R O P   (из рук)
	// ==========================
	public void OnDrop(PointerEventData e)
	{
		if (!Station || !DragManager.I.Active) return;

		var item = DragManager.I.Item;
		int want = DragManager.I.Amount;
		if (!Station.CanAccept(item)) return;

		var s = Station.Inputs[Index];

		// 1) Пустой слот или тот же предмет — стандартное добавление
		if (s.IsEmpty || s.Item == item)
		{
			int added = Station.AddToSlot(Index, item, want);
			if (added > 0) DragManager.I.Consume(added);
			return;
		}

		// 2) Слот занят ДРУГИМ предметом — попытаться заменить
		if (!PlayerInventory)
		{
			Debug.LogWarning("StationSlotUI: нет ссылки на PlayerInventory — замена невозможна.");
			return;
		}

		// Проверим, что инвентарь примет ВЕСЬ старый стак
		if (!PlayerInventory.HasSpaceFor(s.Item, s.Quantity))
		{
			// Места не хватает — ничего не меняем, чтобы не терять предметы
			Debug.Log("Недостаточно места в инвентаре для замены предмета в слоте.");
			return;
		}

		// Переносим старый стак в инвентарь (гарантированно влезет)
		int back = PlayerInventory.Add(s.Item, s.Quantity);
		if (back > 0) Station.RemoveFromSlot(Index, back);

		// Теперь слот пуст — кладём новый
		int addedNew = Station.AddToSlot(Index, item, want);
		if (addedNew > 0) DragManager.I.Consume(addedNew);

		// Обновится через Station.OnChanged
	}

	// ====================================
	// Клики: ЛКМ — положить из "рук"; ПКМ — вернуть в инвентарь
	// ====================================
	public void OnPointerClick(PointerEventData e)
	{
		if (!Station) return;

		if (e.button == PointerEventData.InputButton.Left && DragManager.I.Active)
		{
			// Клик-клик: положить из «рук» без d&d
			var item = DragManager.I.Item;
			int want = DragManager.I.Amount;
			if (!Station.CanAccept(item)) return;

			var s = Station.Inputs[Index];
			if (s.IsEmpty || s.Item == item)
			{
				int added = Station.AddToSlot(Index, item, want);
				if (added > 0) DragManager.I.Consume(added);
			}
			else
			{
				// Поведение как при замене (смотрите OnDrop)
				if (!PlayerInventory) return;
				if (!PlayerInventory.HasSpaceFor(s.Item, s.Quantity))
				{
					Debug.Log("Недостаточно места в инвентаре для замены предмета в слоте.");
					return;
				}
				int back = PlayerInventory.Add(s.Item, s.Quantity);
				if (back > 0) Station.RemoveFromSlot(Index, back);

				int addedNew = Station.AddToSlot(Index, item, want);
				if (addedNew > 0) DragManager.I.Consume(addedNew);
			}
			return;
		}

		if (e.button == PointerEventData.InputButton.Right)
		{
			// Возврат из слота в инвентарь БЕЗ потерь:
			// добавляем сколько влезет → снимаем ровно столько же со слота
			var s = Station.Inputs[Index];
			if (s.IsEmpty || !PlayerInventory) return;

			int toReturn = s.Quantity;
			int actuallyAdded = PlayerInventory.Add(s.Item, toReturn); // может быть частично
			if (actuallyAdded > 0)
			{
				Station.RemoveFromSlot(Index, actuallyAdded);
			}
			else
			{
				Debug.Log("Нет места в инвентаре для возврата.");
			}
		}
	}
}
