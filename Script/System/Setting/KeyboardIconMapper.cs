using System;
using System.Collections.Generic;
using UnityEngine;

public static class KeyboardIconMapper
{
    // Dictionary lưu trữ: Tên phím (Key) -> Index trong Sprite Asset
    public static readonly Dictionary<string, int> KeyToIndex = new Dictionary<string, int>
    {
        // Hàng 1 (8-19)
        { "0", 8 }, { "A", 9 }, { "K", 10 }, { "U", 11 }, { "LeftCtrl", 12 }, { "LeftAlt", 13 }, { "Semicolon", 14 }, { "F8", 15 }, { "Comma", 16 }, { "Period", 17 }, { "MouseLeft", 18 }, { "ZR", 19 },

        // Hàng 2 (28-39)
        { "1", 28 }, { "B", 29 }, { "L", 30 }, { "V", 31 }, { "Return", 32 }, { "Backslash", 33 }, { "Exclamation", 34 }, { "F9", 35 }, { "Underscore", 36 }, { "Tilde", 37 }, { "MouseRight", 38 }, { "Plus", 39 },

        // Hàng 3 (48-59)
        { "2", 48 }, { "C", 49 }, { "M", 50 }, { "W", 51 }, { "Equals", 52 }, { "At", 53 }, { "Comma_Alt", 54 }, { "F10", 55 }, { "Minus", 56 }, { "Tab", 57 }, { "MouseMiddle", 58 }, { "DpadUp", 59 },

        // Hàng 4 (68-79)
        { "3", 68 }, { "D", 69 }, { "N", 70 }, { "X", 71 }, { "End", 72 }, { "Asterisk", 73 }, { "F1", 74 }, { "F11", 75 }, { "Hash", 76 }, { "Space", 77 }, { "Mouse4", 78 }, { "DpadLeft", 79 },

        // Hàng 5 (88-99)
        { "4", 88 }, { "E", 89 }, { "O", 90 }, { "Y", 91 }, { "Quote", 92 }, { "UpArrow", 93 }, { "F2", 94 }, { "F12", 95 }, { "PageDown", 96 }, { "Slash", 97 }, { "Mouse5", 98 }, { "DpadRight", 99 },

        // Hàng 6 (108-119)
        { "5", 108 }, { "F", 109 }, { "P", 110 }, { "Z", 111 }, { "Dollar", 112 }, { "RightArrow", 113 }, { "F3", 114 }, { "Windows", 115 }, { "PageUp", 116 }, { "Quote_Alt", 117 }, { "Plus_Alt", 118 }, { "DpadDown", 119 },
        
        // Hàng 7 (128-139)
        { "6", 128 }, { "G", 129 }, { "Q", 130 }, { "LeftBracket", 131 }, { "Delete", 132 }, { "LeftArrow", 133 }, { "F4", 134 }, { "BackQuote", 135 }, { "LeftParen", 136 }, { "LeftShift", 137 }, { "NumpadMinus", 138 }, { "DpadCenter_1", 139 },

        // Hàng 8 (148-159)
        { "7", 148 }, { "H", 149 }, { "R", 150 }, { "CapsLock", 151 }, { "RightCurly", 152 }, { "DownArrow", 153 }, { "F5", 154 }, { "Greater", 155 }, { "RightParen", 156 }, { "Semicolon_Alt", 157 }, { "LStickClick", 158 }, { "DpadCenter_2", 159 },

        // Hàng 9 (168-179)
        { "8", 168 }, { "I", 169 }, { "S", 170 }, { "RightBracket", 171 }, { "LeftCurly", 172 }, { "Ampersand", 173 }, { "F6", 174 }, { "Home", 175 }, { "Percent", 176 }, { "Question", 177 }, { "RStickClick", 178 }, { "SR", 179 },

        // Hàng 10 (Ví dụ hàng cuối 188-199)
        { "9", 188 }, { "J", 189 }, { "T", 190 }, { "Caret", 191 }, { "Escape", 192 }, { "Backspace", 193 }, { "F7", 194 }, { "Insert", 195 },{ "Plus_Main", 196 }, { "Pipe", 197 }, { "ZL", 198 }, { "SL", 199 }
    };

    public static string GetSpriteTag(string keyName)
    {
        if (KeyToIndex.TryGetValue(keyName, out int index))
        {
            return $"<sprite={index}>";
        }
        return keyName; // Trả về chữ thường nếu không tìm thấy Index
    }

    private static readonly Dictionary<string, string> ControlPathToKeyName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "<Keyboard>/w", "W" },
        { "<Keyboard>/a", "A" },
        { "<Keyboard>/s", "S" },
        { "<Keyboard>/d", "D" },
        { "<Keyboard>/j", "J" },
        { "<Keyboard>/space", "Space" },
        { "<Keyboard>/leftShift", "LeftShift" },
        { "<Keyboard>/upArrow", "UpArrow" },
        { "<Keyboard>/downArrow", "DownArrow" },
        { "<Keyboard>/leftArrow", "LeftArrow" },
        { "<Keyboard>/rightArrow", "RightArrow" },
        { "<Mouse>/leftButton", "MouseLeft" },
        { "<Mouse>/rightButton", "MouseRight" },
        { "<Mouse>/middleButton", "MouseMiddle" }
    };

    private static readonly Dictionary<string, string> KeyNameToControlPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "W", "<Keyboard>/w" },
        { "A", "<Keyboard>/a" },
        { "S", "<Keyboard>/s" },
        { "D", "<Keyboard>/d" },
        { "J", "<Keyboard>/j" },
        { "Space", "<Keyboard>/space" },
        { "LeftShift", "<Keyboard>/leftShift" },
        { "UpArrow", "<Keyboard>/upArrow" },
        { "DownArrow", "<Keyboard>/downArrow" },
        { "LeftArrow", "<Keyboard>/leftArrow" },
        { "RightArrow", "<Keyboard>/rightArrow" },
        { "MouseLeft", "<Mouse>/leftButton" },
        { "MouseRight", "<Mouse>/rightButton" },
        { "MouseMiddle", "<Mouse>/middleButton" }
    };

    private static readonly Dictionary<string, string> KeyboardControlNameToKeyName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "space", "Space" },
        { "upArrow", "UpArrow" },
        { "downArrow", "DownArrow" },
        { "leftArrow", "LeftArrow" },
        { "rightArrow", "RightArrow" },
        { "leftShift", "LeftShift" },
        { "escape", "Escape" },
        { "tab", "Tab" },
        { "backspace", "Backspace" },
        { "insert", "Insert" },
        { "delete", "Delete" },
        { "home", "Home" },
        { "end", "End" },
        { "pageUp", "PageUp" },
        { "pageDown", "PageDown" },
        { "capsLock", "CapsLock" },
        { "backquote", "BackQuote" },
        { "quote", "Quote" },
        { "semicolon", "Semicolon" },
        { "comma", "Comma" },
        { "period", "Period" },
        { "slash", "Slash" },
        { "backslash", "Backslash" },
        { "minus", "Minus" },
        { "equals", "Equals" },
        { "leftBracket", "LeftBracket" },
        { "rightBracket", "RightBracket" }
    };

    public static string GetKeyNameFromControlPath(string controlPath)
    {
        if (string.IsNullOrWhiteSpace(controlPath))
            return string.Empty;

        string normalizedPath = controlPath.Trim();
        if (ControlPathToKeyName.TryGetValue(normalizedPath, out string keyName))
            return keyName;

        int slashIndex = normalizedPath.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < normalizedPath.Length - 1)
        {
            string controlName = normalizedPath.Substring(slashIndex + 1);

            // Keyboard alpha key: <Keyboard>/x -> X
            if (normalizedPath.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase)
                && controlName.Length == 1
                && char.IsLetter(controlName[0]))
            {
                return char.ToUpperInvariant(controlName[0]).ToString();
            }

            // Keyboard numeric key: <Keyboard>/1 -> 1
            if (normalizedPath.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase)
                && controlName.Length == 1
                && char.IsDigit(controlName[0]))
            {
                return controlName;
            }

            if (normalizedPath.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase))
            {
                if (controlName.Equals("leftButton", StringComparison.OrdinalIgnoreCase))
                    return "MouseLeft";
                if (controlName.Equals("rightButton", StringComparison.OrdinalIgnoreCase))
                    return "MouseRight";
                if (controlName.Equals("middleButton", StringComparison.OrdinalIgnoreCase))
                    return "MouseMiddle";
            }

            if (normalizedPath.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase))
            {
                if (KeyboardControlNameToKeyName.TryGetValue(controlName, out string mappedKeyName))
                    return mappedKeyName;

                if (controlName.Length >= 2
                    && (controlName[0] == 'f' || controlName[0] == 'F')
                    && int.TryParse(controlName.Substring(1), out int functionIndex)
                    && functionIndex >= 1 && functionIndex <= 24)
                {
                    return $"F{functionIndex}";
                }
            }
        }

        return normalizedPath;
    }

    public static bool TryGetControlPathFromKeyName(string keyName, out string controlPath)
    {
        controlPath = null;
        if (string.IsNullOrWhiteSpace(keyName))
            return false;

        string normalizedKeyName = keyName.Trim();
        if (KeyNameToControlPath.TryGetValue(normalizedKeyName, out controlPath))
            return true;

        if (normalizedKeyName.Length == 1 && char.IsLetter(normalizedKeyName[0]))
        {
            controlPath = $"<Keyboard>/{char.ToLowerInvariant(normalizedKeyName[0])}";
            return true;
        }

        if (normalizedKeyName.Length == 1 && char.IsDigit(normalizedKeyName[0]))
        {
            controlPath = $"<Keyboard>/{normalizedKeyName}";
            return true;
        }

        if (normalizedKeyName.Length >= 2
            && (normalizedKeyName[0] == 'f' || normalizedKeyName[0] == 'F')
            && int.TryParse(normalizedKeyName.Substring(1), out int functionIndex)
            && functionIndex >= 1 && functionIndex <= 24)
        {
            controlPath = $"<Keyboard>/f{functionIndex}";
            return true;
        }

        return false;
    }
}