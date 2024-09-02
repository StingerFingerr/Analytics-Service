using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using UnityEngine;

public class AnalyticsService : MonoBehaviour
{
    private List<EventData> _eventsToSend;

    private string _serverUrl = "http://serverurl.com";
    private string _saveFileName = "events.json";
    private float _cooldownBeforeSend = 5.0f;
    private string _saveFlePath;

    private async UniTaskVoid Start()
    {
        _saveFlePath = Path.Combine(Application.persistentDataPath, _saveFileName);
        _eventsToSend = await LoadEventsFromDisk();
        await StartSendingEvents();
    }

    private async UniTaskVoid OnApplicationQuit()
    {
        if(_eventsToSend.Count > 0)
            await SaveEventsToDisk();
    }

    public void TrackEvent(string type, string data) => 
        _eventsToSend.Add(new EventData { Type = type, Data = data });

    private async UniTask StartSendingEvents()
    {
        while (true)
        {
            await SendEvents();
            await UniTask.Delay((int)_cooldownBeforeSend * 1000);
        }
    }

    private async UniTask SendEvents()
    {
        if (_eventsToSend.Count == 0)
            return;
        using (HttpClient client = new())
        {
            var sendedEvents = new List<EventData>(_eventsToSend);
            _eventsToSend.Clear();
            string json = JsonUtility.ToJson(sendedEvents);
            var content = new StringContent(json);
            try
            {
                var response = await client.PostAsync(_serverUrl, content).AsUniTask();
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log("data sent successfully");
                }
                else
                {
                    _eventsToSend.AddRange(sendedEvents);
                    Debug.Log("failed to send data");
                }
            }
            catch(Exception e)
            {
                _eventsToSend.AddRange(sendedEvents);
                Debug.Log("failed to send data");
            }
        }
    }

    private async UniTask SaveEventsToDisk()
    {
        using (StreamWriter writer = new StreamWriter(_saveFlePath))
        {
            string json = JsonConvert.SerializeObject(_eventsToSend.ToArray());
            await writer.WriteLineAsync(json);
        }        
    }

    private async UniTask<List<EventData>> LoadEventsFromDisk()
    {
        if(File.Exists(_saveFlePath) is false)
            return new();

        using (StreamReader reader = new StreamReader(_saveFlePath))
        {
            string json = await reader.ReadToEndAsync();
            var savedEvents = JsonConvert.DeserializeObject<List<EventData>>(json);
            return savedEvents;
        }
    }

    [Serializable]
    private struct EventData
    {
        public string Type;
        public string Data;
    }
}
