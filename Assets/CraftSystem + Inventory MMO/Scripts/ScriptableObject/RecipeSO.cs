using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Ingredient
{
	public ItemSO Item;
	[Min(1)] public int Amount;
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class RecipeSO : ScriptableObject
{
	public List<Ingredient> Ingredients = new();
	public ItemSO Result;
	[Min(1)] public int ResultAmount = 1;
}
