using HandTracking.Interfaces;
using RealityCollective.ServiceFramework.Services;
using System;
using System.IO;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

public class TestBatchImages : MonoBehaviour
{   
    [SerializeField]
    public string folder;
    public string date;
    private string directoryPath;

    private IHandTracker handtracker;
    private JointExporter jointExporter;

    public Renderer debugRenderer;

    void Start()
    {
        handtracker = ServiceManager.Instance.GetService<IHandTracker>();
        handtracker.Initialize();
        jointExporter = new JointExporter($"{date}-{folder}");
        var time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var startTime = handtracker.attributeToJSON("startTime", time, "\n");
        jointExporter.appendToFile(startTime);
        StartAsync();
    }

    async void StartAsync()
    {
        /* 
            Get all files in a directory
            For all images in the filepath specified, estimate hand pose
        */
        directoryPath = $"Assets/Scenes/Test/Resources/data/{date}/";
        var info = new DirectoryInfo(directoryPath + $"{folder}/");
        var fileInfo = info.GetFiles();
        int count = 1;
        foreach (FileInfo file in fileInfo) {
            var filename = Path.GetFileNameWithoutExtension(file.Name);
            var handTexture = Resources.Load<Texture2D>($"data/{date}/{folder}/" + filename);
            if (handTexture != null)
            {
                debugRenderer.material.mainTexture = handTexture;
                var palms = await handtracker.DetectPalms(handTexture);
                for (int i = 0; i < palms.rows(); i++)
                {
                    var handPose = await handtracker.EstimateHandPose(handTexture, palms.row(0));
                    var joint = handtracker.jointToJSON(count);
                    jointExporter.appendToFile(joint);
                    Debug.Log($"{count}");
                }
                count++;
            }
            //Destroy(handTexture);
        }
        jointExporter.writeFile();
        Debug.Log("finished!");
    }

    private void OnDestroy()
    {
        handtracker.Destroy();
        jointExporter.Dispose();
    }
}
