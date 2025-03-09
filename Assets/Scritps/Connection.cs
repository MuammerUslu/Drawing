using System;
using UnityEngine;

namespace Drawing
{
    [Serializable]
    public class Connection
    {
        public Node StartNode;
        public Node EndNode;
        public LineRenderer Line;

        public Connection(Node start, Node end, LineRenderer line)
        {
            StartNode = start;
            EndNode = end;
            Line = line;
        }
    }
}