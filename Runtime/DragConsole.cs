using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragConsole : MonoBehaviour, IDragHandler
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform console;


    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        console.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
}
