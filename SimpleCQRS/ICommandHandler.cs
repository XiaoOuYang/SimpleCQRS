using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleCQRS
{
    public interface ICommandHandler<TCommand> where TCommand : Command
    {
        void Handle(TCommand command);
    }


    public interface ICommandHandlerProxy
    {
        void Handle(Command command);
    }

    public class CommandHandlerProxy<TCommand> : ICommandHandlerProxy where TCommand : Command
    {
        private readonly ICommandHandler<TCommand> _commandHandler;
        private readonly Type _commandHandlerType;

        public CommandHandlerProxy(ICommandHandler<TCommand> commandHandler, Type commandHandlerType)
        {
            _commandHandler = commandHandler;
            _commandHandlerType = commandHandlerType;
        }

        public void Handle(Command command)
        {
            _commandHandler.Handle(command as TCommand);
        }

    }


}
