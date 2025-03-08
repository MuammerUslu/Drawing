using UnityEngine;

namespace Drawing
{
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