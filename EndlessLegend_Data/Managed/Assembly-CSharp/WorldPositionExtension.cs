using System;
using Amplitude;
using Amplitude.Utilities.Maps;

public static class WorldPositionExtension
{
	public static T GetValue<T>(this GridMap<T> map, WorldPosition worldPosition)
	{
		if (map == null)
		{
			throw new ArgumentNullException("map");
		}
		if (!worldPosition.IsValid)
		{
			return default(T);
		}
		if (worldPosition.Column < 0 || (int)worldPosition.Column >= map.Width || worldPosition.Row < 0 || (int)worldPosition.Row >= map.Height)
		{
			Diagnostics.LogWarning("Trying to read invalid position (row: {0}, column: {1}) in map (width: {2}, height: {3}).", new object[]
			{
				worldPosition.Row,
				worldPosition.Column,
				map.Width,
				map.Height
			});
			return default(T);
		}
		return map.GetValue((int)worldPosition.Row, (int)worldPosition.Column);
	}

	public static void SetValue<T>(this GridMap<T> map, WorldPosition worldPosition, T value)
	{
		if (map == null)
		{
			throw new ArgumentNullException("map");
		}
		if (!worldPosition.IsValid)
		{
			Diagnostics.LogWarning("worldPosition " + worldPosition.ToString());
			return;
		}
		if (worldPosition.Column < 0 || (int)worldPosition.Column >= map.Width || worldPosition.Row < 0 || (int)worldPosition.Row >= map.Height)
		{
			Diagnostics.LogWarning("Trying to write invalid position (row: {0}, column: {1}) in map (width: {2}, height: {3}).", new object[]
			{
				worldPosition.Row,
				worldPosition.Column,
				map.Width,
				map.Height
			});
		}
		else
		{
			map.SetValue((int)worldPosition.Row, (int)worldPosition.Column, value);
		}
	}
}
