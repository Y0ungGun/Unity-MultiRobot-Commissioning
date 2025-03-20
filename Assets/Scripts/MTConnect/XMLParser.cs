using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Xml.Linq;
using System.IO;
using UnityEngine.Networking;

namespace MyVC
{
    public class XMLParser : MonoBehaviour
    {
        public string ip;
        public int port;
        private XmlTextReader xmltxt;
        private string url;
        public float refreshInterval = 1f;

        private Dictionary<string, double> jointValues = new Dictionary<string, double>();
        void Start()
        {
            url = $"http://{ip}:{port}/current";
            StartCoroutine(GetMTConnectData());
        }
        IEnumerator GetMTConnectData()
        {
            while (true)
            {
                float startTime = Time.time; // Coroutine 시작 시간 기록

                UnityWebRequest request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();

                // 오류 확인
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("HTTP 요청 실패: " + request.error);
                }
                else
                {
                    // XML 응답을 받았으면, XmlTextReader 생성
                    string xmlResponse = request.downloadHandler.text;
                    List<string> DeviceUUIDs = ReadDeviceIDs(xmlResponse);
                    foreach (string Device in DeviceUUIDs)
                    {
                        ReadJointValues(Device, xmlResponse);
                        RenderUtils.DeviceRendering(Device, jointValues);
                    }
                }

                float endTime = Time.time; // 요청 후 시간 기록
                float elapsedTime = endTime - startTime; // 실행에 걸린 시간 계산

                //Debug.Log($"Coroutine 실행 시간: {elapsedTime}초");

                yield return new WaitForSeconds(refreshInterval);
            }
        }

        bool IsJoint(string name)
        {
            return name.EndsWith("j0") || name.EndsWith("j1") || name.EndsWith("j2") ||
                   name.EndsWith("j3") || name.EndsWith("j4") || name.EndsWith("j5");
        }
        List<string> ReadDeviceIDs(string xmlResponse)
        {
            StringReader stringReader = new StringReader(xmlResponse);
            xmltxt = new XmlTextReader(stringReader);
            xmltxt.WhitespaceHandling = WhitespaceHandling.None;

            List<string> deviceUUIDs = new List<string>();
            deviceUUIDs.Clear();

            while (xmltxt.Read())
            {
                if (xmltxt.NodeType == XmlNodeType.Element && xmltxt.Name == "DeviceStream" && xmltxt.GetAttribute("name").EndsWith("Adapter"))
                {
                    string uuid = xmltxt.GetAttribute("uuid");
                    if (!string.IsNullOrEmpty(uuid))
                    {
                        deviceUUIDs.Add(uuid);
                    }
                }
            }

            // XML 리더를 다시 처음으로 되돌림 (다시 읽기 위해)
            xmltxt.MoveToElement();
            return deviceUUIDs;
        }
        void ReadJointValues(string DeviceUUID, string xmlResponse)
        {
            StringReader stringReader = new StringReader(xmlResponse);
            xmltxt = new XmlTextReader(stringReader);
            xmltxt.WhitespaceHandling = WhitespaceHandling.None;

            jointValues.Clear();
            bool isTargetDevice = false;

            while (xmltxt.Read())
            {
                if (xmltxt.NodeType == XmlNodeType.Element)
                {
                    // DeviceStream 태그에서 UUID 확인
                    if (xmltxt.Name == "DeviceStream")
                    {
                        string uuid = xmltxt.GetAttribute("uuid");
                        isTargetDevice = (uuid == DeviceUUID);
                    }

                    // 해당 UUID의 Joint 값만 읽기
                    if (isTargetDevice && xmltxt.Name == "Angle")
                    {
                        string name = xmltxt.GetAttribute("name");

                        if (name != null && IsJoint(name))
                        {
                            xmltxt.Read();
                            if (xmltxt.NodeType == XmlNodeType.Text)
                            {
                                double value = Convert.ToDouble(xmltxt.Value);
                                jointValues[name.Substring(name.Length - 2)] = value;
                            }
                        }
                    }
                }
            }

            // XML 리더를 다시 처음으로 되돌림 (재사용 가능하도록)
            xmltxt.MoveToElement();
        }
    }
}

