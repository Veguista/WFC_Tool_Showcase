This project is licensed under the GNU General Public License v3.0.


This tool is meant to help users utilize the Wave Function Collapse algorithm in Unity.


To begin using it, copy the contents of the WFC folder into your Unity proyect. The tool is comprised of 3 main parts:

- Scriptable Tiles: A Scriptable Object that stores the Prefab information of each of your map tiles. Those are the prefabs that the WFC algorithm will output once is complete. You can create new Scriptable Tiles in the Project section by doing [RightClick] -> Create -> WFC Tools -> Create New Tile.

- Grid Configurations: A Scriptable Object that stores a map's configuration. It is the blueprint of your map. You can create new Grid Configurations in the Project section by doing [RightClick] -> Create -> WFC Tools -> Create New Grid Configuration. Once a map has been created, select what type of grid you are going to create and provide a folder path to were your Scriptable Tiles are stored. Afterwards, open the Grid Editor and configure which tiles will be used in the map and their valid adjacent tiles. You can also set weight to the tiles to increase their spawn rates.

- Quantum Grid: The only class you need to interact with. Create a new QuantumGrid using its constructor and feed it the Grid Configuration that will be used. You will also need to feed it a script using the IGridDrawable interface, where the results of the algorithm will be output. The "Examples" folder contains example scripts to show you how to work with the QuantumGrid class and its public methods.