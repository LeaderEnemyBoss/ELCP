using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Amplitude;
using UnityEngine;

public class AgeFont : ScriptableObject
{
	public AgeFont.Character[] Characters
	{
		get
		{
			return this.characters;
		}
		set
		{
			this.characters = value;
		}
	}

	public char[] CharacterSet
	{
		get
		{
			return this.characterSet.ToCharArray();
		}
		set
		{
			this.characterSet = new string(value);
		}
	}

	public Font Font
	{
		get
		{
			return this.font;
		}
		set
		{
			this.font = value;
		}
	}

	public string FaceStyle
	{
		get
		{
			return this.faceStyle;
		}
		set
		{
			this.faceStyle = value;
		}
	}

	public AgeFont HighdefAlternate
	{
		get
		{
			return this.highdefAlternate;
		}
		set
		{
			this.highdefAlternate = value;
		}
	}

	public int FontSize
	{
		get
		{
			return this.fontSize;
		}
		set
		{
			this.fontSize = value;
		}
	}

	public bool UsePixelMetrics
	{
		get
		{
			return this.usePixelMetrics;
		}
		set
		{
			this.usePixelMetrics = value;
		}
	}

	public Material Material
	{
		get
		{
			return this.material;
		}
		set
		{
			this.material = value;
		}
	}

	public AgeFont.SpecialCharacter[] SpecialCharacters
	{
		get
		{
			return this.specialCharacters;
		}
		set
		{
			this.specialCharacters = value;
		}
	}

	public float LineHeight
	{
		get
		{
			return this.lineHeight;
		}
		set
		{
			this.lineHeight = value;
		}
	}

	public float Ascender
	{
		get
		{
			return this.ascender;
		}
		set
		{
			this.ascender = value;
		}
	}

	public float Descender
	{
		get
		{
			return this.descender;
		}
		set
		{
			this.descender = value;
		}
	}

	public bool Outline
	{
		get
		{
			return this.outline;
		}
		set
		{
			this.outline = value;
		}
	}

	public Color OutlineColor
	{
		get
		{
			return this.outlineColor;
		}
		set
		{
			this.outlineColor = value;
		}
	}

	public int OutlineGradient
	{
		get
		{
			return this.outlineGradient;
		}
		set
		{
			this.outlineGradient = value;
		}
	}

	public bool DistanceField
	{
		get
		{
			return this.distanceField;
		}
		set
		{
			this.distanceField = value;
		}
	}

	public int DistanceFieldSize
	{
		get
		{
			return this.distanceFieldSize;
		}
		set
		{
			this.distanceFieldSize = value;
		}
	}

	public bool IncorporateToAtlas
	{
		get
		{
			return this.incorporateToAtlas;
		}
		set
		{
			this.incorporateToAtlas = value;
		}
	}

	public bool OnlyAlphaChannel
	{
		get
		{
			return this.onlyAlphaChannel;
		}
		set
		{
			this.onlyAlphaChannel = value;
		}
	}

	public AgeFont.KerningPair[] KerningPairs
	{
		get
		{
			return this.kerningPairs;
		}
		set
		{
			this.kerningPairs = value;
			this.CreateKerningLookup();
		}
	}

	public AgeFont.KerningMode KerningUsage
	{
		get
		{
			return this.kerningUsage;
		}
		set
		{
			this.kerningUsage = value;
		}
	}

	public bool HasKerningInfo
	{
		get
		{
			return this.kerningUsage != AgeFont.KerningMode.Disabled && this.kerningPairs != null && this.kerningPairs.Length > 0;
		}
	}

	public FreeTypeFont FreeTypeFont
	{
		get
		{
			return this.freeTypeFont;
		}
	}

	public bool DynamicGlyphAltassing
	{
		get
		{
			return this.dynamicGlyphAltassing;
		}
		set
		{
			this.dynamicGlyphAltassing = value;
		}
	}

	public AgeFont[] FallbackFonts
	{
		get
		{
			return this.fallbackFonts;
		}
		set
		{
			this.fallbackFonts = value;
		}
	}

	public static char RaiseChar(char charCode)
	{
		for (int i = 0; i < AgeFont.RaisableChars.Length; i++)
		{
			if (AgeFont.RaisableChars[i] == charCode)
			{
				return AgeFont.RaisedCharacs[i];
			}
		}
		return charCode;
	}

	public void BreakdownToAtlas(AgeAtlas atlas)
	{
		Texture2D texture2D = (Texture2D)this.Material.mainTexture;
		for (int i = 0; i < this.characters.Length; i++)
		{
			int x = Mathf.RoundToInt(this.characters[i].TextureCoordinates.x * (float)texture2D.width);
			int y = Mathf.RoundToInt(this.characters[i].TextureCoordinates.y * (float)texture2D.height);
			int num = Mathf.RoundToInt(this.characters[i].TextureCoordinates.width * (float)texture2D.width);
			int num2 = Mathf.RoundToInt(this.characters[i].TextureCoordinates.height * (float)texture2D.height);
			if (num <= 0)
			{
				num = 1;
			}
			if (num2 <= 0)
			{
				num2 = 1;
			}
			Texture2D texture2D2 = new Texture2D(num, num2, TextureFormat.ARGB32, false);
			texture2D2.SetPixels(texture2D.GetPixels(x, y, num, num2));
			texture2D2.name = (base.name + this.characters[i].Charcode).ToUpper();
			texture2D2.Apply();
			atlas.AddTexture(texture2D2, base.name);
		}
	}

	public float ComputeTextWidth(string text, bool forcedCaps = false, bool makeClean = false)
	{
		float num = 0f;
		this.CreateCharMap(false);
		if (makeClean)
		{
			AgeUtils.CleanLine(text, ref AgePrimitiveLabel.CleanLine);
			text = AgePrimitiveLabel.CleanLine.ToString();
		}
		for (int i = 0; i < text.Length; i++)
		{
			char charCode = text[i];
			if (forcedCaps)
			{
				charCode = AgeFont.RaiseChar(charCode);
			}
			AgeFont.Character character = null;
			if (this.TryGetCharacter(charCode, this.fontSize, out character))
			{
				num += character.Advance;
				if (this.HasKerningInfo && i + 1 < text.Length)
				{
					char c = text[i + 1];
					if (forcedCaps && c >= 'a' && c <= 'z')
					{
						c = (char)((int)c + -32);
					}
					num += this.GetKerningOffset(text[i], c);
				}
			}
		}
		return num;
	}

	public float ComputeTextWidth(StringBuilder text, bool forcedCaps = false)
	{
		float num = 0f;
		this.CreateCharMap(false);
		for (int i = 0; i < text.Length; i++)
		{
			char charCode = text[i];
			if (forcedCaps)
			{
				charCode = AgeFont.RaiseChar(charCode);
			}
			AgeFont.Character character = null;
			if (this.TryGetCharacter(charCode, this.fontSize, out character))
			{
				num += character.Advance;
				if (this.HasKerningInfo && i + 1 < text.Length)
				{
					char c = text[i + 1];
					if (forcedCaps)
					{
						c = AgeFont.RaiseChar(c);
					}
					num += this.GetKerningOffset(text[i], c);
				}
			}
		}
		return num;
	}

	public void FinalizeForAtlas(AgeAtlas atlas)
	{
		if (this.IncorporateToAtlas)
		{
			this.RuntimeMaterial = atlas.AtlasMaterial;
			string text = string.Empty;
			for (int i = 0; i < this.characters.Length; i++)
			{
				text = (base.name + this.characters[i].Charcode).ToUpper();
				Rect rect = atlas.TextureMap[atlas.TextureLookup[text]];
				this.characters[i].RuntimeTextureCoordinates.x = rect.x;
				this.characters[i].RuntimeTextureCoordinates.y = rect.y;
				this.characters[i].RuntimeTextureCoordinates.width = rect.width;
				this.characters[i].RuntimeTextureCoordinates.height = rect.height;
				atlas.DeleteTexture(text);
			}
		}
	}

	public int GetCharIndexInLineAtPosition(string text, float positionX, bool forcedCaps = false)
	{
		float num = 0f;
		this.CreateCharMap(false);
		for (int i = 0; i < text.Length; i++)
		{
			char charCode = text[i];
			if (forcedCaps)
			{
				charCode = AgeFont.RaiseChar(charCode);
			}
			AgeFont.Character character = null;
			if (this.TryGetCharacter(charCode, this.fontSize, out character))
			{
				num += character.Advance;
				if (this.HasKerningInfo && i + 1 < text.Length)
				{
					char c = text[i + 1];
					if (forcedCaps && c >= 'a' && c <= 'z')
					{
						c = (char)((int)c + -32);
					}
					num += this.GetKerningOffset(text[i], c);
				}
			}
			if (num > positionX)
			{
				return i;
			}
		}
		return text.Length;
	}

	public bool GetCharInfo(char charcode, out Vector2 dimension, out Vector2 offset, out Rect textureCoordinates, out float advance)
	{
		return this.GetCharInfo(charcode, '\0', out dimension, out offset, out textureCoordinates, out advance);
	}

	public bool GetCharInfo(char charcode, char nextCharCode, out Vector2 dimension, out Vector2 offset, out Rect textureCoordinates, out float advance)
	{
		this.CreateCharMap(false);
		AgeFont.Character character;
		if (!this.TryGetCharacter(charcode, this.fontSize, out character))
		{
			dimension = Vector2.zero;
			offset = Vector2.zero;
			textureCoordinates = Rect.MinMaxRect(0f, 0f, 0f, 0f);
			advance = 0f;
			return false;
		}
		dimension = character.Dimension;
		offset = character.Offset;
		advance = character.Advance;
		if (this.IncorporateToAtlas && Application.isPlaying)
		{
			textureCoordinates = character.RuntimeTextureCoordinates;
		}
		else
		{
			textureCoordinates = character.TextureCoordinates;
		}
		if (nextCharCode != '\0')
		{
			advance += this.GetKerningOffset(charcode, nextCharCode);
		}
		return true;
	}

	public Material GetRenderMaterial()
	{
		Material runtimeMaterial;
		if (this.IncorporateToAtlas && Application.isPlaying)
		{
			runtimeMaterial = this.RuntimeMaterial;
		}
		else
		{
			runtimeMaterial = this.Material;
		}
		FontAtlasRenderer fontAtlasRenderer = AgeManager.Instance.FontAtlasRenderer;
		runtimeMaterial.SetTexture("_DynamicAtlasTex", fontAtlasRenderer.Texture());
		return runtimeMaterial;
	}

	public void CreateCharMap(bool force = false)
	{
		if (this.charMap == null || force)
		{
			this.charMap = new Dictionary<char, AgeFont.Character>();
			for (int i = 0; i < this.characters.Length; i++)
			{
				this.charMap.Add((char)this.characters[i].Charcode, this.characters[i]);
			}
		}
	}

	protected void OnEnable()
	{
		if (this.DynamicGlyphAltassing)
		{
			Diagnostics.Assert(this.freeTypeFont == null);
			this.freeTypeFont = new FreeTypeFont();
			if (this.embeddedFontFile != null)
			{
				this.freeTypeFont.LoadFontFromMemory(this.embeddedFontFile, this.font.fontNames[0]);
			}
		}
	}

	protected void OnDisable()
	{
		if (this.DynamicGlyphAltassing)
		{
			Diagnostics.Assert(this.freeTypeFont != null);
			this.freeTypeFont.ReleaseFont();
			this.freeTypeFont.Release();
			this.freeTypeFont = null;
		}
	}

	private bool TryGetCharacter(char charCode, int fontSize, out AgeFont.Character character)
	{
		this.CreateCharMap(false);
		bool flag = this.charMap.TryGetValue(charCode, out character);
		if (!flag)
		{
			if (this.dynamicGlyphAltassing)
			{
				bool flag2 = this.freeTypeFont.IsGlyphDefined((uint)charCode);
				if (flag2)
				{
					FreeTypeFont.GlyphPositioningInformation glyphPositioningInformation;
					this.freeTypeFont.GetGlyphPositioningInformation((uint)charCode, (uint)fontSize, out glyphPositioningInformation);
					AgeFont.Character character2 = new AgeFont.Character();
					character2.Charcode = (int)charCode;
					character2.Advance = glyphPositioningInformation.Advance;
					character2.Dimension = new Vector2(glyphPositioningInformation.Width, glyphPositioningInformation.Height);
					character2.Offset = new Vector2(glyphPositioningInformation.BearingX, -glyphPositioningInformation.BearingY);
					FontAtlasRenderer fontAtlasRenderer = AgeManager.Instance.FontAtlasRenderer;
					Rect orCreateInAtlas = fontAtlasRenderer.GetOrCreateInAtlas((uint)charCode, this, (uint)fontSize);
					float num = 128f;
					orCreateInAtlas.x += num;
					orCreateInAtlas.y += num;
					character2.RuntimeTextureCoordinates = orCreateInAtlas;
					character2.TextureCoordinates = character2.RuntimeTextureCoordinates;
					character = character2;
					return true;
				}
			}
			int num2 = this.fallbackFonts.Length;
			for (int i = 0; i < num2; i++)
			{
				AgeFont ageFont = this.fallbackFonts[i];
				if (ageFont.TryGetCharacter(charCode, fontSize, out character))
				{
					return true;
				}
			}
			return false;
		}
		return flag;
	}

	private void CreateKerningLookup()
	{
		if (this.HasKerningInfo)
		{
			this.kerningLookup = new Dictionary<char, Dictionary<char, float>>();
			for (int i = 0; i < this.kerningPairs.Length; i++)
			{
				char c = (char)this.kerningPairs[i].LeftChar;
				if (!this.kerningLookup.ContainsKey(c))
				{
					this.kerningLookup.Add(c, new Dictionary<char, float>());
				}
				char c2 = (char)this.kerningPairs[i].RightChar;
				if (!this.kerningLookup[c].ContainsKey(c2))
				{
					this.kerningLookup[c][c2] = this.kerningPairs[i].XadvanceOffset;
				}
				else
				{
					Diagnostics.LogWarning(string.Concat(new object[]
					{
						"Kerning pair <",
						c,
						"> <",
						c2,
						"> already exists"
					}));
				}
			}
		}
	}

	private float GetKerningOffset(char leftChar, char rightChar)
	{
		if (this.HasKerningInfo)
		{
			if (this.kerningLookup == null)
			{
				this.CreateKerningLookup();
			}
			if (this.kerningLookup != null && this.kerningLookup.ContainsKey(leftChar) && this.kerningLookup[leftChar].ContainsKey(rightChar))
			{
				return this.kerningLookup[leftChar][rightChar];
			}
		}
		return 0f;
	}

	private static void Init()
	{
		if (!AgeFont._inited)
		{
			if (File.Exists(AgeFont._log))
			{
				File.Delete(AgeFont._log);
			}
			AgeFont.Log("Initialization");
			StringBuilder stringBuilder = new StringBuilder().AppendLine();
			foreach (string str in Font.GetOSInstalledFontNames())
			{
				stringBuilder.AppendLine("Found installed font: " + str);
			}
			AgeFont.Log(stringBuilder.ToString());
			stringBuilder = new StringBuilder().AppendLine();
			foreach (string str2 in Environment.GetCommandLineArgs())
			{
				stringBuilder.AppendLine("Fount command line argument: " + str2);
			}
			AgeFont.Log(stringBuilder.ToString());
			AgeFont._inited = true;
		}
	}

	private static void Log(string message)
	{
	}

	public void Awake()
	{
		AgeFont.Init();
		try
		{
			if (!this._scaled)
			{
				string key = "--fontScale=";
				string text = Environment.GetCommandLineArgs().FirstOrDefault((string a) => a != null && a.StartsWith(key));
				float s = 1.5f;
				if (text != null && float.TryParse(text.Substring(key.Length), out s))
				{
					this.ScaleFont(s);
				}
			}
		}
		catch (Exception ex)
		{
			AgeFont.Log(base.name + " " + ex.Message);
		}
		if (this.HighdefAlternate != null && this.HighdefAlternate != this)
		{
			this.HighdefAlternate.Awake();
		}
		if (this.FallbackFonts != null)
		{
			AgeFont[] array = this.FallbackFonts;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Awake();
			}
		}
	}

	public void ScaleFont(float s)
	{
		if (!this._scaled)
		{
			this._scaled = true;
			this.fontSize = (int)((float)this.fontSize * s);
			this.lineHeight *= s;
			this.ascender *= s;
			this.descender *= s;
			for (int i = 0; i < this.characters.Length; i++)
			{
				AgeFont.Character character = this.characters[i];
				character.Advance *= s;
				character.Offset *= s;
				character.Dimension *= s;
				this.characters[i] = character;
			}
			AgeFont.Log(string.Concat(new string[]
			{
				"Font ",
				base.name,
				"(",
				this.font.name,
				") is scaled for ",
				s.ToString()
			}));
		}
	}

	public void SetupCustomELCPScaling(float scale)
	{
		if (this.customELCPscaling == scale)
		{
			return;
		}
		if (this.customELCPscaling > 0f)
		{
			this.ScaleFont(1f / this.customELCPscaling);
		}
		this.customELCPscaling = scale;
		this.ScaleFont(this.customELCPscaling);
	}

	public static string RaisableChars = "abcdefghijklmnopqrstuvwxyzáàâäéèêëïíìîóòôöûúùüçёœїąćęłńñóśźżабвгдеёжзийклмнопрстуфхцчшщъыьэюяєіїґ";

	public static string RaisedCharacs = "ABCDEFGHIJKLMNOPQRSTUVWXYZÁÀÂÄÉÈÊËÏÍÌÎÓÒÔÖÛÚÙÜÇЁŒЇĄĆĘŁŃÑÓŚŹŻАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯЄІЇҐ";

	public Material RuntimeMaterial;

	[SerializeField]
	private AgeFont.Character[] characters;

	[SerializeField]
	private string characterSet;

	[SerializeField]
	private Font font;

	[SerializeField]
	private string faceStyle = string.Empty;

	[SerializeField]
	private int fontSize;

	[SerializeField]
	private bool usePixelMetrics;

	[SerializeField]
	private Material material;

	[SerializeField]
	private AgeFont.SpecialCharacter[] specialCharacters;

	[SerializeField]
	private AgeFont highdefAlternate;

	[SerializeField]
	private float lineHeight;

	[SerializeField]
	private float descender;

	[SerializeField]
	private float ascender;

	[SerializeField]
	private bool outline;

	[SerializeField]
	private Color outlineColor = Color.white;

	[SerializeField]
	private int outlineGradient = 1;

	[SerializeField]
	private bool distanceField;

	[SerializeField]
	private int distanceFieldSize;

	[SerializeField]
	private bool incorporateToAtlas;

	[SerializeField]
	private bool onlyAlphaChannel;

	[SerializeField]
	private AgeFont.KerningMode kerningUsage;

	[SerializeField]
	private AgeFont.KerningPair[] kerningPairs;

	[SerializeField]
	private AgeFont.AdditionalCharacterSet[] additionalCharacterSets;

	private Dictionary<char, AgeFont.Character> charMap;

	private Dictionary<char, Dictionary<char, float>> kerningLookup;

	[NonSerialized]
	private FreeTypeFont freeTypeFont;

	[SerializeField]
	private byte[] embeddedFontFile;

	[SerializeField]
	private bool dynamicGlyphAltassing;

	[SerializeField]
	private AgeFont[] fallbackFonts;

	private bool _scaled;

	private static bool _inited;

	private static string _log = "log-fonts.txt";

	private float customELCPscaling;

	public enum KerningMode
	{
		Disabled,
		FreeTypeDefault,
		FreeTypeSubPixel,
		GDI
	}

	[Serializable]
	public class Character : IComparable<AgeFont.Character>
	{
		public Character()
		{
		}

		public Character(AgeFont.Character other)
		{
			this.Charcode = other.Charcode;
			this.Dimension = other.Dimension;
			this.Advance = other.Advance;
			this.Offset = other.Offset;
			this.TextureCoordinates = other.TextureCoordinates;
			this.RuntimeTextureCoordinates = other.RuntimeTextureCoordinates;
			this.KerningEncodedData = other.KerningEncodedData;
		}

		int IComparable<AgeFont.Character>.CompareTo(AgeFont.Character other)
		{
			return other.Charcode - this.Charcode;
		}

		public static AgeFont.Character Zero = new AgeFont.Character
		{
			Charcode = 0,
			Advance = 0f,
			Offset = Vector2.zero,
			TextureCoordinates = default(Rect)
		};

		public int Charcode;

		public Vector2 Dimension;

		public float Advance;

		public Vector2 Offset;

		public Rect TextureCoordinates;

		public Rect RuntimeTextureCoordinates;

		public int[] KerningEncodedData;
	}

	[Serializable]
	public class SpecialCharacter
	{
		public int Charcode;

		public float Advance;

		public Vector2 Offset;

		public Texture2D Texture;

		public class SortByCharcode : IComparer<AgeFont.SpecialCharacter>
		{
			public int Compare(AgeFont.SpecialCharacter x, AgeFont.SpecialCharacter y)
			{
				return x.Charcode - y.Charcode;
			}
		}
	}

	[Serializable]
	public class AdditionalCharacterSet
	{
		public string CharacterSet;

		public Font Font;

		public AgeFont.KerningMode KerningUsage;

		public int FontSize;
	}

	[Serializable]
	public class KerningPair
	{
		public KerningPair(int left, int right, float offset)
		{
			this.LeftChar = left;
			this.RightChar = right;
			this.XadvanceOffset = offset;
		}

		public int LeftChar;

		public int RightChar;

		public float XadvanceOffset;
	}
}
