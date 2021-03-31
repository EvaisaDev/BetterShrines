using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EliteSpawningOverhaul;
using JetBrains.Annotations;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using RoR2.Networking;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Evaisa.BetterShrines
{
	[RequireComponent(typeof(PurchaseInteraction))]
	public class ShrineImpBehaviour : MonoBehaviour
	{

		[ConCommand(commandName = "diff_coeff", flags = ConVarFlags.None, helpText = "Print the difficulty coefficient.")]
		private static void DiffCoeff(ConCommandArgs args)
		{
			if (Run.instance)
			{
				BetterShrines.Print("Difficulty Coefficient: " + Run.instance.difficultyCoefficient);
			}
		}

		public List<CharacterMaster> impMasterControllers;

		public static List<ShrineImpInstance> instances = new List<ShrineImpInstance>();

		public ShrineImpInstance instance;

		public Interactor whoInteracted;

		public float timeLeftUnrounded;
		public float startTime;

		public int readyPlayerCount;

		public float monsterCreditLeft;
		public float baseCredit = 20f;

		public int timeLeft;

		public ObjectivePanelController.ObjectiveSourceDescriptor sourceDescriptor;
		public List<ObjectivePanelController.ObjectiveSourceDescriptor> sourceDescriptorList;

		public class ShrineImpInstance
		{
			public ShrineImpBehaviour shrineBehaviour;
			public bool active;
			public HashSet<CharacterMaster> impMasters;
			public int originalImpCount;
			public int killedImpCount;
			public string impColor;

			public ShrineImpInstance(ShrineImpBehaviour shrineBehaviour, bool active, HashSet<CharacterMaster> impMasters, int originalImpCount, int killedImpCount)
			{
				this.shrineBehaviour = shrineBehaviour;
				this.active = active;
				this.impMasters = impMasters;
				this.originalImpCount = originalImpCount;
				this.killedImpCount = killedImpCount;
			}
		}

		private List<PickupIndex> weightedTierPickSpeed()
        {
			var startTier3Weight = 40;
			var startTier2Weight = 45;
			var startTier1Weight = 15;

			var endTier3Weight = 0;
			var endTier2Weight = 30;
			var endTier1Weight = 70;

			var tier3Weight = (int)Math.Floor(Mathf.Lerp(endTier3Weight, startTier3Weight, timeLeftUnrounded / startTime));
			var tier2Weight = (int)Math.Floor(Mathf.Lerp(endTier2Weight, startTier2Weight, timeLeftUnrounded / startTime));
			var tier1Weight = (int)Math.Floor(Mathf.Lerp(endTier1Weight, startTier1Weight, timeLeftUnrounded / startTime));

			var weights = new Dictionary<List<PickupIndex>, int>();
			weights.Add(Run.instance.availableTier3DropList, tier3Weight); 
			weights.Add(Run.instance.availableTier2DropList, tier2Weight); 
			weights.Add(Run.instance.availableTier1DropList, tier1Weight);

			Evaisa.BetterShrines.BetterShrines.Print("Time percentage: " + timeLeftUnrounded / startTime);
			Evaisa.BetterShrines.BetterShrines.Print("Tier 3 weight: " + tier3Weight);
			Evaisa.BetterShrines.BetterShrines.Print("Tier 2 weight: " + tier2Weight);
			Evaisa.BetterShrines.BetterShrines.Print("Tier 1 weight: " + tier1Weight);

			var outcomePick = WeightedRandomizer.From(weights).TakeOne();

			return outcomePick;
		}
		private List<PickupIndex> weightedTierPick()
		{
			var tier3Weight = 10;
			var tier2Weight = 40;
			var tier1Weight = 50;

			var weights = new Dictionary<List<PickupIndex>, int>();
			weights.Add(Run.instance.availableTier3DropList, tier3Weight);
			weights.Add(Run.instance.availableTier2DropList, tier2Weight);
			weights.Add(Run.instance.availableTier1DropList, tier1Weight);

			Evaisa.BetterShrines.BetterShrines.Print("Time percentage: " + timeLeftUnrounded / startTime);
			Evaisa.BetterShrines.BetterShrines.Print("Tier 3 weight: " + tier3Weight);
			Evaisa.BetterShrines.BetterShrines.Print("Tier 2 weight: " + tier2Weight);
			Evaisa.BetterShrines.BetterShrines.Print("Tier 1 weight: " + tier1Weight);

			var outcomePick = WeightedRandomizer.From(weights).TakeOne();

			return outcomePick;
		}

		private void DropRewards()
		{
			int participatingPlayerCount = Run.instance.participatingPlayerCount;
			if (participatingPlayerCount != 0 && symbolTransform)
			{
				List<PickupIndex> list = new List<PickupIndex>();

				if (Evaisa.BetterShrines.BetterShrines.itemRarityBasedOnSpeed.Value)
                {
					list = weightedTierPickSpeed();
                }
                else
                {
					list = weightedTierPick();
				}


				PickupIndex pickupIndex = Evaisa.BetterShrines.BetterShrines.EvaRng.NextElementUniform<PickupIndex>(list);
				int num = 1;

                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.funkfrog_sipondo.sharesuite")){
					num *= participatingPlayerCount;
				}

				num += Evaisa.BetterShrines.BetterShrines.extraItemCount.Value;


				float angle = 360f / (float)num;
				Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
				Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
				int i = 0;
				while (i < num)
				{
					PickupDropletController.CreatePickupDroplet(pickupIndex, symbolTransform.position, vector);
					i++;
					vector = rotation * vector;
				}
			}
		}

        internal struct requestEndShrine : INetMessage
        {
			internal bool won;
			internal NetworkInstanceId shrineId;
			internal GameObject shrineObject;
			public void Serialize(NetworkWriter writer)
			{
				writer.Write(won);
				writer.Write(shrineId);
			}
			public void Deserialize(NetworkReader reader)
			{
				this.won = reader.ReadBoolean();
				this.shrineId = reader.ReadNetworkId();
				this.shrineObject = Util.FindNetworkObject(shrineId);
			}

			public void OnReceived()
			{
				var shrineBehaviour = this.shrineObject.GetComponent<ShrineImpBehaviour>();
				shrineBehaviour.readyPlayerCount++;
				if (shrineBehaviour.readyPlayerCount == Run.instance.participatingPlayerCount)
				{
					if (won)
					{
						new ShrineImpBehaviour.clientEndShrine
						{
							won = true,
							shrineId = shrineId,
							shrineObject = shrineObject
						}.Send(NetworkDestination.Clients);
						shrineBehaviour.DropRewards();
						Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
						{
							subjectAsCharacterBody = shrineBehaviour.whoInteracted.GetComponent<CharacterBody>(),
							baseToken = "SHRINE_IMP_COMPLETED"
						});
						shrineBehaviour.readyPlayerCount = 0;

					}
					else
					{
						new ShrineImpBehaviour.clientEndShrine
						{
							won = false,
							shrineId = shrineId,
							shrineObject = shrineObject
						}.Send(NetworkDestination.Clients);

						Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
						{

							subjectAsCharacterBody = shrineBehaviour.whoInteracted.GetComponent<CharacterBody>(),
							baseToken = "SHRINE_IMP_FAILED"
						});
						shrineBehaviour.readyPlayerCount = 0;
					}
				}
			}
		}

		internal struct clientEndShrine : INetMessage
		{
			internal bool won;
			internal NetworkInstanceId shrineId;
			internal GameObject shrineObject;
			public void Serialize(NetworkWriter writer)
			{
				writer.Write(won);
				writer.Write(shrineId);
			}
			public void Deserialize(NetworkReader reader)
			{
				this.won = reader.ReadBoolean();
				this.shrineId = reader.ReadNetworkId();
				this.shrineObject = Util.FindNetworkObject(shrineId);
			}

			public void OnReceived()
			{
				var shrineBehaviour = this.shrineObject.GetComponent<ShrineImpBehaviour>();
                if (won)
                {
					BetterShrines.Print("Cleared imps with " + shrineBehaviour.timeLeft + " seconds left.");
					shrineBehaviour.instance.active = false;
					shrineBehaviour.sourceDescriptorList.Remove(shrineBehaviour.sourceDescriptor);
                }
                else
                {
					shrineBehaviour.instance.active = false;
					shrineBehaviour.sourceDescriptorList.Remove(shrineBehaviour.sourceDescriptor);
					shrineBehaviour.getAliveImps().ForEach(imp =>
					{
						imp.TrueKill();
					});
				}
			}
		}


		internal struct SendImpObjects : INetMessage
		{
			internal List<GameObject> impObjects;
			internal List<NetworkInstanceId> impIds;
			internal GameObject shrineObject;
			internal NetworkInstanceId shrineId;
			internal Color impColor;

			public void Serialize(NetworkWriter writer)
			{
				writer.Write(impIds.Count);

				impIds.ForEachTry(impId =>
				{
					writer.Write(impId);
				});

				writer.Write(shrineId);
				writer.Write(impColor);
			}
			public void Deserialize(NetworkReader reader)
			{
				var newImpIds = new List<NetworkInstanceId>();

				//BetterShrines.Print(reader.Length);

				var impCount = reader.ReadInt32();

				for (int i = 0; i < impCount; i++)
				{
					var newObjectId = reader.ReadNetworkId();
					newImpIds.Add(newObjectId);
				}


				var shrineObjectId = reader.ReadNetworkId();

				this.impIds = newImpIds;
				this.impColor = reader.ReadColor();

				var shrine = Util.FindNetworkObject(shrineObjectId);
				this.shrineObject = shrine;

				/*while (reader.Position < reader.Length - 2)
				{
					
				}
				*/
			}
			public void OnReceived()
			{


				BetterShrines.instance.StartCoroutine(this.delayImpSpawn(this));

			}
			private IEnumerator delayImpSpawn(SendImpObjects sendImpObjects)
			{
				yield return new WaitUntil(() =>
				{
					if (sendImpObjects.impIds != null)
					{
						var impObjects = new List<GameObject>();
						sendImpObjects.impIds.ForEach(impId =>
						{
							var impObject = Util.FindNetworkObject(impId);
							if (impObject != null) {
								if (impObject.GetComponent<CharacterMaster>())
								{
									if (impObject.GetComponent<CharacterMaster>().GetBody())
									{
										impObjects.Add(impObject);
									}
								}
							}
						});
						if (impObjects.Count == sendImpObjects.impIds.Count)
						{
							sendImpObjects.impObjects = impObjects;
							return true;
						}
					}
					return false;
				});

				var impMasters = new HashSet<CharacterMaster>();

				sendImpObjects.impObjects.ForEach(impObject =>
				{

					var impMaster = impObject.GetComponent<CharacterMaster>();

					impMasters.Add(impMaster);

				});

				var shrineImpBehaviour = sendImpObjects.shrineObject.GetComponent<ShrineImpBehaviour>();

				shrineImpBehaviour.instance.impMasters = impMasters;

				shrineImpBehaviour.instance.originalImpCount = impMasters.Count;

				shrineImpBehaviour.instance.killedImpCount = shrineImpBehaviour.instance.originalImpCount - impMasters.Count;

				shrineImpBehaviour.instance.active = true;

				shrineImpBehaviour.instance.impColor = "#" + ColorUtility.ToHtmlStringRGB(sendImpObjects.impColor);

				Evaisa.BetterShrines.ShrineImpBehaviour.markGameObjects(impMasters, sendImpObjects.impColor);

			}
		}


		public void Awake()
        {
			
			startTime = BetterShrines.impShrineTime.Value;
			timeLeftUnrounded = (float)BetterShrines.impShrineTime.Value;
			instance = new ShrineImpInstance(this, false, new HashSet<CharacterMaster>(), 0, 0);
			ShrineImpBehaviour.instances.Add(instance);

		}

		public void Update()
        {
			if (instance.active) {
				timeLeftUnrounded -= Time.deltaTime;
				timeLeft = (int)Math.Round(timeLeftUnrounded);
				instance.killedImpCount = instance.originalImpCount - getAliveImps().Count;
				var shrineId = GetComponent<NetworkBehaviour>().netId;
				if (instance.killedImpCount == instance.originalImpCount)
				{
					
					
					if (NetworkClient.active)
					{

						new requestEndShrine
						{
							won = true,
							shrineId = shrineId,
							shrineObject = gameObject
						}.Send(NetworkDestination.Server);
						/**/
					}
				}
				if (timeLeft < 1)
				{
					
					
					if (NetworkClient.active)
					{
						new requestEndShrine
						{
							won = false,
							shrineId = shrineId,
							shrineObject = gameObject
						}.Send(NetworkDestination.Server);

					}
				}
			}
		}

		public List<CharacterMaster> getAliveImps()
        {
			var aliveImps = new List<CharacterMaster>();
			instance.impMasters.ForEachTry(imp =>
			{
                if (imp.GetBody())
                {
					if (imp.GetBody().healthComponent.alive)
					{
						aliveImps.Add(imp);
					}
                }
			});
			return aliveImps;
        }

		public void AddShrineStack(Interactor interactor)
		{
			whoInteracted = interactor;
			symbolTransform.gameObject.SetActive(false);
			EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
			{
				origin = base.transform.position,
				rotation = Quaternion.identity,
				scale = 1f,
				color = shrineEffectColor
			}, true);


			if (NetworkServer.active)
			{

				Evaisa.BetterShrines.BetterShrines.Print("Player interacted with shrine!");
				//combatDirector.CombatShrineActivation(interactor, monsterCredit, chosenDirectorCard);
				var difficulty = Run.instance.difficultyCoefficient;
				var difficultyMultiplier = DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty).scalingValue - 1;
				var stagesCleared = Run.instance.stageClearCount;


				monsterCreditLeft = ((baseCredit * (difficultyMultiplier + 1)) + ((baseCredit + 30) * (difficulty - 1)));

				var imps = spawnImps(impNumber);

				Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
				{
					subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
					baseToken = "SHRINE_IMP_MESSAGE"
				});

				var ImpIds = new List<NetworkInstanceId>();
				imps.ForEachTry(impObject =>
				{
					var impId = impObject.GetComponent<CharacterMaster>().netId;
					ImpIds.Add(impId);
				});

				Color impColor = Random.ColorHSV(0f, 1f, 0.5f, 0.7f, 1f, 1f);

				new SendImpObjects
				{
					impIds = ImpIds,
					shrineId = gameObject.GetComponent<NetworkBehaviour>().netId,
					shrineObject = gameObject,
					impObjects = imps,
					impColor = impColor
				}.Send(NetworkDestination.Clients);
			}
		}

		private int impNumber
		{
			get
			{
				var baseCount = Evaisa.BetterShrines.BetterShrines.baseImpCount.Value;
				var playerCount = Run.instance.participatingPlayerCount;
				var runDifficulty = DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty).scalingValue;
				var difficulty = Run.instance.difficultyCoefficient;

				var count = Math.Min((int)Math.Round((float)(baseCount - 1) + (playerCount * runDifficulty)), 17);

                if (BetterShrines.impCountScale.Value)
                {
					count += (int)Math.Round(difficulty * 1.3f);
                }

                if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.swarmsArtifactDef))
                {
					count = count * 2;
                }

				return count;
			}
		}

		public static void markGameObjects(HashSet<CharacterMaster> imps, Color impColor)
        {
			imps.ForEachTry(imp =>
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/PositionIndicators/PoiPositionIndicator"), imp.GetBodyObject().transform.position, imp.GetBodyObject().transform.rotation);
				var positionIndicator = gameObject.GetComponent<PositionIndicator>();
				//positionIndicator.alwaysVisibleObject = true;

				positionIndicator.insideViewObject.GetComponent<SpriteRenderer>().color = impColor;
				Destroy(positionIndicator.insideViewObject.GetComponent<ObjectScaleCurve>());
				positionIndicator.insideViewObject.transform.localScale = positionIndicator.insideViewObject.transform.localScale / 2f;
				positionIndicator.insideViewObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("textures/miscicons/texAttackIcon");

				positionIndicator.outsideViewObject.transform.Find("Sprite").GetComponent<SpriteRenderer>().color = impColor;
				positionIndicator.outsideViewObject.transform.Find("Sprite").Find("Sprite").GetComponent<SpriteRenderer>().color = impColor;

				positionIndicator.targetTransform = imp.GetBodyObject().transform;
				gameObject.AddComponent<ImpMarkerKiller>();
			});
			
		}

		public List<GameObject> spawnImps(int count)
        {
			var placement = new DirectorPlacementRule
			{
				placementMode = DirectorPlacementRule.PlacementMode.Approximate,
				preventOverhead = false,
				minDistance = 0,
				maxDistance = 20,
				spawnOnTarget = symbolTransform
			};

			var spawnedImps = new List<GameObject>();

			for (var i = 0; i < count; i++)
			{
				var spawnedImp = summonImp(placement, Evaisa.BetterShrines.BetterShrines.EvaRng);
				if (spawnedImp != null)
				{
					spawnedImps.Add(spawnedImp.gameObject);
				}
			}
			return spawnedImps;
		}

		public GameObject TrySpawnImp(DirectorSpawnRequest directorSpawnRequest)
		{
			var directorCore = DirectorCore.instance;
			SpawnCard spawnCard = directorSpawnRequest.spawnCard;
			var placementRule = directorSpawnRequest.placementRule;
			Xoroshiro128Plus rng = directorSpawnRequest.rng;
			NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(spawnCard.nodeGraphType);
			GameObject result = null;

			switch (placementRule.placementMode)
			{
				case DirectorPlacementRule.PlacementMode.Direct:
					{
						Quaternion quaternion = Quaternion.Euler(0f, rng.nextNormalizedFloat * 360f, 0f);
						result = spawnCard.DoSpawn(placementRule.spawnOnTarget ? placementRule.spawnOnTarget.position : directorSpawnRequest.placementRule.position, placementRule.spawnOnTarget ? placementRule.spawnOnTarget.rotation : quaternion, directorSpawnRequest).spawnedInstance;
						break;
					}
				case DirectorPlacementRule.PlacementMode.Approximate:
					{
						List<NodeGraph.NodeIndex> list = nodeGraph.FindNodesInRangeWithFlagConditions(placementRule.targetPosition, placementRule.minDistance, placementRule.maxDistance, (HullMask)(1 << (int)spawnCard.hullSize), spawnCard.requiredFlags, spawnCard.forbiddenFlags, placementRule.preventOverhead);
						while (list.Count > 0)
						{
							int index = rng.RangeInt(0, list.Count);
							NodeGraph.NodeIndex nodeIndex = list[index];
							Vector3 vector;
							nodeGraph.GetNodePosition(nodeIndex, out vector);

							Quaternion rotation = placementRule.spawnOnTarget.rotation;
							result = spawnCard.DoSpawn(vector, rotation, directorSpawnRequest).spawnedInstance;

							break;
						}
						break;
					}
				case DirectorPlacementRule.PlacementMode.NearestNode:
					{
						NodeGraph.NodeIndex nodeIndex3 = nodeGraph.FindClosestNodeWithFlagConditions(placementRule.targetPosition, spawnCard.hullSize, spawnCard.requiredFlags, spawnCard.forbiddenFlags, placementRule.preventOverhead);
						Vector3 vector3;
						if (nodeGraph.GetNodePosition(nodeIndex3, out vector3))
						{
							Quaternion rotation3 = placementRule.spawnOnTarget.rotation;
							result = spawnCard.DoSpawn(vector3, rotation3, directorSpawnRequest).spawnedInstance;
						}
						break;
					}
			}
			return result;
		}

		public CharacterMaster summonImp(DirectorPlacementRule placement, Xoroshiro128Plus rng)
		{
			

			var directorCard = new DirectorCard();

			directorCard.allowAmbushSpawn = true;
			directorCard.forbiddenUnlockable = "";
			directorCard.minimumStageCompletions = 0;
			directorCard.preventOverhead = false;
			directorCard.requiredUnlockable = "";
			directorCard.selectionWeight = 1;
			directorCard.spawnCard = Evaisa.BetterShrines.BetterShrines.impSpawnCard;
			directorCard.spawnDistance = DirectorCore.MonsterSpawnDistance.Close;

			var spawnRequest = new DirectorSpawnRequest(directorCard.spawnCard, placement, rng)
			{
				teamIndexOverride = TeamIndex.Monster,
				ignoreTeamMemberLimit = true
			};
			var spawned = TrySpawnImp(spawnRequest);

			if (spawned == null)
				return null;

			var spawnedMaster = spawned.GetComponent<CharacterMaster>();

			var difficulty = Run.instance.difficultyCoefficient;
			var difficultyMultiplier = (int)Math.Round(DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty).scalingValue) - 1;
			var stagesCleared = Run.instance.stageClearCount;

			if (difficultyMultiplier < 0) difficultyMultiplier = 0;
			

			BetterShrines.Print("HP Boosts: " + (Mathf.RoundToInt((float)(difficulty - 1)) + (difficultyMultiplier)));

			spawnedMaster.inventory.GiveItem(ItemIndex.BoostHp, Mathf.RoundToInt((float)(difficulty - 1)) + (difficultyMultiplier));

			BetterShrines.Print("Elite Credit: " + monsterCreditLeft);

			if (BetterShrines.allowImpElite.Value)
			{
				var affixCard = EsoLib.ChooseEliteAffix(directorCard, monsterCreditLeft, rng);

				//	BetterShrines.Print("Affix: " + affixCard.ToString());

				if (affixCard != null)
				{
					monsterCreditLeft -= directorCard.cost * affixCard.costMultiplier;
					//Elites are boosted
					var healthBoost = affixCard.healthBoostCoeff;

					spawnedMaster.inventory.GiveItem(ItemIndex.BoostHp, Mathf.RoundToInt((float)((healthBoost - 1.0) * 10.0)));

					var eliteDef = EliteCatalog.GetEliteDef(affixCard.eliteType);
					if (eliteDef != null)
						spawnedMaster.inventory.SetEquipmentIndex(eliteDef.eliteEquipmentIndex);

					affixCard.onSpawned?.Invoke(spawnedMaster);
				}


			}

			spawned.GetComponent<BaseAI>().localNavigator.allowWalkOffCliff = false;

			return spawnedMaster;
		}

		public Color shrineEffectColor;

		public Transform symbolTransform;

		public GameObject spawnPositionEffectPrefab;

		public ShrineImpObjective impObjective;

	}
}
