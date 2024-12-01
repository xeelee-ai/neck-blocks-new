using UnityEngine;

namespace Tomino.Model
{
    public static class BlockColors
    {
        // 更鲜艳的颜色配置
        public static readonly Color[] Colors = new Color[]
        {
            new Color(1.0f, 0.1f, 0.1f),      // 亮红色
            new Color(0.1f, 1.0f, 0.1f),      // 亮绿色
            new Color(0.1f, 0.1f, 1.0f),      // 亮蓝色
            new Color(1.0f, 1.0f, 0.1f),      // 亮黄色
            new Color(1.0f, 0.1f, 1.0f),      // 亮紫色
            new Color(0.1f, 1.0f, 1.0f),      // 亮青色
            new Color(1.0f, 0.5f, 0.0f)       // 亮橙色
        };

        public static Color GetRandomColor()
        {
            return Colors[Random.Range(0, Colors.Length)];
        }
    }
} 