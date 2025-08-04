using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CraftingInputSlot
{
	public ItemSO Item;
	public int Quantity;

	public bool IsEmpty => Item == null || Quantity <= 0;
	public void Clear() { Item = null; Quantity = 0; }
}

public class CraftingStation : MonoBehaviour
{
	[Header("Setup")]
	public StationSO Station;              // <-- ассет с рецептами
	[SerializeField, Min(1)] private int inputSlotsCount = 4;
	[SerializeField] private List<CraftingInputSlot> inputSlots;

	[SerializeField] private Inventory outputInventory;

	[Header("Rules")]
	public bool StrictMatch = true;        // лишние предметы запрещены
	public bool RestrictSlotsToKnown = true; // в слоты можно класть только предметы из любых рецептов станции

	[Header("State (readonly)")]
	[SerializeField] private RecipeSO currentMatch;
	[SerializeField] private int maxCraftsAvailable;

	public System.Action OnChanged;

	public IReadOnlyList<CraftingInputSlot> Inputs => inputSlots;
	public RecipeSO CurrentRecipe => currentMatch;
	public int MaxCraftsAvailable => maxCraftsAvailable;
	public ItemSO OutputItem => currentMatch ? currentMatch.Result : null;
	public int OutputPerCraft => currentMatch ? currentMatch.ResultAmount : 0;

	void Awake()
	{
		if (inputSlots == null || inputSlots.Count != inputSlotsCount)
		{
			inputSlots = new List<CraftingInputSlot>(inputSlotsCount);
			for (int i = 0; i < inputSlotsCount; i++) inputSlots.Add(new CraftingInputSlot());
		}
		Recalculate();
	}

	// === Публичное API для UI ===

	public bool CanAccept(ItemSO item)
	{
		if (!RestrictSlotsToKnown || item == null || Station == null) return true;
		if (Station.Recipes == null) return false;

		foreach (var r in Station.Recipes)
			if (r != null)
				for (int i = 0; i < r.Ingredients.Count; i++)
					if (r.Ingredients[i].Item == item) return true;

		return false;
	}

	// Положить в конкретный слот (возвращает реально положенное кол-во)
	public int AddToSlot(int index, ItemSO item, int amount)
	{
		if (!InRange(index) || item == null || amount <= 0) return 0;
		if (!CanAccept(item)) return 0;

		var s = inputSlots[index];

		if (s.IsEmpty)
		{
			s.Item = item;
			s.Quantity = amount;
			Recalculate();
			return amount;
		}

		if (s.Item != item)
			return 0;

		s.Quantity += amount;
		Recalculate();
		return amount;
	}

	public int RemoveFromSlot(int index, int amount)
	{
		if (!InRange(index) || amount <= 0) return 0;
		var s = inputSlots[index];
		if (s.IsEmpty) return 0;
		int rem = Mathf.Min(amount, s.Quantity);
		s.Quantity -= rem;
		if (s.Quantity <= 0) s.Clear();
		Recalculate();
		return rem;
	}

	public void ClearAll()
	{
		foreach (var s in inputSlots) s.Clear();
		Recalculate();
	}

	
	public int AutoFillFromInventory(Inventory inv, RecipeSO recipe, int crafts = 1)
	{
		if (inv == null || recipe == null || crafts <= 0) return 0;
		int moved = 0;

		foreach (var ing in recipe.Ingredients)
		{
			int need = ing.Amount * crafts;

			// сколько уже лежит (во всех слотах)
			int haveHere = 0;
			for (int i = 0; i < inputSlots.Count; i++)
				if (!inputSlots[i].IsEmpty && inputSlots[i].Item == ing.Item)
					haveHere += inputSlots[i].Quantity;

			int toTake = Mathf.Max(0, need - haveHere);
			if (toTake <= 0) continue;

			int taken = inv.Remove(ing.Item, toTake); 
			if (taken <= 0) continue;

			for (int i = 0; i < inputSlots.Count && taken > 0; i++)
				if (!inputSlots[i].IsEmpty && inputSlots[i].Item == ing.Item)
				{
					inputSlots[i].Quantity += taken;
					moved += taken;
					taken = 0;
				}

			for (int i = 0; i < inputSlots.Count && taken > 0; i++)
				if (inputSlots[i].IsEmpty || inputSlots[i].Item == ing.Item)
				{
					if (!CanAccept(ing.Item)) break;
					if (inputSlots[i].IsEmpty) inputSlots[i].Item = ing.Item;
					inputSlots[i].Quantity += taken;
					moved += taken;
					taken = 0;
				}
		}

		if (moved > 0) Recalculate();
		return moved;
	}

	public int TryCraft(int crafts = 1)
	{
		crafts = Mathf.Clamp(crafts, 1, maxCraftsAvailable);
		if (currentMatch == null || crafts <= 0) return 0;

		int totalOut = currentMatch.ResultAmount * crafts;

		if (outputInventory && !outputInventory.HasSpaceFor(currentMatch.Result, totalOut))
			return 0;

		// Списываем ингредиенты
		foreach (var ing in currentMatch.Ingredients)
		{
			int need = ing.Amount * crafts;
			for (int i = 0; i < inputSlots.Count && need > 0; i++)
				if (!inputSlots[i].IsEmpty && inputSlots[i].Item == ing.Item)
					need -= RemoveFromSlotInternal(i, need);
		}

		if (outputInventory) outputInventory.Add(currentMatch.Result, totalOut);

		Recalculate();
		return crafts;
	}

	public int TryCraftAll() => TryCraft(maxCraftsAvailable);

	// === Внутреннее ===

	private int RemoveFromSlotInternal(int index, int amount)
	{
		var s = inputSlots[index];
		int rem = Mathf.Min(amount, s.Quantity);
		s.Quantity -= rem;
		if (s.Quantity <= 0) s.Clear();
		return rem;
	}

	private bool InRange(int i) => i >= 0 && i < inputSlots.Count;

	private void Recalculate()
	{
		currentMatch = null;
		maxCraftsAvailable = 0;

		var inputCounts = new Dictionary<ItemSO, int>();
		foreach (var s in inputSlots)
		{
			if (s.IsEmpty) continue;
			if (!inputCounts.TryGetValue(s.Item, out int c)) c = 0;
			inputCounts[s.Item] = c + s.Quantity;
		}

		if (Station == null || Station.Recipes == null || Station.Recipes.Count == 0 || inputCounts.Count == 0)
		{
			OnChanged?.Invoke();
			return;
		}

		foreach (var recipe in Station.Recipes)
		{
			if (recipe == null || recipe.Result == null || recipe.Ingredients == null || recipe.Ingredients.Count == 0)
				continue;

			if (StrictMatch)
			{
				foreach (var pair in inputCounts)
				{
					bool present = false;
					foreach (var ing in recipe.Ingredients)
						if (ing.Item == pair.Key) { present = true; break; }
					if (!present) goto NextRecipe;
				}
			}

			int crafts = int.MaxValue;
			foreach (var ing in recipe.Ingredients)
			{
				inputCounts.TryGetValue(ing.Item, out int have);
				if (have < ing.Amount) { crafts = 0; break; }
				crafts = Mathf.Min(crafts, have / ing.Amount);
			}

			if (crafts > 0)
			{
				currentMatch = recipe;
				maxCraftsAvailable = crafts;
				break;
			}

		NextRecipe:;
		}

		OnChanged?.Invoke();
	}
}
