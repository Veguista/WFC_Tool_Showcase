

namespace WFC
{
    public struct QuantumTile
    {
        public ushort[] statesLeft;

        // Drop-Out Stack. It tracks previous Tile states after the Grid is arc-consistent.
        // The top of the stack data is stored on a Grid Level.
        // When stateBackups[topOfStack] == NULL, no changes were recorded.
        public ushort[][] stateBackups;
    }
}

