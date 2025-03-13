using System;
using UnityEngine;

namespace Drawing
{
    [Serializable]
    public class Connection
    {
        public Node StartNode;
        public Node EndNode;

        public Connection(Node start, Node end)
        {
            StartNode = start;
            EndNode = end;
        }
    }
}