using System;

public class TradableCategoryStockFactor
{
	public static implicit operator float(TradableCategoryStockFactor stockFactor)
	{
		return stockFactor.Value;
	}

	public static TradableCategoryStockFactor Zero = new TradableCategoryStockFactor();

	public float Value;
}
