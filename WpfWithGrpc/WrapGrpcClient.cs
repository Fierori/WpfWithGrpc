using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace WpfWithGrpc
{
    using GreetModel;    
    using static GreetModel.GrpcGreeter;

    /// <summary>
    /// Предоставляет функционал для подписки на получение данных по позициям прямоугольников.
    /// </summary>
    public class WrapGrpcClient
    {
        protected Channel channel;
        protected GrpcGreeterClient client;

        public WrapGrpcClient()
        {
            // make grpc channel and client

            const string Host = "127.0.0.1";
            const int    Port = 5001;

            channel = new Channel(Host, Port, ChannelCredentials.Insecure);
            client  = new GrpcGreeterClient(channel);
        }

        public async Task SendHelloMsg()
        {
            var reply = client.SayHelloAsync(new HelloRequest { Name = nameof(WrapGrpcClient) });

            await reply;

            Debug.WriteLine($"The response message containing the greetings: {reply.ResponseAsync.Result}");
        }

        /// <summary>
        /// Подпишитесь, чтобы получать данные о положении прямоугольников.
        /// </summary>
        public Task SubscribeReceivePositionOfRectangles(CancellationToken cancellationToken)
        {
            LastStatusCode = StatusCode.OK;

            var streamReceivePositionOfRectangles = 
                client.SubscribeRectPositionReply(request: new EmptyRequest(), cancellationToken: cancellationToken);

            var taskReceivePositionOfRectangles =
                RoutineReceivePositionOfRectangles(
                    streamReceivePositionOfRectangles.ResponseStream,
                    cancellationToken
                    );

            async Task RoutineReceivePositionOfRectangles(IAsyncStreamReader<PositionRectReplyEx> stream, CancellationToken сt)
            {
                try
                {
                    while (await stream.MoveNext(сt))
                    {
                        var positionRects = stream.Current;

                        EvReceivePositionOfRectangles?.Invoke(null, positionRects);
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    // Stream cancelled (typically by the caller).
                    LastStatusCode = ex.StatusCode;
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
                {
                    // Stream is currently unavailable.
                    LastStatusCode = ex.StatusCode;
                }
                catch (RpcException ex)
                {
                    // Some mistakes.
                    LastStatusCode = ex.StatusCode;
                }
            }

            return taskReceivePositionOfRectangles;
        }

        public StatusCode LastStatusCode
        { get; protected set; }

        /// <summary>
        /// Событие увдомляющее о поступлении данных по позициям прямоугольников.
        /// </summary>
        public event EventHandler<PositionRectReplyEx> EvReceivePositionOfRectangles = delegate { };
    }
}
