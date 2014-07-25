﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FB2SMV
{
    namespace FBCollections
    {
        public enum ConnectionType
        {
            Data,
            Event
        }

        public class FBInstance : FBPart
        {
            public FBInstance(string name, string instanceType, string comment, string fbType)
            {
                Name = name;
                Comment = comment;
                FBType = fbType; //Type of Parent FB
                InstanceType = instanceType;
            }

            public readonly string InstanceType; //Attribute "Type"

            public override string ToString()
            {
                return String.Format("({0}) {1}/{2}", InstanceType, Name, FBType);
            }
        }

        public class Connection
        {
            public Connection(string source, string destination, ConnectionType type, string fbType)
            {
                Source = source;
                Destination = destination;
                Type = type;
                FBType = fbType;
            }

            public readonly string Source;
            public readonly string Destination;
            public readonly string FBType;
            public readonly ConnectionType Type;

            public override string ToString()
            {
                return String.Format("{0}->{1}/{2}", Source, Destination, FBType);
            }
        }
    }
}