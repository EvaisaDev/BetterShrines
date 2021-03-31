using System;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using R2API.Utils;
using System.Collections.Generic;

namespace Evaisa.BetterShrines
{
	[RequireComponent(typeof(PurchaseInteraction))]
	public class ShrineFallenBehavior : MonoBehaviour
	{
		public Interactor whoInteracted;
		public Transform symbolTransform;
		public Color shrineEffectColor;
		public int maxUses = 1;
		public int timesUsed = 0;
		public bool scalePerUse;
		public float scaleMultiplier = 1.5f;
		public float wait = 2f;
		public float stopwatch;
		public PurchaseInteraction purchaseInteraction;
		public bool inUse = false;
		public bool isAvailable = true;
		
		public void Awake()
        {
			purchaseInteraction = GetComponent<PurchaseInteraction>();

			//BetterShrines.Print("Scaled cost: "+GetDifficultyScaledCost(BetterShrines.fallenShrineBaseCost.Value));

			//purchaseInteraction.Networkcost = GetDifficultyScaledCost(BetterShrines.fallenShrineBaseCost.Value);
		}

		public void Update()
        {
			//BetterShrines.Print(Run.instance.difficultyCoefficient);
			if (timesUsed < maxUses)
			{
				if (inUse)
				{
					stopwatch -= Time.deltaTime;
					if (stopwatch < 0)
					{
						inUse = false;
					}
				}
				else
				{
					if (IsAnyoneDead())
					{
						
						symbolTransform.gameObject.SetActive(true);
						isAvailable = true;
					}
					else
					{
						symbolTransform.gameObject.SetActive(false);
						isAvailable = false;

					}
					if (NetworkServer.active)
					{
						purchaseInteraction.SetAvailable(true);
					}
				}
            }
            else
            {
				symbolTransform.gameObject.SetActive(false);
				isAvailable = false;
				if (NetworkServer.active)
				{
					purchaseInteraction.SetAvailable(false);
				}
			}

		}
		
		public void AddShrineStack(Interactor interactor)
		{
			BetterShrines.Print(interactor.name + " has used a Shrine of the Fallen");
			if (IsAnyoneDead())
			{
				timesUsed += 1;
				stopwatch = wait;
				inUse = true;
				whoInteracted = interactor;
				symbolTransform.gameObject.SetActive(false);
				EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
				{
					origin = base.transform.position,
					rotation = Quaternion.identity,
					scale = 1f,
					color = shrineEffectColor
				}, true);

				

				if (scalePerUse)
                {
					purchaseInteraction.Networkcost = (int)Math.Round(purchaseInteraction.cost * scaleMultiplier);
					purchaseInteraction.cost = (int)Math.Round(purchaseInteraction.cost * scaleMultiplier);
                }



				if (NetworkServer.active)
				{
					BetterShrines.Print("attempting to revive user.");
					var player = getRandomDeadPlayer();
					if (player != null)
					{
						player.master.RespawnExtraLife();
						Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
						{
							subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
							baseToken = "SHRINE_FALLEN_USED",
							paramTokens = new string[]
							{
								player.networkUser.userName
							}
						});
					}
				}
			}
		}

		public bool IsAnyoneDead()
        {
			var isAnyoneDead = false;
			PlayerCharacterMasterController.instances.ForEachTry(instance =>
			{
                if (instance.master)
                {
					CharacterBody body = instance.master.GetBody();
					if ((!body || !body.healthComponent.alive) && instance.master.inventory.GetItemCount(ItemIndex.ExtraLife) <= 0)
                    {
						isAnyoneDead = true;
                    }
                }
			});
			return isAnyoneDead;
        }

		public PlayerCharacterMasterController getRandomDeadPlayer()
        {
			var deadPlayers = new List<PlayerCharacterMasterController>();
			PlayerCharacterMasterController.instances.ForEachTry(instance =>
			{
				if (instance.master)
				{
					CharacterBody body = instance.master.GetBody();
					if ((!body || !body.healthComponent.alive) && instance.master.inventory.GetItemCount(ItemIndex.ExtraLife) <= 0)
					{
						deadPlayers.Add(instance);
					}
				}
			});
			if(deadPlayers.Count == 0)
            {
				return null;
            }
			var player = BetterShrines.EvaRng.NextElementUniform<PlayerCharacterMasterController>(deadPlayers);
			return player;
		}

	}
}
