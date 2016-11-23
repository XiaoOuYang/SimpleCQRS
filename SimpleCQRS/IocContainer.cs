using System;
using Autofac;
namespace SimpleCQRS
{
    public class IocContainer
    {
        private static IContainer _container;

        public static IContainer Container
        {
            get { return _container; }
        }

        public static void RegisterServices(ContainerBuilder builder)
        {
            _container = builder.Build();
        }

    }
}
