using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WpfWithGrpc
{
    /// <summary>
    /// Предоставляет параметры для визуализации фигуры прямоугольника.
    /// </summary>
    public class RectItem : INotifyPropertyChanged
    {
        private bool   isVisible;
        private double left;
        private double top;
        private double width;
        private double height;

        public bool IsVisible 
        { 
            get => isVisible; 
            set 
            { 
                if (isVisible != value)
                {
                    isVisible = value;
                    OnPropertyChanged();
                }
            } 
        }

        public double Left   
        { 
            get => left;   
            set
            { 
                left = value;
                OnPropertyChanged();
            } 
        }

        public double Top
        {
            get => top;
            set
            {
                top = value;
                OnPropertyChanged();
            }
        }

        public double Width
        {
            get => width;
            set
            {
                width = value;
                OnPropertyChanged();
            }
        }

        public double Height
        {
            get => height;
            set
            {
                height = value;
                OnPropertyChanged();
            }
        }

        public Brush Color 
        { get; set; } = new SolidColorBrush(Colors.AliceBlue);

        #region INotifyPropertyChanged

        /// <summary>
        /// Возникает при изменении значения свойства.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Вызывает событие <c>PropertyChanged</c>, уведомляющего об изменении свойства.
        /// (Атрибут CallerMemberName, который применяется к необязательному свойству propertyName
        /// приводит к замене имени свойства вызывающего объекта в качестве аргумента.)
        /// </summary>
        /// <param name="propertyName">Имя свойства.</param>
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
