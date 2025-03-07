using System;
using System.Collections.Generic;
using UnityEngine;

namespace Drawing
{
    [Serializable]
    public class Node
    {
        public Vector3 Position;
        public List<Connection> Connections;

        public Node(Vector3 position)
        {
            Position = position;
            Connections = new List<Connection>();
        }
    }
}
