using UnityEngine.UIElements;

public interface IGroup
{
    FlexDirection Direction { get; set; }

    public void SetDirection(FlexDirection direction);
}
