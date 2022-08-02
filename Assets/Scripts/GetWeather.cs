using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using System.Text.Json;
using System.Text.Json.Nodes;
using UnityEngine.UI;
using System.Linq;
using Cysharp.Threading.Tasks.Triggers;
using TMPro;


[SuppressMessage("ReSharper", "SuggestVarOrType_BuiltInTypes")]
[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
public class GetWeather : MonoBehaviour
{
	[SerializeField] private TMP_Dropdown dropDown;
	[SerializeField] private Button nextButton;
	[SerializeField] private Button backButton;
	[SerializeField] private TextMeshProUGUI weatherText;
	private JsonNode? _areaDict;

	private static readonly string[] Categories = {"centers", "offices"};

	private JsonNode CurrentNode { get; set; } = null;
	private string CurrentAreaCode { get; set; }
	private int _currentIndex = 0;
	private const string AreaUri = "https://www.jma.go.jp/bosai/common/const/area.json";
	private Dictionary<string, JsonNode> _currentChoices = null;

	private static string GetInfoUri(string areaCode) =>
		"https://www.jma.go.jp/bosai/forecast/data/overview_forecast/" + areaCode + ".json";

	private string CurrentCategory
	{
		get
		{
			if (_currentIndex < 0)
			{
				return Categories.First();
			}

			return Categories[_currentIndex];
		}
	}


	private async void Awake()
	{
		await AreaInit();
		backButton.onClick.AddListener(BackProcess);
		nextButton.onClick.AddListener(NextProcess);
		dropDown.onValueChanged.AddListener(OnDropdownChanged);
	}

	private void BackProcess()
	{
		if (_currentIndex - 1 < 0)
		{
			return;
		}

		_currentIndex--;

		UpdateDropdown(false);
		OnDropdownChanged(0);
	}

	private void NextProcess()
	{
		if (_currentIndex + 1 >= Categories.Length)
		{
			return;
		}

		_currentIndex++;

		UpdateDropdown(true);
		OnDropdownChanged(0);
	}

	private void OnDropdownChanged(int n)
	{
		// KeyValuePair<string, JsonNode> choice = _areaDict[CurrentCategory].AsDictionary().Where(node =>
		// {
		// 	if (!node.Value.AsDictionary().ContainsKey("parent") || CurrentNode == null)
		// 	{
		// 		return true;
		// 	}
		// 	else
		// 	{
		// 		return CurrentNode.AsDictionary()["parent"].ToString().Equals(node.Value["parent"].ToString());
		// 	}
		// }).ToArray()[n];
		var choice = _currentChoices.ToArray()[dropDown.value];
		CurrentAreaCode = choice.Key;
		CurrentNode = choice.Value;

		UpdateWeatherInfo().Forget();
	}

	private async UniTask UpdateWeatherInfo()
	{
		string areaCode = CurrentAreaCode;
		string infoText = await GetTextAsync(UnityWebRequest.Get(GetInfoUri(areaCode)));
		if (infoText != null)
		{
			var weatherJson = JsonNode.Parse(infoText);
			weatherText.text = weatherJson["text"].AsValue().ToString();
		}
		else
		{
			weatherText.text = "情報は見つかりませんでした。";
		}
	}

	private async UniTask AreaInit()
	{
		string areaData = await GetTextAsync(UnityWebRequest.Get(AreaUri));
		_areaDict = JsonNode.Parse(areaData);

		UpdateDropdown(true);
	}

	private void UpdateDropdown(bool isNext)
	{
		if (CurrentNode == null)
		{
			_currentChoices = _areaDict[CurrentCategory].AsDictionary();
		}
		else
		{
			Dictionary<string, JsonNode> nextElems;
			if (isNext)
			{
				string[] cu = CurrentNode["children"].AsArray().Select(node => node.ToString()).ToArray();

				IEnumerable<KeyValuePair<string, JsonNode>> GetArea()
				{
					foreach (var pair in _areaDict[CurrentCategory].AsDictionary())
					{
						if (cu.Contains(pair.Key))
						{
							yield return pair;
						}
					}
				}

				nextElems = GetArea().ToDictionary(pair => pair.Key, pair => pair.Value);
			}
			else
			{
				string cu = CurrentNode["parent"].ToString();
				JsonNode parent = _areaDict[CurrentCategory][cu];

				IEnumerable<KeyValuePair<string, JsonNode>> GetArea()
				{
					foreach (var pair in _areaDict[CurrentCategory].AsDictionary())
					{
						if (parent["parent"].ToString().Equals(pair.Value["parent"].ToString()))
						{
							yield return pair;
						}
					}
				}

				nextElems = parent.AsDictionary().ContainsKey("parent")
					? GetArea().ToDictionary(pair => pair.Key, pair => pair.Value)
					: _areaDict[CurrentCategory].AsDictionary();
			}

			_currentChoices = nextElems;
		}

		dropDown.options = _currentChoices.Values.Select(node => new TMP_Dropdown.OptionData(node["name"].ToString()))
			.ToList();

		dropDown.value = 0;
	}

	private static async UniTask<string> GetTextAsync(UnityWebRequest req, int timeOut = 5)
	{
		var cts = new CancellationTokenSource();
		try
		{
			var op = await UniTask.WhenAny(req.SendWebRequest().ToUniTask(cancellationToken: cts.Token),
				UniTask.Delay(TimeSpan.FromSeconds(timeOut), cancellationToken: cts.Token));
			if (!op.hasResultLeft)
			{
				Debug.LogWarning("接続がタイムアウトしました。" + req.url);
				cts.Cancel();
				return null;
			}
		}
		catch (UnityWebRequestException e)
		{
			//存在してない
			return null;
		}

		Debug.Log(req.responseCode);
		return req.downloadHandler.text;
	}

	private void Update()
	{
		Debug.ClearDeveloperConsole();
		Debug.Log(CurrentCategory);
		Debug.Log(CurrentNode);
	}
}