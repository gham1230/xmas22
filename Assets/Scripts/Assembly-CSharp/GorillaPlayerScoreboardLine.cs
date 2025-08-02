using System;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x0200018C RID: 396
public class GorillaPlayerScoreboardLine : MonoBehaviourPunCallbacks, IInRoomCallbacks
{
	// Token: 0x0600085C RID: 2140 RVA: 0x000023F3 File Offset: 0x000005F3
	public void UpdateLevel()
	{
	}

	// Token: 0x0600085D RID: 2141 RVA: 0x00034D9C File Offset: 0x00032F9C
	public void HideShowLine(bool active)
	{
		if (this.playerVRRig != null)
		{
			foreach (Text text in this.texts)
			{
				if (text.enabled != active)
				{
					text.enabled = active;
				}
			}
			foreach (SpriteRenderer spriteRenderer in this.sprites)
			{
				if (spriteRenderer.enabled != active)
				{
					spriteRenderer.enabled = active;
				}
			}
			foreach (MeshRenderer meshRenderer in this.meshes)
			{
				if (meshRenderer.enabled != active)
				{
					meshRenderer.enabled = active;
				}
			}
			foreach (Image image in this.images)
			{
				if (image.enabled != active)
				{
					image.enabled = active;
				}
			}
		}
	}

	// Token: 0x0600085E RID: 2142 RVA: 0x00034E70 File Offset: 0x00033070
	public void PressButton(bool isOn, GorillaPlayerLineButton.ButtonType buttonType)
	{
		if (buttonType != GorillaPlayerLineButton.ButtonType.Mute)
		{
			if (buttonType == GorillaPlayerLineButton.ButtonType.Report)
			{
				this.SetReportState(true, buttonType);
				return;
			}
			this.SetReportState(false, buttonType);
			return;
		}
		else
		{
			if (this.linePlayer != null && this.playerVRRig != null)
			{
				int num = isOn ? 1 : 0;
				PlayerPrefs.SetInt(this.linePlayer.UserId, num);
				this.playerVRRig.muted = (num != 0);
				PlayerPrefs.Save();
				this.muteButton.UpdateColor();
				return;
			}
			return;
		}
	}

	// Token: 0x0600085F RID: 2143 RVA: 0x00034EE8 File Offset: 0x000330E8
	public void SetReportState(bool reportState, GorillaPlayerLineButton.ButtonType buttonType)
	{
		this.canPressNextReportButton = (buttonType != GorillaPlayerLineButton.ButtonType.Toxicity && buttonType != GorillaPlayerLineButton.ButtonType.Report);
		if (reportState)
		{
			foreach (GorillaPlayerLineButton gorillaPlayerLineButton in base.GetComponentsInChildren<GorillaPlayerLineButton>(true))
			{
				gorillaPlayerLineButton.gameObject.SetActive(gorillaPlayerLineButton.buttonType != GorillaPlayerLineButton.ButtonType.Report);
			}
		}
		else
		{
			foreach (GorillaPlayerLineButton gorillaPlayerLineButton2 in base.GetComponentsInChildren<GorillaPlayerLineButton>(true))
			{
				gorillaPlayerLineButton2.gameObject.SetActive(gorillaPlayerLineButton2.buttonType == GorillaPlayerLineButton.ButtonType.Report || gorillaPlayerLineButton2.buttonType == GorillaPlayerLineButton.ButtonType.Mute);
			}
			if (this.linePlayer != null && this.playerVRRig != null && buttonType != GorillaPlayerLineButton.ButtonType.Cancel)
			{
				this.ReportPlayer(this.linePlayer.UserId, buttonType, this.linePlayer.NickName);
				this.reportButton.isOn = true;
				this.reportButton.UpdateColor();
			}
		}
		base.transform.parent.GetComponent<GorillaScoreBoard>().RedrawPlayerLines();
	}

	// Token: 0x06000860 RID: 2144 RVA: 0x00034FE0 File Offset: 0x000331E0
	public void GetUserLevel(string myPlayFabeId)
	{
		GetUserDataRequest getUserDataRequest = new GetUserDataRequest();
		getUserDataRequest.PlayFabId = myPlayFabeId;
		getUserDataRequest.Keys = null;
		PlayFabClientAPI.GetUserReadOnlyData(getUserDataRequest, delegate (GetUserDataResult result)
		{
			if (result.Data == null || !result.Data.ContainsKey("PlayerLevel"))
			{
				this.playerLevelValue = "1";
			}
			else
			{
				this.playerLevelValue = result.Data["PlayerLevel"].Value;
			}
			if (result.Data == null || !result.Data.ContainsKey("Player1v1MMR"))
			{
				this.playerMMRValue = "-1";
			}
			else
			{
				this.playerMMRValue = result.Data["Player1v1MMR"].Value;
			}
			this.playerLevel.text = this.playerLevelValue;
			this.playerMMR.text = this.playerMMRValue;
		}, delegate (PlayFabError error)
		{
		}, null, null);
	}

	// Token: 0x06000861 RID: 2145 RVA: 0x00035034 File Offset: 0x00033234
	public string NormalizeName(bool doIt, string text)
	{
		if (doIt)
		{
			text = new string(Array.FindAll<char>(text.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
			if (text.Length > 12)
			{
				text = text.Substring(0, 12);
			}
			text = text.ToUpper();
		}
		return text;
	}

	// Token: 0x06000862 RID: 2146 RVA: 0x00007404 File Offset: 0x00005604
	public GorillaPlayerScoreboardLine()
	{
	}

	// Token: 0x06000863 RID: 2147 RVA: 0x00035094 File Offset: 0x00033294
	public void Update()
	{
		if (this.playerVRRig != null)
		{
			if (!this.initialized && this.linePlayer != null)
			{
				this.initialized = true;
				if (this.linePlayer != PhotonNetwork.LocalPlayer)
				{
					int @int = PlayerPrefs.GetInt(this.linePlayer.UserId, 0);
					PlayerPrefs.SetInt(this.linePlayer.UserId, @int);
					this.muteButton.isOn = (@int != 0);
					this.muteButton.UpdateColor();
					this.playerVRRig.muted = (@int != 0);
				}
				else
				{
					this.muteButton.gameObject.SetActive(false);
					this.reportButton.gameObject.SetActive(false);
				}
			}
			if (this.linePlayer != null)
			{
				if (this.playerVRRig.setMatIndex != this.currentMatIndex && this.playerVRRig.setMatIndex != 0 && this.playerVRRig.setMatIndex > -1 && this.playerVRRig.setMatIndex < this.playerVRRig.materialsToChangeTo.Length)
				{
					this.playerSwatch.material = this.playerVRRig.materialsToChangeTo[this.playerVRRig.setMatIndex];
					this.currentMatIndex = this.playerVRRig.setMatIndex;
				}
				if (this.playerVRRig.setMatIndex == 0 && this.playerSwatch.material != null)
				{
					this.playerSwatch.material = null;
					this.currentMatIndex = 0;
				}
				if (this.playerName.text != this.linePlayer.NickName)
				{
					this.playerName.text = this.NormalizeName(true, this.linePlayer.NickName);
				}
				if (this.playerMMRValue != this.playerMMR.text)
				{
					this.playerMMR.text = this.playerMMRValue;
				}
				if (this.playerLevelValue != this.playerLevel.text)
				{
					this.playerLevel.text = this.playerLevelValue;
				}
				if (this.playerSwatch.color != this.playerVRRig.materialsToChangeTo[0].color)
				{
					this.playerSwatch.color = this.playerVRRig.materialsToChangeTo[0].color;
				}
				if (this.linePlayer != PhotonNetwork.LocalPlayer.Get(this.playerActorNumber))
				{
					this.linePlayer = PhotonNetwork.LocalPlayer.Get(this.playerActorNumber);
					this.playerSwatch.color = this.playerVRRig.materialsToChangeTo[0].color;
					this.playerSwatch.material = this.playerVRRig.materialsToChangeTo[this.playerVRRig.setMatIndex];
				}
				if (base.GetComponentInParent<GorillaScoreBoard>().includeMMR && !this.playerMMR.gameObject.activeSelf)
				{
					this.playerMMR.gameObject.SetActive(true);
				}
				if ((this.playerVRRig != null && this.playerVRRig.GetComponent<PhotonVoiceView>().IsSpeaking) || (this.playerVRRig.photonView.IsMine && PhotonNetworkController.Instance.GetComponent<Recorder>().IsCurrentlyTransmitting))
				{
					this.speakerIcon.SetActive(true);
					return;
				}
				this.speakerIcon.SetActive(false);
				return;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	// Token: 0x06000864 RID: 2148 RVA: 0x000353DC File Offset: 0x000335DC
	public void ReportPlayer(string PlayerID, GorillaPlayerLineButton.ButtonType buttonType, string OtherPlayerNickName)
	{
		ExecuteCloudScriptRequest executeCloudScriptRequest = new ExecuteCloudScriptRequest();
		executeCloudScriptRequest.FunctionName = "Report";
		executeCloudScriptRequest.FunctionParameter = new
		{
			playerdoing = "UserID: " + PhotonNetwork.LocalPlayer.UserId + "\nName: " + PhotonNetwork.LocalPlayer.NickName,
			reason = buttonType.ToString(),
			target = "UserID: " + PlayerID + "\nName: " + OtherPlayerNickName,
			todo = PlayerID
		};
		PlayFabClientAPI.ExecuteCloudScript(executeCloudScriptRequest, delegate (ExecuteCloudScriptResult result)
		{
			Debug.Log("YEYEYEYEYEY IT WORWOKED");
		}, null, null, null);
	}

	// Token: 0x06000865 RID: 2149 RVA: 0x00007413 File Offset: 0x00005613
	public Player FindPlayerforVRRig(VRRig vRRig)
	{
		if (vRRig.photonView != null && vRRig.photonView.Owner != null)
		{
			return vRRig.photonView.Owner;
		}
		return null;
	}

	// Token: 0x04000A59 RID: 2649
	public Text playerName;

	// Token: 0x04000A5A RID: 2650
	public Text playerLevel;

	// Token: 0x04000A5B RID: 2651
	public Text playerMMR;

	// Token: 0x04000A5C RID: 2652
	public Image playerSwatch;

	// Token: 0x04000A5D RID: 2653
	public Texture infectedTexture;

	// Token: 0x04000A5E RID: 2654
	public Player linePlayer;

	// Token: 0x04000A5F RID: 2655
	public VRRig playerVRRig;

	// Token: 0x04000A60 RID: 2656
	public int currentMatIndex;

	// Token: 0x04000A61 RID: 2657
	public string playerLevelValue;

	// Token: 0x04000A62 RID: 2658
	public string playerMMRValue;

	// Token: 0x04000A63 RID: 2659
	public string playerNameValue;

	// Token: 0x04000A64 RID: 2660
	public int playerActorNumber;

	// Token: 0x04000A65 RID: 2661
	public bool initialized;

	// Token: 0x04000A66 RID: 2662
	public GorillaPlayerLineButton muteButton;

	// Token: 0x04000A67 RID: 2663
	public GorillaPlayerLineButton reportButton;

	// Token: 0x04000A68 RID: 2664
	public GameObject speakerIcon;

	// Token: 0x04000A69 RID: 2665
	public bool canPressNextReportButton = true;

	// Token: 0x04000A6A RID: 2666
	public Text[] texts;

	// Token: 0x04000A6B RID: 2667
	public SpriteRenderer[] sprites;

	// Token: 0x04000A6C RID: 2668
	public MeshRenderer[] meshes;

	// Token: 0x04000A6D RID: 2669
	public Image[] images;
}