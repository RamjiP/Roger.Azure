using System;

namespace Roger.Azure.Cosmos.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CollectionNameAttribute : Attribute
    {
        public string Name { get; }
        public int DefaultTimeToLive { get; set; }

        public CollectionNameAttribute(string name)
        {
            Name = name;
            DefaultTimeToLive = -1;
        }
    }
}
