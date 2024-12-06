using UnityEngine;

namespace Tomino.Model
{
    public static class TextID
    {
        public const string NextPiece = "next";
        public const string Score = "score";
        public const string Lines = "lines";
        public const string Level = "level";
        public const string GameFinished = "game-finished";
        public const string GamePaused = "game-paused";
        public const string PlayAgain = "play-again";
        public const string Resume = "resume";
        public const string NewGame = "new-game";
        public const string Settings = "settings";
        public const string Music = "music";
        public const string ScreenButtons = "screen-buttons";
        public const string Theme = "theme";
        public const string DefaultTheme = "theme-default";
        public const string AutumnTheme = "theme-autumn";
        public const string SummerTheme = "theme-summer";
        public const string TealTheme = "theme-teal";
        public const string Close = "close";
        public const string Language = "language";
    }

    [CreateAssetMenu(fileName = "Localization", menuName = "Tomino/Localization", order = 3)]
    public class Localization : ScriptableObject
    {
        public SystemLanguage currentLanguage = SystemLanguage.English;

        private readonly string[] englishTexts = {
            "NEXT",
            "SCORE",
            "LINES",
            "LEVEL",
            "GAME FINISHED",
            "GAME PAUSED",
            "PLAY AGAIN",
            "RESUME",
            "NEW GAME",
            "SETTINGS",
            "MUSIC",
            "SCREEN BUTTONS",
            "THEME",
            "DEFAULT",
            "AUTUMN",
            "SUMMER",
            "TEAL",
            "CLOSE"
        };

        private readonly string[] chineseTexts = {
            "下一个",
            "分数",
            "行数",
            "等级",
            "游戏结束",
            "游戏暂停",
            "再玩一次",
            "继续",
            "新游戏",
            "设置",
            "音乐",
            "屏幕按钮",
            "主题",
            "默认",
            "秋季",
            "夏季",
            "青色",
            "关闭"
        };

        private readonly string[] spanishTexts = {
            "SIGUIENTE",
            "PUNTOS",
            "LÍNEAS",
            "NIVEL",
            "JUEGO TERMINADO",
            "JUEGO PAUSADO",
            "JUGAR DE NUEVO",
            "CONTINUAR",
            "NUEVO JUEGO",
            "AJUSTES",
            "MÚSICA",
            "BOTONES EN PANTALLA",
            "TEMA",
            "PREDETERMINADO",
            "OTOÑO",
            "VERANO",
            "TURQUESA",
            "CERRAR"
        };

        public string GetLocalizedTextForID(string textID)
        {
            int index = GetTextIndex(textID);
            if (index == -1) return "<null>";

            return currentLanguage switch
            {
                SystemLanguage.Chinese or 
                SystemLanguage.ChineseSimplified or 
                SystemLanguage.ChineseTraditional => chineseTexts[index],
                
                SystemLanguage.Spanish => spanishTexts[index],
                
                _ => englishTexts[index]
            };
        }

        private int GetTextIndex(string textID)
        {
            return textID switch
            {
                TextID.NextPiece => 0,
                TextID.Score => 1,
                TextID.Lines => 2,
                TextID.Level => 3,
                TextID.GameFinished => 4,
                TextID.GamePaused => 5,
                TextID.PlayAgain => 6,
                TextID.Resume => 7,
                TextID.NewGame => 8,
                TextID.Settings => 9,
                TextID.Music => 10,
                TextID.ScreenButtons => 11,
                TextID.Theme => 12,
                TextID.DefaultTheme => 13,
                TextID.AutumnTheme => 14,
                TextID.SummerTheme => 15,
                TextID.TealTheme => 16,
                TextID.Close => 17,
                _ => -1
            };
        }

        private void OnEnable()
        {
            currentLanguage = Application.systemLanguage;
        }
    }
}
