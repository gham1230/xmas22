using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

// Token: 0x02000140 RID: 320
public abstract class GorillaGameManager : MonoBehaviourPunCallbacks, IInRoomCallbacks, IPunInstantiateMagicCallback
{
	// Token: 0x060006BE RID: 1726 RVA: 0x00006573 File Offset: 0x00004773
	public virtual void Awake()
	{
		this.currentRoom = PhotonNetwork.CurrentRoom;
		this.currentPlayerArray = PhotonNetwork.PlayerList;
		this.DestroyInvalidManager();
		this.localPlayerProjectileCounter = 0;
		this.playerProjectiles.Add(PhotonNetwork.LocalPlayer, new List<GorillaGameManager.ProjectileInfo>());
	}

	// Token: 0x060006BF RID: 1727 RVA: 0x0002B1AC File Offset: 0x000293AC
	public virtual void Update()
	{
		if (GorillaGameManager.instance == null)
		{
			GorillaGameManager.instance = this;
		}
		else if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient && GorillaGameManager.instance != this && PhotonNetwork.IsMasterClient)
		{
			PhotonNetwork.Destroy(base.photonView);
		}
		if (this.lastCheck + this.checkCooldown < Time.time)
		{
			List<string> list = new List<string>();
			foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerListOthers)
			{
				string text;
				if (!this.playerCosmeticsLookup.TryGetValue(player.UserId, out text))
				{
					list.Add(player.UserId);
				}
			}
			if (list.Count > 0)
			{
				Debug.Log("group id to look up: " + PhotonNetwork.CurrentRoom.Name + Regex.Replace(PhotonNetwork.CloudRegion, "[^a-zA-Z0-9]", "").ToUpper());
				foreach (string key in list)
				{
					this.playerCosmeticsLookup[key] = GorillaGameManager.VRRigData.allcosmetics;
				}
				GetSharedGroupDataRequest getSharedGroupDataRequest = new GetSharedGroupDataRequest();
				getSharedGroupDataRequest.Keys = list;
				getSharedGroupDataRequest.SharedGroupId = PhotonNetwork.CurrentRoom.Name + Regex.Replace(PhotonNetwork.CloudRegion, "[^a-zA-Z0-9]", "").ToUpper();
				PlayFabClientAPI.GetSharedGroupData(getSharedGroupDataRequest, delegate (GetSharedGroupDataResult result)
				{
					foreach (KeyValuePair<string, SharedGroupDataRecord> keyValuePair in result.Data)
					{
						this.playerCosmeticsLookup[keyValuePair.Key] = keyValuePair.Value.Value;
						if (base.photonView.IsMine && keyValuePair.Value.Value == "BANNED")
						{
							foreach (Photon.Realtime.Player player2 in PhotonNetwork.PlayerList)
							{
								if (player2.UserId == keyValuePair.Key)
								{
									Debug.Log("this guy needs banned: " + player2.NickName);
									PhotonNetwork.CloseConnection(player2);
								}
							}
						}
					}
				}, delegate (PlayFabError error)
				{
					Debug.Log("Got error retrieving user data:");
					Debug.Log(error.GenerateErrorReport());
					if (error.Error == PlayFabErrorCode.NotAuthenticated)
					{
						PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
						return;
					}
					if (error.Error == PlayFabErrorCode.AccountBanned)
					{
						Application.Quit();
						PhotonNetwork.Disconnect();
					}
				}, null, null);
			}
			this.lastCheck = Time.time;
			if (base.photonView.IsMine && PhotonNetwork.InRoom)
			{
				int num = 0;
				if (PhotonNetwork.CurrentRoom.ExpectedUsers != null && PhotonNetwork.CurrentRoom.ExpectedUsers.Length != 0)
				{
					foreach (string key2 in PhotonNetwork.CurrentRoom.ExpectedUsers)
					{
						float num2;
						if (this.expectedUsersDecay.TryGetValue(key2, out num2))
						{
							if (num2 + this.userDecayTime < Time.time)
							{
								num++;
							}
						}
						else
						{
							this.expectedUsersDecay.Add(key2, Time.time);
						}
					}
					if (num >= PhotonNetwork.CurrentRoom.ExpectedUsers.Length && num != 0)
					{
						PhotonNetwork.CurrentRoom.ClearExpectedUsers();
					}
				}
			}
			this.InfrequentUpdate();
		}
	}

	// Token: 0x060006C0 RID: 1728 RVA: 0x000065AD File Offset: 0x000047AD
	public virtual void InfrequentUpdate()
	{
		this.currentPlayerArray = PhotonNetwork.PlayerList;
	}

	// Token: 0x060006C1 RID: 1729 RVA: 0x000065BA File Offset: 0x000047BA
	public virtual string GameMode()
	{
		return "NONE";
	}

	// Token: 0x060006C2 RID: 1730 RVA: 0x000023F3 File Offset: 0x000005F3
	public virtual void ReportTag(Photon.Realtime.Player taggedPlayer, Photon.Realtime.Player taggingPlayer)
	{
	}

	// Token: 0x060006C3 RID: 1731 RVA: 0x0002B414 File Offset: 0x00029614
	public void ReportStep(VRRig steppingRig, bool isLeftHand, float velocityRatio)
	{
		float num = 0f;
		if (isLeftHand)
		{
			num = 1f;
		}
		PhotonView.Get(steppingRig).RPC("PlayHandTap", RpcTarget.All, new object[]
		{
			num + Mathf.Max(velocityRatio * this.stepVolumeMax, this.stepVolumeMin)
		});
		Debug.Log(string.Concat(new object[]
		{
			"bbbb:sending tap to ",
			isLeftHand.ToString(),
			" at volume ",
			Mathf.Max(velocityRatio * this.stepVolumeMax, this.stepVolumeMin)
		}));
	}

	// Token: 0x060006C4 RID: 1732 RVA: 0x0002B4AC File Offset: 0x000296AC
	void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
	{
		if (info.Sender != null && !PhotonNetwork.CurrentRoom.Players.TryGetValue(info.Sender.ActorNumber, out this.outPlayer) && info.Sender != PhotonNetwork.MasterClient)
		{
			GorillaNot.instance.SendReport("trying to inappropriately create game managers", info.Sender.UserId, info.Sender.NickName);
			if (PhotonNetwork.IsMasterClient)
			{
				PhotonNetwork.Destroy(base.gameObject);
				return;
			}
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		else
		{
			if (info.Sender == null || !(GorillaGameManager.instance != null) || !(GorillaGameManager.instance != this))
			{
				if ((GorillaGameManager.instance == null && info.Sender != null && info.Sender.ActorNumber == PhotonNetwork.CurrentRoom.MasterClientId) || (base.photonView.Owner != null && base.photonView.Owner.ActorNumber == PhotonNetwork.CurrentRoom.MasterClientId))
				{
					GorillaGameManager.instance = this;
				}
				else if (GorillaGameManager.instance != null && GorillaGameManager.instance != this)
				{
					if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
					{
						Debug.Log("existing game manager! i'm host. destroying newly created manager");
						PhotonNetwork.Destroy(base.photonView);
						return;
					}
					Debug.Log("existing game manager! i'm not host. destroying newly created manager");
					UnityEngine.Object.Destroy(this);
					return;
				}
				base.transform.parent = GorillaParent.instance.transform;
				return;
			}
			GorillaNot.instance.SendReport("trying to create multiple game managers", info.Sender.UserId, info.Sender.NickName);
			if (PhotonNetwork.IsMasterClient)
			{
				PhotonNetwork.Destroy(base.gameObject);
				return;
			}
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
	}

	// Token: 0x060006C5 RID: 1733 RVA: 0x0002B670 File Offset: 0x00029870
	public virtual void NewVRRig(Photon.Realtime.Player player, int vrrigPhotonViewID, bool didTutorial)
	{
		if (this.playerVRRigDict.ContainsKey(player.ActorNumber))
		{
			this.playerVRRigDict[player.ActorNumber] = PhotonView.Find(vrrigPhotonViewID).GetComponent<VRRig>();
			return;
		}
		this.playerVRRigDict.Add(player.ActorNumber, PhotonView.Find(vrrigPhotonViewID).GetComponent<VRRig>());
	}

	// Token: 0x060006C6 RID: 1734 RVA: 0x00002217 File Offset: 0x00000417
	public virtual bool LocalCanTag(Photon.Realtime.Player myPlayer, Photon.Realtime.Player otherPlayer)
	{
		return false;
	}

	// Token: 0x060006C7 RID: 1735 RVA: 0x0002B6CC File Offset: 0x000298CC
	public virtual PhotonView FindVRRigForPlayer(Photon.Realtime.Player player)
	{
		if (player == null)
		{
			return null;
		}
		if (GorillaParent.instance.vrrigDict.TryGetValue(player, out this.returnRig) && this.returnRig != null)
		{
			if (this.returnRig != null && this.returnRig.photonView != null)
			{
				return this.returnRig.photonView;
			}
			return null;
		}
		else
		{
			if (this.playerVRRigDict.TryGetValue(player.ActorNumber, out this.returnRig))
			{
				return this.returnRig.photonView;
			}
			foreach (VRRig vrrig in GorillaParent.instance.vrrigs)
			{
				if (!vrrig.isOfflineVRRig && vrrig.GetComponent<PhotonView>().Owner == player)
				{
					return vrrig.GetComponent<PhotonView>();
				}
			}
			return null;
		}
	}

	// Token: 0x060006C8 RID: 1736 RVA: 0x0002B7C4 File Offset: 0x000299C4
	public static PhotonView StaticFindRigForPlayer(Photon.Realtime.Player player)
	{
		if (GorillaGameManager.instance != null)
		{
			return GorillaGameManager.instance.FindVRRigForPlayer(player);
		}
		if (player == null)
		{
			return null;
		}
		VRRig vrrig;
		if (GorillaParent.instance.vrrigDict.TryGetValue(player, out vrrig))
		{
			return vrrig.photonView;
		}
		foreach (VRRig vrrig2 in GorillaParent.instance.vrrigs)
		{
			if (!vrrig2.isOfflineVRRig && vrrig2.GetComponent<PhotonView>().Owner == player)
			{
				return vrrig2.GetComponent<PhotonView>();
			}
		}
		return null;
	}

	// Token: 0x060006C9 RID: 1737 RVA: 0x000065C1 File Offset: 0x000047C1
	[PunRPC]
	public virtual void ReportTagRPC(Photon.Realtime.Player taggedPlayer, PhotonMessageInfo info)
	{
		GorillaNot.IncrementRPCCall(info, "ReportTagRPC");
		this.ReportTag(taggedPlayer, info.Sender);
	}

	// Token: 0x060006CA RID: 1738 RVA: 0x0002B878 File Offset: 0x00029A78
	public void AttemptGetNewPlayerCosmetic(Photon.Realtime.Player playerToUpdate, int attempts)
	{
		List<string> list = new List<string>();
		list.Add(playerToUpdate.UserId);
		GetSharedGroupDataRequest getSharedGroupDataRequest = new GetSharedGroupDataRequest();
		getSharedGroupDataRequest.Keys = list;
		getSharedGroupDataRequest.SharedGroupId = PhotonNetwork.CurrentRoom.Name + Regex.Replace(PhotonNetwork.CloudRegion, "[^a-zA-Z0-9]", "").ToUpper();
		PlayFabClientAPI.GetSharedGroupData(getSharedGroupDataRequest, delegate (GetSharedGroupDataResult result)
		{
			foreach (KeyValuePair<string, SharedGroupDataRecord> keyValuePair in result.Data)
			{
				Debug.Log("for player " + playerToUpdate.UserId);
				Debug.Log("current allowed: " + this.playerCosmeticsLookup[keyValuePair.Key]);
				Debug.Log("new allowed: " + keyValuePair.Value.Value);
				if (this.playerCosmeticsLookup[keyValuePair.Key] != keyValuePair.Value.Value)
				{
					this.playerCosmeticsLookup[keyValuePair.Key] = keyValuePair.Value.Value;
					this.FindVRRigForPlayer(playerToUpdate).GetComponent<VRRig>().UpdateAllowedCosmetics();
					this.FindVRRigForPlayer(playerToUpdate).GetComponent<VRRig>().SetCosmeticsActive();
					Debug.Log("success on attempt " + attempts);
				}
				else if (attempts - 1 >= 0)
				{
					Debug.Log("failure on attempt " + attempts + ". trying again");
					this.AttemptGetNewPlayerCosmetic(playerToUpdate, attempts - 1);
				}
			}
		}, delegate (PlayFabError error)
		{
			Debug.Log("Got error retrieving user data:");
			Debug.Log(error.GenerateErrorReport());
			if (error.Error == PlayFabErrorCode.NotAuthenticated)
			{
				PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
				return;
			}
			if (error.Error == PlayFabErrorCode.AccountBanned)
			{
				Application.Quit();
				PhotonNetwork.Disconnect();
				UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
				UnityEngine.Object.DestroyImmediate(GorillaLocomotion.Player.Instance);
				GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
				for (int i = 0; i < array.Length; i++)
				{
					UnityEngine.Object.Destroy(array[i]);
				}
			}
		}, null, null);
	}

	// Token: 0x060006CB RID: 1739 RVA: 0x000065DB File Offset: 0x000047DB
	public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
	{
		base.OnPlayerLeftRoom(otherPlayer);
		this.playerVRRigDict.Remove(otherPlayer.ActorNumber);
		this.playerCosmeticsLookup.Remove(otherPlayer.UserId);
		this.currentPlayerArray = PhotonNetwork.PlayerList;
	}

	// Token: 0x060006CC RID: 1740 RVA: 0x00006613 File Offset: 0x00004813
	public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		base.OnPlayerEnteredRoom(newPlayer);
		this.currentPlayerArray = PhotonNetwork.PlayerList;
	}

	// Token: 0x060006CD RID: 1741 RVA: 0x00006627 File Offset: 0x00004827
	public override void OnJoinedRoom()
	{
		base.OnJoinedRoom();
		this.currentPlayerArray = PhotonNetwork.PlayerList;
	}

	// Token: 0x060006CE RID: 1742 RVA: 0x0000663A File Offset: 0x0000483A
	[PunRPC]
	public void UpdatePlayerCosmetic(PhotonMessageInfo info)
	{
		GorillaNot.IncrementRPCCall(info, "UpdatePlayerCosmetic");
		this.AttemptGetNewPlayerCosmetic(info.Sender, 2);
	}

	// Token: 0x060006CF RID: 1743 RVA: 0x0002B924 File Offset: 0x00029B24
	[PunRPC]
	public void JoinPubWithFriends(PhotonMessageInfo info)
	{
		GorillaNot.IncrementRPCCall(info, "JoinPubWithFriends");
		if (GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(PhotonNetwork.LocalPlayer.UserId))
		{
			this.startingToLookForFriend = Time.time;
			PhotonNetworkController.Instance.AttemptToFollowFriendIntoPub(info.Sender.UserId, info.Sender.ActorNumber, PhotonNetwork.CurrentRoom.Name);
			return;
		}
		GorillaNot.instance.SendReport("possible kick attempt", info.Sender.UserId, info.Sender.NickName);
	}

	// Token: 0x060006D0 RID: 1744 RVA: 0x00006654 File Offset: 0x00004854
	public virtual float[] LocalPlayerSpeed()
	{
		return new float[]
		{
			6.5f,
			1.1f
		};
	}

	// Token: 0x060006D1 RID: 1745 RVA: 0x0002B9C0 File Offset: 0x00029BC0
	public bool FindUserIDInRoom(string userID)
	{
		Photon.Realtime.Player[] playerList = PhotonNetwork.PlayerList;
		for (int i = 0; i < playerList.Length; i++)
		{
			if (playerList[i].UserId == userID)
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x060006D2 RID: 1746 RVA: 0x00002217 File Offset: 0x00000417
	public virtual int MyMatIndex(Photon.Realtime.Player forPlayer)
	{
		return 0;
	}

	// Token: 0x060006D3 RID: 1747 RVA: 0x0002B9F4 File Offset: 0x00029BF4
	public virtual void DestroyInvalidManager()
	{
		if (PhotonNetwork.InRoom)
		{
			PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameMode", out this.obj);
			if (!this.obj.ToString().Contains(this.GameMode()))
			{
				if (base.photonView.IsMine)
				{
					PhotonNetwork.Destroy(base.photonView);
					return;
				}
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	// Token: 0x060006D4 RID: 1748 RVA: 0x0002BA6C File Offset: 0x00029C6C
	[PunRPC]
	public void LaunchSlingshotProjectile(Vector3 slingshotLaunchLocation, Vector3 slingshotLaunchVelocity, int projHash, int trailHash, bool forLeftHand, int projectileCount, PhotonMessageInfo info)
	{
		GorillaNot.IncrementRPCCall(info, "LaunchSlingshotProjectile");
		this.tempRig = this.FindVRRigForPlayer(info.Sender).GetComponent<VRRig>();
		if (this.tempRig != null && (this.tempRig.transform.position - slingshotLaunchLocation).magnitude < this.tagDistanceThreshold)
		{
			this.tempRig.slingshot.LaunchNetworkedProjectile(slingshotLaunchLocation, slingshotLaunchVelocity, projHash, trailHash, projectileCount, this.tempRig.scaleFactor, info);
		}
	}

	// Token: 0x060006D5 RID: 1749 RVA: 0x0002BAF8 File Offset: 0x00029CF8
	[PunRPC]
	public void SpawnSlingshotPlayerImpactEffect(Vector3 hitLocation, float teamColorR, float teamColorG, float teamColorB, float teamColorA, int projectileCount, PhotonMessageInfo info)
	{
		GorillaNot.IncrementRPCCall(info, "SpawnSlingshotPlayerImpactEffect");
		Color color = new Color(teamColorR, teamColorG, teamColorB, teamColorA);
		GameObject gameObject = ObjectPools.instance.Instantiate(this.playerImpactEffectPrefab, hitLocation);
		this.tempRig = this.FindVRRigForPlayer(info.Sender).GetComponent<VRRig>();
		gameObject.transform.localScale = Vector3.one * this.tempRig.scaleFactor;
		gameObject.GetComponent<GorillaColorizableBase>().SetColor(color);
	}

	// Token: 0x060006D6 RID: 1750 RVA: 0x0000666C File Offset: 0x0000486C
	public int IncrementLocalPlayerProjectileCount()
	{
		this.localPlayerProjectileCounter++;
		return this.localPlayerProjectileCounter;
	}

	// Token: 0x060006D7 RID: 1751 RVA: 0x0002BB74 File Offset: 0x00029D74
	protected GorillaGameManager()
	{
	}

	// Token: 0x040007A6 RID: 1958
	public static volatile GorillaGameManager instance;

	// Token: 0x040007A7 RID: 1959
	public Room currentRoom;

	// Token: 0x040007A8 RID: 1960
	public object obj;

	// Token: 0x040007A9 RID: 1961
	public float stepVolumeMax = 0.2f;

	// Token: 0x040007AA RID: 1962
	public float stepVolumeMin = 0.05f;

	// Token: 0x040007AB RID: 1963
	public float fastJumpLimit;

	// Token: 0x040007AC RID: 1964
	public float fastJumpMultiplier;

	// Token: 0x040007AD RID: 1965
	public float slowJumpLimit;

	// Token: 0x040007AE RID: 1966
	public float slowJumpMultiplier;

	// Token: 0x040007AF RID: 1967
	public byte roomSize;

	// Token: 0x040007B0 RID: 1968
	public float lastCheck;

	// Token: 0x040007B1 RID: 1969
	public float checkCooldown = 3f;

	// Token: 0x040007B2 RID: 1970
	public float userDecayTime = 15f;

	// Token: 0x040007B3 RID: 1971
	public Dictionary<int, VRRig> playerVRRigDict = new Dictionary<int, VRRig>();

	// Token: 0x040007B4 RID: 1972
	public Dictionary<string, float> expectedUsersDecay = new Dictionary<string, float>();

	// Token: 0x040007B5 RID: 1973
	public Dictionary<string, string> playerCosmeticsLookup = new Dictionary<string, string>();

	// Token: 0x040007B6 RID: 1974
	public string tempString;

	// Token: 0x040007B7 RID: 1975
	public float startingToLookForFriend;

	// Token: 0x040007B8 RID: 1976
	public float timeToSpendLookingForFriend = 10f;

	// Token: 0x040007B9 RID: 1977
	public bool successfullyFoundFriend;

	// Token: 0x040007BA RID: 1978
	public int maxProjectilesToKeepTrackOfPerPlayer = 50;

	// Token: 0x040007BB RID: 1979
	public GameObject playerImpactEffectPrefab;

	// Token: 0x040007BC RID: 1980
	private int localPlayerProjectileCounter;

	// Token: 0x040007BD RID: 1981
	public Dictionary<Photon.Realtime.Player, List<GorillaGameManager.ProjectileInfo>> playerProjectiles = new Dictionary<Photon.Realtime.Player, List<GorillaGameManager.ProjectileInfo>>();

	// Token: 0x040007BE RID: 1982
	public float tagDistanceThreshold = 8f;

	// Token: 0x040007BF RID: 1983
	public bool testAssault;

	// Token: 0x040007C0 RID: 1984
	public bool endGameManually;

	// Token: 0x040007C1 RID: 1985
	public Photon.Realtime.Player currentMasterClient;

	// Token: 0x040007C2 RID: 1986
	public PhotonView returnPhotonView;

	// Token: 0x040007C3 RID: 1987
	public VRRig returnRig;

	// Token: 0x040007C4 RID: 1988
	private Photon.Realtime.Player outPlayer;

	// Token: 0x040007C5 RID: 1989
	private int outInt;

	// Token: 0x040007C6 RID: 1990
	private VRRig tempRig;

	// Token: 0x040007C7 RID: 1991
	public Photon.Realtime.Player[] currentPlayerArray;

	// Token: 0x02000141 RID: 321
	public struct ProjectileInfo
	{
		// Token: 0x060006DA RID: 1754 RVA: 0x00006682 File Offset: 0x00004882
		public ProjectileInfo(double newTime, Vector3 newVel, Vector3 origin, int newCount, float newScale)
		{
			this.timeLaunched = newTime;
			this.shotVelocity = newVel;
			this.launchOrigin = origin;
			this.projectileCount = newCount;
			this.scale = newScale;
		}

		// Token: 0x040007C8 RID: 1992
		public double timeLaunched;

		// Token: 0x040007C9 RID: 1993
		public Vector3 shotVelocity;

		// Token: 0x040007CA RID: 1994
		public Vector3 launchOrigin;

		// Token: 0x040007CB RID: 1995
		public int projectileCount;

		// Token: 0x040007CC RID: 1996
		public float scale;
	}

	// Token: 0x02000121 RID: 289
	public class VRRigData
	{
		// Token: 0x0400071A RID: 1818
		public static string allcosmetics = "1 Early Access Supporter Pack, 1 1000SHINYROCKS, 1 2200SHINYROCKS, 1 5000SHINYROCKS, 1 DAILY LOGIN, 1 LBAAA., 1 QQQQQ., 1 LBAAB., 1 LBAAC., 1 LBAAF., 1 LBAAG., 1 LBAAH., 1 LBAAI., 1 LBAAJ., 1 LFAAA., 1 LFAAB., 1 LFAAC., 1 LFAAD., 1 LFAAE., 1 LFAAF., 1 LFAAG., 1 LFAAH., 1 LFAAI., 1 LFAAJ., 1 LFAAK., 1 LFAAL., 1 LFAAM., 1 LFAAN., 1 LFAAO., 1 LHAAA., 1 LHAAB., 1 LHAAC., 1 LHAAD., 1 LHAAE., 1 LHAAF., 1 LHAAH., 1 LHAAI., 1 LHAAJ., 1 LHAAK., 1 LHAAL., 1 LHAAM., 1 LHAAN., 1 LHAAO., 1 LHAAP., 1 LHAAQ., 1 LHAAR., 1 LHAAS., 1 FIRST LOGIN, 1 LHAAG., 1 LBAAE., 1 LBAAK., 1 LHAAT., 1 LHAAU., 1 LHAAV., 1 LHAAW., 1 LHAAX., 1 LHAAY., 1 LHAAZ., 1 LFAAP., 1 LFAAQ., 1 LFAAR., 1 LFAAS., 1 LFAAT., 1 LFAAU., 1 LBAAL., 1 LBAAM., 1 LBAAN., 1 LBAAO., 1 LSAAA., 1 LSAAB., 1 LSAAC., 1 LSAAD., 1 LHABA., 1 LHABB., 1 LHABC., 1 LFAAV., 1 LFAAW., 1 LBAAP., 1 LBAAQ., 1 LBAAR., 1 LBAAS., 1 LFAAX., 1 LFAAY., 1 LFAAZ., 1 LFABA., 1 LHABD., 1 LHABE., 1 LHABF., 1 LHABG., 1 LSAAE., 1 LFABB., 1 LFABC., 1 LHABH., 1 LHABI., 1 LHABJ., 1 LHABK., 1 LHABL., 1 LHABM., 1 LHABN., 1 LHABO., 1 LBAAT., 1 LHABP., 1 LHABQ., 1 LHABR., 1 LFABD., 1 LBAAU., 1 LBAAV., 1 LBAAW., 1 LBAAX., 1 LBAAY., 1 LBAAZ., 1 LBABA., 1 LBABB., 1 LBABC., 1 LBABD., 1 LBABE., 1 LFABE., 1 LHABS., 1 LHABT., 1 LHABU., 1 LHABV., 1 LFABF., 1 LFABG., 1 LBABF., 1 LBABG., 1 LHABW., 1 LBABH., 1 LHABX., 1 LHABY., 1 LMAAA., 1 LMAAB., 1 LHABZ., 1 LHACA., 1 LBABJ., 1 LBABK., 1 LBABL., 1 LMAAC., 1 LMAAD., 1 LMAAE., 1 LBABI., 1 LMAAF., 1 LMAAG., 1 LMAAH., 1 LFABH., 1 LHACB., 1 LHACC., 1 LFABI., 1 LBABM., 1 LBABN., 1 LHACD., 1 LMAAI., 1 LMAAJ., 1 LMAAK., 1 LMAAL., 1 LMAAM., 1 LMAAN., 1 LMAAO., 1 LHACE., 1 LFABJ., 1 LFABK., 1 LFABL., 1 LFABM., 1 LFABN., 1 LFABO., 1 LBABO., 1 LBABP., 1 LMAAP., 1 LBABQ., 1 LBABR., 1 LBABS., 1 LBABT., 1 LBABU., 1 LFABP., 1 LFABQ., 1 LFABR., 1 LHACF., 1 LHACG., 1 LHACH., 1 LMAAQ., 1 LMAAR., 1 LMAAS., 1 LMAAT., 1 LMAAU., 1 LMAAV., 1 LSAAF., 1 LSAAG., 1 LBAJC., 1 LBAGH., 1 LBAGC., 1 LBADG., 1 LBACC., 1 LBAGB., 1 LBAVH., 1 LBASH., 1 LBAVG., 1 LBAVK., 1 LBAVJ., 1 LBAGJ., 1 LBATD., 1 LBAFJ., 1 LBAFV., 1 LBAFD., 1 LBATR., 1 LBATH., 1 LBAGS., 1 LBATY., 1 LBAYU., 1 LBATK., 1 LBAGL., 1 LBAUG., 1 LBARG., 1 LBAUF., 1 LBAGK., 1 LBARF., 1 LBAHK., 1 LBAFL., 1 LFPBD., 1 LBACP., 1 LBACS., 1 LBAAD., 1 LMABF., 1 LMODT., 1 LAOER., 1 LASER. , 1 LBAAA., 1 LBAAB., 1 LBAAC., 1 LBAAD., 1 LBAAE., 1 LBAAF., 1 LBAAG., 1 LBAAH., 1 LBAAI., 1 LBAAJ., 1 LBAAK., 1 LBAAL., 1 LBAAM., 1 LBAAN., 1 LBAAO., 1 	LBAAP., 1 	LBAAQ., 1 	LBAAR., 1 	LBAAS., 1 LBAAT., 1 LBAAU., 1 LBAAV., 1 LBAAW., 1 	LBAAX., 1 	LBAAY., 1 LBAAZ., 1 LBABA., 1 LBABB., 1 LBABC., 1 LBABD., 1 LBABE., 1 LBABF., 1 LBABG., 1 LBABG., 1 LBABH., 1 LBABI., 1 LBABJ., 1 LBABK., 1 LBABL., 1 LBABM., 1 LBABN., 1 LBABO., 1 LBABP., 1 LBABQ., 1 LBABR., 1 LBABS., 1 LBABT., 1 LBABU., 1 LBACA., 1 LBACB., 1 LBACC., 1 LBACD., 1 LBACE., 1 LBACF., 1 LBACG., 1 LBACH., 1 LBACI., 1 LBACJ., 1 LBACK., 1 LBACL., 1 LBACM, 1 LBACN., 1 LBACO., 1 LBACP., 1 LBACS., 1 LBACX., 1 LBACY., 1 LBACZ., 1 LBADA., 1 LBADB., 1 LBADE., 1 LBADF., 1 LBADG., 1 LBADH., 1 LBADI., 1 LBADK., 1 LBADL., 1 LBADM., 1 LBADN., 1 LBADO., 1 LBADP., 1 LBADQ., 1 LBADR., 1 LBADS., 1 LBADT., 1 LBADU., 1 LBADV., 1 LBAFD., 1 LBAFL., 1 LBAFV., 1 LBAGB., 1 LBAGC., 1 LBAGH., 1 LBAGJ., 1 LBAGL., 1 LBAGS., 1 LBAHK., 1 LBAJC., 1 LBAPR., 1 LBARF., 1 LBARG., 1 LBASD., 1 LBASH., 1 LBATD., 1 LBATH., 1 LBATK., 1 LBATY., 1 LBAUF., 1 LBAVG., 1 LBAVH., 1 LBAVJ., 1 LBAVK., 1 LBAWR., 1 LFAAA., 1 LFAAB., 1 LFAAC., 1 LFAAD., 1 LFAAE., 1 LFAAF., 1 LFAAG., 1 LFAAH., 1 LFAAI., 1 LFAAJ., 1 LFAAK., 1 LFAAL., 1 LFAAM., 1 LFAAN., 1 LFAAO., 1 LFAAP., 1 LFAAQ., 1 LFAAR., 1 LFAAS., 1 LFAAT., 1 LFAAU., 1 LFAAV., 1 LFAAW., 1 LFAAX., 1 LFAAY., 1 LFAAZ., 1 LFABA., 1 LFABB., 1 LFABC., 1 LFABD., 1 LFABE., 1 LFABF., 1 LFABG., 1 LFABH., 1 LFABI., 1 LFABJ., 1 LFABK., 1 LFABL., 1 LFABM., 1 LFABN., 1 LFABO., 1 LFABP., 1 LFABQ., 1 LFABR., 1 LFABX., 1 LFABZ., 1 LFACA., 1 LFACB., 1 LFACC., 1 LFACD., 1 LFACG., 1 LFACH., 1 LFACI., 1 LFACJ., 1 LFACM., 1 LFACO., 1 LFACP., 1 LFACQ., 1 LFACR., 1 LFACS, 1 LFACT., 1 LFACU., 1 LFACV., 1 LFACW., 1 LFACX., 1 LFACY., 1 LFACZ., 1 LHAAA., 1 LHAAB., 1 LHAAC., 1 LHAAD., 1 LHAAE., 1 LHAAF., 1 LHAAG., 1 LHAAH., 1 LHAAI., 1 LHAAJ., 1 LHAAK., 1 LHAAL., 1 LHAAM., 1 LHAAN., 1 LHAAO., 1 LHAAP., 1 LHAAQ., 1 LHAAR., 1 LHAAS., 1 LHAAT., 1 LHAAU., 1 LHAAV., 1 LHAAW., 1 LHAAX., 1 LHAAY., 1 LHAAZ., 1 LHABA., 1 LHABB., 1 LHABC., 1 LHABD., 1 LHABE., 1 LHABF., 1 LHABG., 1 LHABH., 1 LHABI., 1 LHABJ., 1 LHABK., 1 LHABL., 1 LHABM., 1 LHABN., 1 LHABO., 1 LHABP., 1 LHABQ., 1 LHABR., 1 LHABS., 1 LHABT., 1 LHABU., 1 LHABV., 1 LHABW., 1 LHABX., 1 LHABY., 1 LHABZ., 1 LHACA., 1 LHACB., 1 LHACC., 1 LHACD., 1 LHACE., 1 LHACF., 1 LHACG., 1 LHACH., 1 LHACR., 1 LHACS., 1 LHACT., 1 LHACU., 1 LHACV., 1 LHACW., 1 LHACX., 1 LHACY., 1 LHACZ., 1 LHADA., 1 LHADC., 1 LHADD., 1 LHADE., 1 LHADF., 1 LHADG., 1 LHADH., 1 LHADI., 1 LHADJ., 1 LHADK., 1 LHADL., 1 LHADR., 1 LHADS., 1 LHADT., 1 LHADU., 1 LHAEC., 1 LHAED., 1 LHAEE., 1 LHAEF., 1 LHAEG., 1 LHAEH., 1 LHAEI., 1 LHAEJ., 1 LHAEK., 1 LHAEL., 1 LHAEM, 1 LMAAA., 1 LMAAB., 1 LMAAC., 1 LMAAD., 1 LMAAE., 1 LMAAF., 1 LMAAG., 1 LMAAH., 1 LMAAI., 1 LMAAJ., 1 LMAAK., 1 LMAAL., 1 LMAAM., 1 LMAAN., 1 LMAAO., 1 LMAAP., 1 LMAAQ., 1 LMAAR., 1 LMAAS., 1 LMAAT., 1 LMAAU., 1 LMAAV., 1 LMABP., 1 LMABQ., 1 LMABR., 1 LMABS., 1 LMABT., 1 LMABU., 1 LMABV., 1 LMABW., 1 LMABX., 1 LMABY., 1 LMABZ. , 1 LMACB., 1 LMACC., 1 LMACD., 1 LMACG., 1 LMACH., 1 LMACI., 1 LMACJ., 1 LMACK., 1 LMACL.";
	}
}
