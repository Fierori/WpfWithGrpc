using System.Collections.Concurrent;
using Grpc.Core;

namespace GrpcGreeterConsoleServer
{
    using GreetModel;

    internal class GrpcGreeterImpl : GrpcGreeter.GrpcGreeterBase
    {
        /// <summary>
        /// Обрабатывает запрос на приветственное сообщение.
        /// </summary>
        public override Task<HelloReply> SayHello(
            HelloRequest      request,
            ServerCallContext context
            )
        {
            return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
        }

        /// <summary>
        /// Обрабатывает запрос на получение данных по позициям прямоугольников.
        /// </summary>
        public override async Task SubscribeRectPositionReply(
            EmptyRequest                             request, 
            IServerStreamWriter<PositionRectReplyEx> responseStream, 
            ServerCallContext                        context
            )
        {
            var mre = new ManualResetEvent(false);

            var packageNowPositions = new List<PositionRectReply>();

            var queueNowPositions = new ConcurrentQueue<WrapRectPosition>();

            using var innerProccessMovePositions = new InnerProccessMovePositions();

            const int NumberOfItemsInPackage = 
                UsefulConstants.NumberOfRectangles / UsefulConstants.NumberOfThreads;

            innerProccessMovePositions.TurnOn(queueNowPositions.Enqueue);

            while (!context.CancellationToken.IsCancellationRequested)
            {
                if (queueNowPositions.Count > 0)
                {
                    if (queueNowPositions.TryDequeue(out var item))
                    {
                        packageNowPositions.Add(item.reply);
                        if (packageNowPositions.Count >= NumberOfItemsInPackage)
                        {
                            var replyEx = new PositionRectReplyEx();
                            replyEx.PositionRects.AddRange(packageNowPositions);

                            await responseStream.WriteAsync(replyEx);

                            packageNowPositions.Clear();
                        }
                        
                        continue;
                    }
                }

                //technological pause
                mre.WaitOne(10);
            }
        }
    }
}
