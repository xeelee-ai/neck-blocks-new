using Tomino.Model;
using UnityEngine;
using Text = UnityEngine.UI.Text;

namespace Tomino.View
{
    [ExecuteAlways]
    public class TextView : MonoBehaviour
    {
        public ThemeProvider themeProvider;
        public LocalizationProvider localizationProvider;
        public Text text;
        public ThemeColorName colorName;
        public string textID;

        private void Awake()
        {
            SetFont();
        }

        private void Update()
        {
            text.color = themeProvider.currentTheme.GetColor(colorName);

            if (!string.IsNullOrEmpty(textID))
            {
                text.text = localizationProvider.currentLocalization.GetLocalizedTextForID(textID);
            }
        }

        private void SetFont()
        {
            // 在 iOS 上使用系统字体
            #if UNITY_IOS
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            #endif
        }

        private void OnValidate()
        {
            SetFont();
        }
    }
}
