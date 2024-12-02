using UnityEngine;

namespace Tomino.Model
{
    public static class BlockColors
    {
        private static readonly Color[] Colors = {
            new Color(1.0f, 0.85f, 0.0f),  // O - 黄色
            new Color(0.8f, 0.0f, 0.8f),   // T - 紫色
            new Color(0.0f, 0.8f, 0.0f),   // S - 绿色
            new Color(0.8f, 0.0f, 0.0f),   // Z - 红色
            new Color(0.0f, 0.0f, 0.8f),   // J - 蓝色
            new Color(1.0f, 0.5f, 0.0f),   // L - 橙色
            new Color(0.0f, 0.8f, 0.8f)    // I - 青色
        };

        public static Color GetColorForPieceType(PieceType type)
        {
            return Colors[(int)type];
        }

        public static Color GetRandomColor()
        {
            return Colors[Random.Range(0, Colors.Length)];
        }
    }
} 