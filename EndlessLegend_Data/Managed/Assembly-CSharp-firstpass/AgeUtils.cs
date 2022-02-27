using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amplitude;
using UnityEngine;

public class AgeUtils
{
	static AgeUtils()
	{
		string key = "--uiScale=";
		string text = Environment.GetCommandLineArgs().FirstOrDefault((string a) => a != null && a.StartsWith(key));
		float highDefinitionFactor;
		if (text != null && float.TryParse(text.Substring(key.Length), out highDefinitionFactor))
		{
			AgeUtils.HighDefinitionFactor = highDefinitionFactor;
		}
	}

	public static Rect ClipRectangle(Rect clipper, Rect clippee)
	{
		Rect result = Rect.MinMaxRect(-1f, -1f, -1f, -1f);
		if (clippee.xMax > clipper.xMin && clippee.xMin < clipper.xMax && clippee.yMax > clipper.yMin && clippee.yMin < clipper.yMax)
		{
			result.xMin = Mathf.Max(clippee.xMin, clipper.xMin);
			result.yMin = Mathf.Max(clippee.yMin, clipper.yMin);
			result.xMax = Mathf.Min(clippee.xMax, clipper.xMax);
			result.yMax = Mathf.Min(clippee.yMax, clipper.yMax);
		}
		return result;
	}

	public static float ComputeTexelOffset(int atlasWidth)
	{
		if (AgeUtils.ApplyTexelOffset && atlasWidth > 0)
		{
			return 0.5f / (float)atlasWidth;
		}
		return 0f;
	}

	public static float CurrentUpscaleFactor()
	{
		return (!AgeUtils.HighDefinition) ? 1f : AgeUtils.HighDefinitionFactor;
	}

	public static Rect PlaceRectAtMouse(Rect originalPosition, float offset)
	{
		originalPosition.x = Input.mousePosition.x + offset;
		originalPosition.y = (float)Screen.height - Input.mousePosition.y + offset;
		if (originalPosition.xMax > (float)Screen.width)
		{
			originalPosition.x = Input.mousePosition.x - originalPosition.width;
		}
		if (originalPosition.yMax > (float)Screen.height)
		{
			originalPosition.y = (float)Screen.height - Input.mousePosition.y - originalPosition.height;
		}
		return originalPosition;
	}

	public static bool IsColorFormatRevert(StringBuilder text)
	{
		bool result = true;
		for (int i = 0; i < "#REVERT#".Length; i++)
		{
			if (text[i] != "#REVERT#"[i])
			{
				result = false;
			}
		}
		return result;
	}

	public static bool HexaKeyToColor(string hexaKey, out Color color)
	{
		bool result = true;
		color = Color.white;
		bool flag = true;
		for (int i = 1; i < 7; i++)
		{
			if (AgeUtils.ValidHex.IndexOf(hexaKey[i]) < 0)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			color.r = (AgeUtils.ComputeHexa(hexaKey[1]) * 16f + AgeUtils.ComputeHexa(hexaKey[2])) / 255f;
			color.g = (AgeUtils.ComputeHexa(hexaKey[3]) * 16f + AgeUtils.ComputeHexa(hexaKey[4])) / 255f;
			color.b = (AgeUtils.ComputeHexa(hexaKey[5]) * 16f + AgeUtils.ComputeHexa(hexaKey[6])) / 255f;
		}
		else
		{
			result = false;
		}
		return result;
	}

	public static bool HexaKeyToColor(StringBuilder hexaKey, out Color color)
	{
		bool result = true;
		color = Color.white;
		bool flag = true;
		for (int i = 1; i < 7; i++)
		{
			if (AgeUtils.ValidHex.IndexOf(hexaKey[i]) < 0)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			color.r = (AgeUtils.ComputeHexa(hexaKey[1]) * 16f + AgeUtils.ComputeHexa(hexaKey[2])) / 255f;
			color.g = (AgeUtils.ComputeHexa(hexaKey[3]) * 16f + AgeUtils.ComputeHexa(hexaKey[4])) / 255f;
			color.b = (AgeUtils.ComputeHexa(hexaKey[5]) * 16f + AgeUtils.ComputeHexa(hexaKey[6])) / 255f;
		}
		else
		{
			result = false;
		}
		return result;
	}

	public static float ComputeHexa(char digit)
	{
		if (digit >= '0' && digit <= '9')
		{
			return (float)(digit - '0');
		}
		if (digit >= 'a' && digit <= 'f')
		{
			return 10f + (float)(digit - 'a');
		}
		if (digit >= 'A' && digit <= 'F')
		{
			return 10f + (float)(digit - 'A');
		}
		return 0f;
	}

	public static void ColorToHexaKey(Color color, out string hexaKey)
	{
		hexaKey = string.Empty + '#';
		hexaKey += ((int)(color.r * 255f)).ToString("X2");
		hexaKey += ((int)(color.g * 255f)).ToString("X2");
		hexaKey += ((int)(color.b * 255f)).ToString("X2");
		hexaKey += '#';
	}

	public static void ColorToHexaKey(Color color, ref StringBuilder hexaKey, bool reset = false)
	{
		if (reset)
		{
			hexaKey.Length = 0;
		}
		hexaKey.Append('#');
		hexaKey.Append(((int)(color.r * 255f)).ToString("X2"));
		hexaKey.Append(((int)(color.g * 255f)).ToString("X2"));
		hexaKey.Append(((int)(color.b * 255f)).ToString("X2"));
		hexaKey.Append('#');
	}

	public static void CleanLine(string formattedLine, ref StringBuilder cleanLine)
	{
		cleanLine.Length = 0;
		cleanLine.EnsureCapacity(formattedLine.Length);
		for (int i = 0; i < formattedLine.Length; i++)
		{
			if (formattedLine[i] == '#' && i < formattedLine.Length - 8 + 1 && formattedLine[i + 8 - 1] == '#')
			{
				i += 7;
			}
			else if (formattedLine[i] == '\\')
			{
				int num = formattedLine.IndexOf('\\', i + 1);
				if (num != -1 && num > i + 1)
				{
					char c = AgeUtils.ParseCharCode(formattedLine, i, num);
					if (c > '\0')
					{
						cleanLine.Append(c);
						i += num - i;
					}
					else
					{
						cleanLine.Append(formattedLine[i]);
					}
				}
				else
				{
					cleanLine.Append(formattedLine[i]);
				}
			}
			else
			{
				cleanLine.Append(formattedLine[i]);
			}
		}
	}

	public static char ParseCharCode(string formattedString, int startIndex, int endIndex)
	{
		int num;
		if (int.TryParse(formattedString.Substring(startIndex + 1, endIndex - startIndex - 1), out num))
		{
			return (char)num;
		}
		return '\0';
	}

	public static string ToRoman(int number)
	{
		if (number < 0 || number > 3999)
		{
			Diagnostics.LogError("Cannot convert number " + number + " to roman characters");
		}
		AgeUtils.line.Length = 0;
		while (number > 0)
		{
			if (number >= 1000)
			{
				AgeUtils.line.Append("M");
				number -= 1000;
			}
			else if (number >= 900)
			{
				AgeUtils.line.Append("CM");
				number -= 900;
			}
			else if (number >= 500)
			{
				AgeUtils.line.Append("D");
				number -= 500;
			}
			else if (number >= 400)
			{
				AgeUtils.line.Append("CD");
				number -= 400;
			}
			else if (number >= 100)
			{
				AgeUtils.line.Append("C");
				number -= 100;
			}
			else if (number >= 90)
			{
				AgeUtils.line.Append("XC");
				number -= 90;
			}
			else if (number >= 50)
			{
				AgeUtils.line.Append("L");
				number -= 50;
			}
			else if (number >= 40)
			{
				AgeUtils.line.Append("XL");
				number -= 40;
			}
			else if (number >= 10)
			{
				AgeUtils.line.Append("X");
				number -= 10;
			}
			else if (number >= 9)
			{
				AgeUtils.line.Append("IX");
				number -= 9;
			}
			else if (number >= 5)
			{
				AgeUtils.line.Append("V");
				number -= 5;
			}
			else if (number >= 4)
			{
				AgeUtils.line.Append("IV");
				number -= 4;
			}
			else if (number >= 1)
			{
				AgeUtils.line.Append("I");
				number--;
			}
		}
		return AgeUtils.line.ToString();
	}

	public static void TruncateString(string src, AgePrimitiveLabel label, out string dest, char truncateChar)
	{
		float width = label.AgeTransform.Width;
		if (width == 0f)
		{
			width = label.AgeTransform.Width;
		}
		AgeUtils.TruncateString(src, label.Font, width, out dest, truncateChar, label.ForceCaps);
	}

	public static void TruncateString(string src, AgeFont font, float maxWidth, out string dest, char truncateChar, bool forcedCaps = false)
	{
		dest = src;
		AgeUtils.CleanLine(dest, ref AgeUtils.line);
		float num = font.ComputeTextWidth(AgeUtils.line.ToString(), forcedCaps, false);
		while (num > maxWidth && AgeUtils.line.Length > 1)
		{
			dest = dest.Substring(0, dest.Length - 2) + truncateChar;
			AgeUtils.CleanLine(dest, ref AgeUtils.line);
			num = font.ComputeTextWidth(AgeUtils.line.ToString(), forcedCaps, false);
		}
	}

	public static void TruncateString(string src, int maxChars, out string dest, char truncateChar)
	{
		dest = src;
		AgeUtils.CleanLine(dest, ref AgeUtils.line);
		while (AgeUtils.line.Length > maxChars && AgeUtils.line.Length > 1)
		{
			dest = dest.Substring(0, dest.Length - 2) + truncateChar;
			AgeUtils.CleanLine(dest, ref AgeUtils.line);
		}
	}

	public static void TruncateStringWithSuffix(string src, string suffix, AgePrimitiveLabel label, out string dest, char truncateChar)
	{
		AgeUtils.TruncateStringWithSuffix(src, suffix, label, label.AgeTransform.Width, out dest, truncateChar, label.ForceCaps);
	}

	public static void TruncateStringWithSuffix(string src, string suffix, AgePrimitiveLabel label, float maxWidth, out string dest, char truncateChar, bool forcedCaps = false)
	{
		dest = src;
		AgeUtils.CleanLine(dest + suffix, ref AgeUtils.line);
		float num = label.Font.ComputeTextWidth(AgeUtils.line.ToString(), forcedCaps, false);
		while (num > maxWidth && AgeUtils.line.Length > 1)
		{
			dest = dest.Substring(0, dest.Length - 2) + truncateChar;
			AgeUtils.CleanLine(dest + suffix, ref AgeUtils.line);
			num = label.Font.ComputeTextWidth(AgeUtils.line.ToString(), forcedCaps, false);
		}
		dest += suffix;
	}

	public static void TruncateStringWithSubst(string srcKey, string match, string subst, AgePrimitiveLabel label, out string dest, char truncateChar)
	{
		dest = subst;
		AgeLocalizer.Instance.LocalizeString(srcKey, out AgeUtils.lineString);
		AgeUtils.lineString = AgeUtils.lineString.Replace(match, dest);
		AgeUtils.CleanLine(AgeUtils.lineString, ref AgeUtils.line);
		float num = label.Font.ComputeTextWidth(AgeUtils.line.ToString(), label.ForceCaps, false);
		while (num > label.AgeTransform.Width && dest.Length > 1)
		{
			dest = dest.Substring(0, dest.Length - 2) + truncateChar;
			AgeLocalizer.Instance.LocalizeString(srcKey, out AgeUtils.lineString);
			AgeUtils.lineString = AgeUtils.lineString.Replace(match, dest);
			AgeUtils.CleanLine(AgeUtils.lineString, ref AgeUtils.line);
			num = label.Font.ComputeTextWidth(AgeUtils.line.ToString(), label.ForceCaps, false);
		}
		dest = AgeUtils.lineString;
	}

	public static void TruncateStringWithSubst(string srcKey, string match, string subst, AgePrimitiveLabel label, float maxWidth, out string dest, char truncateChar)
	{
		dest = subst;
		AgeLocalizer.Instance.LocalizeString(srcKey, out AgeUtils.lineString);
		AgeUtils.lineString = AgeUtils.lineString.Replace(match, dest);
		AgeUtils.CleanLine(AgeUtils.lineString, ref AgeUtils.line);
		float num = label.Font.ComputeTextWidth(AgeUtils.line.ToString(), label.ForceCaps, false);
		while (num > maxWidth && dest.Length > 1)
		{
			dest = dest.Substring(0, dest.Length - 2) + truncateChar;
			AgeLocalizer.Instance.LocalizeString(srcKey, out AgeUtils.lineString);
			AgeUtils.lineString = AgeUtils.lineString.Replace(match, dest);
			AgeUtils.CleanLine(AgeUtils.lineString, ref AgeUtils.line);
			num = label.Font.ComputeTextWidth(AgeUtils.line.ToString(), label.ForceCaps, false);
		}
		dest = AgeUtils.lineString;
	}

	public const int ColorFormatLength = 8;

	public const char ColorFormatChar = '#';

	public const string ColorFormatRevert = "#REVERT#";

	public const char CharFormatSymbol = '\\';

	public static float ColorSwitchStandardDuration = 0.1f;

	public static bool HighDefinition = false;

	public static float HighDefinitionFactor = 1.5f;

	public static bool ApplyTexelOffset = true;

	public static string Format = new string('A', 2);

	public static StringBuilder FormatSB = new StringBuilder(2);

	public static string ValidHex = "0123456789ABCDEFabcdef";

	private static StringBuilder line = new StringBuilder(512);

	private static string lineString = string.Empty;

	public class NameComparer : IComparer<AgeTransform>
	{
		public int Compare(AgeTransform x, AgeTransform y)
		{
			if (x.name.CompareTo(y.name) < 0)
			{
				return -1;
			}
			if (x.name == y.name)
			{
				return 0;
			}
			return 1;
		}
	}

	public class SiblingIndexComparer : IComparer<AgeTransform>
	{
		public int Compare(AgeTransform x, AgeTransform y)
		{
			return x.transform.GetSiblingIndex().CompareTo(y.transform.GetSiblingIndex());
		}
	}
}
