using UnityEngine;

public enum ItemType { Generic, Consumable, Equipment, Quest }

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item")]
public class ItemSO : ScriptableObject
{
	[SerializeField] private string id = System.Guid.NewGuid().ToString();
	public string Id => id;

	[Header("Display")]
	public string DisplayName;
	[TextArea] public string Description;
	public Sprite Icon;

	[Header("Logic")]
	public ItemType Type = ItemType.Generic;
	[Min(1)] public int MaxStack = 99;

	[Header("Optional")]
	public GameObject WorldPrefab; // ךאך גûדכÿהטע ג לטנו (ןמהבמנ)
}
