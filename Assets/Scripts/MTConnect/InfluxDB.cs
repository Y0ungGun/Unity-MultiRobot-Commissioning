using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace MyVC
{
    public class InfluxDB : MonoBehaviour
    {
        public string ip;
        public int port;
        public string dbName;
        private string url;
        private string id;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            url = $"http://{ip}:{port}/query";
            url = "http://localhost:8086/query";
            StartCoroutine(GetData());
        }

        // Update is called once per frame
        void Update()
        {
        }

        IEnumerator GetData()
        {
            string query = "SELECT * FROM joint WHERE robot_id='DSR_M1013_001'";  // ���� ����
            string fullUrl = url + "?q=" + UnityWebRequest.EscapeURL(query) + "&db=" + dbName;

            UnityWebRequest request = UnityWebRequest.Get(fullUrl);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                // JSON �Ľ��Ͽ� ������ ���
                yield return StartCoroutine(ProcessInfluxDBResponse(jsonResponse));
            }
        }

        IEnumerator ProcessInfluxDBResponse(string jsonResponse)
        {
            Debug.Log(jsonResponse);
            Root root = JsonConvert.DeserializeObject<Root>(jsonResponse);

            // ���ϴ� ������ ����
            List<double> time = new List<double>();
            List<Dictionary<string, double>> jointValues = new List<Dictionary<string, double>>();
            string id = string.Empty;

            foreach (var result in root.results)
            {
                foreach (var series in result.series)
                {
                    if (series.name == "joint")
                    {
                        foreach (var value in series.values)
                        {
                            // time�� float�� ��ȯ�Ͽ� ����Ʈ�� �߰�
                            DateTime timeValue = DateTime.Parse(value[0].ToString());
                            double unixTime = timeValue.Subtract(DateTime.UnixEpoch).TotalSeconds;
                            time.Add(unixTime);

                            // joint ���� Dictionary�� ����
                            var jointDict = new Dictionary<string, double>
                        {
                            { "j0", Convert.ToDouble(value[1])*3.141592f/180 },
                            { "j1", Convert.ToDouble(value[2])*3.141592f/180 },
                            { "j2", Convert.ToDouble(value[3])*3.141592f/180 },
                            { "j3", Convert.ToDouble(value[4])*3.141592f/180 },
                            { "j4", Convert.ToDouble(value[5])*3.141592f/180 },
                            { "j5", Convert.ToDouble(value[6])*3.141592f/180 }
                        };
                            jointValues.Add(jointDict);

                            // robot_id ����
                            id = value[7].ToString();
                        }
                    }
                }
            }

            for(int i= 0; i < time.Count; i++)
            {
                double delta = 1.0;
                if (i<time.Count - 1)
                {
                    delta = time[i+1] - time[i];
                }


                RenderUtils.DeviceRendering(id, jointValues[i]);
                yield return new WaitForSeconds((float)delta);
            }
            Debug.Log("Time List:");
            foreach (var t in time)
            {
                Debug.Log(t);
            }

            Debug.Log("\nJoint Values:");
            foreach (var joint in jointValues)
            {
                foreach (var kvp in joint)
                {
                    Debug.Log($"{kvp.Key}: {kvp.Value}");
                }
            }

            Debug.Log($"\nRobot ID: {id}");

        }
        public class Result
        {
            public List<Series> series { get; set; }
        }

        public class Series
        {
            public string name { get; set; }
            public List<string> columns { get; set; }
            public List<List<object>> values { get; set; }
        }

        public class Root
        {
            public List<Result> results { get; set; }
        }
    }
}

