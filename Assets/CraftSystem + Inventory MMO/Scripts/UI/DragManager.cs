using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DragManager : MonoBehaviour
{
	public static DragManager I;

	[Header("UI")]
	public Canvas UICanvas;
	public Image DragIcon;
	public TMP_Text CountText;

	private ItemSO item;
	private int amount;
	private System.Action<int> consumeCallback;
	private bool active;

	public bool Active => active;
	public ItemSO Item => item;
	public int Amount => amount;

	void Awake()
	{
		if (I != null && I != this) { Destroy(gameObject); return; }
		I = this;

		if (!UICanvas) UICanvas = GetComponentInParent<Canvas>();
		if (DragIcon)
		{
			DragIcon.raycastTarget = false; // критично: не блокировать дроп
			DragIcon.enabled = false;
		}
		if (CountText) CountText.enabled = false;
	}

	void LateUpdate()
	{
		if (!active || UICanvas == null || DragIcon == null) return;
		MoveIconTo(Input.mousePosition);
		((RectTransform)DragIcon.transform).SetAsLastSibling();
		if (CountText) CountText.text = amount > 1 ? $"x{amount}" : "";
	}

	public void Begin(ItemSO item, int amount, Sprite icon, System.Action<int> onConsume)
	{
		if (item == null || amount <= 0 || DragIcon == null) return;

		this.item = item;
		this.amount = amount;
		this.consumeCallback = onConsume;

		// убедимся, что иконка — прямой ребёнок root-Canvas
		var iconRect = (RectTransform)DragIcon.transform;
		iconRect.SetParent(UICanvas.transform, false);
		iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
		iconRect.pivot = new Vector2(0.5f, 0.5f);

		DragIcon.sprite = icon;
		DragIcon.raycastTarget = false;
		DragIcon.enabled = true;
		if (CountText) CountText.enabled = true;

		// сразу поставить под курсор в первый кадр
		MoveIconTo(Input.mousePosition);

		active = true;
	}

	private void MoveIconTo(Vector2 screenPoint)
	{
		var canvasRect = (RectTransform)UICanvas.transform;
		var iconRect = (RectTransform)DragIcon.transform;

		if (UICanvas.renderMode == RenderMode.ScreenSpaceOverlay)
		{
			// в Overlay можно просто ставить screenPoint в мировую позицию
			iconRect.position = screenPoint;
		}
		else
		{
			// для Screen Space - Camera / World Space
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				canvasRect, screenPoint, UICanvas.worldCamera, out var local);
			iconRect.anchoredPosition = local;
		}
	}


	public void Consume(int used)
	{
		if (!active || used <= 0) return;
		consumeCallback?.Invoke(used);
		End();
	}

	public void Cancel() => End();

	private void End()
	{
		active = false;
		item = null; amount = 0; consumeCallback = null;

		if (DragIcon) DragIcon.enabled = false;
		if (CountText) CountText.enabled = false;
	}
}
