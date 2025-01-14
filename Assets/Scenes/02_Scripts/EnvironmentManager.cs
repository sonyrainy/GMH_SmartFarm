using System.Collections;
using UnityEngine;

public class EnviromentManager : MonoBehaviour
{
    private static EnviromentManager instance;
    public static EnviromentManager inst { get { return instance; } }

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

    int year = 2024;
    int month = 1;
    int day = 1;
    float temperature;
    float humidity;

    // 하루 길이 설정
    [SerializeField]
    float dayDuration = 10f; 

    private bool dataReceived = false;

    void Start()
    {
        // 날씨 데이터 업데이트 콜백 설정
        WeatherAPI.updateCallBack += TempAndHumidityUpdate;
        StartCoroutine(DayUpdate());
    }

    IEnumerator DayUpdate()
    {
        while (year < 2025) 
        {
            dataReceived = false;

            // 날씨 데이터 요청
            WeatherAPI.inst.StartGetData(year, month, day);

            // 데이터 업데이트될 때까지 대기
            yield return new WaitUntil(() => dataReceived);

            PrintDailyWeather();

            // 하루 대기
            yield return new WaitForSeconds(dayDuration);

            // 날짜 업데이트
            day++;
            if (day > System.DateTime.DaysInMonth(year, month))
            {
                day = 1;
                month++;
                if (month > 12)
                {
                    month = 1;
                    year++;
                }
            }
        }
    }

    public void TempAndHumidityUpdate(WeatherAPI.Root _data)
    {
        if (_data != null && _data.data != null && _data.data.Length > 0)
        {
            temperature = WeatherAPI.KToC(_data.data[0].temp);
            humidity = _data.data[0].humidity;
            dataReceived = true;
        }
        else
        {
            Debug.LogWarning("Invalid data received in TempAndHumidityUpdate.");
        }
    }

    void PrintDailyWeather()
    {
        Debug.Log($"Year {year}, Month {month}, Day {day} - Temperature: {temperature:F1}°C, Humidity: {humidity:F1}%");
    }
}
