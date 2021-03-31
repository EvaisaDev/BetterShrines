using BepInEx;
using RoR2;
using UnityEngine;
using System.Collections.Generic;
using System;
using BepInEx.Configuration;
using System.Reflection;
using MonoMod.Cil;
using KinematicCharacterController;
using UnityEngine.Networking;
using R2API.Utils;
using Object = UnityEngine.Object;
using System.Linq;
using System.Collections;
using R2API;
using EliteSpawningOverhaul;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using RoR2.DirectionalSearch;
using RoR2.Orbs;
using RoR2.Projectile;
using R2API.Networking.Interfaces;
using R2API.Networking;

namespace Evaisa.BetterShrines
{
    class ChanceShrine : MonoBehaviour
	{
		public static List<ShrineIconMsg> shrineIconMessages = new List<ShrineIconMsg>();


		internal struct AddData : INetMessage
        {
			internal string iconFile;
			internal GameObject shrineObject;

			public void Serialize(NetworkWriter writer)
			{
				writer.Write(iconFile);
				writer.Write(shrineObject);
			}
			public void Deserialize(NetworkReader reader)
            {
				iconFile = reader.ReadString();
				shrineObject = reader.ReadGameObject();
			}
			public void OnReceived()
			{
				ChanceShrine.ShrineIconMsg msg = new ChanceShrine.ShrineIconMsg(iconFile, shrineObject);

				GameObject display = msg.gameObject.transform.Find("iconDisplay").gameObject;
				if (display)
				{
					display.SetActive(true);
				}

				if (ChanceShrine.shrineIconMessages.Any(r => r.gameObject == msg.gameObject))
				{
					ChanceShrine.shrineIconMessages[ChanceShrine.shrineIconMessages.FindIndex(r => r.gameObject == msg.gameObject)] = msg;
				}
				else
				{
					ChanceShrine.shrineIconMessages.Add(msg);
				}
			}
		}

		internal struct RemoveData : INetMessage
		{
			internal string iconFile;
			internal GameObject shrineObject;

			public void Serialize(NetworkWriter writer)
			{
				writer.Write(iconFile);
				writer.Write(shrineObject);
			}
			public void Deserialize(NetworkReader reader)
			{
				iconFile = reader.ReadString();
				shrineObject = reader.ReadGameObject();
			}
			public void OnReceived()
			{
				GameObject display = shrineObject.transform.Find("iconDisplay").gameObject;
				if (display)
				{
					display.SetActive(false);
				}
				/*var theShrine = shrineObject;
				ChanceShrine.shrineIconMessages.RemoveAt(ChanceShrine.shrineIconMessages.FindIndex(r => r.gameObject == theShrine));*/
			}
		}

		internal struct PlayNetworkSound : INetMessage
		{
			internal string soundString;
			internal GameObject soundObject;

			public void Serialize(NetworkWriter writer)
			{
				writer.Write(soundString);
				writer.Write(soundObject);
			}
			public void Deserialize(NetworkReader reader)
			{
				soundString = reader.ReadString();
				soundObject = reader.ReadGameObject();
			}
			public void OnReceived()
			{
				Util.PlaySound(soundString, soundObject);
			}
		}

		private struct UserTargetInfo
		{
			public UserTargetInfo(HurtBox source)
			{
				this.hurtBox = source;
				this.rootObject = (this.hurtBox ? this.hurtBox.healthComponent.gameObject : null);
				this.pickupController = null;
				this.transformToIndicateAt = (this.hurtBox ? this.hurtBox.transform : null);
			}

			public UserTargetInfo(GenericPickupController source)
			{
				this.pickupController = source;
				this.hurtBox = null;
				this.rootObject = (this.pickupController ? this.pickupController.gameObject : null);
				this.transformToIndicateAt = (this.pickupController ? this.pickupController.pickupDisplay.transform : null);
			}

			public readonly HurtBox hurtBox;

			public readonly GameObject rootObject;

			public readonly GenericPickupController pickupController;

			public readonly Transform transformToIndicateAt;
		}
		public static class ResourcesCached
		{
			public static Dictionary<string, Object> resourceCache = new Dictionary<string, Object>();

			public static T Load<T>(string path) where T : Object
			{
				if (!resourceCache.ContainsKey("Assets/SmiteIcon.png"))
					resourceCache["Assets/SmiteIcon.png"] = (T)EvaResources.SmiteIcon;

				if (!resourceCache.ContainsKey("Assets/FreezeIcon.png"))
					resourceCache["Assets/FreezeIcon.png"] = (T)EvaResources.FreezeIcon;

				if (!resourceCache.ContainsKey(path))
					resourceCache[path] = Resources.Load<T>(path);

				return (T)resourceCache[path];
			}
			public static bool Loaded { get; private set; }
		}

		public class ShrineIconData
		{
			public Vector3 iconPosition;
			public int iconSize;
			public string iconFile;
			public PickupIndex pickupIndex;

			public ShrineIconData(Vector3 iconPosition, int iconSize, string itemString, PickupIndex my_index)
			{
				this.iconPosition = iconPosition;
				this.iconSize = iconSize;
				this.iconFile = itemString;
				this.pickupIndex = my_index;
			}
		}

		public Dictionary<GameObject, GameObject> UISprites = new Dictionary<GameObject, GameObject>();

		public class ShrineIconMsg
		{
			public string iconFile;
			public GameObject gameObject;

			public ShrineIconMsg(string itemString, GameObject gameObject)
			{
				this.iconFile = itemString;
				this.gameObject = gameObject;
			}

		}

		string GetPickupItemPath(PickupIndex pickupIndex)
		{
			if (pickupIndex.value == -1)
			{
				string[] special = new string[] { "textures/difficultyicons/texDifficultyHardIcon", "Assets/SmiteIcon.png", "Assets/FreezeIcon.png" };
				string randomString = special[UnityEngine.Random.Range(0, special.Length)];
				return randomString;
			}

			var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);

			if (pickupDef != null)
			{
				if (pickupDef.itemIndex != ItemIndex.None)
				{
					return ItemCatalog.GetItemDef(pickupDef.itemIndex).pickupIconPath;
				}
				else if (pickupDef.equipmentIndex != EquipmentIndex.None)
				{
					return EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex).pickupIconPath;
				}
				else
				{
					string[] special = new string[] { "textures/difficultyicons/texDifficultyHardIcon", "Assets/SmiteIcon.png", "Assets/FreezeIcon.png" };
					string randomString = special[UnityEngine.Random.Range(0, special.Length)];
					return randomString;
				}
            }
            else
            {
				string[] special = new string[] { "textures/difficultyicons/texDifficultyHardIcon", "Assets/SmiteIcon.png", "Assets/FreezeIcon.png" };
				string randomString = special[UnityEngine.Random.Range(0, special.Length)];
				return randomString;
			}
		}
		public PickupIndex RandomPickupIndex(ShrineChanceBehavior self)
		{
			PickupIndex none = PickupIndex.none;

			PickupIndex value = self.GetFieldValue<Xoroshiro128Plus>("rng").NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList);
			PickupIndex value2 = self.GetFieldValue<Xoroshiro128Plus>("rng").NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList);
			PickupIndex value3 = self.GetFieldValue<Xoroshiro128Plus>("rng").NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);
			PickupIndex value4 = self.GetFieldValue<Xoroshiro128Plus>("rng").NextElementUniform<PickupIndex>(Run.instance.availableEquipmentDropList);

			WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>(8);
			weightedSelection.AddChoice(none, self.failureWeight);
			weightedSelection.AddChoice(value, self.tier1Weight);
			weightedSelection.AddChoice(value2, self.tier2Weight);
			weightedSelection.AddChoice(value3, self.tier3Weight);
			weightedSelection.AddChoice(value4, self.equipmentWeight);
			PickupIndex pickupIndex = weightedSelection.Evaluate(self.GetFieldValue<Xoroshiro128Plus>("rng").nextNormalizedFloat);

			return pickupIndex;
		}

		public ShrineIconData getItemData(ShrineChanceBehavior self, ShrineIconData dataOld)
		{


			PickupIndex pickupIndex = RandomPickupIndex(self);

			Vector3 iconPosition = self.symbolTransform.transform.position + self.symbolTransform.up * -0.2f;
			int iconSize = 150;

			string ItemString = GetPickupItemPath(pickupIndex);

			ShrineIconData data = new ShrineIconData(iconPosition, iconSize, ItemString, pickupIndex);

			if (dataOld != null)
			{
				if (dataOld.iconFile == ItemString)
				{
					return getItemData(self, dataOld);
				}
				else
				{
					return data;
				}
			}
			else
			{
				return data;
			}

		}



		float getMonsterCredit()
		{
			return (100 * Run.instance.difficultyCoefficient);
		}

		public void drawChanceShrineDisplay()
        {
		
			foreach (var iconData in ChanceShrine.shrineIconMessages)
			{
				if (iconData.gameObject != null)
				{
					GameObject display = iconData.gameObject.transform.Find("iconDisplay").gameObject;
					if (display != null)
					{

						CameraRigController[] HudObjects = GameObject.FindObjectsOfType(typeof(CameraRigController)) as CameraRigController[];
						GameObject MainCamera = HudObjects[0].gameObject;

						MainCamera.transform.Find("Scene Camera").gameObject.GetComponent<Camera>().cullingMask = MainCamera.transform.Find("Scene Camera").gameObject.GetComponent<Camera>().cullingMask & ~(1 << LayerMask.NameToLayer("TransparentFX"));

						if (MainCamera.transform.Find("Mask Camera") == null)
						{
							var newCamera = new GameObject("Mask Camera");
							newCamera.transform.SetParent(MainCamera.transform);
							Camera camera = newCamera.AddComponent<Camera>();
							camera.cullingMask = 1 << LayerMask.NameToLayer("TransparentFX");
							MatchCamera match = newCamera.AddComponent<MatchCamera>();
							match.srcCamera = MainCamera.transform.Find("Scene Camera").gameObject.GetComponent<Camera>();
							match.matchFOV = true;
							match.matchRect = true;
							match.matchPosition = true;
							camera.clearFlags = CameraClearFlags.Nothing;
							camera.depth = 3;
							camera.targetDisplay = MainCamera.transform.Find("Scene Camera").gameObject.GetComponent<Camera>().targetDisplay;
						}



						var icon = ChanceShrine.ResourcesCached.Load<Texture2D>(iconData.iconFile);
						//var sprite = Sprite.Create(icon, new Rect(0.0f, 0.0f, icon.width, icon.height), new Vector2(0.5f, 0.5f), 100f);

						display.transform.LookAt(display.transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);

						display.GetComponent<Renderer>().material.SetTexture("_MainTex", icon);



						//display.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(1f, 1f, 1f, 1f));

						/*display.GetComponent<SpriteRenderer>().sprite = sprite;
						var bounds = display.GetComponent<SpriteRenderer>().sprite.bounds;
						var factor = 1f / bounds.size.y;
						display.transform.localScale = new Vector3(factor, factor, factor);*/
					}
				}
			}
		}

		List<CharacterMaster> GetAllCharactersInRadius(Vector3 position, int radius)
        {
			var masters = CharacterMaster.readOnlyInstancesList;


			List<CharacterMaster> masterList = masters.Cast<CharacterMaster>().ToList();


			List<CharacterMaster> players = new List<CharacterMaster>();
			masterList.ForEachTry(instance =>
			{
				if (instance.hasBody)
				{
					if (Vector3.Distance(instance.GetBodyObject().transform.position, position) <= radius)
					{
						players.Add(instance);
					}
				}
			});

			return players;
		}

		public IEnumerator ChanceGambleCoroutine(object[] parms)
		{
			ShrineChanceBehavior self = (ShrineChanceBehavior)parms[0];
			Interactor activator = (Interactor)parms[1];

			self.SetFieldValue<bool>("waitingForRefresh", true);
			self.SetFieldValue<float>("refreshTimer", 10f);


			ShrineIconData data = getItemData(self, null);

			/*AddData.Invoke(x =>
			{
				x.Write(data.iconFile);
				x.Write(data.iconPosition);
				x.Write(data.iconSize);
				x.Write(self.gameObject);
			});*/
			new AddData
			{
				iconFile = data.iconFile,
				shrineObject = self.gameObject
			}.Send(NetworkDestination.Clients);


			for (float i = 20; i <= 60; i++)
			{
				ShrineIconData oldData = data;
				data = getItemData(self, data);

				new AddData
				{
					iconFile = data.iconFile,
					shrineObject = self.gameObject
				}.Send(NetworkDestination.Clients);

				new PlayNetworkSound
				{
					soundString = "Play_UI_obj_casinoChest_swap",
					soundObject = self.gameObject
				}.Send(NetworkDestination.Clients);

				yield return new WaitForSeconds(0.8f - (i / 50));
				if (i == 60)
				{
					yield return new WaitForSeconds(1.5f);
				}
			}

			PickupIndex pickupIndex = data.pickupIndex;

			bool flag = pickupIndex == PickupIndex.none;
			string baseToken;
			if (flag)
			{
				baseToken = "SHRINE_CHANCE_PUNISHED_MESSAGE";
				if (data.iconFile == "textures/difficultyicons/texDifficultyHardIcon")
				{
					for (var i = 0; i < 5; i++)
					{
						var monsterSelection = ClassicStageInfo.instance.monsterSelection;
						var weightedSelection = new WeightedSelection<DirectorCard>(8);
						float eliteCostMultiplier = CombatDirector.highestEliteCostMultiplier;
						Debug.Log("Credit available: " + getMonsterCredit());
						for (int index = 0; index < monsterSelection.Count; ++index)
						{
							//Debug.Log(monsterSelection.choices[index]);
							DirectorCard directorCard = monsterSelection.choices[index].value;
							var noElites = ((CharacterSpawnCard)directorCard.spawnCard).noElites;
							float highestCost = (float)(directorCard.cost * (noElites ? 1.0 : eliteCostMultiplier));


							if (directorCard.CardIsValid() && directorCard.cost <= getMonsterCredit())// && highestCost / 3.0 > getMonsterCredit())
								weightedSelection.AddChoice(directorCard, monsterSelection.choices[index].weight);
						}
						if (weightedSelection.Count != 0)
						{
							var chosenDirectorCard = weightedSelection.Evaluate(self.GetFieldValue<Xoroshiro128Plus>("rng").nextNormalizedFloat);

							var placement = new DirectorPlacementRule
							{
								placementMode = DirectorPlacementRule.PlacementMode.Approximate,
								preventOverhead = chosenDirectorCard.preventOverhead,
								minDistance = 0,
								maxDistance = 50,
								spawnOnTarget = self.transform
							};

							var eliteAffix = EsoLib.ChooseEliteAffix(chosenDirectorCard, getMonsterCredit(), self.GetFieldValue<Xoroshiro128Plus>("rng"));

							TrySpawnEnemy((CharacterSpawnCard)chosenDirectorCard.spawnCard, eliteAffix, placement, self.GetFieldValue<Xoroshiro128Plus>("rng"));

						}
					}
				}else if(data.iconFile == "Assets/SmiteIcon.png")
                {
					GetAllCharactersInRadius(self.transform.position, 50).ForEachTry(character =>
					{
						OrbManager.instance.AddOrb(new LightningStrikeOrb
						{
							attacker = self.gameObject,
							damageColorIndex = DamageColorIndex.Item,
							damageValue = character.GetBody().GetComponent<HealthComponent>().health / 2,
							isCrit = false,
							procChainMask = default(ProcChainMask),
							procCoefficient = 1f,
							target = character.GetBody().mainHurtBox,
						});
					});

				}
				else if (data.iconFile == "Assets/FreezeIcon.png")
				{
					GetAllCharactersInRadius(self.transform.position, 50).ForEachTry(character =>
					{

						Vector3 corePosition = Util.GetCorePosition(self.gameObject);
						GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/NetworkedObjects/GenericDelayBlast"), corePosition, Quaternion.identity);
						float num = 50;
						gameObject2.transform.localScale = new Vector3(num, num, num);
						DelayBlast component = gameObject2.GetComponent<DelayBlast>();
						component.position = corePosition;
						component.baseDamage = 5.5f;
						component.baseForce = 2300f;
						component.attacker = self.gameObject;
						component.radius = num;
						component.crit = false;
						component.procCoefficient = 0.75f;
						component.maxTimer = 2f;
						component.falloffModel = BlastAttack.FalloffModel.None;
						component.explosionEffect = Resources.Load<GameObject>("Prefabs/Effects/ImpactEffects/AffixWhiteExplosion");
						component.delayEffect = Resources.Load<GameObject>("Prefabs/Effects/AffixWhiteDelayEffect");
						component.damageType = DamageType.Freeze2s;
						gameObject2.GetComponent<TeamFilter>().teamIndex = TeamComponent.GetObjectTeam(component.attacker);

					});

				}
			}
			else
			{
				baseToken = "SHRINE_CHANCE_SUCCESS_MESSAGE";

				self.SetFieldValue<int>("successfulPurchaseCount", self.GetFieldValue<int>("successfulPurchaseCount") + 1);
				PickupDropletController.CreatePickupDroplet(pickupIndex, self.dropletOrigin.position, self.dropletOrigin.forward * 20f);
			}
			Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
			{
				subjectAsCharacterBody = activator.GetComponent<CharacterBody>(),
				baseToken = baseToken
			});

			Action<bool, Interactor> action = typeof(ShrineChanceBehavior).GetFieldValue<Action<bool, Interactor>>("onShrineChancePurchaseGlobal");
			if (action != null)
			{
				action(flag, activator);
			}

			// Remove here
			/*RemoveData.Invoke(x =>
			{
				x.Write(data.iconFile);
				x.Write(data.iconPosition);
				x.Write(data.iconSize);
				x.Write(self.gameObject);
			});*/
			new RemoveData
			{
				iconFile = data.iconFile,
				shrineObject = self.gameObject
			}.Send(NetworkDestination.Clients);

			EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
			{
				origin = self.transform.position,
				rotation = Quaternion.identity,
				scale = 1f,
				color = self.shrineColor
			}, true);

			if (self.GetFieldValue<int>("successfulPurchaseCount") >= self.maxPurchaseCount)
			{
				self.symbolTransform.gameObject.SetActive(false);
			}
		}

		public static CharacterMaster TrySpawnEnemy(CharacterSpawnCard spawnCard, EliteAffixCard affixCard, DirectorPlacementRule placement, Xoroshiro128Plus rng)
		{
			var spawnRequest = new DirectorSpawnRequest(spawnCard, placement, rng)
			{
				teamIndexOverride = TeamIndex.Monster,
				ignoreTeamMemberLimit = true
			};
			var spawned = DirectorCore.instance.TrySpawnObject(spawnRequest);

			if (spawned == null)
				return null;

			var spawnedMaster = spawned.GetComponent<CharacterMaster>();
			if (affixCard != null)
			{
				//Elites are boosted
				var healthBoost = affixCard.healthBoostCoeff;
				var damageBoost = affixCard.damageBoostCoeff;

				spawnedMaster.inventory.GiveItem(ItemIndex.BoostHp, Mathf.RoundToInt((float)((healthBoost - 1.0) * 10.0)));
				spawnedMaster.inventory.GiveItem(ItemIndex.BoostDamage, Mathf.RoundToInt((float)((damageBoost - 1.0) * 10.0)));
				var eliteDef = EliteCatalog.GetEliteDef(affixCard.eliteType);
				if (eliteDef != null)
					spawnedMaster.inventory.SetEquipmentIndex(eliteDef.eliteEquipmentIndex);

				affixCard.onSpawned?.Invoke(spawnedMaster);
			}

			return spawnedMaster;
		}
	}
}
