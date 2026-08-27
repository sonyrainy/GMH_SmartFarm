// EnvironmentManager.cs
// 역할: 과거 날씨 데이터를 하루 단위로 받아,
// 하루를 짧은 주기(dayDuration)로 재현하며 외부 환경(온도/습도)을 갱신한다.

using System;
using System.Collections;
using UnityEngine;

namespace SmartFarm {
    public class EnvironmentManager : MonoBehaviour {
        public static EnvironmentManager Instance { get; private set; }

        [Header("시뮬레이션")]
        [Tooltip("이 해의 1월 1일부터 12월 31일까지 하루씩 재현한다")]
        [SerializeField] private int simulationYear = 2024;
        [Tooltip("하루가 실제로 흐르는 시간(초)")]
        [SerializeField] private float dayDuration = 10f;

        public DateTime CurrentDate { get; private set; }
        public float Temperature { get; private set; } // °C
        public int Humidity { get; private set; }      // %

        private bool isWeatherReceived = false;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start() {
            WeatherApi.WeatherDataReceived += OnWeatherDataReceived;
            StartCoroutine(CoSimulateDays());
        }

        private void OnDestroy() {
            WeatherApi.WeatherDataReceived -= OnWeatherDataReceived;
        }

        private IEnumerator CoSimulateDays() {
            if (WeatherApi.Instance == null) {
                Debug.LogError("씬에 WeatherApi가 없다.");
                yield break;
            }

            CurrentDate = new DateTime(simulationYear, 1, 1);

            while (CurrentDate.Year == simulationYear) {
                isWeatherReceived = false;
                WeatherApi.Instance.RequestWeather(CurrentDate);
                yield return new WaitUntil(() => isWeatherReceived);

                PrintDailyWeather();
                yield return new WaitForSeconds(dayDuration);

                CurrentDate = CurrentDate.AddDays(1);
            }
        }

        private void OnWeatherDataReceived(WeatherResponse response) {
            if (response == null || response.data == null || response.data.Length == 0) {
                Debug.LogWarning("유효하지 않은 날씨 데이터를 받았다.");
                return;
            }

            WeatherInfo today = response.data[0];
            Temperature = WeatherApi.KelvinToCelsius(today.temp);
            Humidity = today.humidity;
            isWeatherReceived = true;
        }

        private void PrintDailyWeather() {
            Debug.Log($"{CurrentDate:yyyy-MM-dd} - 온도: {Temperature:F1}°C, 습도: {Humidity}%");
        }
    }
}
