using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Roger.Azure.Cosmos.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CollectionNameAttribute : Attribute
    {
        public string Name { get; }
        public int DefaultTimeToLive { get; set; }

        public string PartitionKeyPath { get; set; }

        public CollectionNameAttribute(string name)
        {
            Name = name;
            DefaultTimeToLive = -1;
        }
    }
}
