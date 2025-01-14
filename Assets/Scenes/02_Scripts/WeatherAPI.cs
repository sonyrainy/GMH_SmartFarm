using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class WeatherAPI : MonoBehaviour
{
    private static WeatherAPI instance;
    public static WeatherAPI inst { get { return instance; } }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // API 키 설정
    private string apiKey = "developer's api key _ from weatherAPI";
    private int apiCallLimit = 1000;

    // 위도, 경도 설정
    public float Latitude = 37f;
    public float Longitude = 127f;

    public delegate void WeatherDataUpdated(Root data);
    public static WeatherDataUpdated updateCallBack;

    Root weatherData;
    public Root data { get { return weatherData; } }

    public void StartGetData(int year, int month, int day)
    {
        StartCoroutine(GetWeatherData(year, month, day));
    }

    IEnumerator GetWeatherData(int year, int month, int day)
    {
        // 유닉스 타임스탬프 생성
        DateTimeOffset dateTO = new DateTimeOffset(new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc));
        var timestamp = dateTO.ToUnixTimeSeconds();

        // URL 생성
        string url = $"https://api.openweathermap.org/data/3.0/onecall/timemachine?lat={Latitude}&lon={Longitude}&dt={timestamp}&appid={apiKey}";
        Debug.Log("Request URL: " + url);

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            // API 호출 제한 확인
            if (apiCallLimit <= 0)
            {
                Debug.LogError("API Call Limit reached!");
                yield break;
            }

            // 요청 보내기
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching weather data: " + www.error);
            }
            else
            {
                Debug.Log("Received data: " + www.downloadHandler.text);

                Root wd = JsonUtility.FromJson<Root>(www.downloadHandler.text);

                if (wd.data != null && wd.data.Length > 0)
                {
                    weatherData = wd;

                    Debug.Log($"Temperature (K): {weatherData.data[0].temp}, Temperature (C): {KToC(weatherData.data[0].temp)}, Humidity: {weatherData.data[0].humidity}");

                    updateCallBack?.Invoke(weatherData);
                }
                else
                {
                    Debug.LogError("No weather data found in the response.");
                }

                apiCallLimit--;
            }
        }
    }

    public static float KToC(float kelvin)
    {
        return kelvin - 273.15f;
    }

    [Serializable]
    public class WeatherInfo
    {
        public float temp;
        public int humidity;
    }

    [Serializable]
    public class Root
    {
        public WeatherInfo[] data;
    }
}
