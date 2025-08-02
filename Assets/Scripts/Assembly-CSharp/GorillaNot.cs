using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GorillaNot : MonoBehaviourPunCallbacks
{
	public static volatile GorillaNot instance;

	private bool _sendReport;

	private string _suspiciousPlayerId = "";

	private string _suspiciousPlayerName = "";

	private string _suspiciousReason = "";

	public List<string> reportedPlayers = new List<string>();

	public byte roomSize;

	public float lastCheck;

	public float checkCooldown = 3f;

	public float userDecayTime = 15f;

	public Player currentMasterClient;

	public bool testAssault;

	private const byte ReportAssault = 8;

	private int lowestActorNumber;

	public Dictionary<string, int> userRPCCalls = new Dictionary<string, int>();

	public Dictionary<string, int> userRPCCallsMax = new Dictionary<string, int>();

	private int calls;

	public int rpcCallLimit = 50;

	public int logErrorMax = 50;

	private object outObj;

	private Player tempPlayer;

	private int logErrorCount;

	private int stringIndex;

	private string playerID;

	private string playerNick;

	private int lastServerTimestamp;

	private ExitGames.Client.Photon.Hashtable hashTable;

	private bool sendReport
	{
		get
		{
			return _sendReport;
		}
		set
		{
			if (!_sendReport)
			{
				_sendReport = true;
			}
		}
	}

	private string suspiciousPlayerId
	{
		get
		{
			return _suspiciousPlayerId;
		}
		set
		{
			if (_suspiciousPlayerId == "")
			{
				_suspiciousPlayerId = value;
			}
		}
	}

	private string suspiciousPlayerName
	{
		get
		{
			return _suspiciousPlayerName;
		}
		set
		{
			if (_suspiciousPlayerName == "")
			{
				_suspiciousPlayerName = value;
			}
		}
	}

	private string suspiciousReason
	{
		get
		{
			return _suspiciousReason;
		}
		set
		{
			if (_suspiciousReason == "")
			{
				_suspiciousReason = value;
			}
		}
	}

	private void Start()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Object.Destroy(this);
		}
		StartCoroutine(CheckReports());
		logErrorCount = 0;
		Application.logMessageReceived += LogErrorCount;
	}

	public void LogErrorCount(string logString, string stackTrace, LogType type)
	{
		if (type == LogType.Error)
		{
			logErrorCount++;
			stringIndex = logString.LastIndexOf("Sender is ");
			if (logString.Contains("RPC") && stringIndex >= 0)
			{
				playerID = logString.Substring(stringIndex + 10);
				playerNick = "n/a";
				for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
				{
					if (PhotonNetwork.PlayerList[i].UserId == playerID)
					{
						playerNick = PhotonNetwork.PlayerList[i].NickName;
						break;
					}
				}
				SendReport("invalid RPC stuff", playerID, playerNick);
			}
		}
		if (logErrorCount > logErrorMax)
		{
			Debug.unityLogger.logEnabled = false;
		}
	}

	public void SendReport(string susReason, string susId, string susNick)
	{
		suspiciousReason = susReason;
		suspiciousPlayerId = susId;
		suspiciousPlayerName = susNick;
		sendReport = true;
	}

	private IEnumerator CheckReports()
	{
		while (true)
		{
			try
			{
				logErrorCount = 0;
				if (PhotonNetwork.InRoom)
				{
					lastCheck = Time.time;
					lastServerTimestamp = PhotonNetwork.ServerTimestamp;
					if (PhotonNetwork.PlayerList.Length > PhotonNetworkController.Instance.GetRoomSize(PhotonNetworkController.Instance.currentGameType))
					{
						sendReport = true;
						suspiciousReason = "too many players";
						SetToRoomCreatorIfHere();
						CloseInvalidRoom();
					}
					if (currentMasterClient != PhotonNetwork.MasterClient || LowestActorNumber() != PhotonNetwork.MasterClient.ActorNumber)
					{
						Player[] playerList = PhotonNetwork.PlayerList;
						foreach (Player player in playerList)
						{
							if (currentMasterClient == player)
							{
								sendReport = true;
								suspiciousReason = "room host force changed";
								suspiciousPlayerId = PhotonNetwork.MasterClient.UserId;
								suspiciousPlayerName = PhotonNetwork.MasterClient.NickName;
							}
						}
						currentMasterClient = PhotonNetwork.MasterClient;
					}
					if (sendReport || testAssault)
					{
						if (suspiciousPlayerId != "" && reportedPlayers.IndexOf(suspiciousPlayerId) == -1)
						{
							reportedPlayers.Add(suspiciousPlayerId);
							testAssault = false;
							RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
							WebFlags flags = new WebFlags(1);
							raiseEventOptions.Flags = flags;
							string[] array = new string[PhotonNetwork.PlayerList.Length];
							int num = 0;
							Player[] playerList = PhotonNetwork.PlayerList;
							foreach (Player player2 in playerList)
							{
								array[num] = player2.UserId;
								num++;
							}
							object[] eventContent = new object[7]
							{
								PhotonNetwork.CurrentRoom.ToStringFull(),
								array,
								PhotonNetwork.MasterClient.UserId,
								suspiciousPlayerId,
								suspiciousPlayerName,
								suspiciousReason,
								PhotonNetworkController.Instance.gameVersion
							};
							PhotonNetwork.RaiseEvent(8, eventContent, raiseEventOptions, SendOptions.SendReliable);
							if (ShouldDisconnectFromRoom())
							{
								StartCoroutine(QuitDelay());
							}
						}
						_sendReport = false;
						_suspiciousPlayerId = "";
						_suspiciousPlayerName = "";
						_suspiciousReason = "";
					}
					foreach (string item in userRPCCalls.Keys.ToList())
					{
						userRPCCalls[item] = 0;
					}
				}
			}
			catch
			{
			}
			yield return new WaitForSeconds(1f);
		}
	}

	private int LowestActorNumber()
	{
		lowestActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
		Player[] playerList = PhotonNetwork.PlayerList;
		foreach (Player player in playerList)
		{
			if (player.ActorNumber < lowestActorNumber)
			{
				lowestActorNumber = player.ActorNumber;
			}
		}
		return lowestActorNumber;
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		base.OnPlayerLeftRoom(otherPlayer);
		if (userRPCCalls.ContainsKey(otherPlayer.UserId))
		{
			userRPCCalls.Remove(otherPlayer.UserId);
		}
		if (userRPCCallsMax.ContainsKey(otherPlayer.UserId))
		{
			userRPCCallsMax.Remove(otherPlayer.UserId);
		}
	}

	public static void IncrementRPCCall(PhotonMessageInfo info, string callingMethod)
	{
		instance.IncrementRPCCallLocal(info);
	}

	private void IncrementRPCCallLocal(PhotonMessageInfo info)
	{
		if (info.Sender == null || info.Sender.UserId == null || info.SentServerTimestamp < lastServerTimestamp)
		{
			return;
		}
		if (userRPCCalls.TryGetValue(info.Sender.UserId, out calls) && userRPCCalls.TryGetValue(info.Sender.UserId, out calls))
		{
			userRPCCalls[info.Sender.UserId] = userRPCCalls[info.Sender.UserId] + 1;
			if (userRPCCalls[info.Sender.UserId] > userRPCCallsMax[info.Sender.UserId])
			{
				userRPCCallsMax[info.Sender.UserId] = userRPCCalls[info.Sender.UserId];
			}
		}
		else
		{
			if (!userRPCCalls.TryGetValue(info.Sender.UserId, out calls))
			{
				userRPCCalls.Add(info.Sender.UserId, 1);
			}
			if (!userRPCCallsMax.TryGetValue(info.Sender.UserId, out calls))
			{
				userRPCCallsMax.Add(info.Sender.UserId, 1);
			}
		}
		if (userRPCCalls[info.Sender.UserId] > rpcCallLimit)
		{
			SendReport("too many rpc calls!", info.Sender.UserId, info.Sender.NickName);
		}
	}

	private IEnumerator QuitDelay()
	{
		yield return new WaitForSeconds(1f);
		PhotonNetworkController.Instance.AttemptDisconnect();
	}

	private void SetToRoomCreatorIfHere()
	{
		tempPlayer = PhotonNetwork.CurrentRoom.GetPlayer(1);
		if (tempPlayer != null)
		{
			suspiciousPlayerId = tempPlayer.UserId;
			suspiciousPlayerName = tempPlayer.NickName;
		}
		else
		{
			suspiciousPlayerId = "n/a";
			suspiciousPlayerName = "n/a";
		}
	}

	private bool ShouldDisconnectFromRoom()
	{
		if (!_suspiciousReason.Contains("too many players") && !_suspiciousReason.Contains("invalid room name"))
		{
			return _suspiciousReason.Contains("invalid game mode");
		}
		return true;
	}

	private void CloseInvalidRoom()
	{
		PhotonNetwork.CurrentRoom.IsOpen = false;
		PhotonNetwork.CurrentRoom.IsVisible = false;
		PhotonNetwork.CurrentRoom.MaxPlayers = PhotonNetworkController.Instance.GetRoomSize(PhotonNetworkController.Instance.currentGameType);
	}
}
