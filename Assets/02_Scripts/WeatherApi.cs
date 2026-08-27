// WeatherApi.cs
// 역할: OpenWeather timemachine API로 특정 날짜/위치의 과거 날씨를 받아 오고,
// 결과를 이벤트로 알린다.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace SmartFarm {
    public class WeatherApi : MonoBehaviour {
        private const string TimemachineUrl = "https://api.openweathermap.org/data/3.0/onecall/timemachine";
        private const int NoonHour = 12; // 하루를 대표하는 시각으로 정오(UTC)를 사용
        private const float KelvinOffset = 273.15f;

        public static WeatherApi Instance { get; private set; }

        // 날씨 데이터를 성공적으로 받을 때마다 발생
        public static event Action<WeatherResponse> WeatherDataReceived;

        [Header("OpenWeather")]
        [Tooltip("OpenWeather API 키. 인스펙터에서 입력하고 저장소에는 올리지 않는다")]
        [SerializeField] private string apiKey;
        [Tooltip("남은 호출 횟수. 무료 플랜 한도 초과를 막기 위한 안전장치로, 0이 되면 요청하지 않는다")]
        [SerializeField] private int remainingApiCalls = 1000;

        [Header("위치")]
        [FormerlySerializedAs("Latitude")]
        [SerializeField] private float latitude = 37f;
        [FormerlySerializedAs("Longitude")]
        [SerializeField] private float longitude = 127f;

        public WeatherResponse LatestWeather { get; private set; }

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void RequestWeather(DateTime date) {
            StartCoroutine(CoRequestWeather(date));
        }

        public static float KelvinToCelsius(float kelvin) {
            return kelvin - KelvinOffset;
        }

        private IEnumerator CoRequestWeather(DateTime date) {
            if (string.IsNullOrEmpty(apiKey)) {
                Debug.LogError("API 키가 설정되지 않았다. WeatherApi 인스펙터에서 입력한다.");
                yield break;
            }

            if (remainingApiCalls <= 0) {
                Debug.LogError("API 호출 한도에 도달했다.");
                yield break;
            }

            Debug.Log($"날씨 요청: {date:yyyy-MM-dd}");

            using (UnityWebRequest request = UnityWebRequest.Get(BuildRequestUrl(date))) {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success) {
                    Debug.LogError($"날씨 데이터 요청 실패: {request.error}");
                    yield break;
                }

                remainingApiCalls--;
                HandleResponse(request.downloadHandler.text);
            }
        }

        private string BuildRequestUrl(DateTime date) {
            DateTime noonUtc = DateTime.SpecifyKind(date.Date.AddHours(NoonHour), DateTimeKind.Utc);
            long timestamp = new DateTimeOffset(noonUtc).ToUnixTimeSeconds();

            return $"{TimemachineUrl}?lat={latitude}&lon={longitude}&dt={timestamp}&appid={apiKey}";
        }

        private void HandleResponse(string json) {
            Debug.Log($"응답: {json}");

            WeatherResponse response = JsonUtility.FromJson<WeatherResponse>(json);
            if (response.data == null || response.data.Length == 0) {
                Debug.LogError("응답에 날씨 데이터가 없다.");
                return;
            }

            LatestWeather = response;
            WeatherDataReceived?.Invoke(response);
        }
    }

    // JsonUtility가 JSON 키 이름으로 매핑하므로, 필드 이름은 API 응답과 같아야 한다
    [Serializable]
    public class WeatherResponse {
        public WeatherInfo[] data;
    }

    [Serializable]
    public class WeatherInfo {
        public float temp;   // 켈빈(K)
        public int humidity; // %
    }
}
