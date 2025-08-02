using System.Collections.Generic;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

public class PlayFabTitleDataTextDisplay : MonoBehaviour
{
	[SerializeField]
	private Text textBox;

	[Tooltip("PlayFab Title Data key from where to pull display text")]
	[SerializeField]
	private string playfabKey;

	[Tooltip("Should append current app version to Playfab key for version specific display text")]
	[SerializeField]
	private bool appendAppVersion;

	[Tooltip("Text to display when error occurs during fetch")]
	[TextArea(3, 5)]
	[SerializeField]
	private string fallbackText;

	private string PlayFabKey
	{
		get
		{
			if (!appendAppVersion)
			{
				return playfabKey;
			}
			return playfabKey + "_" + Application.version;
		}
	}

	private async void Start()
	{
		while (!PlayFabClientAPI.IsClientLoggedIn())
		{
			await Task.Yield();
		}
		PlayFabClientAPI.GetTitleData(new GetTitleDataRequest
		{
			Keys = new List<string> { PlayFabKey }
		}, OnTitleDataRequestComplete, OnPlayFabError);
	}

	private void OnPlayFabError(PlayFabError error)
	{
		textBox.text = fallbackText;
	}

	private void OnTitleDataRequestComplete(GetTitleDataResult titleDataResult)
	{
		if (titleDataResult.Data.ContainsKey(PlayFabKey))
		{
			string text = titleDataResult.Data[PlayFabKey].Replace("\\r", "\r").Replace("\\n", "\n");
			if (text[0] == '"' && text[text.Length - 1] == '"')
			{
				text = text.Substring(1, text.Length - 2);
			}
			textBox.text = text;
		}
	}
}
