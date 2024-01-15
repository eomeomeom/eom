using System.Collections;
using System.IO;
using UnityEngine.Networking;
using LitJson;
using System;


public class DataGenerator
{
    [UnityEditor.MenuItem("SOA/데이터 코드 생성")]
    static void Generator()
    {
        Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(GeneratorData());
    }

    static IEnumerator GeneratorData()
    {
        var error = "";
        var configSO = ResourceManager.LoadAddressableWaitForCompletion<GameConfig>(DefineName.ScriptableObject.GAME_CONFIG_DATA);
        //데이터 URL
        var schemaPath = configSO.SchemaPath;
        var enumPath = configSO.EnumPath;

        //common enum
        while (string.IsNullOrEmpty(error))
        {
            var request = UnityWebRequest.Get(enumPath);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var writer = new StreamWriter("Assets/Game/Scripts/Data/Generated/Data_Enum.cs");
                    writer.Write(request.downloadHandler.text);
                    writer.Close();
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
                break;
            }
            else
            {
                error = "data network error";
            }
        }
        //common enum

        

        JsonData jsonData = null;
        while (string.IsNullOrEmpty(error))
        {
            var request = UnityWebRequest.Get(schemaPath);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                jsonData = JsonMapper.ToObject(request.downloadHandler.text)[DataLoader._VersionFileName];
                break;
            }
            else
            {
                error = "Schema Load Error";
                break;
            }
        }

        if (string.IsNullOrEmpty(error))
        {
            var tempTableList = "";
            var tempClassList = "";
            var tempSwitch = "";
            var parseStringShort = "Data.JsonParseToString(";
            var parseFloatShort = "Data.JsonParseToSingle(";
            var parseUIntgShort = "Data.JsonParseUInt32(";
            var parseBoolShort = "Data.JsonParseToBoolean(";
            var parseIntShort = "Data.JsonParseToInt32(";
            var parseString = "Data.JsonParseToString(data,";
            var parseFloat = "Data.JsonParseToSingle(data,";
            var parseUInt = "Data.JsonParseUInt32(data,";
            var parseBool = "Data.JsonParseToBoolean(data,";
            var parseInt = "Data.JsonParseToInt32(data,";


            foreach (string v in jsonData.Keys)
            {
                if (!string.IsNullOrEmpty(error))
                    break;

                var url = string.Format(DataLoader._DataUrlFormat, v, jsonData[v]);
                var request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var data = JsonMapper.ToObject(request.downloadHandler.text);
                    if (data != null)
                    {
                        try
                        {
                            tempClassList += "\npublic class " + v + "Table" + "{";
                            var temp = "";
                            var keyType = "string";
                            var keyIsEnum = false;
                            var keyParse = parseStringShort;
                            var keyName = "Enum_Id";
                            foreach (string value in data["SCHEMA"].Keys)
                            {
                                var schemaData = data["SCHEMA"][value];
                                var dataType = "";
                                var enumData = "";
                                var key = false;
                                var refTable = "";
                                var array = false;
                                int arrayCnt = 0;
                                if (schemaData.ContainsKey("DATA_TYPE"))
                                    dataType = schemaData["DATA_TYPE"].ToString();
                                if (schemaData.ContainsKey("ENUM"))
                                    enumData = schemaData["ENUM"].ToString();
                                if (schemaData.ContainsKey("DATA_KEY"))
                                    key = Convert.ToBoolean(schemaData["DATA_KEY"].ToString());
                                if (schemaData.ContainsKey("REFERENCE_SHEET"))
                                    refTable = schemaData["REFERENCE_SHEET"].ToString();
                                if (schemaData.ContainsKey("IS_ARRAY"))
                                {
                                    array = Convert.ToBoolean(schemaData["IS_ARRAY"].ToString());
                                    if (schemaData.ContainsKey("DATA_LENGTH"))
                                        arrayCnt = Convert.ToInt32(schemaData["DATA_LENGTH"].ToString());
                                }

                                if (!string.IsNullOrEmpty(dataType))
                                {
                                    var tempParse = parseString;
                                    var tempParseShort = parseStringShort;
                                    if (dataType == "uint")
                                    { tempParse = parseUInt; tempParseShort = parseUIntgShort; }
                                    else if (dataType == "float")
                                    { tempParse = parseFloat; tempParseShort = parseFloatShort; }
                                    else if (dataType == "bool")
                                    { tempParse = parseBool; tempParseShort = parseBoolShort; }
                                    else if (dataType == "int")
                                    { tempParse = parseInt; tempParseShort = parseIntShort; }

                                    var accessModifier = string.IsNullOrEmpty(refTable) ? "public" : "private";
                                    var setter = string.IsNullOrEmpty(refTable) ? "private set" : "set";

                                    if (key)
                                    {
                                        keyIsEnum = !string.IsNullOrEmpty(enumData);
                                        keyType = keyIsEnum ? $"Data.Enum.{enumData}" : dataType;
                                        keyParse = keyIsEnum ? "Data.JsonParseToEnum(" : tempParseShort;
                                        keyName = value;
                                    }

                                    var keyVarName = $"_{value}";
                                    if (!string.IsNullOrEmpty(refTable))
                                        keyVarName += "_Key";

                                    if (!array)
                                    {
                                        if (!string.IsNullOrEmpty(enumData))
                                        {
                                            temp += $"\n        {keyVarName} = Data.JsonParseToEnum(data, \"{value}\", Data.Enum.{enumData}.CNT);";
                                            tempClassList += $"\n    {accessModifier} Data.Enum.{enumData} {keyVarName} " + $"{{ get; {setter}; }}";
                                        }
                                        else
                                        {
                                            temp += $"\n        {keyVarName} = {tempParse} \"{value}\");";
                                            tempClassList += $"\n    {accessModifier} {dataType} {keyVarName} " + $"{{ get; {setter}; }}";
                                        }

                                        if (!string.IsNullOrEmpty(refTable))
                                        {
                                            tempClassList += $"\n    public {refTable.ToLower()}Table _{value}_Data " + $"{{ get => Data.GetDataFromTable(Data._{refTable.ToLower()}Table, {keyVarName}); }}";
                                        }
                                    }
                                    else if (array && arrayCnt > 0)
                                    {
                                        if (!string.IsNullOrEmpty(enumData))
                                        {
                                            temp += $"\n        {keyVarName} = new Data.Enum.{enumData}[data[\"{value}\"].Count];\n" +
                                                    $"        for (int i = 0; i < {keyVarName}.Length; i++)\n            {keyVarName}[i] = Data.JsonParseToEnum(data, \"{value}\", Data.Enum.{enumData}.CNT, i);";
                                            tempClassList += $"\n    {accessModifier} Data.Enum.{enumData}[] {keyVarName} " + $"{{ get; {setter}; }}";
                                        }
                                        else
                                        {
                                            temp += $"\n        {keyVarName} = new {dataType}[data[\"{value}\"].Count];\n" +
                                                    $"        for (int i = 0; i < {keyVarName}.Length; i++)\n            {keyVarName}[i] = {tempParse} \"{value}\", i);";
                                            tempClassList += $"\n    {accessModifier} {dataType}[] {keyVarName} " + $"{{ get; {setter}; }}";
                                        }

                                        if (!string.IsNullOrEmpty(refTable))
                                        {
                                            tempClassList += $"\n    public {refTable.ToLower()}Table[] _{value}_Data " + $"{{ get => Data.GetDataListFromTable(Data._{refTable.ToLower()}Table, {keyVarName}).ToArray(); }}";
                                        }
                                    }
                                }
                            }
                            tempClassList += $"\n    public {v}Table(JsonData data)" + "{" + temp + "\n    }\n}";
                            tempTableList += string.Format("    public static Dictionary<{2}, {0}Table> _{0}Table {1} = new Dictionary<{2}, {0}Table>();\n" +
                            "    const string _{0}TableName = \"{0}\";\n", v, "{get; private set;}", keyType);
                            string keyString = $"{keyParse}jsonData[v], \"{keyName}\"";
                            if (keyIsEnum)
                                keyString += $", {keyType}.CNT";
                            keyString += ")";
                            tempSwitch += $"\n                case _{v}TableName : \n                    _{v}Table.Clear();\n                    " +
                                $"foreach (var v in jsonData.Keys) _{v}Table.Add({keyString}, new {v}Table(jsonData[v])); break;";
                        }
                        catch (Exception e)
                        {
                            error = e.Message;
                        }
                    }
                    else
                        error = $"{v} TableLoad Error";
                }
                else
                    error = $"{v} TableLoad Error";
            }

            if (string.IsNullOrEmpty(error))
            {
                try
                {
                    var temp =
                        "using System;\nusing System.Collections;\nusing System.Collections.Generic;\nusing UnityEngine;\nusing LitJson;\nusing  System.IO;\nusing Debug = SOA_DEBUG.Debug;\n" +
                        "public static partial class Data {\n" +
                        tempTableList +
                        "    public static bool LoadTableDataFromJsonData(string tableName, JsonData jsonData){\n        try{\n            switch (tableName){" +
                        tempSwitch +
                        "\n            }\n        }\n        catch (Exception ex){\n            Debug.LogError(tableName);\n            Debug.LogError(ex.Message);\n            return false;\n        }\n        return true;\n    }\n}\n";

                    var writer = new StreamWriter("Assets/Game/Scripts/Data/Generated/Data_Table.cs");
                    writer.Write(temp);
                    writer.Write(tempClassList);
                    writer.Close();
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
            }
        }

        if (string.IsNullOrEmpty(error))
            UnityEngine.Debug.Log("Success");
        else
            UnityEngine.Debug.LogError(error);

        UnityEditor.AssetDatabase.Refresh();
    }

    [UnityEditor.MenuItem("SOA/캐시된 데이터 테이블 삭제")]
    static void DisposeCachedTable()
    {
        Directory.Delete(DataLoader.GetDataPath(), true);
    }
}
