using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceProjectionViewManager
{
    private static SliceProjectionViewManager _instance;
    public static SliceProjectionViewManager Instance 
    { 
        get
        {
            if (_instance == null)
            {
                _instance = new SliceProjectionViewManager();
                VolumeLoader.OnCreateVolume += Instance.VolumeCreatedEvent;
            }
            return _instance;
        }
    }
    
    private static readonly int MinLayer = 12;
    private static readonly int MaxLayer = 24;


    private Dictionary<int, SliceProjectionView> viewsByLayer = new Dictionary<int, SliceProjectionView>();

    public static SliceProjectionView CreateView(SliceProjectionAxis axis)
    {
        int layer = Instance.FindFreeLayer();
        if (layer == -1)
        {
            Debug.LogError("Не удалось найти свободный слой для SliceProjectionView");
            return null;
        }

        var view = new SliceProjectionView(layer, axis);
        Instance.viewsByLayer[layer] = view;
        return view;
    }

    private int FindFreeLayer()
    {
        for (int i = MinLayer; i <= MaxLayer; i++)
        {
            if (!viewsByLayer.ContainsKey(i))
            {
                return i;
            }
        }
        return -1; // Все слои заняты
    }
    
    private void VolumeCreatedEvent()
    {
        foreach (var pair in viewsByLayer)
        {
            SliceProjectionView view = pair.Value;

            Transform slice = view.ProjectionAxis switch
            {
                SliceProjectionAxis.Axial => VolumeLoader.CreateAxialSlice(),
                SliceProjectionAxis.Coronal => VolumeLoader.CreateCoronalSlice(),
                SliceProjectionAxis.Sagittal => VolumeLoader.CreateSagittalSlice(),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            view.AddSliceProjections(slice);
        }
    }
}