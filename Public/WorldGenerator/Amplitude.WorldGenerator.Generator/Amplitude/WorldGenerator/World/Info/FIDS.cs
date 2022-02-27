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
			return string.Format("FIDS {0} {1} {2} {3}", new object[]
			{
				this.Food,
				this.Industry,
				this.Dust,
				this.Science
			});
		}

		public int Negativity
		{
			get
			{
				if (this.Food < 0)
				{
					return -this.Food;
				}
				if (this.Industry < 0)
				{
					return -this.Industry;
				}
				if (this.Dust < 0)
				{
					return -this.Dust;
				}
				if (this.Science >= 0)
				{
					return 0;
				}
				return -this.Science;
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

		public int Food;

		public int Industry;

		public int Dust;

		public int Science;
	}
}
