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
	[Header("Данные рецептов")]
	public StationSO StationData;                 // книга рецептов (ассет)

	[Header("Куда класть результат")]
	public Inventory OutputInventory;

	[Header("Правила")]
	public bool RestrictSlotsToKnown = true;      // в слоты можно класть только ингредиенты рецептов этой станции
	public bool StrictMatch = true;               // лишние предметы запрещены при совпадении рецепта

	[Header("UI слоты (назначьте вручную, ПО ПОРЯДКУ СЛЕВА НАПРАВО)")]
	public List<StationSlotUI> SlotUIs = new();   // ← перетащите сюда свои UI-слоты

	// Состояние
	[SerializeField] private List<CraftingInputSlot> inputSlots = new(); // столько же, сколько SlotUIs
	[SerializeField] private RecipeSO currentMatch;
	[SerializeField] private int maxCraftsAvailable;

	public System.Action OnChanged;

	public IReadOnlyList<CraftingInputSlot> Inputs => inputSlots;
	public RecipeSO CurrentRecipe => currentMatch;
	public int MaxCraftsAvailable => maxCraftsAvailable;
	public ItemSO OutputItem => currentMatch ? currentMatch.Result : null;
	public int OutputPerCraft => currentMatch ? currentMatch.ResultAmount : 0;

	// ===== Жизненный цикл / привязка UI слотов =====
	private void OnValidate() { SyncSlotsFromUI(); }
	private void Awake() { SyncSlotsFromUI(); Recalculate(); }

	private void SyncSlotsFromUI()
	{
		// 1) подгоняем количество логических слотов под количество UI-слотов
		int n = Mathf.Max(1, SlotUIs?.Count ?? 0);
		if (inputSlots == null) inputSlots = new List<CraftingInputSlot>(n);
		while (inputSlots.Count < n) inputSlots.Add(new CraftingInputSlot());
		while (inputSlots.Count > n) inputSlots.RemoveAt(inputSlots.Count - 1);

		// 2) жёстко биндим каждый UI-слот к этой станции и индексу
		if (SlotUIs != null)
		{
			for (int i = 0; i < SlotUIs.Count; i++)
			{
				var ui = SlotUIs[i];
				if (!ui) continue;
				ui.Bind(this, i);                     // ← единственное место, где задаются station+index
			}
		}

		// 3) дернуть UI на случай правок в редакторе
		OnChanged?.Invoke();
	}

	// ===== API для UI-слотов =====
	public bool CanAccept(ItemSO item)
	{
		if (item == null) return false;
		if (!RestrictSlotsToKnown || !StationData) return true;

		foreach (var r in StationData.Recipes)
		{
			if (!r) continue;
			for (int i = 0; i < r.Ingredients.Count; i++)
				if (r.Ingredients[i].Item == item) return true;
		}
		return false;
	}

	public int AddToSlot(int index, ItemSO item, int amount)
	{
		if (!InRange(index) || item == null || amount <= 0) return 0;
		if (!CanAccept(item)) return 0;

		var s = inputSlots[index];
		if (s.IsEmpty)
		{
			s.Item = item;
			s.Quantity = amount;
		}
		else if (s.Item == item)
		{
			s.Quantity += amount;
		}
		else
		{
			return 0; // в слоте другой предмет
		}

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

	public int ReturnInputsToInventory()
	{
		if (OutputInventory == null || Inputs == null) return 0;

		int totalReturned = 0;
		// Проходим по ВСЕМ слотам и пытаемся вернуть содержимое
		for (int i = 0; i < inputSlots.Count; i++)
		{
			var s = inputSlots[i];
			if (s.IsEmpty) continue;

			int want = s.Quantity;
			// inv.Add вернёт сколько реально поместилось (может быть частично)
			int added = OutputInventory.Add(s.Item, want);
			if (added > 0)
			{
				// снимаем из слота ровно столько, сколько удалось вернуть
				RemoveFromSlotInternal(i, added);
				totalReturned += added;
			}
		}

		Recalculate(); // обновит UI
		return totalReturned;
	}
	public int TryCraft(int crafts = 1)
	{
		crafts = Mathf.Clamp(crafts, 1, maxCraftsAvailable);
		if (!currentMatch || crafts <= 0) return 0;

		int totalOut = currentMatch.ResultAmount * crafts;
		if (OutputInventory && !OutputInventory.HasSpaceFor(currentMatch.Result, totalOut))
			return 0;

		foreach (var ing in currentMatch.Ingredients)
		{
			int need = ing.Amount * crafts;
			for (int i = 0; i < inputSlots.Count && need > 0; i++)
				if (!inputSlots[i].IsEmpty && inputSlots[i].Item == ing.Item)
					need -= RemoveFromSlotInternal(i, need);
		}

		if (OutputInventory) OutputInventory.Add(currentMatch.Result, totalOut);

		Recalculate();
		return crafts;
	}

	public int TryCraftAll() => TryCraft(maxCraftsAvailable);

	// ===== Внутреннее =====
	private bool InRange(int i) => i >= 0 && i < inputSlots.Count;

	private int RemoveFromSlotInternal(int index, int amount)
	{
		var s = inputSlots[index];
		int rem = Mathf.Min(amount, s.Quantity);
		s.Quantity -= rem;
		if (s.Quantity <= 0) s.Clear();
		return rem;
	}

	private void Recalculate()
	{
		currentMatch = null;
		maxCraftsAvailable = 0;

		var counts = new Dictionary<ItemSO, int>();
		foreach (var s in inputSlots)
		{
			if (s.IsEmpty) continue;
			counts.TryGetValue(s.Item, out int c);
			counts[s.Item] = c + s.Quantity;
		}

		if (!StationData || StationData.Recipes == null || StationData.Recipes.Count == 0 || counts.Count == 0)
		{
			OnChanged?.Invoke();   // чтобы UI обнулился
			return;
		}

		foreach (var r in StationData.Recipes)
		{
			if (!r || !r.Result || r.Ingredients == null || r.Ingredients.Count == 0) continue;

			if (StrictMatch)
			{
				foreach (var pair in counts)
				{
					bool present = false;
					for (int i = 0; i < r.Ingredients.Count; i++)
						if (r.Ingredients[i].Item == pair.Key) { present = true; break; }
					if (!present) goto Next;
				}
			}

			int crafts = int.MaxValue;
			for (int i = 0; i < r.Ingredients.Count; i++)
			{
				var ing = r.Ingredients[i];
				counts.TryGetValue(ing.Item, out int have);
				if (have < ing.Amount) { crafts = 0; break; }
				crafts = Mathf.Min(crafts, have / ing.Amount);
			}

			if (crafts > 0) { currentMatch = r; maxCraftsAvailable = crafts; break; }

		Next:;
		}

		OnChanged?.Invoke(); // ← оповещаем все SlotUI
	}
}
