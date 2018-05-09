using System;
using System.Collections.Generic;

namespace NServiceBus.Raw.Pipeline
{
    using System.Linq;
    using System.Threading.Tasks;

    public class ModuleContext
    {
        Dictionary<Type, Pipe> pipes = new Dictionary<Type, Pipe>();
        List<Connector> connectors = new List<Connector>();

        public void ConnectPipeline<TFrom, TTo>(IPipe<TFrom, TTo> connector) 
            where TFrom : IPipeContext
            where TTo : IPipeContext
        {
            var newConnector = new Connector(typeof(TFrom), typeof(TTo), connector);

            if (!pipes.TryGetValue(typeof(TFrom), out var inletPipe))
            {
                inletPipe = new Pipe(typeof(TFrom));
                pipes[typeof(TFrom)] = inletPipe;
            }
            inletPipe.AttachOutletTo(newConnector);

            if (!pipes.TryGetValue(typeof(TTo), out var outletPipe))
            {
                outletPipe = new Pipe(typeof(TTo));
                pipes[typeof(TTo)] = outletPipe;
            }
            outletPipe.AttachInletTo(newConnector);

            connectors.Add(newConnector);
        }

        public List<IPipe[]> BuildPipes()
        {
            var result = new List<IPipe[]>();

        }

        IPipe[] BuildPipe(Type inletType)
        {
            var inletPipe = pipes[inletType];
            if (inletPipe.Inlet != null)
            {
                throw new Exception($"The pipe of requested shape {inletPipe} cannot be exposed to outside because it is already connected to {inletPipe.Inlet.InletShape} pipe.");
            }
            var segments = new List<IPipe>();
            segments.AddRange(inletPipe.Segments);

            ContinueBuilding Pipeline(segments, inletPipe.Outlet);
        }

        public void AddSection<T>(IPipe<T, T> section)
            where T : IPipeContext
        {
            if (!pipes.TryGetValue(typeof(T), out var pipe))
            {
                pipe = new Pipe(typeof(T));
                pipes[typeof(T)] = pipe;
            }

            pipe.AddSegment(section);
        }

        class Connector
        {
            public Pipe From { get; private set; }
            public Pipe To { get; private set; }
            public Type InletShape { get; }
            public Type OutletShape { get; }
            public IPipe Instance { get; }

            public Connector(Type inletShape, Type outletShape, IPipe instance)
            {
                this.InletShape = inletShape;
                this.OutletShape = outletShape;
                this.Instance = instance;
            }

            public void ConnectInletToPipe(Pipe inlet)
            {
                From = inlet;
            }

            public void ConnectOutletToPipe(Pipe outlet)
            {
                To = outlet;
            }
        }

        class Pipe
        {
            public Type Shape { get; }
            public Connector Inlet { get; private set; }
            public Connector Outlet { get; private set; }

            public List<IPipe> Segments { get; } = new List<IPipe>();

            public Pipe(Type shape)
            {
                this.Shape = shape;
            }

            public void AttachInletTo(Connector inlet)
            {
                Inlet = inlet;
                inlet.ConnectOutletToPipe(this);
            }

            public void AttachOutletTo(Connector outlet)
            {
                Outlet = outlet;
                outlet.ConnectInletToPipe(this);
            }

            public void AddSegment(IPipe section)
            {
                Segments.Add(section);
            }
        }
    }
}