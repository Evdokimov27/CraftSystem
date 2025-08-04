using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySlot
{
	public ItemSO Item;
	public int Quantity;

	public bool IsEmpty => Item == null || Quantity <= 0;
	public bool IsFull => Item != null && Quantity >= Item.MaxStack;

	public void Clear()
	{
		Item = null;
		Quantity = 0;
	}

	public int Add(ItemSO item, int amount)
	{
		if (amount <= 0) return 0;

		if (IsEmpty)
		{
			Item = item;
			int toAdd = Mathf.Min(item.MaxStack, amount);
			Quantity = toAdd;
			return toAdd;
		}

		if (Item != item || IsFull) return 0;

		int space = Item.MaxStack - Quantity;
		int added = Mathf.Min(space, amount);
		Quantity += added;
		return added;
	}

	public int Remove(int amount)
	{
		if (IsEmpty || amount <= 0) return 0;
		int removed = Mathf.Min(amount, Quantity);
		Quantity -= removed;
		if (Quantity <= 0) Clear();
		return removed;
	}

	public bool CanStack(ItemSO item) => !IsEmpty && Item == item && !IsFull;
}

public class Inventory : MonoBehaviour
{
	[SerializeField, Min(1)] private int size = 20;
	[SerializeField] private List<InventorySlot> slots;

	public System.Action OnChanged;
	public IReadOnlyList<InventorySlot> Slots => slots;

	private void Awake()
	{
		if (slots == null || slots.Count != size)
		{
			slots = new List<InventorySlot>(size);
			for (int i = 0; i < size; i++)
				slots.Add(new InventorySlot());
		}
	}

	public int Add(ItemSO item, int amount)
	{
		if (item == null || amount <= 0) return 0;
		int remaining = amount;

		for (int i = 0; i < slots.Count && remaining > 0; i++)
			if (slots[i].CanStack(item))
				remaining -= slots[i].Add(item, remaining);

		for (int i = 0; i < slots.Count && remaining > 0; i++)
			if (slots[i].IsEmpty)
				remaining -= slots[i].Add(item, remaining);

		int added = amount - remaining;
		if (added > 0) OnChanged?.Invoke();
		return added;
	}

	public int Remove(ItemSO item, int amount)
	{
		if (item == null || amount <= 0) return 0;
		int remaining = amount;

		for (int i = 0; i < slots.Count && remaining > 0; i++)
			if (!slots[i].IsEmpty && slots[i].Item == item)
				remaining -= slots[i].Remove(remaining);

		int removed = amount - remaining;
		if (removed > 0) OnChanged?.Invoke();
		return removed;
	}

	public int Count(ItemSO item)
	{
		if (item == null) return 0;
		int total = 0;
		for (int i = 0; i < slots.Count; i++)
			if (!slots[i].IsEmpty && slots[i].Item == item)
				total += slots[i].Quantity;
		return total;
	}

	public bool HasSpaceFor(ItemSO item, int amount)
	{
		if (item == null || amount <= 0) return false;
		int free = 0;

		for (int i = 0; i < slots.Count; i++)
		{
			var s = slots[i];
			if (s.IsEmpty) free += item.MaxStack;
			else if (s.Item == item) free += (item.MaxStack - s.Quantity);
			if (free >= amount) return true;
		}
		return false;
	}
}
