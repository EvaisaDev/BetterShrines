using BepInEx;
using RoR2;
using UnityEngine;
using System.Collections.Generic;
using System;
using BepInEx.Configuration;
using System.Reflection;
using MonoMod.Cil;
using R2API;
using KinematicCharacterController;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using System.Linq;
using System.Collections;
using Mono.Cecil.Cil;
using HarmonyLib;
using System.Security;
using System.Security.Permissions;
using RoR2.CharacterAI;
using RoR2.UI;
using static Evaisa.BetterShrines.ShrineImpBehaviour;
using RoR2.Navigation;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace Evaisa.BetterShrines
{
    [BepInDependency("com.bepis.r2api")]
	[BepInDependency(EsoPlugin.PluginGuid)]
	[R2APISubmoduleDependency(nameof(NetworkingAPI), nameof(LoadoutAPI), nameof(SurvivorAPI), nameof(LanguageAPI), nameof(PrefabAPI), nameof(BuffAPI), nameof(EffectAPI))]
	//[BepInDependency(MiniRpcPlugin.Dependency)]
	[BepInPlugin(ModGuid, ModName, ModVer)]
	public class BetterShrines : BaseUnityPlugin
    {
		private const string ModVer = "0.3.3";
		private const string ModName = "BetterShrines";
		private const string ModGuid = "com.Evaisa.BetterShrines";

		public static ConfigEntry<bool> enableAlternateChanceShrines;
		public static ConfigEntry<bool> enableImpShrines;
		public static ConfigEntry<bool> enableShrineOfTheFallen;
		public static ConfigEntry<bool> itemRarityBasedOnSpeed;
		public static ConfigEntry<int> extraItemCount;
		public static ConfigEntry<int> baseImpCount;
		public static ConfigEntry<int> impShrineTime;
		public static ConfigEntry<float> ImpShrineWeight;
		public static ConfigEntry<bool> allowImpElite;
		public static ConfigEntry<bool> impCountScale;
		public static ConfigEntry<float> FallenShrineWeight;
		public static ConfigEntry<int> fallenShrineUseCount;
		public static ConfigEntry<bool> fallenShrineDuringTeleporter;
		public static ConfigEntry<bool> fallenShrineScaleEachUse;
		public static ConfigEntry<int> fallenShrineBaseCost;

		ChanceShrine chanceShrine;

		public static BetterShrines instance;

		public static SpawnCard impSpawnCard;
		public static Xoroshiro128Plus EvaRng;
		public static SpawnCard impShrineSpawnCard;
		public static SpawnCard fallenShrineSpawnCard;
		public BetterShrines ()
        {
			instance = this;
			System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
			int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
			EvaRng = new Xoroshiro128Plus((ulong)cur_time);
			chanceShrine = new ChanceShrine();

			buildConfig();

			NetworkingAPI.RegisterMessageType<ChanceShrine.AddData>();
			NetworkingAPI.RegisterMessageType<ChanceShrine.RemoveData>();
			NetworkingAPI.RegisterMessageType<ChanceShrine.PlayNetworkSound>();
			NetworkingAPI.RegisterMessageType<ShrineImpBehaviour.SendImpObjects>();
			NetworkingAPI.RegisterMessageType<ShrineImpBehaviour.clientEndShrine>();
			NetworkingAPI.RegisterMessageType<ShrineImpBehaviour.requestEndShrine>();
			Tokens.Init();
			EvaResources.Init();

            if (enableShrineOfTheFallen.Value)
            {
				GenerateFallenShrine();
				//IL.RoR2.SceneDirector.PopulateScene += SceneDirector_PopulateScene_Fallen;
			}

			
			if (enableImpShrines.Value)
			{
				GenerateTinyImp();
				GenerateImpShrine();


				RoR2.UI.ObjectivePanelController.collectObjectiveSources += (master, list) =>
				{
					if (ShrineImpBehaviour.instances.Count > 0)
					{
						ShrineImpBehaviour.instances.ForEach(instance =>
						{
							if (instance.active)
							{
								var sourceDescriptor = new ObjectivePanelController.ObjectiveSourceDescriptor
								{
									source = instance.shrineBehaviour,
									master = master,
									objectiveType = typeof(ShrineImpObjective)
								};
								instance.shrineBehaviour.sourceDescriptor = sourceDescriptor;
								instance.shrineBehaviour.sourceDescriptorList = list;
								list.Add(sourceDescriptor);
							}
						});
					}
				};

                On.RoR2.PurchaseInteraction.CanBeAffordedByInteractor += PurchaseInteraction_CanBeAffordedByInteractor;

                On.RoR2.Artifacts.SwarmsArtifactManager.OnSpawnCardOnSpawnedServerGlobal += SwarmsArtifactManager_OnSpawnCardOnSpawnedServerGlobal;

				On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
			}
			if (enableAlternateChanceShrines.Value)
			{
				On.RoR2.ShrineChanceBehavior.Awake += ShrineChanceBehavior_Awake;

				On.RoR2.ShrineChanceBehavior.AddShrineStack += ShrineChanceBehavior_AddShrineStack;

				On.RoR2.Run.Start += Run_Start;
				On.RoR2.Stage.Start += Stage_Start;
				On.RoR2.UI.MainMenu.MainMenuController.Start += MainMenuController_Start;
			}
			IL.RoR2.SceneDirector.PopulateScene += SceneDirector_PopulateScene;

		}

        private bool PurchaseInteraction_CanBeAffordedByInteractor(On.RoR2.PurchaseInteraction.orig_CanBeAffordedByInteractor orig, PurchaseInteraction self, Interactor activator)
        {
            if (self.gameObject.GetComponent<ShrineFallenBehavior>())
            {
				if(self.gameObject.GetComponent<ShrineFallenBehavior>().isAvailable == false)
                {
					return false;
                }
            }
			return orig(self, activator);
        }

        private void SwarmsArtifactManager_OnSpawnCardOnSpawnedServerGlobal(On.RoR2.Artifacts.SwarmsArtifactManager.orig_OnSpawnCardOnSpawnedServerGlobal orig, SpawnCard.SpawnResult result)
        {
			if (result.spawnRequest.spawnCard as CharacterSpawnCard)
			{
				if (result.spawnedInstance.gameObject.GetComponent<CharacterMaster>())
				{
					if (!result.spawnedInstance.gameObject.GetComponent<TinyImp>())
					{
						orig(result);
					}
				}
			}
	    }

        private void CharacterMaster_OnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
			orig(self, body);
            if (body.master)
            {
				var masterObject = body.masterObject;
                if (masterObject.GetComponent<TinyImp>())
                {
					body.AddTimedBuff(BuffIndex.Immune, 2);
				}
            }
		}

        private void MainMenuController_Start(On.RoR2.UI.MainMenu.MainMenuController.orig_Start orig, RoR2.UI.MainMenu.MainMenuController self)
        {
			ChanceShrine.shrineIconMessages.Clear();
			//Print("Chance shrine data was clear!");
			orig(self);
		}

        private void Stage_Start(On.RoR2.Stage.orig_Start orig, Stage self)
        {
			ChanceShrine.shrineIconMessages.Clear();
			orig(self);
		}

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
			ChanceShrine.shrineIconMessages.Clear();
			orig(self);
        }

        public void buildConfig()
		{
			// Imp Shrine

			enableImpShrines = Config.Bind<bool>(
				"Shrine of Imps",
				"Enable Shrine of Imps",
				true,
				"Enable/Disable Shrines of Imps, they work like the imp shrines from risk of rain 1."
			);


			itemRarityBasedOnSpeed = Config.Bind<bool>(
				"Shrine of Imps",
				"Item Rarity Based On Speed",
				true,
				"Increase item rarity based on how fast you killed all the imps."
			);

			extraItemCount = Config.Bind<int>(
				"Shrine of Imps",
				"Extra Item Count",
				0,
				"Drop X extra items along with the base amount when a Shrine of Imps is beaten."
			);

			baseImpCount = Config.Bind<int>(
				"Shrine of Imps",
				"Imp Base count",
				5,
				"The base amount of imps that spawns from a Shrine of Imps, this number scales with player count and difficulty."
			);

			impShrineTime = Config.Bind<int>(
				"Shrine of Imps",
				"Time",
				30,
				"The amount of time you get to finish a Shrine of Imps."
			);

			impCountScale = Config.Bind<bool>(
				"Shrine of Imps",
				"Count Scale",
				false,
				"Scale the amount of imps with stage difficulty."
			);

			allowImpElite = Config.Bind<bool>(
				"Shrine of Imps",
				"Allow Imp Elites",
				true,
				"Allow imps to spawn as elite, scaling with difficulty."
			);

			ImpShrineWeight = Config.Bind<float>(
				"Shrine of Imps",
				"Spawn Weight",
				3.5f,
				"The spawn weight of Shrine of Imps, increase this number to make Shrine of Imps more common, do keep in mind this will make other interactibles like chests more rare."
			);

			// Chance Shrine

			enableAlternateChanceShrines = Config.Bind<bool>(
				"Shrine of Chance",
				"Alternate Chance Shrines",
				false,
				"Enable/Disable alternate chance shrines, likely incompatible with other mods that modify chance shrines. May also hurt performance slightly."
			);



			// Fallen Shrine

			enableShrineOfTheFallen = Config.Bind<bool>(
				"Shrine of the Fallen",
				"Enable Shrine of the Fallen",
				true,
				"Enable/Disable Shrine of the Fallen, these let you revive fallen players."
			);


			fallenShrineUseCount = Config.Bind<int>(
				"Shrine of the Fallen",
				"Max Use Count",
				1,
				"The amount of times a single Shrine of the Fallen can be used."
			);

			fallenShrineDuringTeleporter = Config.Bind<bool>(
				"Shrine of the Fallen",
				"Allow Use During Teleporter",
				false,
				"Allow use of Shrine of the Fallen while the teleporter charges."
			);

			fallenShrineBaseCost = Config.Bind<int>(
				"Shrine of the Fallen",
				"Base Cost",
				300,
				"The starting cost of Shrine of the Fallen."
			);

			fallenShrineScaleEachUse = Config.Bind<bool>(
				"Shrine of the Fallen",
				"Increase Cost Per Use",
				false,
				"Increase cost of Shrine of the Fallen each time it is used."
			);

			FallenShrineWeight = Config.Bind<float>(
				"Shrine of the Fallen",
				"Spawn Weight",
				3f,
				"The spawn weight of Shrines of the Fallen, increase this number to make Shrines of the Fallen more common, do keep in mind this will make other interactibles like chests more rare."
			);
			
		}

		public void GenerateTinyImp()
        {

			var impCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
			var impCardOriginal = Resources.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscImp");
			impCard.directorCreditCost = 10;
			impCard.forbiddenFlags = NodeFlags.None;
			impCard.hullSize = HullClassification.Human;
			impCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
			impCard.occupyPosition = false;
			impCard.requiredFlags = NodeFlags.None;
			impCard.sendOverNetwork = true;
			impCard.forbiddenAsBoss = true;
			impCard.noElites = false;
			impCard.loadout = impCardOriginal.loadout;

			var impPrefab = PrefabAPI.InstantiateClone(impCardOriginal.prefab, "TinyImpMaster");

			var impMaster = impPrefab.GetComponent<CharacterMaster>();

			var impBody = PrefabAPI.InstantiateClone(impMaster.bodyPrefab, "TinyImpBody");

			impMaster.bodyPrefab = impBody;

			var impCharBody = impBody.GetComponent<CharacterBody>();

			var impModelTransform = impBody.GetComponent<ModelLocator>().modelTransform;

			impModelTransform.localScale = impModelTransform.localScale / 2f;

			var skillDrivers = impPrefab.GetComponents<AISkillDriver>();

			impCharBody.baseMaxHealth = impCharBody.baseMaxHealth / 2;

			impCharBody.levelMaxHealth = impCharBody.levelMaxHealth / 2;

			impCharBody.baseJumpPower = impCharBody.baseJumpPower / 5;

			impCharBody.levelJumpPower = 0;

			impCharBody.baseMoveSpeed = impCharBody.baseMoveSpeed * 1.5f;

			foreach (var oldDriver in skillDrivers)
			{
				Object.Destroy(oldDriver);
				Print("Old skill destroyed");
				//Print("Skill destroyed");
				/*if (oldDriver.skillSlot != SkillSlot.Utility)
				{
					Object.Destroy(oldDriver);
					Print("Skill destroyed");
                }
                else
                {
					oldDriver.movementType = AISkillDriver.MovementType.FleeMoveTarget;
					oldDriver.aimType = AISkillDriver.AimType.MoveDirection;
					oldDriver.maxDistance = 30;
                }*/
			}

			var walkDriver = impPrefab.AddComponent<AISkillDriver>();
			walkDriver.minDistance = 0;
			walkDriver.maxDistance = 150;
			walkDriver.aimType = AISkillDriver.AimType.MoveDirection;
			walkDriver.ignoreNodeGraph = false;
			walkDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
			walkDriver.shouldSprint = true;
			walkDriver.movementType = AISkillDriver.MovementType.FleeMoveTarget;
			walkDriver.moveInputScale = 1.0f;
			walkDriver.driverUpdateTimerOverride = -1;
			walkDriver.skillSlot = SkillSlot.None;

			impPrefab.AddComponent<TinyImp>();

			//impPrefab.GetComponent<BaseAI>().localNavigator.allowWalkOffCliff = false;

			BodyCatalog.getAdditionalEntries += delegate (List<GameObject> list)
			{

				Print("getAdditionalEntries");
				list.Add(impMaster.bodyPrefab);
			};

			On.RoR2.LocalNavigator.Update += LocalNavigator_Update;

			impCard.prefab = impPrefab;
			impSpawnCard = impCard; // set a public static
		}

        private void LocalNavigator_Update(On.RoR2.LocalNavigator.orig_Update orig, LocalNavigator self, float deltaTime)
        {

			if (self.body)
			{
				if (self.body.master)
				{
					if (self.body.master.gameObject)
					{
						if (self.body.master.gameObject.GetComponent<TinyImp>())
						{
							self.allowWalkOffCliff = false;
							self.lookAheadDistance = 12f;
						}

						//print("lookAheadDistance: " + self.lookAheadDistance);
					}
				}
			}
			orig(self, deltaTime);
		}

		public void GenerateFallenShrine()
		{

			var newSpawnCard = ScriptableObject.CreateInstance<SpawnCard>();

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineHealing");

			newSpawnCard.directorCreditCost = ChanceCard.directorCreditCost;
			newSpawnCard.forbiddenFlags = ChanceCard.forbiddenFlags;
			newSpawnCard.hullSize = ChanceCard.hullSize;
			newSpawnCard.nodeGraphType = ChanceCard.nodeGraphType;
			newSpawnCard.occupyPosition = ChanceCard.occupyPosition;
			newSpawnCard.requiredFlags = ChanceCard.requiredFlags;
			newSpawnCard.sendOverNetwork = ChanceCard.sendOverNetwork;

			var oldPrefab = ChanceCard.prefab;
			var oldSymbol = oldPrefab.transform.Find("Symbol");
			var oldSymbolRenderer = oldSymbol.GetComponent<MeshRenderer>();
			var oldSymbolMaterial = oldSymbolRenderer.material;



			newSpawnCard.prefab = (GameObject)Evaisa.BetterShrines.EvaResources.ShrineFallenPrefab;
			var mdlBase = newSpawnCard.prefab.transform.Find("Base").Find("mdlShrineFallen");

			mdlBase.GetComponent<MeshRenderer>().material.shader = Shader.Find("Hopoo Games/Deferred/Standard");
			mdlBase.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.8549f, 0.7647f, 1.0f);

			var symbolTransform = newSpawnCard.prefab.transform.Find("Symbol");

			var purchaseInteraction = newSpawnCard.prefab.GetComponent<PurchaseInteraction>();
			purchaseInteraction.Networkcost = BetterShrines.fallenShrineBaseCost.Value;
			purchaseInteraction.cost = BetterShrines.fallenShrineBaseCost.Value;
			purchaseInteraction.setUnavailableOnTeleporterActivated = !BetterShrines.fallenShrineDuringTeleporter.Value;
			//purchaseInteraction.automaticallyScaleCostWithDifficulty = false;

			var symbolMesh = symbolTransform.gameObject.GetComponent<MeshRenderer>();

			var texture = symbolMesh.material.mainTexture;

			symbolMesh.material = new Material(Shader.Find("Hopoo Games/FX/Cloud Remap"));

			symbolMesh.material.CopyPropertiesFromMaterial(oldSymbolMaterial);
			symbolMesh.material.mainTexture = texture;

			var fallenBehaviour = newSpawnCard.prefab.AddComponent<ShrineFallenBehavior>();
			fallenBehaviour.shrineEffectColor = new Color(0.384f, 0.874f, 0.435f);
			fallenBehaviour.symbolTransform = symbolTransform;
			fallenBehaviour.maxUses = fallenShrineUseCount.Value;
			fallenBehaviour.scalePerUse = fallenShrineScaleEachUse.Value;

			newSpawnCard.prefab.AddComponent<modifyAfterSpawn>();

			PrefabAPI.RegisterNetworkPrefab(newSpawnCard.prefab);

			fallenShrineSpawnCard = newSpawnCard;
		}

		public void GenerateImpShrine()
        {

			var newSpawnCard = ScriptableObject.CreateInstance<SpawnCard>();

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineChance");

			newSpawnCard.directorCreditCost = ChanceCard.directorCreditCost;
			newSpawnCard.forbiddenFlags = ChanceCard.forbiddenFlags;
			newSpawnCard.hullSize = ChanceCard.hullSize;
			newSpawnCard.nodeGraphType = ChanceCard.nodeGraphType;
			newSpawnCard.occupyPosition = ChanceCard.occupyPosition;
			newSpawnCard.requiredFlags = ChanceCard.requiredFlags;
			newSpawnCard.sendOverNetwork = ChanceCard.sendOverNetwork;

			var oldPrefab = ChanceCard.prefab;
			var oldSymbol = oldPrefab.transform.Find("Symbol");
			var oldSymbolRenderer = oldSymbol.GetComponent<MeshRenderer>();
			var oldSymbolMaterial = oldSymbolRenderer.material;



			newSpawnCard.prefab = (GameObject)Evaisa.BetterShrines.EvaResources.ShrineImpPrefab;
			var mdlBase = newSpawnCard.prefab.transform.Find("Base").Find("mdlShrineImp");

			mdlBase.GetComponent<MeshRenderer>().material.shader = Shader.Find("Hopoo Games/Deferred/Standard");
			mdlBase.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.8549f, 0.7647f, 1.0f);

			var symbolTransform = newSpawnCard.prefab.transform.Find("Symbol");



			var symbolMesh = symbolTransform.gameObject.GetComponent<MeshRenderer>();

			var texture = symbolMesh.material.mainTexture;

			symbolMesh.material = new Material(Shader.Find("Hopoo Games/FX/Cloud Remap"));

			symbolMesh.material.CopyPropertiesFromMaterial(oldSymbolMaterial);
			symbolMesh.material.mainTexture = texture;

			var impBehaviour = newSpawnCard.prefab.AddComponent<ShrineImpBehaviour>();
			impBehaviour.shrineEffectColor = new Color(0.6661001f, 0.5333304f, 0.8018868f);
			impBehaviour.symbolTransform = symbolTransform;

			newSpawnCard.prefab.AddComponent<modifyAfterSpawn>();

			PrefabAPI.RegisterNetworkPrefab(newSpawnCard.prefab);

			impShrineSpawnCard = newSpawnCard;
		}

		/*
        private void SceneDirector_PopulateScene(ILContext il)
        {
			var cursor = new ILCursor(il);
			cursor.GotoNext(MoveType.After, x => x.MatchCallvirt("RoR2.SceneDirector", "GenerateInteractableCardSelection"));
			cursor.Index += 1;
			cursor.Emit(OpCodes.Ldloc_0);
			cursor.EmitDelegate<System.Action<WeightedSelection<DirectorCard>>>(AddObject);
        }

		private void AddObject(WeightedSelection<DirectorCard> weightedSelection)
		{
			Debug.Log("wtf is happening");
			weightedSelection.choices.ForEachTry(choice => 
			{
				Debug.Log("Object Weight: "+choice.weight);
				Debug.Log("Object Prefab: "+choice.value.spawnCard.prefab.name);
			});
		}
		*/

		private void SceneDirector_PopulateScene(ILContext il)
		{
			var cursor = new ILCursor(il);
			cursor.GotoNext(MoveType.After, x => x.MatchCallvirt("RoR2.SceneDirector", "GenerateInteractableCardSelection"));
			cursor.Index += 1;
			cursor.Emit(OpCodes.Ldloc_0);
			cursor.EmitDelegate<System.Action<WeightedSelection<DirectorCard>>>(AddObject);
		}

		private void AddObject(WeightedSelection<DirectorCard> weightedSelection)
		{
			if (enableImpShrines.Value)
			{
				addImpShrine(weightedSelection);
			}
			if (enableShrineOfTheFallen.Value && RoR2Application.isInMultiPlayer)
			{
				addFallenShrine(weightedSelection);
				Print("Player is in multiplayer!");
			}
		}

		public static void Print(string message)
		{
			Debug.Log("[Better Shrines] " + message);
		}

		private void addImpShrine(WeightedSelection<DirectorCard> weightedSelection)
        {
			var newDirectorCard = new DirectorCard();
			//Print(impShrineSpawnCard.prefab.name + " has been loaded!");

			newDirectorCard.spawnCard = impShrineSpawnCard;
			newDirectorCard.selectionWeight = 3;
			newDirectorCard.spawnDistance = DirectorCore.MonsterSpawnDistance.Close;
			newDirectorCard.allowAmbushSpawn = true;
			newDirectorCard.preventOverhead = false;
			newDirectorCard.minimumStageCompletions = 0;
			newDirectorCard.requiredUnlockable = "";
			newDirectorCard.forbiddenUnlockable = "";

			weightedSelection.AddChoice(newDirectorCard, ImpShrineWeight.Value);

		/*	weightedSelection.choices.ForEachTry(choice =>
			{
				//Print(choice.value.spawnCard.prefab.name);
				Print("Name: "+choice.value.spawnCard.prefab.name);
				Print("Weight: " + choice.weight);
			});*/
		}

		private void addFallenShrine(WeightedSelection<DirectorCard> weightedSelection)
		{
			var newDirectorCard = new DirectorCard();

			newDirectorCard.spawnCard = fallenShrineSpawnCard;
			newDirectorCard.selectionWeight = 3;
			newDirectorCard.spawnDistance = DirectorCore.MonsterSpawnDistance.Close;
			newDirectorCard.allowAmbushSpawn = true;
			newDirectorCard.preventOverhead = false;
			newDirectorCard.minimumStageCompletions = 0;
			newDirectorCard.requiredUnlockable = "";
			newDirectorCard.forbiddenUnlockable = "";

			weightedSelection.AddChoice(newDirectorCard, FallenShrineWeight.Value);

			/*	weightedSelection.choices.ForEachTry(choice =>
				{
					//Print(choice.value.spawnCard.prefab.name);
					Print("Name: "+choice.value.spawnCard.prefab.name);
					Print("Weight: " + choice.weight);
				});*/
		}

		private void ShrineChanceBehavior_AddShrineStack(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, ShrineChanceBehavior self, Interactor activator)
        {
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineChanceBehavior::AddShrineStack(RoR2.Interactor)' called on client");
				return;
			}

			object[] parms = new object[] { self, activator };

			StartCoroutine(chanceShrine.ChanceGambleCoroutine(parms));
		}

        private void ShrineChanceBehavior_Awake(On.RoR2.ShrineChanceBehavior.orig_Awake orig, ShrineChanceBehavior self)
        {
			TeamComponent component = self.gameObject.AddComponent<TeamComponent>();
			component.teamIndex = TeamIndex.Neutral;

			var SpritePosition = self.symbolTransform.transform.localPosition + self.symbolTransform.up * -0.2f;

			//UIUtils.CreateCanvasImage(SpritePosition, new Vector2(75, 75), "Assets/SmiteIcon.png");
			if (!self.transform.Find("iconDisplay"))
			{
				GameObject display = Instantiate((GameObject)EvaResources.IconPrefab, self.symbolTransform.transform.position + self.symbolTransform.up * -0.2f, Quaternion.identity); ;
				//display.AddComponent<SpriteRenderer>();
				display.name = "iconDisplay";
				display.transform.SetParent(self.transform, false);
				display.transform.localPosition = self.symbolTransform.transform.localPosition + self.symbolTransform.up * -0.2f;
				display.layer = LayerMask.NameToLayer("TransparentFX");
				display.SetActive(false);
				//Debug.Log(.name);


				/*
				GameObject area = Instantiate((GameObject)EvaResources.LightningAreaPrefab, self.symbolTransform.transform.position + self.symbolTransform.up * -0.2f, Quaternion.identity);
				area.name = "LightningArea";
				area.transform.SetParent(self.transform, false);
				area.transform.localPosition = self.symbolTransform.transform.localPosition + self.symbolTransform.up * -0.2f;
				area.SetActive(false);

				*/

			}
			orig(self);
		}

        void Update()
        {
			if (enableAlternateChanceShrines.Value)
			{
				chanceShrine.drawChanceShrineDisplay();
			}
			
		}
    }
}
