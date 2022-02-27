using System;

public class TradableCategoryStockFactor
{
	public TradableCategoryStockFactor()
	{
	}

	public static implicit operator float(TradableCategoryStockFactor stockFactor)
	{
		return stockFactor.Value;
	}

	public TradableCategoryStockFactor(float value)
	{
		this.Value = value;
	}

	public static TradableCategoryStockFactor Zero = new TradableCategoryStockFactor(0.5f);

	public float Value;
}
