using System.Collections.Concurrent;

namespace GrpcGreeterConsoleServer
{
    /// <summary>
    /// Предоставляет функционал для запуска\останова потоков расета положения прямоугольников.
    /// </summary>
    public class InnerProccessMovePositions : IDisposable
    {
        protected readonly List<WrapRectPosition> list = new();
        protected volatile bool shouldStop = false;
        protected readonly ManualResetEvent mre = new(false);
        protected readonly List<Task> tasks = new();
        protected ConcurrentQueue<WrapRectPosition> queue = new();

        public InnerProccessMovePositions()
        {
            ReFillRectItems();
        }

        public void Dispose()
        {
            TurnOff();
            mre.Dispose();
        }

        protected void ReFillRectItems()
        {
            list.Clear();

            var rnd = new Random();

            for (int i = 0; i < UsefulConstants.NumberOfRectangles; i++)
            {
                var wrapItem = new WrapRectPosition();

                wrapItem.reply.Number = i + 1;
                wrapItem.reply.W = rnd.Next(5, 21);
                wrapItem.reply.H = rnd.Next(5, 21);
                wrapItem.reply.X = rnd.Next(0, UsefulConstants.CanvasWidth  - wrapItem.reply.W);
                wrapItem.reply.Y = rnd.Next(0, UsefulConstants.CanvasHeight - wrapItem.reply.H);

                var abs1 = rnd.Next(2);
                var abs2 = rnd.Next(2);
                if (abs1 == 0) { wrapItem.dx = -wrapItem.dx; }
                if (abs2 == 0) { wrapItem.dy = -wrapItem.dy; }

                list.Add(wrapItem);
            }
        }

        /// <summary>
        /// Запускает потоки выполнения расчета положения промоугольников.
        /// </summary>
        /// <param name="visitItem">Передайте метод, который будет вызван при обновлении позиции прямоугольника.</param>
        public void TurnOn(Action<WrapRectPosition> visitItem)
        {
            queue.Clear();

            foreach (var item in list)
            {
                queue.Enqueue(item);
            }

            tasks.Clear();

            for (int i = 0; i < UsefulConstants.NumberOfThreads; i++)
            {
                var task =
                    Task.Factory.StartNew(() =>
                    {
                        const int NumberOfItemsInPackage =
                            UsefulConstants.NumberOfRectangles / UsefulConstants.NumberOfThreads;

                        int count = 0;

                        while (!shouldStop)
                        {
                            if (queue.TryDequeue(out var rect))
                            {
                                NextPos(rect);

                                visitItem?.Invoke(rect);

                                queue.Enqueue(rect);

                                SpeedCorrector();
                                void SpeedCorrector()
                                {
                                    count++;
                                    if (count >= NumberOfItemsInPackage)
                                    {
                                        count = 0;
                                        mre.WaitOne(20);
                                    }
                                }
                            }
                        }
                    });

                tasks.Add(task);
            }

            void NextPos(WrapRectPosition wrapItem)
            {
                var item = 
                    wrapItem.reply;

                // calc next pos

                int npX = item.X + wrapItem.dx;
                int npY = item.Y + wrapItem.dy;

                // check bounding area

                int limWidth  = UsefulConstants.CanvasWidth  - item.W;
                int limHeight = UsefulConstants.CanvasHeight - item.H;

                npX = npX < 0 ? 0 : npX;
                npY = npY < 0 ? 0 : npY;
                npX = npX > limWidth  ? limWidth  : npX;
                npY = npY > limHeight ? limHeight : npY;

                // wall bounce test

                if (npX >= limWidth || npX <= 0)
                {
                    wrapItem.dx = -wrapItem.dx;
                }
                if (npY >= limHeight || npY <= 0)
                {
                    wrapItem.dy = -wrapItem.dy;
                }

                // set next pos

                item.X = npX;
                item.Y = npY;
            }
        }

        /// <summary>
        /// Останавливает потоки выполнения расчета положения промоугольников.
        /// </summary>
        public void TurnOff()
        {
            shouldStop = true;

            Task.WaitAll(tasks.ToArray());

            foreach (var task in tasks)
                task.Dispose();

            tasks.Clear();

            shouldStop = false;
        }
    }
}
