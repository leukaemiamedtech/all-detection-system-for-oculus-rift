
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class Raycast : MonoBehaviour
{
    string[] materials = {
            "Im006_1",
            "Im020_1",
            "Im024_1",
            "Im026_1",
            "Im028_1",
            "Im031_1",
            "Im035_0",
            "Im041_0",
            "Im047_0",
            "Im053_1",
            "Im057_1",
            "Im060_1",
            "Im063_1",
            "Im069_0",
            "Im074_0",
            "Im088_0",
            "Im095_0",
            "Im099_0",
            "Im101_0",
            "Im106_0"
    };

    private static string apiKey = "";
    private static string apiSecret = "";
    private static string server = "";
    public static string serverPath = server + "/AI/YourEndpoint/Inference";

    public string image;
    public string imageName;

    bool processing = false;

    GameObject hitObject;

    string create_auth()
    {
        string auth = apiKey + ":" + apiSecret;
        auth = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(auth));
        return auth;
    }

    void resetBlocks()
    {
        foreach (string i in materials)
        {
            GameObject dataCube = GameObject.Find(i);
            dataCube.GetComponent<MeshRenderer>().material.color = Color.white;
        }
    }

    void checkButtons()
    {
        if (OVRInput.Get(OVRInput.Button.One))
        {
            resetBlocks();
        }
        if (OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0.2f)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 30))
            {
                int pos = Array.IndexOf(materials, hit.collider.name);
                if (pos > -1)
                {
                    StartCoroutine(SendApiRequest(hit));
                }
            }
        }
    }

    IEnumerator SendApiRequest(RaycastHit hit)
    {
        if (processing)
            yield break;
        processing = true;

        imageName = hit.collider.name + ".jpg";
        print(imageName);
        image = Path.Combine(Application.streamingAssetsPath, imageName);

        List<IMultipartFormSection> form = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("file", File.ReadAllBytes(image), imageName, "image/jpeg")
        };

        byte[] boundary = UnityWebRequest.GenerateBoundary();
        byte[] formSections = UnityWebRequest.SerializeFormSections(form, boundary);
        byte[] terminate = Encoding.UTF8.GetBytes(string.Concat("\r\n--", Encoding.UTF8.GetString(boundary), "--"));
        byte[] body = new byte[formSections.Length + terminate.Length];

        Buffer.BlockCopy(formSections, 0, body, 0, formSections.Length);
        Buffer.BlockCopy(terminate, 0, body, formSections.Length, terminate.Length);

        string contentType = string.Concat("multipart/form-data; boundary=", Encoding.UTF8.GetString(boundary));

        UnityWebRequest wr = new UnityWebRequest(serverPath, "POST");
        wr.SetRequestHeader("Authorization", "Basic " + create_auth());
        UploadHandler uploader = new UploadHandlerRaw(body);
        uploader.contentType = contentType;

        wr.uploadHandler = uploader;
        wr.downloadHandler = new DownloadHandlerBuffer();

        yield return wr.SendWebRequest();

        if (wr.isNetworkError || wr.isHttpError)
        {
            print(wr.error);
            processing = false;
        }
        else
        {
            string json = wr.downloadHandler.text;
            JSONNode jsonData = JSON.Parse(Encoding.UTF8.GetString(wr.downloadHandler.data));
            hitObject = hit.collider.gameObject;

            if (jsonData["Diagnosis"] == "Negative")
            {
                if (imageName.Contains("_0"))
                {
                    print("True Negative Classification");
                    print(hitObject.gameObject.GetComponent<MeshRenderer>().material.color);
                    hitObject.gameObject.GetComponent<MeshRenderer>().material.color = Color.green;
                    print(hitObject.gameObject.GetComponent<MeshRenderer>().material.color);
                    yield return null;
                }
                else
                {
                    print("False Negative Classification");
                    print(hitObject.gameObject.GetComponent<MeshRenderer>().material.color);
                    hitObject.gameObject.GetComponent<MeshRenderer>().material.color = Color.cyan;
                    print(hitObject.gameObject.GetComponent<MeshRenderer>().material.color);
                    yield return null;
                }
            }
            else if (jsonData["Diagnosis"] == "Positive")
            {
                if (imageName.Contains("_0"))
                {
                    print("False Postive Classification");
                    print(hitObject.gameObject.GetComponent<MeshRenderer>().material.color);
                    hitObject.gameObject.GetComponent<MeshRenderer>().material.color = Color.magenta;
                    print(hitObject.gameObject.GetComponent<MeshRenderer>().material.color);
                    yield return null;
                }
                else
                {
                    print("True Postive Classification");
                    print(hitObject.gameObject.GetComponent<MeshRenderer>().material.color);
                    hitObject.gameObject.GetComponent<MeshRenderer>().material.color = Color.red;
                    print(hitObject.gameObject.GetComponent<MeshRenderer>().material.color);
                    yield return null;
                }
            }
            processing = false;
        }
    }

    void Update()
    {
        OVRInput.FixedUpdate();
        OVRInput.Update();
        checkButtons(); 
    }
}