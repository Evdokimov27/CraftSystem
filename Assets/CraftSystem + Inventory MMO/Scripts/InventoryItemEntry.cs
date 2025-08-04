using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InventoryItemEntry : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
	public Image Icon;
	public TMP_Text NameText;
	public TMP_Text CountText;

	private Inventory inv;
	private ItemSO item;
	private int currentCount;
	private CanvasGroup cg;

	public void Awake()
	{
		cg = GetComponent<CanvasGroup>();
		if (!cg) cg = gameObject.AddComponent<CanvasGroup>(); // нужно для blocksRaycasts
	}

	public void Init(Inventory inventory, ItemSO i) { inv = inventory; item = i; Icon.sprite = i.Icon; NameText.text = i.DisplayName; }
	public void SetCount(int c) { currentCount = c; CountText.text = $"x{c}"; gameObject.SetActive(c > 0); }

	public void OnBeginDrag(PointerEventData e)
	{
		Debug.Log($"[Entry.OnBeginDrag] {item?.name} x{currentCount}");
		if (currentCount <= 0 || item == null) return;

		cg.blocksRaycasts = false; // ← важно: иначе этот элемент перекрывает слоты под курсором
		DragManager.I.Begin(item, currentCount, Icon ? Icon.sprite : null,
			used => inv.Remove(item, used));
		e.Use();
	}

	public void OnDrag(PointerEventData e) { /* пусто, но оставим интерфейс */ }

	public void OnEndDrag(PointerEventData e)
	{
		cg.blocksRaycasts = true;   // вернуть приём лучей
		if (DragManager.I.Active) DragManager.I.Cancel();
	}
}
