using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryListUI : MonoBehaviour
{
	public Inventory SourceInventory;
	public RectTransform Content;               // контейнер ScrollView
	public InventoryItemEntry EntryPrefab;      // элемент списка

	private readonly Dictionary<ItemSO, InventoryItemEntry> map = new();

	private void OnEnable()
	{
		if (SourceInventory) SourceInventory.OnChanged += Refresh;
		Refresh();
	}

	private void OnDisable()
	{
		if (SourceInventory) SourceInventory.OnChanged -= Refresh;
		Clear();
	}

	private void Clear()
	{
		foreach (Transform c in Content) Destroy(c.gameObject);
		map.Clear();
	}

	public void Refresh()
	{
		if (!SourceInventory || !Content || !EntryPrefab) return;

		// 1) Считаем агрегированные количества
		var counts = new Dictionary<ItemSO, int>();
		foreach (var s in SourceInventory.Slots)
		{
			if (s.IsEmpty) continue;
			counts.TryGetValue(s.Item, out int c);
			counts[s.Item] = c + s.Quantity;
		}

		// 2) Обновляем/создаём элементы
		var toRemove = new List<ItemSO>(map.Keys);
		foreach (var kv in counts)
		{
			if (!map.TryGetValue(kv.Key, out var entry))
			{
				entry = Instantiate(EntryPrefab, Content);
				entry.Init(SourceInventory, kv.Key);
				map.Add(kv.Key, entry);
			}
			entry.SetCount(kv.Value);
			toRemove.Remove(kv.Key);
		}

		// 3) Удаляем отсутствующие
		foreach (var item in toRemove)
		{
			Destroy(map[item].gameObject);
			map.Remove(item);
		}
	}
}
