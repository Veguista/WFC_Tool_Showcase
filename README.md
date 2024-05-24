[![Showcase Video](https://img.youtube.com/vi/ALU8YIzwWMg/0.jpg)](https://www.youtube.com/watch?v=ALU8YIzwWMg)

This tool is meant to help users utilize the Wave Function Collapse algorithm in Unity. To begin using it, copy the contents of the "WFC" folder into your Unity project.

![alt text](https://github.com/Veguista/WFC_Tool_Showcase/blob/main/Media/CustomMenuGif.gif?raw=true)

The tool is comprised of 3 main parts:

- Scriptable Tiles: A Scriptable Object that stores the Prefab information of each of your map tiles. Those are the prefabs that the WFC algorithm will output once is complete. You can create new Scriptable Tiles in the Project section by doing [RightClick] -> Create -> WFC Tools -> Create New Tile.

![alt text](https://github.com/Veguista/WFC_Tool_Showcase/blob/main/Media/ScriptableTile.png?raw=true)

- Grid Configurations: A Scriptable Object that stores a map's configuration. It is the blueprint of your map. You can create new Grid Configurations in the Project section by doing [RightClick] -> Create -> WFC Tools -> Create New Grid Configuration. Once a map has been created, select what type of grid you are going to create and provide a folder path to where your Scriptable Tiles are stored. Afterward, open the Grid Editor and configure which tiles will be used in the map and their valid adjacent tiles. You can also set the weight of the tiles to increase their spawn rates.

![alt text](https://github.com/Veguista/WFC_Tool_Showcase/blob/main/Media/GridConfiguration.png?raw=true)

- Quantum Grid: The only class you need to interact with. Create a new QuantumGrid using its constructor and feed it the Grid Configuration that will be used. You will also need to feed it a script using the IGridDrawable interface, where the results of the algorithm will be output. The "Example_Project_Unity2022.3.14f1" folder contains example scripts to show you how to work with the QuantumGrid class and its public methods.

![alt text](https://github.com/Veguista/WFC_Tool_Showcase/blob/main/Media/QuantumGridFunctions.png?raw=true)

A full breakdown of the project can be found on my portfolio page here: https://pgv200080.wixsite.com/pablogvportfolio/tool-creation.

This project is licensed under the GNU General Public License v3.0.
