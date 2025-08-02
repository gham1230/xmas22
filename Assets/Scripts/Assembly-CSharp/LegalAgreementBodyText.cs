using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

public class LegalAgreementBodyText : MonoBehaviour
{
	private enum State
	{
		Ready = 0,
		Loading = 1,
		Error = 2
	}

	[SerializeField]
	private Text textBox;

	[SerializeField]
	private TextAsset textAsset;

	[SerializeField]
	private RectTransform rectTransform;

	private List<Text> textCollection = new List<Text>();

	private Dictionary<string, string> cachedTextDict = new Dictionary<string, string>();

	private State state;

	public float Height => rectTransform.rect.height;

	private void Awake()
	{
		textCollection.Add(textBox);
	}

	public void SetText(string text)
	{
		string[] array = text.Split(new string[2]
		{
			Environment.NewLine,
			"\\r\\n"
		}, StringSplitOptions.None);
		for (int i = 0; i < array.Length; i++)
		{
			Text text2 = null;
			if (i >= textCollection.Count)
			{
				text2 = UnityEngine.Object.Instantiate(textBox, base.transform);
				textCollection.Add(text2);
			}
			else
			{
				text2 = textCollection[i];
			}
			text2.text = array[i];
		}
	}

	public void ClearText()
	{
		foreach (Text item in textCollection)
		{
			item.text = string.Empty;
		}
		state = State.Ready;
	}

	public async Task<bool> UpdateTextFromPlayFabTitleData(string key, string version)
	{
		string versionedKey = key + "_" + version;
		state = State.Loading;
		PlayFabClientAPI.GetTitleData(new GetTitleDataRequest
		{
			Keys = new List<string> { versionedKey }
		}, OnTitleDataReceived, OnPlayFabError);
		while (state == State.Loading)
		{
			await Task.Yield();
		}
		if (cachedTextDict.TryGetValue(versionedKey, out var value))
		{
			SetText(value.Substring(1, value.Length - 2));
			return true;
		}
		return false;
	}

	private void OnPlayFabError(PlayFabError obj)
	{
		Debug.LogError("ERROR: " + obj.ErrorMessage);
		state = State.Error;
	}

	private void OnTitleDataReceived(GetTitleDataResult obj)
	{
		cachedTextDict = obj.Data;
		state = State.Ready;
	}
}
