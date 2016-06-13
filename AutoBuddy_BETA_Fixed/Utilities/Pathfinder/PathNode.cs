namespace AutoBuddy.Utilities.Pathfinder
{
    internal class PathNode
    {
        public double gCost;
        public double hCost;
        public PathNode Parent;
        public int node;
        public double fCost { get { return gCost + hCost; } }

        public PathNode(double gCost, double hCost, int node, PathNode parent)
        {
            Parent = parent;
            this.node = node;
            this.gCost = gCost;
            this.hCost = hCost;
        }
    }
}
