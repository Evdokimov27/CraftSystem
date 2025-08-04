using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Station", menuName = "Crafting/Station")]
public class StationSO : ScriptableObject
{
	public List<RecipeSO> Recipes = new();   // <-- именно RecipeSO
}
