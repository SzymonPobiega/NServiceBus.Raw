namespace NServiceBus.Raw.Pipeline
{
    using System;
    using System.Linq;

    static class RegisterStepExtensions
    {
        public static Type GetContextType(this Type behaviorType)
        {
            var behaviorInterface = behaviorType.GetBehaviorInterface();
            return behaviorInterface.GetGenericArguments()[0];
        }

        public static bool IsBehavior(this Type behaviorType)
        {
            return behaviorType.GetInterfaces()
                .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == BehaviorInterfaceType);
        }

        public static Type GetBehaviorInterface(this Type behaviorType)
        {
            return behaviorType.GetInterfaces()
                .First(x => x.IsGenericType && x.GetGenericTypeDefinition() == BehaviorInterfaceType);
        }

        public static Type GetOutputContext(this Type behaviorType)
        {
            var behaviorInterface = GetBehaviorInterface(behaviorType);
            return behaviorInterface.GetGenericArguments()[1];
        }

        public static Type GetInputContext(this Type behaviorType)
        {
            var behaviorInterface = GetBehaviorInterface(behaviorType);
            return behaviorInterface.GetGenericArguments()[0];
        }

        static Type BehaviorInterfaceType = typeof(IPipe<,>);
    }
}