using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text;
using System;

public class SlackMenu : EditorWindow
{
    static UnityWebRequest _www;
    static Action<string> _finishedCallback;
    static string _message;

    [MenuItem("Slack/메시지 보내기")]
    public static void SendLineUp()
    {
        SendLineup(msg => EditorUtility.DisplayDialog("Slack", msg, "확인"));
    }

    public static void SendLineup(Action<string> finishedCallback)
    { 
        try
        {
            string json = PlayerPrefs.GetString("", string.Empty);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("*메시지*\n");

            if (json.Length > 0)
            {
                //sb.Append("```");
                sb.Append(json);
                //sb.Append("```");
            }

            SendLog(sb.ToString(), finishedCallback);
        }
        catch(Exception e)
        {
            if (null != finishedCallback)
            {
                finishedCallback(string.Format("전송에 실패했습니다.\n(Exception:{0})", e.ToString()));
            }
        }
    }
    public static void SendLog(string message, Action<string> finishedCallback)
    {
        _www = null;
        _finishedCallback = finishedCallback;
        _message = message;

        EditorApplication.update += EditorUpdate;
    }

    static void EditorUpdate()
    {
        try
        {
            if (null != _www && false == _www.isDone)
            {
                return;
            }

            if (null != _message && 0 < _message.Length)
            {
                string url = "https://hooks.slack.com/services/";

                string message;

                if (_message.Length > 4000)
                {
                    int pos = _message.Substring(0, 4000).LastIndexOf('\n');

                    if (pos >= 0)
                    {
                        message = _message.Substring(0, pos);
                        _message = _message.Substring(pos + 1);
                    }
                    else
                    {
                        message = _message;
                        _message = null;
                    }
                }
                else
                {
                    message = _message;
                    _message = null;
                }

                PostData data = new PostData();
                data.text = message;
                data.username = string.Format("{0}(Editor)", SystemInfo.deviceName);
                data.mrkdwn = false;
                string json_data = JsonUtility.ToJson(data);

                var form = new WWWForm();
                form.AddField("payload", JsonUtility.ToJson(data));

                _www = UnityWebRequest.Post(url, form);
                _www.SendWebRequest();
            }
            else
            {
                if (_www.isNetworkError)
                {
                    //Debug.Log(_www.error);

                    if (null != _finishedCallback)
                    {
                        _finishedCallback(string.Format("전송에 실패했습니다.\n(www error:{0})", _www.error));
                    }
                }
                else
                {
                    //Debug.Log(_www.downloadHandler.text);

                    if (null != _finishedCallback)
                    {
                        _finishedCallback("전송에 성공했습니다.");
                    }
                }

                _www = null;

                EditorApplication.update -= EditorUpdate;
            }
        }
        catch(Exception e)
        {
            EditorApplication.update -= EditorUpdate;
            Debug.LogError(e.ToString());
        }
    }

    [Serializable]
    class PostData
    {
        public string text;
        public string username;
        public bool mrkdwn;
    }
}
