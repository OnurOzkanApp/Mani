using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Utility class to help determine whether the mouse is interacting with UI elements.
/// Useful for preventing in-game actions when clicking on buttons or overlays.
/// </summary>
public class UIHelper : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Raycaster used to detect UI elements under the cursor.")]
    [SerializeField] private GraphicRaycaster graphicRaycaster;

    [Tooltip("Event System used for processing UI input events.")]
    [SerializeField] private EventSystem eventSystem;

    /// <summary>
    /// Checks if the mouse pointer is currently over any UI element.
    /// Can be used to block gameplay input when interacting with UI.
    /// </summary>
    /// <returns>True if the pointer is over a UI element; otherwise, false.</returns>
    public bool IsPointerOverUI()
    {
        // Create pointer data based on the current mouse position
        PointerEventData pointerEventData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        // Perform a UI raycast
        List<RaycastResult> allRaycasts = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, allRaycasts);

        // Return true if any UI elements were hit
        return allRaycasts.Count > 0;
    }
}