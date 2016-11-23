using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using SimpleCQRS;
using Autofac;
using System.Reflection;

namespace CQRSGui
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterRoutes(RouteTable.Routes);

            var bulder = new ContainerBuilder();

            var eventPublisher = new EventPublisher();
            var detail = new InventoryItemDetailView();
            eventPublisher.RegisterHandler<InventoryItemCreated>(detail.Handle);
            eventPublisher.RegisterHandler<InventoryItemDeactivated>(detail.Handle);
            eventPublisher.RegisterHandler<InventoryItemRenamed>(detail.Handle);
            eventPublisher.RegisterHandler<ItemsCheckedInToInventory>(detail.Handle);
            eventPublisher.RegisterHandler<ItemsRemovedFromInventory>(detail.Handle);
            var list = new InventoryListView();
            eventPublisher.RegisterHandler<InventoryItemCreated>(list.Handle);
            eventPublisher.RegisterHandler<InventoryItemRenamed>(list.Handle);
            eventPublisher.RegisterHandler<InventoryItemDeactivated>(list.Handle);

            bulder.RegisterInstance<IEventPublisher>(eventPublisher).SingleInstance();

            bulder.RegisterType<EventStore>().As<IEventStore>().AsImplementedInterfaces();
            bulder.RegisterType<Repository>().As<IRepository>().AsImplementedInterfaces();

            var reg = bulder.RegisterType<InventoryCommandHandlers>().As<InventoryCommandHandlers>().SingleInstance();

            var commandService = new CommandService();
            bulder.RegisterInstance<ICommandSender>(commandService).SingleInstance();
            IocContainer.RegisterServices(bulder);

            commandService.Initialize(new[] { Assembly.Load("SimpleCQRS") });


        }
    }
}