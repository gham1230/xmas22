using UnityEngine;

[CreateAssetMenu(fileName = "NewLegalAgreementAsset", menuName = "Gorilla Tag/Legal Agreement Asset")]
public class LegalAgreementTextAsset : ScriptableObject
{
	public string title;

	public string playFabKey;

	public string latestVersionKey;

	[TextArea(3, 5)]
	public string errorMessage;
}
