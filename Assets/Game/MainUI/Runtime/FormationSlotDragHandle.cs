using UnityEngine;
using UnityEngine.EventSystems;

namespace TrickcalRevive.MainUI
{
    /// <summary>활성화된 진형 Spine을 드래그하고, 놓은 슬롯 좌표를 PartySetupView에 전달한다.</summary>
    public sealed class FormationSlotDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private PartySetupView owner;
        [SerializeField] private int slotX;
        [SerializeField] private int slotY;

        private RectTransform rectTransform;
        private Vector2 startPosition;
        private bool isDragging;

        public int SlotX => slotX;
        public int SlotY => slotY;
        public bool IsReady => owner != null && (rectTransform != null || transform is RectTransform) && IsCoordinate(slotX, slotY);

        public void Configure(PartySetupView partySetupView, int x, int y)
        {
            owner = partySetupView;
            slotX = x;
            slotY = y;
            rectTransform = transform as RectTransform;
        }

        private void Awake()
        {
            rectTransform = transform as RectTransform;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsReady || !owner.CanBeginFormationDrag(slotX, slotY))
                return;

            startPosition = rectTransform.anchoredPosition;
            isDragging = true;
            MoveToPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isDragging)
                MoveToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            isDragging = false;
            rectTransform.anchoredPosition = startPosition;
            owner.CompleteFormationDrag(slotX, slotY, eventData.position, eventData.pressEventCamera);
        }

        private void MoveToPointer(PointerEventData eventData)
        {
            var parent = rectTransform.parent as RectTransform;
            if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                rectTransform.anchoredPosition = localPoint;
            }
        }

        private static bool IsCoordinate(int x, int y) => x >= 1 && x <= 3 && y >= 1 && y <= 3;
    }
}
