using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Crosstales.FB;
using UnityEngine;
using UnityVolumeRendering;

public class DebugProgressView : IProgressView
{
    public void StartProgress(string title, string description)
    {
        Debug.Log($@"{title}: {description}");
    }

    public void FinishProgress(ProgressStatus status = ProgressStatus.Succeeded)
    {
        Debug.Log($@"Finish with status: {status.ToString()}");
    }

    public void UpdateProgress(float totalProgress, float currentStageProgress, string description)
    {
        //Debug.Log($@"{currentStageProgress}/{totalProgress}");
    }
}

public class VolumeLoader : MonoBehaviour
{
    #region Events and Deleagates

    public delegate void VolumeCreatedHandler();
    public static event VolumeCreatedHandler OnCreateVolume;

    #endregion

    private static VolumeRenderedObject _volumeRenderedObject;
    
    public static void LoadVolume()
    {
        string path = FileBrowser.Instance.OpenSingleFolder();
        DicomImportAsync(path);
    }

    public static Transform CreateAxialSlice()
    {
        if (_volumeRenderedObject == null) return null;

        SlicingPlane plane = _volumeRenderedObject.CreateSlicingPlane();
        plane.name = "AxialSlice";
        plane.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        return plane.transform;
    }

    public static Transform CreateCoronalSlice()
    {
        if (_volumeRenderedObject == null) return null;

        SlicingPlane plane = _volumeRenderedObject.CreateSlicingPlane();
        plane.name = "CoronalSlice";
        return plane.transform;
    }
    
    public static Transform CreateSagittalSlice()
    {
        if (_volumeRenderedObject == null) return null;

        SlicingPlane plane = _volumeRenderedObject.CreateSlicingPlane();
        plane.name = "SagittalSlice";
        plane.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
        return plane.transform;
    }
    
    private static async void DicomImportAsync(string dir)
    {
        if (Directory.Exists(dir))
        {
            Debug.Log("Async dataset load. Hold on.");
            using (ProgressHandler progressHandler = new ProgressHandler(new DebugProgressView()))
            {
                progressHandler.StartStage(0.7f, "Importing dataset");
                Task<VolumeDataset[]> importTask = DicomImportDirectoryAsync(dir, progressHandler);
                await importTask;
                progressHandler.EndStage();
                progressHandler.StartStage(0.3f, "Spawning dataset");
                for (int i = 0; i < importTask.Result.Length; i++)
                {
                    VolumeDataset dataset = importTask.Result[i];
                    _volumeRenderedObject = await VolumeObjectFactory.CreateObjectAsync(dataset);
                    _volumeRenderedObject.transform.position = new Vector3(i, 0, 0);
                    OnCreateVolume?.Invoke();
                }
                progressHandler.EndStage();
            }
        }
        else
        {
            Debug.LogError("Directory doesn't exist: " + dir);
        }
    }

    private static async Task<VolumeDataset[]> DicomImportDirectoryAsync(string dir, ProgressHandler progressHandler)
    {
        Debug.Log("Async dataset load. Hold on.");

        List<VolumeDataset> importedDatasets = new List<VolumeDataset>();
        bool recursive = true;

        // Read all files
        IEnumerable<string> fileCandidates = Directory.EnumerateFiles(dir, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Where(p => p.EndsWith(".dcm", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicom", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicm", StringComparison.InvariantCultureIgnoreCase));

        if (fileCandidates.Any())
        {
            progressHandler.StartStage(0.2f, "Loading DICOM series");

            IImageSequenceImporter importer = ImporterFactory.CreateImageSequenceImporter(ImageSequenceFormat.DICOM);
            IEnumerable<IImageSequenceSeries> seriesList = await importer.LoadSeriesAsync(fileCandidates, new ImageSequenceImportSettings { progressHandler = progressHandler });

            progressHandler.EndStage();
            progressHandler.StartStage(0.8f);

            int seriesIndex = 0, numSeries = seriesList.Count();
            foreach (IImageSequenceSeries series in seriesList)
            {
                progressHandler.StartStage(1.0f / numSeries, $"Importing series {seriesIndex + 1} of {numSeries}");
                VolumeDataset dataset = await importer.ImportSeriesAsync(series, new ImageSequenceImportSettings { progressHandler = progressHandler });
                if (dataset != null)
                {
                    importedDatasets.Add(dataset);
                }
                seriesIndex++;
                progressHandler.EndStage();
            }

            progressHandler.EndStage();
        }
        else
            Debug.LogError("Could not find any DICOM files to import.");

        return importedDatasets.ToArray();
    }

}
