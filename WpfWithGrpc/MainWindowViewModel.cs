using System;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Grpc.Core;

#pragma warning disable 8612
#pragma warning disable 8618

namespace WpfWithGrpc
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {        
        private Dispatcher _uiDispatcher;
        private readonly WrapGrpcClient wrapGrpcClient = new();
        private CancellationTokenSource ctsReceivePositionOfRectangles;
        private RelayCommand cmdTurnMoveRectangles;

        public MainWindowViewModel()
            : this(Dispatcher.CurrentDispatcher)
        {
            return;
        }

        public MainWindowViewModel(Dispatcher uiDispatcher)
        {
            Debug.Assert(uiDispatcher != null);

            this._uiDispatcher = uiDispatcher;
            
            ReFillRectItems();
        }

        protected void ReFillRectItems()
        {
            int   x    = 10;
            int   y    = 10;
            int   size = 10;

            RectItems.Clear();

            for (int i = 0; i < UsefulConstants.NumberOfRectangles; i++)
            {
                // make and add rect item to collection

                var rectItem =
                    new RectItem()
                    {
                        IsVisible = false,
                        Left      = x,
                        Top       = y,
                        Width     = size,
                        Height    = size,
                    };

                RectItems.Add(rectItem);
            }
        }

        protected void HideRectItems()
        {            
            foreach (var rectItems in RectItems)
            {
                rectItems.IsVisible = false;
            }
        }

        private async void TurnMoveRectangles(object state)
        {
            //bool isEnabled = (bool)state!;

            if ((state is bool isChecked) && isChecked == true)
            {//toggle checked

                DescrStatus = "Checking the connection with the server";

                try
                {
                    await wrapGrpcClient.SendHelloMsg();
                }
                catch (Exception ex)
                {
                    DescrStatus = "Failed connect to server";

                    return;
                }

                HideRectItems();

                //SubscribeReceivePositionOfRectangles

                ctsReceivePositionOfRectangles = new CancellationTokenSource();

                wrapGrpcClient.EvReceivePositionOfRectangles += ReceivePositionOfRectanglesHandler;

                DescrStatus = "Stream in progress";

                try
                {
                    await wrapGrpcClient.SubscribeReceivePositionOfRectangles(ctsReceivePositionOfRectangles.Token);
                }
                catch (Exception ex)
                {
                    DescrStatus = "Uh-oh! Something went wrong!";

                    return;
                }

                var lastStatusCode = wrapGrpcClient.LastStatusCode;
                if (lastStatusCode != StatusCode.Cancelled)
                {
                    DescrStatus = $"Receive data stream aborted, with status {lastStatusCode}";
                }
            }
            else
            {//toggle unchecked

                ctsReceivePositionOfRectangles?.Cancel();

                wrapGrpcClient.EvReceivePositionOfRectangles -= ReceivePositionOfRectanglesHandler;

                DescrStatus = "Subscribe to receive data about the position of rectangles";
            }

            void ReceivePositionOfRectanglesHandler(object? sender, GreetModel.PositionRectReplyEx re)
            {
                foreach (var positionRect in re.PositionRects)
                {
                    if (RectItems.Count >= positionRect.Number)
                    {
                        int indx = 
                            positionRect.Number - 1;

                        // If you update the properties taking part of the binding from a different thread,
                        // WPF will automatically serialize the call to the UI thread.

                        RectItems[indx].IsVisible = true;
                        RectItems[indx].Left      = positionRect.X;
                        RectItems[indx].Top       = positionRect.Y;
                        RectItems[indx].Width     = positionRect.W;
                        RectItems[indx].Height    = positionRect.H;
                    }
                }                
            }
        }

        public RelayCommand CmdTurnMoveRectangles => cmdTurnMoveRectangles ??= new RelayCommand(TurnMoveRectangles);

        #region Props

        private ObservableCollection<RectItem> rectItems = new();
        private double canvasWidth = UsefulConstants.CanvasWidth;
        private double canvasHeight = UsefulConstants.CanvasHeight;
        public string descrStatus = "Subscribe to receive data about the position of rectangles";

        public ObservableCollection<RectItem> RectItems
        {
            get => rectItems;
            set
            {
                rectItems = value;
                OnPropertyChanged();
            }
        }

        public double CanvasWidth
        {
            get => canvasWidth;
            private set
            {
                canvasWidth = value;
                OnPropertyChanged();
            }
        }

        public double CanvasHeight
        {
            get => canvasHeight;
            set
            {
                canvasHeight = value;
                OnPropertyChanged();
            }
        }

        public string DescrStatus
        {
            get => descrStatus;
            set
            {
                descrStatus = value;
                OnPropertyChanged();
            }
        }

        #endregion

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
