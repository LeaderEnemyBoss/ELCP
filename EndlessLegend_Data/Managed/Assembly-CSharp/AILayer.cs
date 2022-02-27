using System;
using System.Collections;
using Amplitude;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

[Diagnostics.TagAttribute("AI")]
public abstract class AILayer : IXmlSerializable
{
	public AILayer()
	{
		this.InternalGUID = GameEntityGUID.Zero;
	}

	public virtual void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		if (num >= 3)
		{
			this.InternalGUID = reader.GetAttribute<ulong>("InternalGUID");
		}
		if (this.InternalGUID == GameEntityGUID.Zero)
		{
			this.InternalGUID = AIScheduler.Services.GetService<IAIEntityGUIDAIHelper>().GenerateAIEntityGUID();
		}
		reader.ReadStartElement();
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(3);
		if (num >= 3)
		{
			writer.WriteAttributeString<ulong>("InternalGUID", this.InternalGUID);
		}
	}

	~AILayer()
	{
	}

	public AIEntity AIEntity { get; set; }

	public GameEntityGUID InternalGUID { get; set; }

	public static float Boost(float normalizedScore, float boostFactor)
	{
		if (boostFactor < -1f || boostFactor > 1f)
		{
			AILayer.LogWarning("[SCORING] clamping invalid booster of {0}", new object[]
			{
				boostFactor
			});
			boostFactor = Math.Min(1f, Math.Max(-1f, boostFactor));
		}
		if (boostFactor < 0f)
		{
			if (normalizedScore < 0f)
			{
				AILayer.LogWarning("[SCORING] score of {0} boosted by {1}. returning 0", new object[]
				{
					normalizedScore,
					boostFactor
				});
				return 0f;
			}
			if (normalizedScore > 1f)
			{
				AILayer.LogWarning("[SCORING] score of {0} boosted by {1}. returning 0", new object[]
				{
					normalizedScore,
					boostFactor,
					1f - boostFactor
				});
				return 1f - Math.Abs(boostFactor);
			}
			return normalizedScore - normalizedScore * -boostFactor;
		}
		else
		{
			if (normalizedScore < 0f)
			{
				AILayer.LogWarning("[SCORING] score of {0} boosted by {1}. returning {1}", new object[]
				{
					normalizedScore,
					boostFactor
				});
				return boostFactor;
			}
			if (normalizedScore > 1f)
			{
				AILayer.LogWarning("[SCORING] score of {0} boosted by {1}. returning 1", new object[]
				{
					normalizedScore,
					boostFactor
				});
				return 1f;
			}
			return normalizedScore + boostFactor * (1f - normalizedScore);
		}
	}

	public static float ComputeBoost(float normalizedScoreA, float normalizedScoreB)
	{
		if (normalizedScoreA < 0f || normalizedScoreA > 1f)
		{
			AILayer.LogWarning("[SCORING] clamping invalid scoreA of {0}", new object[]
			{
				normalizedScoreA
			});
			normalizedScoreA = Math.Max(1f, Math.Min(0f, normalizedScoreA));
		}
		if (normalizedScoreB < 0f || normalizedScoreB > 1f)
		{
			AILayer.LogWarning("[SCORING] clamping invalid scoreB of {0}", new object[]
			{
				normalizedScoreB
			});
			normalizedScoreB = Math.Max(1f, Math.Min(0f, normalizedScoreB));
		}
		if (normalizedScoreA == 0f)
		{
			return normalizedScoreB;
		}
		if (normalizedScoreA == 1f)
		{
			return -(normalizedScoreA - normalizedScoreB);
		}
		if (normalizedScoreA <= normalizedScoreB)
		{
			return (normalizedScoreB - normalizedScoreA) / (1f - normalizedScoreA);
		}
		return -(normalizedScoreA - normalizedScoreB) / normalizedScoreA;
	}

	public static void Log(string format, params object[] args)
	{
	}

	public static void Log(string format)
	{
	}

	public static void LogError(string format)
	{
	}

	public static void LogError(string format, params object[] args)
	{
	}

	public static void LogWarning(string format)
	{
	}

	public static void LogWarning(string format, params object[] args)
	{
	}

	public virtual bool CanEndTurn()
	{
		return true;
	}

	public virtual IEnumerator Initialize(AIEntity aiEntity)
	{
		if (this.InternalGUID == GameEntityGUID.Zero)
		{
			this.InternalGUID = AIScheduler.Services.GetService<IAIEntityGUIDAIHelper>().GenerateAIEntityGUID();
		}
		this.AIEntity = aiEntity;
		yield break;
	}

	public abstract bool IsActive();

	public virtual IEnumerator Load()
	{
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(this.AIEntity.Empire, this);
		yield break;
	}

	public virtual void Release()
	{
		this.AIEntity = null;
	}

	protected virtual void CreateLocalNeeds(StaticString context, StaticString pass)
	{
	}

	protected virtual void EvaluateNeeds(StaticString context, StaticString pass)
	{
	}

	protected virtual void ExecuteNeeds(StaticString context, StaticString pass)
	{
	}

	protected virtual void RefreshObjectives(StaticString context, StaticString pass)
	{
	}
}
