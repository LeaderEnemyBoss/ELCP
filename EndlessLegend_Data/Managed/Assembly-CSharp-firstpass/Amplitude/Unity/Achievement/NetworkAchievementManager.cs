using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Runtime;
using UnityEngine;

namespace Amplitude.Unity.Achievement
{
	public abstract class NetworkAchievementManager : Manager, IAchievementService, IService
	{
		public bool IsDisabled { get; protected set; }

		public override IEnumerator BindServices()
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && !Amplitude.Unity.Framework.Application.Preferences.ELCPDevMode)
			{
				Diagnostics.LogWarning("The network achievement manager has been disabled because the modding tools are enabled...");
				this.IsDisabled = true;
			}
			yield return base.BindServices();
			Services.AddService<IAchievementService>(this);
			yield break;
		}

		public void AddToStatistic(StaticString statisticName, float value, bool commit = false)
		{
			if (this.IsDisabled)
			{
				return;
			}
			AchievementStatistic statistic = this.GetStatistic(statisticName);
			if (statistic != null)
			{
				statistic.AddToStat(value);
				this.SetNetworkStatistic(statisticName, statistic.Value);
				if (commit)
				{
					this.Commit();
				}
			}
		}

		public void Commit()
		{
			if (this.IsDisabled)
			{
				return;
			}
			if (this.debug)
			{
				Diagnostics.Log("[Achievement] Commiting network statistics:");
				foreach (AchievementStatistic achievementStatistic in this.statistics)
				{
					Diagnostics.Log(string.Concat(new object[]
					{
						"[Achievement Statistics] ",
						achievementStatistic.Name,
						" = ",
						achievementStatistic.Value
					}));
				}
			}
			this.NetworkCommit();
		}

		public void IncrementStatistic(StaticString statisticName, bool commit = false)
		{
			if (this.IsDisabled)
			{
				return;
			}
			AchievementStatistic statistic = this.GetStatistic(statisticName);
			if (statistic != null)
			{
				statistic.Increment();
				this.SetNetworkStatistic(statisticName, statistic.Value);
				if (commit)
				{
					this.Commit();
				}
			}
		}

		public bool GetAchievement(StaticString achievementName)
		{
			if (this.IsDisabled)
			{
				return false;
			}
			if (this.achievements == null && !StaticString.IsNullOrEmpty(achievementName))
			{
				Diagnostics.LogWarning("Setting network achievement '{0}' before it has been received.", new object[]
				{
					achievementName.ToString()
				});
				return false;
			}
			if (!this.achievements.Contains(achievementName))
			{
				Diagnostics.LogWarning("Can't find network achievement '{0}'.", new object[]
				{
					achievementName.ToString()
				});
				return false;
			}
			return this.GetNetworkAchievement(achievementName);
		}

		public float GetStatisticValue(StaticString statisticName)
		{
			if (this.IsDisabled)
			{
				return -1f;
			}
			AchievementStatistic statistic = this.GetStatistic(statisticName);
			if (statistic != null)
			{
				return statistic.Value;
			}
			return -1f;
		}

		public virtual void Load()
		{
			this.LoadStatistics();
			this.LoadAchievements();
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && !Amplitude.Unity.Framework.Application.Preferences.ELCPDevMode)
			{
				Diagnostics.LogWarning("The network achievement manager has been disabled because the modding tools are enabled...");
				this.IsDisabled = true;
			}
		}

		public void ResetAllStatistics()
		{
			if (this.IsDisabled)
			{
				return;
			}
			Diagnostics.Log("[Achievement] Reseting all network statistics");
			this.ResetNetworkStatistics();
			this.NetworkCommit();
			this.LoadStatistics();
		}

		public void SetAchievement(StaticString achievementName)
		{
			if (this.IsDisabled)
			{
				return;
			}
			if (this.achievements == null)
			{
				Diagnostics.LogWarning("Asking for network achivement {0} before it has been received");
				return;
			}
			if (!this.achievements.Contains(achievementName))
			{
				Diagnostics.LogWarning("Can't find network achievement: " + achievementName);
			}
			else
			{
				if (this.debug)
				{
					Diagnostics.Log("[Achievement] Achieved network achievement: " + achievementName);
				}
				this.SetNetworkAchievement(achievementName);
			}
		}

		public void SetStatisticValue(StaticString statisticName, float value, bool commit = false)
		{
			if (this.IsDisabled)
			{
				return;
			}
			AchievementStatistic statistic = this.GetStatistic(statisticName);
			if (statistic != null)
			{
				statistic.SetValue(value);
				this.SetNetworkStatistic(statisticName, statistic.Value);
				if (commit)
				{
					this.Commit();
				}
			}
		}

		public void PrintAllStatistics()
		{
			Diagnostics.LogWarning("NetworkAchivementManager.PrintAllStatistics");
			for (int i = 0; i < this.statistics.Count; i++)
			{
				Diagnostics.Log("{0}={1}", new object[]
				{
					this.statistics[i].Name,
					this.statistics[i].Value
				});
			}
		}

		public void PrintAllAchievements()
		{
			Diagnostics.LogWarning("NetworkAchivementManager.PrintAllAchievements");
			for (int i = 0; i < this.achievements.Count; i++)
			{
				Diagnostics.Log("{0}={1}", new object[]
				{
					this.achievements[i],
					this.GetAchievement(this.achievements[i])
				});
			}
		}

		protected abstract bool GetNetworkAchievement(string achievementName);

		protected abstract float GetNetworkStatisticFloat(StaticString statisticName);

		protected abstract int GetNetworkStatisticInteger(StaticString statisticName);

		protected virtual void LoadAchievements()
		{
			AchievementDefinition[] values = Databases.GetDatabase<AchievementDefinition>(true).GetValues();
			this.achievements = new List<StaticString>();
			foreach (AchievementDefinition achievementDefinition in values)
			{
				this.achievements.Add(achievementDefinition.Name);
				if (this.debug)
				{
					bool networkAchievement = this.GetNetworkAchievement(achievementDefinition.Name);
					Diagnostics.Log("[Achievement] Loaded network achievement (name: '{0}', state: {1}).", new object[]
					{
						achievementDefinition.Name,
						networkAchievement
					});
				}
			}
			Diagnostics.Log("[Achievement] {0} achievement(s) have been loaded.", new object[]
			{
				this.achievements.Count
			});
		}

		protected virtual void LoadStatistics()
		{
			AchievementStatisticDefinition[] values = Databases.GetDatabase<AchievementStatisticDefinition>(true).GetValues();
			this.statistics = new List<AchievementStatistic>();
			foreach (AchievementStatisticDefinition achievementStatisticDefinition in values)
			{
				float networkStatisticValue = this.GetNetworkStatisticValue(achievementStatisticDefinition.Name, achievementStatisticDefinition.StatisticType);
				this.statistics.Add(new AchievementStatistic(achievementStatisticDefinition, networkStatisticValue));
				if (this.debug)
				{
					Diagnostics.Log("[Achievement] Loaded network statistic (name: '{0}', type: '{1}', value: {2}).", new object[]
					{
						achievementStatisticDefinition.Name,
						achievementStatisticDefinition.StatisticType,
						networkStatisticValue
					});
				}
			}
			Diagnostics.Log("[Achievement] {0} statistics(s) have been loaded.", new object[]
			{
				this.statistics.Count
			});
		}

		protected abstract void NetworkCommit();

		protected abstract void ResetNetworkStatistics();

		protected abstract void SetNetworkAchievement(string achievementName);

		protected abstract bool SetNetworkStatisticFloat(StaticString statisticName, float value);

		protected abstract bool SetNetworkStatisticInteger(StaticString statisticName, int value);

		private float GetNetworkStatisticValue(StaticString statisticName, AchievementStatistic.StatisticType statisticType)
		{
			if (statisticType == AchievementStatistic.StatisticType.Float)
			{
				return this.GetNetworkStatisticFloat(statisticName);
			}
			if (statisticType == AchievementStatistic.StatisticType.Int)
			{
				return (float)this.GetNetworkStatisticInteger(statisticName);
			}
			Diagnostics.LogWarning("Unknown statistic type (name: '{0}', type: '{1}').", new object[]
			{
				statisticName,
				statisticType
			});
			return -1f;
		}

		private AchievementStatistic GetStatistic(StaticString statisticName)
		{
			AchievementStatistic achievementStatistic = this.statistics.FirstOrDefault((AchievementStatistic s) => s.Name == statisticName);
			if (achievementStatistic != null)
			{
				return achievementStatistic;
			}
			Diagnostics.LogWarning("Can't get network statistic (name: '{0}').", new object[]
			{
				statisticName
			});
			return null;
		}

		private void RuntimeService_RuntimeChange(object sender, RuntimeChangeEventArgs e)
		{
			if (e.Action == RuntimeChangeAction.Loaded)
			{
				this.Load();
			}
		}

		private void SetNetworkStatistic(StaticString statisticName, float statisticValue)
		{
			AchievementStatistic statistic = this.GetStatistic(statisticName);
			if (statistic != null)
			{
				if (statistic.Type == AchievementStatistic.StatisticType.Float)
				{
					this.SetNetworkStatisticFloat(statisticName, statisticValue);
				}
				else if (statistic.Type == AchievementStatistic.StatisticType.Int)
				{
					this.SetNetworkStatisticInteger(statisticName, (int)statisticValue);
				}
				if (this.OnStatisticSet != null)
				{
					this.OnStatisticSet(this, new EventArgs());
				}
				if (this.debug)
				{
					Diagnostics.Log("[Achievement Statistic] Network statistic (name: '{0}') has been set to '{1}'.", new object[]
					{
						statisticName,
						statistic.Value
					});
				}
			}
		}

		public EventHandler<EventArgs> OnStatisticSet;

		[SerializeField]
		protected bool debug;

		protected List<AchievementStatistic> statistics = new List<AchievementStatistic>();

		protected List<StaticString> achievements = new List<StaticString>();
	}
}
