using UnityEngine;
using Tomino.View;

namespace Tomino.Model
{
    public static class BlockColors
    {
        private static Theme _currentTheme;

        public static void Initialize(Theme theme)
        {
            _currentTheme = theme;
        }

        public static Color GetColorForPieceType(PieceType type)
        {
            if (_currentTheme == null)
            {
                Debug.LogWarning("Theme not initialized in BlockColors!");
                return Color.white;
            }
            return _currentTheme.BlockColors[(int)type];
        }

        public static Color GetRandomColor()
        {
            if (_currentTheme == null)
            {
                Debug.LogWarning("Theme not initialized in BlockColors!");
                return Color.white;
            }
            return _currentTheme.BlockColors[Random.Range(0, _currentTheme.BlockColors.Length)];
        }
    }
} 