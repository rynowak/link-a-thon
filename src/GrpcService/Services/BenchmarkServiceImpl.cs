using System.Threading.Tasks;
using Grpc.Testing;
using Grpc.Core;
using Google.Protobuf;
using System;

class BenchmarkServiceImpl : BenchmarkService.BenchmarkServiceBase
{
    public override Task<SimpleResponse> UnaryCall(SimpleRequest request, ServerCallContext context)
    {
        return Task.FromResult(CreateResponse(request));
    }

    public override async Task StreamingCall(IAsyncStreamReader<SimpleRequest> requestStream, IServerStreamWriter<SimpleResponse> responseStream, ServerCallContext context)
    {
        await foreach (var item in requestStream.ReadAllAsync())
        {
            await responseStream.WriteAsync(CreateResponse(item));
        }
    }

    public override async Task StreamingFromServer(SimpleRequest request, IServerStreamWriter<SimpleResponse> responseStream, ServerCallContext context)
    {
        while (!context.CancellationToken.IsCancellationRequested)
        {
            await responseStream.WriteAsync(CreateResponse(request));
        }
    }

    public override async Task<SimpleResponse> StreamingFromClient(IAsyncStreamReader<SimpleRequest> requestStream, ServerCallContext context)
    {
        SimpleRequest lastRequest = null;
        await foreach (var item in requestStream.ReadAllAsync())
        {
            lastRequest = item;
        };

        if (lastRequest == null)
        {
            throw new InvalidOperationException("No client requests received.");
        }

        return CreateResponse(lastRequest);
    }

    public override async Task StreamingBothWays(IAsyncStreamReader<SimpleRequest> requestStream, IServerStreamWriter<SimpleResponse> responseStream, ServerCallContext context)
    {
        var messageData = ByteString.CopyFrom(new byte[100]);
        var clientComplete = false;

        var readTask = Task.Run(async () =>
        {
            await foreach (var message in requestStream.ReadAllAsync())
            {
                // Nom nom nom
            }

            clientComplete = true;
        });

        // Write outgoing messages until client is complete
        while (!clientComplete)
        {
            await responseStream.WriteAsync(new SimpleResponse
            {
                Payload = new Payload { Body = messageData }
            });
        }

        await readTask;
    }

    public static SimpleResponse CreateResponse(SimpleRequest request)
    {
        var body = ByteString.CopyFrom(new byte[request.ResponseSize]);

        var payload = new Payload { Body = body };
        return new SimpleResponse { Payload = payload };
    }
}