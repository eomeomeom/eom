using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine;
using LitJson;
using Debug = SOA_DEBUG.Debug;


public class DataLoader : MonoBehaviour
{
    public const string _DataUrlFormat = "https://data/{0}/{1}/data.json";
    public const string _VersionFileName = "version";

    public class SchemaObject
    {
        public Dictionary<string, int> version;
    }

    
    private Dictionary<string, int> _CachedTableVersionData = null;

    public enum TableUpdatePolicy
    {
        Normal_UpdateTableIfVersionDiffers,
        NoUpdate_ForceUsingCachedTable,
        ForceUpdate_ForceUsingTableFromServer,
    }
    public TableUpdatePolicy _TableUpdatePolicy = TableUpdatePolicy.Normal_UpdateTableIfVersionDiffers;

    public bool loadCompleted { get; private set; }
    public string errorString { get; private set; }

    public int totalTableCount { get; private set; }
    public int curTableIndex { get; private set; }
    public float curTableLoadProgress { get; private set; }

    public void LoadTableDataAsync()
    {
        this.loadCompleted = false;
        StopAllCoroutines();
        StartCoroutine(LoadTableWorker());
    }
    
    private IEnumerator LoadTableWorker()
    {
        var configSO = ResourceManager.LoadAddressableWaitForCompletion<GameConfig>(DefineName.ScriptableObject.GAME_CONFIG_DATA);
        //데이터 URL
        var schemaPath = configSO.SchemaPath;

        this.errorString = "";
        Dictionary<string, int> tableVersionDataFromServer = null;

        // 스키마를 서버로부터 받아서 version 항목을 테이블 버전 정보로 사용
        {
            var request = UnityWebRequest.Get(schemaPath);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                SchemaObject tmp = JsonMapper.ToObject<SchemaObject>(request.downloadHandler.text);
                tableVersionDataFromServer = tmp?.version;
            }
            else
            {
                this.errorString = "data network error";
                yield break;
            }
            yield return new WaitForSeconds(1f);
        }

        // 서버로부터 받은 버전정보 항목들을 순환하면서
        // 해당 항목의 테이블명의 테이블을 로드하여 사용 가능하도록 세팅한다
        this.totalTableCount = tableVersionDataFromServer.Count;
        this.curTableIndex = 0;
        foreach (var tableNameVersionPair in tableVersionDataFromServer)
        {
            string tableName = tableNameVersionPair.Key;

            int tableVersionFromServer = tableNameVersionPair.Value;

            if (!string.IsNullOrEmpty(this.errorString))
                break;

            this.curTableLoadProgress = 0.0f;

            JsonData cachedTableJsonDataFromFile = GetCachedTableJsonDataFromFile(tableName, tableVersionFromServer);

            // 캐시된 테이블의 Json 데이터를 읽어오는데 성공했을경우 이로부터 실제 테이블 데이터를 세팅함
            if (cachedTableJsonDataFromFile != null)
            {
                this.curTableLoadProgress = 1.0f; 
                
                if (!Data.LoadTableDataFromJsonData(tableName, cachedTableJsonDataFromFile))
                {
                    this.errorString = string.Format("{0} Table Parsing Error. Invalid cached table file contents.", tableName);
                }
                yield return null;
            }
            // 캐시된 테이블 데이터를 업데이트 정책상 읽어오지 않아야 하는 상황이거나, 캐시된 테이블 데이터 읽어오기에 실패하면
            // 서버로부터 전송받아 세팅함
            else
            {
                while (string.IsNullOrEmpty(this.errorString))
                {
                    var url = string.Format(_DataUrlFormat, tableName, tableVersionFromServer);
                    
                    var request = UnityWebRequest.Get(url);
                    var requestOp = request.SendWebRequest();
                    while (false == request.isDone)
                    {
                        this.curTableLoadProgress = requestOp.progress;
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        JsonData tableJsonDataFromServer = JsonMapper.ToObject(request.downloadHandler.text);
                        tableJsonDataFromServer.Remove("SCHEMA");
                        if (!Data.LoadTableDataFromJsonData(tableName, tableJsonDataFromServer))
                        {
                            this.errorString = string.Format("{0} Table Parsing Error. Invalid table contents from table server.",
                                tableName);
                        }
                        else
                        {
                            // 서버에서 전송받은 테이블 로딩에 성공하면 해당 테이블 내용을 파일로 저장
                            SaveData(tableName, tableJsonDataFromServer);
                            Debug.Log(string.Format("{0} Table Data Version {1} cached", tableName, tableVersionFromServer));
                        }

                        break;
                    }
                    else
                    {
                        this.errorString = request.result.ToString();
                    }

                    yield return new WaitForSeconds(1f);
                }
            }
            
            this.curTableIndex++;
        }

        // 전체 테이블 로딩에 문제가 없으면, 서버로부터 전송받은 버전정보를 파일에 기록한다
        if (string.IsNullOrEmpty(this.errorString) &&
            _TableUpdatePolicy != TableUpdatePolicy.NoUpdate_ForceUsingCachedTable)
        {
            JsonWriter jsonWriter = new JsonWriter();
            jsonWriter.PrettyPrint = true;

            JsonMapper.ToJson(tableVersionDataFromServer, jsonWriter);
            JsonData jsonTableVersionDataFromServer = JsonMapper.ToObject(jsonWriter.ToString());
            SaveData(_VersionFileName, jsonTableVersionDataFromServer);
        }
        this.loadCompleted = true;
    }

    public static string GetDataPath()
    {
        return Application.persistentDataPath + "/StaticData";
    }

    private void SaveData(string fileName, JsonData data)
    {
        var di = new DirectoryInfo(GetDataPath());
        if (false == di.Exists)
            di.Create();

        var path = GetDataPath() + "/" + fileName;
        var text = data.ToJson();
        File.WriteAllText(path, text);
    }

    // 캐시되어있는 테이블의 버전값을 구해온다
    // 버전 정보 자체가 저장되어있지 않거나, 해당 테이블명에 대한 버전 정보가 없을 경우 null 반환
    private int? GetCachedTableVersion(string tableName)
    {
        if (false == Utility.IsStringValid(tableName))
            return null;

        if (null == _CachedTableVersionData)
        {
            var path = GetDataPath() + "/" + _VersionFileName;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                _CachedTableVersionData = JsonMapper.ToObject<Dictionary<string, int>>(json);
            }
            else
            {
                _CachedTableVersionData = new Dictionary<string, int>();
            }
        }

        if (false == _CachedTableVersionData.TryGetValue(tableName, out int verVal))
            return null;

        return verVal;
    }

    // 테이블 업데이트 정책에 따라, 캐시된 파일로부터 JSON 데이터를 불러온다.
    // 업데이트 정책이 캐시된 파일을 불러오지 않아야 하는 상황이거나 파일로부터 불러오기가 실패할 경우 null 을 반환
    private JsonData GetCachedTableJsonDataFromFile(string tableName, int tableVersionFromServer)
    {
        if (_TableUpdatePolicy == TableUpdatePolicy.ForceUpdate_ForceUsingTableFromServer)
            return null;

        int? cachedTableVersion = GetCachedTableVersion(tableName);

        if (false == cachedTableVersion.HasValue)
            return null;

        JsonData cachedTableJsonDataFromFile = null;
        if (cachedTableVersion.Value == tableVersionFromServer ||
            TableUpdatePolicy.NoUpdate_ForceUsingCachedTable == _TableUpdatePolicy)
        {
            // 테이블 파일명은 테이블 이름과 같다.
            var path = GetDataPath() + "/" + tableName;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                cachedTableJsonDataFromFile = JsonMapper.ToObject(json);
            }
        }

        return cachedTableJsonDataFromFile;
    }
}
