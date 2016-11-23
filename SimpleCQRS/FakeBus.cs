using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Autofac;

namespace SimpleCQRS
{
    public class CommandService : ICommandSender
    {
        private readonly IDictionary<Type, IList<ICommandHandlerProxy>> _handlerDict = new Dictionary<Type, IList<ICommandHandlerProxy>>();

        public void Initialize(params Assembly[] assemblies)
        {
            foreach (var handlerType in assemblies.SelectMany(assembly => assembly.GetTypes().Where(IsHandlerType)))
            {
                RegisterHandler(handlerType);
            }

        }

        private bool IsHandlerType(Type type)
        {
            return type != null && type.IsClass && !type.IsAbstract && ScanHandlerInterfaces(type).Any();
        }

        public virtual IEnumerable<Type> ScanHandlerInterfaces(Type type)
        {
            return type.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
        }

        protected virtual Type GetHandlerProxyImplementationType(Type handlerInterfaceType)
        {
            return typeof(CommandHandlerProxy<>).MakeGenericType(handlerInterfaceType.GetGenericArguments().Single());
        }

        public void RegisterHandler(Type handlerType)
        {
            var handlerInterfaceTypes = ScanHandlerInterfaces(handlerType);

            foreach (var handlerInterfaceType in handlerInterfaceTypes)
            {
                var key = handlerInterfaceType.GetGenericArguments().Single();
                var handlerProxyType = GetHandlerProxyImplementationType(handlerInterfaceType);
                IList<ICommandHandlerProxy> handlers;
                if (!_handlerDict.TryGetValue(key, out handlers))
                {
                    handlers = new List<ICommandHandlerProxy>();
                    _handlerDict.Add(key, handlers);
                }

                var realHandler = IocContainer.Container.Resolve(handlerType);

                handlers.Add(Activator.CreateInstance(handlerProxyType, new[] { realHandler, handlerType }) as ICommandHandlerProxy);
            }
        }

        public void Send<TCommand>(TCommand command) where TCommand : Command
        {
            IList<ICommandHandlerProxy> handlers;

            if (_handlerDict.TryGetValue(typeof(TCommand), out handlers))
            {
                foreach (var handler in handlers)
                    handler.Handle(command);
            }
            else
            {
                throw new InvalidOperationException("no handler registered");
            }
        }

    }

    public interface Handles<T>
    {
        void Handle(T message);
    }

    public interface ICommandSender
    {
        void Send<T>(T command) where T : Command;

    }
    public interface IEventPublisher
    {
        void Publish<T>(T @event) where T : Event;
    }

    public class EventPublisher : IEventPublisher
    {
        private readonly Dictionary<Type, List<Action<Event>>> _routes = new Dictionary<Type, List<Action<Event>>>();

        public void RegisterHandler<T>(Action<T> handler) where T : Event
        {
            List<Action<Event>> handlers;

            if (!_routes.TryGetValue(typeof(T), out handlers))
            {
                handlers = new List<Action<Event>>();
                _routes.Add(typeof(T), handlers);
            }

            handlers.Add((x => handler((T)x)));
        }

        public void Publish<T>(T @event) where T : Event
        {
            List<Action<Event>> handlers;

            if (!_routes.TryGetValue(@event.GetType(), out handlers)) return;

            foreach (var handler in handlers)
            {
                //dispatch on thread pool for added awesomeness
                var handler1 = handler;
                ThreadPool.QueueUserWorkItem(x => handler1(@event));
            }
        }
    }



}
