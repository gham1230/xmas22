using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GorillaParent : MonoBehaviour
{
	public GameObject tagUI;

	public GameObject playerParent;

	public GameObject vrrigParent;

	public static volatile GorillaParent instance;

	public static bool hasInstance;

	public List<VRRig> vrrigs;

	public Dictionary<Player, VRRig> vrrigDict = new Dictionary<Player, VRRig>();

	private int i;

	private PhotonView[] childPhotonViews;

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
			hasInstance = true;
		}
		else if (instance != this)
		{
			Object.Destroy(base.gameObject);
		}
		StartCoroutine(OccasionalUpdate());
	}

	public void LateUpdate()
	{
		for (i = vrrigs.Count - 1; i > -1; i--)
		{
			if (vrrigs[i] == null)
			{
				vrrigs.RemoveAt(i);
			}
		}
	}

	private IEnumerator OccasionalUpdate()
	{
		while (true)
		{
			try
			{
				if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
				{
					childPhotonViews = vrrigParent.GetPhotonViewsInChildren();
					if (childPhotonViews.Length > 10)
					{
						for (int num = childPhotonViews.Length - 1; num > -1; num--)
						{
							if ((bool)childPhotonViews[num].GetComponent<VRRig>() && childPhotonViews[num].IsRoomView && childPhotonViews[num].IsMine)
							{
								PhotonNetwork.Destroy(childPhotonViews[num].gameObject);
							}
						}
					}
				}
			}
			catch
			{
			}
			yield return new WaitForSeconds(5f);
		}
	}
}
