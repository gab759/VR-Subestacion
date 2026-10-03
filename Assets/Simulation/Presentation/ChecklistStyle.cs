using System;
using TMPro;
using UnityEngine;
using VRSubestacion.Simulation;

namespace VRSubestacion.Presentation
{
    public readonly struct ChecklistVisual
    {
        public readonly Color Color;
        public readonly FontStyles FontStyle;
        public readonly bool IconVisible;
        public readonly Sprite IconSprite;

        public ChecklistVisual(Color color, FontStyles fontStyle, bool iconVisible, Sprite iconSprite)
        {
            Color = color;
            FontStyle = fontStyle;
            IconVisible = iconVisible;
            IconSprite = iconSprite;
        }
    }

    /// <summary>
    /// Paleta por estado. Los valores por defecto replican el aspecto de <c>CheckList.UpdateUI</c>.
    /// </summary>
    [Serializable]
    public sealed class ChecklistStyle
    {
        [Header("Pendiente")]
        public Color pendingColor = Color.gray;
        public FontStyles pendingFontStyle = FontStyles.Normal;
        public bool pendingIconVisible = false;
        public Sprite pendingIcon;

        [Header("En curso")]
        public Color activeColor = Color.yellow;
        public FontStyles activeFontStyle = FontStyles.Bold;
        public bool activeIconVisible = true;
        public Sprite activeIcon;

        [Header("Completado")]
        public Color completedColor = Color.green;
        public FontStyles completedFontStyle = FontStyles.Strikethrough;
        public bool completedIconVisible = true;
        public Sprite completedIcon;

        public ChecklistVisual Resolve(ProcedureStepState state)
        {
            switch (state)
            {
                case ProcedureStepState.Active:
                    return new ChecklistVisual(activeColor, activeFontStyle, activeIconVisible, activeIcon);
                case ProcedureStepState.Completed:
                    return new ChecklistVisual(completedColor, completedFontStyle, completedIconVisible, completedIcon);
                default:
                    return new ChecklistVisual(pendingColor, pendingFontStyle, pendingIconVisible, pendingIcon);
            }
        }
    }
}
