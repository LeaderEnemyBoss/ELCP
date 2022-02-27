using System;

namespace Amplitude.WorldGenerator.World.Info
{
	public struct FIDS
	{
		public bool HasAnyNegative
		{
			get
			{
				return this.Food < 0 || this.Industry < 0 || this.Dust < 0 || this.Science < 0;
			}
		}

		public override string ToString()
		{
			return string.Format("FIDS {0} {1} {2} {3} | {4}", new object[]
			{
				this.Food,
				this.Industry,
				this.Dust,
				this.Science,
				this.GetTotalValue()
			});
		}

		public int Negativity
		{
			get
			{
				int num = 0;
				if (this.Food < 0)
				{
					num -= this.Food;
				}
				if (this.Industry < 0)
				{
					num -= this.Industry;
				}
				if (this.Dust < 0)
				{
					num -= this.Dust;
				}
				if (this.Science < 0)
				{
					num -= this.Science;
				}
				return num;
			}
		}

		public static FIDS operator +(FIDS a, FIDS b)
		{
			return new FIDS
			{
				Food = a.Food + b.Food,
				Industry = a.Industry + b.Industry,
				Dust = a.Dust + b.Dust,
				Science = a.Science + b.Science
			};
		}

		public static FIDS operator -(FIDS a, FIDS b)
		{
			return new FIDS
			{
				Food = a.Food - b.Food,
				Industry = a.Industry - b.Industry,
				Dust = a.Dust - b.Dust,
				Science = a.Science - b.Science
			};
		}

		public static int operator *(FIDS a, FIDS b)
		{
			return 0 + a.Food * b.Food + a.Industry * b.Industry + a.Dust * b.Dust + a.Science * b.Science;
		}

		public void Minus(int i = 1)
		{
			this.Food -= i;
			this.Industry -= i;
			this.Dust -= i;
			this.Science -= i;
		}

		public int GetTotalValue()
		{
			return this.Food + this.Industry + this.Dust + this.Science;
		}

		public int Food;

		public int Industry;

		public int Dust;

		public int Science;
	}
}
