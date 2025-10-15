namespace ECS.Components
{
    using Bridges;
    
    public struct ActorComponent
    {
        public ActorBridge Bridge;
        public Data.GlobalTag Tag;
    }
}