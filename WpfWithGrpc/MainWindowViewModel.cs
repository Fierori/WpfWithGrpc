using System;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Grpc.Core;

namespace WpfWithGrpc
{
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Constructors

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

            return;
        }

        #endregion

        #region UiDispatcher

        private Dispatcher _uiDispatcher;

        #endregion

        #region CanvasInfo

        private double canvasWidth  = UsefulConstants.CanvasWidth;
        private double canvasHeight = UsefulConstants.CanvasHeight;

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

        #endregion

        #region RectItems

        protected ObservableCollection<RectItem> rectItems
            = new();

        public ObservableCollection<RectItem> RectItems
        {
            get { return rectItems; }
            set
            {
                rectItems = value;
                OnPropertyChanged();
            }
        }

        protected void ReFillRectItems()
        {
            int   x     = 10;
            int   y     = 10;
            int   size  = 10;

            RectItems.Clear();

            for (int i = 0; i < UsefulConstants.NumberOfRectangles; i++)
            {
                //make and add rect item to collection

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

        #endregion

        #region WrapGrpcClient

        public WrapGrpcClient wrapGrpcClient 
            = new WrapGrpcClient();

        #endregion

        #region CmdTurnMoveRectangles

        private RelayCommand cmdTurnMoveRectangles;

        public RelayCommand CmdTurnMoveRectangles
            => cmdTurnMoveRectangles ?? (cmdTurnMoveRectangles = new RelayCommand(TurnMoveRectangles));

        private CancellationTokenSource ctsReceivePositionOfRectangles;

        private async void TurnMoveRectangles(object state)
        {
            if ((state is bool isChecked) && isChecked == true)
            {//toggle checked

                //CheckConnectToServer

                DescrStatus =
                    "Checking the connection with the server";

                try
                {
                    await wrapGrpcClient.SendHelloMsg();
                }
                catch (Exception ex)
                {
                    //failed connect to server

                    DescrStatus = 
                        "Failed connect to server";

                    return;
                }

                HideRectItems();

                //SubscribeReceivePositionOfRectangles

                ctsReceivePositionOfRectangles =
                    new CancellationTokenSource();

                wrapGrpcClient.EvReceivePositionOfRectangles
                    += ReceivePositionOfRectanglesHandler;

                DescrStatus =
                    "Stream in progress";

                try
                {
                    await wrapGrpcClient.SubscribeReceivePositionOfRectangles(ctsReceivePositionOfRectangles.Token);
                }
                catch (Exception ex)
                {
                    DescrStatus = 
                        "Uh-oh! Something went wrong!";

                    return;
                }

                var lastStatusCode = wrapGrpcClient.LastStatusCode;
                if (lastStatusCode != StatusCode.Cancelled)
                {
                    DescrStatus =
                        $"Receive data stream aborted, with status {lastStatusCode}";
                }
            }
            else
            {//toggle unchecked

                ctsReceivePositionOfRectangles?.Cancel();

                wrapGrpcClient.EvReceivePositionOfRectangles
                    -= ReceivePositionOfRectanglesHandler;

                DescrStatus = 
                    "Subscribe to receive data about the position of rectangles";
            }

            void ReceivePositionOfRectanglesHandler(object? sender, GreetModel.PositionRectReplyEx re)
            {
                foreach (var positionRect in re.PositionRects)
                {
                    if (RectItems.Count >= positionRect.Number)
                    {
                        int indx = 
                            positionRect.Number - 1;

                        //If you update the properties taking part of the binding from a different thread,
                        //WPF will automatically serialize the call to the UI thread.

                        RectItems[indx].IsVisible = true;
                        RectItems[indx].Left      = positionRect.X;
                        RectItems[indx].Top       = positionRect.Y;
                        RectItems[indx].Width     = positionRect.W;
                        RectItems[indx].Height    = positionRect.H;
                    }
                }                
            }
        }

        public string descrStatus
            = "Subscribe to receive data about the position of rectangles";

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
    }

    public partial class MainWindowViewModel 
    {
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
