using UnityEngine;

namespace Unity.Industry.Viewer.Streaming.Measurement
{
    public interface IMeasureListItem
    {
        string Id { get; set; }
        MeasureLineData Data { get; }
        bool IsShown { get; }
        bool IsSelected { get; }
        void Select(bool value);
        void Show(bool value);
        void UpdateData(MeasureLineData data);
    }
}
