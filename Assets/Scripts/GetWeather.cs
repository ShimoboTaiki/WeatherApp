using System;
using System.Collections;
using System.Collections.Generic;
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
using UnityEngine.Serialization;


public class GetWeather : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private TMP_Dropdown _dropDown;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private TextMeshProUGUI weatherText;
    private string _url;
    private JsonNode? _areaDict;
    private static readonly string[] _categories = new string[] { "centers","offices","class10s","class15s","class20s"};
    JsonNode currentNode = null;
    private int currentIndex;

    async UniTask Awake()
    {
        //DropDownProcess().Forget();
        _dropDown.onValueChanged.AddListener ( _ => UpdateWeatherText().Forget());
        _nextButton.onClick.AddListener(()=>currentIndex=Mathf.Clamp(currentIndex+1,0,_categories.Length-1));
        _backButton.onClick.AddListener(()=>currentIndex=Mathf.Clamp(currentIndex-1,0,_categories.Length-1));
        _url = "https://www.jma.go.jp/bosai/common/const/area.json";
        string text = await GetTextAsync(UnityWebRequest.Get(_url));
        Debug.Log(text);
        // Weather sigaWeather = JsonUtility.FromJson<Weather>(text);
        // Debug.Log(sigaWeather.ToString());



        _areaDict = JsonNode.Parse(text);


        
        while(true)
        {
            
            await ChangeDropDown(_categories[currentIndex]);
            Debug.Log(currentIndex);
            if (currentIndex == 1)
            {
                int a = 0;
            }
            
        }

        #region APITest

        

        
        var kinkiArea = _areaDict["centers"]["010600"];
        //Debug.Log(area.Value["name"]+":"+area.Value["children"]);
        foreach (var child in kinkiArea["children"].AsArray())
        {
            string prefText =
                await GetTextAsync(UnityWebRequest.Get("https://www.jma.go.jp/bosai/forecast/data/overview_forecast/" +
                                                       child + ".json"));
            var prefDict=JsonNode.Parse(prefText);
            Debug.Log(prefDict["targetArea"] + ":" + prefDict["text"]);
        }

        var sigaArea = _areaDict["offices"]["250000"];
        foreach (var child in sigaArea["children"].AsArray())
        {
            string sigaText =
                await GetTextAsync(UnityWebRequest.Get("https://www.jma.go.jp/bosai/forecast/data/overview_forecast/" +
                                                       child + ".json"));
            if (sigaText != null)
            {
                var sigaDict=JsonNode.Parse(sigaText);
                Debug.Log(sigaDict["targetArea"] + ":" + sigaDict["text"]);
            }
            else
            {
                var officesName = _areaDict["offices"]["250000"]["name"].ToString();
                var class10sName = _areaDict["class10s"][child.ToString()]["name"].ToString();
                Debug.Log(officesName+class10sName + "の情報はありませんでした");
            }
            
        }
        #endregion



    }

    private void Start()
    {
        UpdateWeatherText().Forget();
    }

    private async UniTask ChangeDropDown(string category)
    {
        var centersDictionary = _areaDict[category].AsDictionary();
        string linkCate = "children";
        var names = LinkCateNames(centersDictionary, linkCate);
        _dropDown.options = names.Select(node=>node["name"])
            .Select(officeName => new TMP_Dropdown.OptionData(officeName.ToString())).ToList();
        var result=await UniTask.WhenAny(_nextButton.onClick.GetAsyncEventHandler(CancellationToken.None).OnInvokeAsync(),_backButton.onClick.GetAsyncEventHandler(CancellationToken.None).OnInvokeAsync());
        if (result == 0)
        {
            currentNode = names.ToArray()[_dropDown.value];
        }
        else
        {
            currentNode = LinkCateNames(centersDictionary, "parent").ToArray().FirstOrDefault();
            return;
        }
        //await UniTask.WaitUntil(()=>Input.GetKeyDown(KeyCode.A));
        
        
        await UpdateWeatherText();
    }

    private IEnumerable<JsonNode> LinkCateNames(Dictionary<string, JsonNode> centersDictionary, string linkCate)
    {
        IEnumerable<JsonNode> names = centersDictionary.Values;
        if (currentNode != null)
        {
            List<JsonNode> list = new List<JsonNode>();
            if (linkCate == "children")
            {
                foreach (var child in currentNode[linkCate].AsArray())
                {
                    list.Add(centersDictionary[child.ToString()]);
                }
            }
            else
            {
                var parentCategory = _areaDict[_categories[currentIndex]];
                list.Add(parentCategory[currentNode[linkCate].AsValue().ToString()]);
            }

            names = list;
        }

        return names;
    }

    async UniTask DropDownProcess()
    {
        await _dropDown.onValueChanged.GetAsyncEventHandler(CancellationToken.None).OnInvokeAsync();
        Debug.Log(_dropDown.value+"番目が選択された");
    }
    async UniTask<string> GetTextAsync(UnityWebRequest req, int timeOut=5)
    {
        var cts = new CancellationTokenSource();
        try
        {
            var op = await UniTask.WhenAny(req.SendWebRequest().ToUniTask(cancellationToken: cts.Token),
                UniTask.Delay(TimeSpan.FromSeconds(timeOut)));
            if (!op.hasResultLeft)
            {
                Debug.LogWarning("接続がタイムアウトしました。" + req.url);
                cts.Cancel();
                return null;
            }
        }
        catch (UnityWebRequestException e)
        {
            Debug.LogError("タイムアウトしました。");
            return null;
        }

        Debug.Log(req.responseCode);
        return req.downloadHandler.text;
    }

    async UniTask UpdateWeatherText()
    {
        string areaCode = "0";
        if (currentNode != null)
        {
            areaCode = currentNode["children"].AsArray()[_dropDown.value].AsValue().ToString();
        }
        else
        {
            areaCode = _areaDict[_categories[0]].AsDictionary().Keys.ToArray()[_dropDown.value].ToString();
        }
        Debug.Log("awaitする");
        string text =
            await GetTextAsync(UnityWebRequest.Get("https://www.jma.go.jp/bosai/forecast/data/overview_forecast/" + areaCode + ".json"));
        Debug.Log("await終わり");
        if (text != null)
        {
            var weatherJson = JsonNode.Parse(text);
            weatherText.text = weatherJson["text"].AsValue().ToString();
        }
        else
        {
            weatherText.text = _areaDict[_categories[currentIndex]][areaCode]["name"]+ "の情報は見つかりませんでした。";
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.E))
        {
            Debug.Log(("Eキーが押された。"));
        }
       
    }
}