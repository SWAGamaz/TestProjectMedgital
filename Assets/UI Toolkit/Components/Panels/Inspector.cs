using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class Inspector : PanelBase, IResizible
    {
        public Inspector()
        {
            // Пример кнопки загрузки Volume
            Button loadDicomBtn = new Button(() =>
            {
                Debug.Log("Нажато");
            });
            loadDicomBtn.text = "Загрузить DICOM";
            this.Add(loadDicomBtn);
            
            // Дальше добавляем необходимые элементы интерфейса
            // ..
            // ..
        }
    }
}
