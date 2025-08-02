using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class GorillaScoreBoard : MonoBehaviourPunCallbacks, IInRoomCallbacks, IOnEventCallback
{
	public GameObject scoreBoardLinePrefab;

	public int startingYValue;

	public int lineHeight;

	public GorillaGameManager gameManager;

	public string gameType;

	public bool includeMMR;

	public bool isActive;

	public List<GorillaPlayerScoreboardLine> lines;

	public Text boardText;

	public Text buttonText;

	private Player playerForVRRig;

	private int i;

	private VRRig currentRig;

	private Player outPlayer;

	public void Awake()
	{
		PhotonNetwork.AddCallbackTarget(this);
		if (PhotonNetwork.InRoom && GorillaGameManager.instance != null)
		{
			boardText.text = GetBeginningString();
		}
		StartCoroutine(InfrequentUpdateCoroutine());
	}

	public string GetBeginningString()
	{
		if (GorillaGameManager.instance != null)
		{
			return "ROOM ID: " + ((!PhotonNetwork.CurrentRoom.IsVisible) ? "-PRIVATE- GAME MODE: " : (PhotonNetwork.CurrentRoom.Name + "    GAME MODE: ")) + GorillaGameManager.instance.GameMode() + "\n   PLAYER      COLOR   MUTE   REPORT";
		}
		return "ROOM ID: " + ((!PhotonNetwork.CurrentRoom.IsVisible) ? "-PRIVATE-" : PhotonNetwork.CurrentRoom.Name) + "\n   PLAYER      COLOR   MUTE   REPORT";
	}

	private IEnumerator InfrequentUpdateCoroutine()
	{
		while (true)
		{
			InfrequentUpdate();
			yield return new WaitForSeconds(1f);
		}
	}

	private void InfrequentUpdate()
	{
		try
		{
			this.i = lines.Count - 1;
			while (this.i > -1)
			{
				if (lines[this.i] == null)
				{
					lines.RemoveAt(this.i);
				}
				else if (lines[this.i].linePlayer == null || !PhotonNetwork.CurrentRoom.Players.TryGetValue(lines[this.i].linePlayer.ActorNumber, out outPlayer) || (PhotonNetwork.CurrentRoom.Players.TryGetValue(lines[this.i].linePlayer.ActorNumber, out outPlayer) && outPlayer == null))
				{
					lines[this.i].enabled = false;
					UnityEngine.Object.Destroy(lines[this.i].gameObject);
					lines.RemoveAt(this.i);
				}
				this.i--;
			}
			if (PhotonNetwork.CurrentRoom != null && lines.Count != PhotonNetwork.PlayerList.Length)
			{
				Player[] playerList = PhotonNetwork.PlayerList;
				foreach (Player player in playerList)
				{
					if (player == null)
					{
						continue;
					}
					bool flag = false;
					foreach (GorillaPlayerScoreboardLine line in lines)
					{
						if (line.playerActorNumber == player.ActorNumber)
						{
							flag = true;
						}
					}
					if (!flag)
					{
						GameObject gameObject = UnityEngine.Object.Instantiate(scoreBoardLinePrefab, base.transform);
						lines.Add(gameObject.GetComponent<GorillaPlayerScoreboardLine>());
						gameObject.GetComponent<GorillaPlayerScoreboardLine>().playerActorNumber = player.ActorNumber;
						gameObject.GetComponent<GorillaPlayerScoreboardLine>().linePlayer = player;
						if (GorillaGameManager.StaticFindRigForPlayer(player) != null)
						{
							gameObject.GetComponent<GorillaPlayerScoreboardLine>().playerVRRig = GorillaGameManager.StaticFindRigForPlayer(player).GetComponent<VRRig>();
						}
						gameObject.GetComponent<GorillaPlayerScoreboardLine>().playerNameValue = player.NickName;
					}
				}
			}
			RedrawPlayerLines();
		}
		catch
		{
		}
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		InfrequentUpdate();
	}

	public void RedrawPlayerLines()
	{
		lines.Sort((GorillaPlayerScoreboardLine line1, GorillaPlayerScoreboardLine line2) => line1.playerActorNumber.CompareTo(line2.playerActorNumber));
		boardText.text = GetBeginningString();
		buttonText.text = "";
		for (int i = 0; i < lines.Count; i++)
		{
			try
			{
				lines[i].gameObject.GetComponent<RectTransform>().localPosition = new Vector3(0f, startingYValue - lineHeight * i, 0f);
				if (lines[i].linePlayer == null)
				{
					continue;
				}
				Text text = boardText;
				text.text = text.text + "\n " + NormalizeName(doIt: true, lines[i].linePlayer.NickName);
				if (lines[i].linePlayer != PhotonNetwork.LocalPlayer)
				{
					if (lines[i].reportButton.isActiveAndEnabled)
					{
						buttonText.text += "MUTE                                REPORT\n";
					}
					else
					{
						buttonText.text += "MUTE                HATE SPEECH    TOXICITY      CHEATING      CANCEL\n";
					}
				}
				else
				{
					buttonText.text += "\n";
				}
			}
			catch
			{
			}
		}
	}

	void IOnEventCallback.OnEvent(EventData photonEvent)
	{
	}

	public IEnumerator RefreshData(int actorNumber1, int actorNumber2)
	{
		yield return new WaitForSeconds(1f);
		foreach (GorillaPlayerScoreboardLine line in lines)
		{
			if (line.playerActorNumber != actorNumber1)
			{
				_ = line.playerActorNumber;
				_ = actorNumber2;
			}
		}
	}

	private int GetActorIDFromUserID(string userID)
	{
		Player[] playerList = PhotonNetwork.PlayerList;
		foreach (Player player in playerList)
		{
			if (player.UserId == userID)
			{
				return player.ActorNumber;
			}
		}
		return -1;
	}

	public string NormalizeName(bool doIt, string text)
	{
		if (doIt)
		{
			text = new string(Array.FindAll(text.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
			if (text.Length > 12)
			{
				text = text.Substring(0, 10);
			}
			text = text.ToUpper();
		}
		return text;
	}
}
